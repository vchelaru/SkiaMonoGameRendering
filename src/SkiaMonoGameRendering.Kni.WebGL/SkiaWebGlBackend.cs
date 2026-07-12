using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Xna.Framework.Graphics;
using SkiaMonoGameRendering.Kni.WebGL.Components;
using SkiaSharp;

namespace SkiaMonoGameRendering.Kni.WebGL;

[SupportedOSPlatform("browser")]
public sealed class SkiaWebGlBackend : SkiaBackend
{
    private readonly SkiaMonoGameWebGlHost _host;
    private readonly SkiaWebGlOptions _options;
    private int _ownerThreadId;
    private bool _isRendering;
    private bool _isDisposed;
    private int _sourceWidth;
    private int _sourceHeight;

    public SkiaWebGlBackend(SkiaMonoGameWebGlHost host, SkiaWebGlOptions? options = null)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _options = options ?? new SkiaWebGlOptions();
        Diagnostics = new SkiaWebGlDiagnostics();
        _host.ContextLost += OnContextLost;
        _host.ContextRestored += OnContextRestored;
    }

    public SkiaWebGlDiagnostics Diagnostics { get; }
    public override GRContext GRContext => _host.GRContext;

    public override void Initialize(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        if (!_host.IsReady)
            throw new InvalidOperationException("Await the SkiaMonoGameWebGlHost.Ready task before initializing the backend.");
        if (_options.RequireWebGl2 && !_host.WebGlVersion.Contains("WebGL 2", StringComparison.OrdinalIgnoreCase))
            throw new PlatformNotSupportedException("WebGL 2 is required by this backend configuration.");
        if (_options.ResourceCacheBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(_options.ResourceCacheBytes));

        _host.ConfigureResourceCache(_options.ResourceCacheBytes);
        GraphicsDevice = graphicsDevice;
        _ownerThreadId = Environment.CurrentManagedThreadId;
    }

    internal override void BeginDraw() => EnsureThread();
    internal override void EndDraw() { }

    internal override SkiaTarget CreateTarget(int width, int height, SurfaceFormat format)
    {
        EnsureThread();
        if (format != SurfaceFormat.Color)
            throw new NotSupportedException("The WebGL backend supports SurfaceFormat.Color only.");

        Diagnostics.TargetCreationCount++;
        return new SkiaWebGlTarget(new Texture2D(GraphicsDevice, width, height, false, format));
    }

    internal override void Render(SkiaTarget target, ISkiaRenderable renderable)
    {
        EnsureThread();
        if (_isRendering)
            throw new InvalidOperationException("SkiaWebGlBackend.Render is not reentrant.");
        if (target is not SkiaWebGlTarget webTarget)
            throw new ArgumentException("The target was not created by this WebGL backend.", nameof(target));

        _isRendering = true;
        var totalStart = Stopwatch.GetTimestamp();
        try
        {
            if (_sourceWidth != target.Width || _sourceHeight != target.Height)
            {
                if (_sourceWidth > 0 && _sourceHeight > 0)
                    Diagnostics.ResizeCount++;
                _sourceWidth = target.Width;
                _sourceHeight = target.Height;
            }

            BrowserCanvasSource source;
            try
            {
                source = _host.RenderNow(target.Width, target.Height, renderable);
            }
            catch (Exception exception)
            {
                Diagnostics.DroppedFrameCount++;
                throw CreateRenderException("Skia render", target, exception);
            }

            var uploadStart = Stopwatch.GetTimestamp();
            try
            {
                webTarget.Texture.UploadFromCanvas(source, CreateUploadOptions());
            }
            catch (Exception exception)
            {
                Diagnostics.DroppedFrameCount++;
                throw CreateRenderException("canvas upload", target, exception);
            }
            finally
            {
                if (_options.EnableDiagnostics)
                    Diagnostics.LastUploadCpuMilliseconds = Stopwatch.GetElapsedTime(uploadStart).TotalMilliseconds;
            }

            Diagnostics.UploadCount++;
            Diagnostics.BytesUploaded += (long)target.Width * target.Height * 4;
        }
        finally
        {
            if (_options.EnableDiagnostics)
                Diagnostics.LastRenderCpuMilliseconds = Stopwatch.GetElapsedTime(totalStart).TotalMilliseconds;
            _isRendering = false;
        }
    }

    private CanvasUploadOptions CreateUploadOptions()
    {
        var useTexImage = _options.UploadMode == WebGlUploadMode.DiagnosticTexImage2D;
        Diagnostics.UploadPath = useTexImage ? "texImage2D(canvas)" : "texSubImage2D(canvas)";

        return new CanvasUploadOptions
        {
            ValidateDimensions = true,
            FlipY = _options.FlipY,
            PremultiplyAlpha = _options.PremultiplyAlpha,
            ColorSpaceConversion = _options.DisableColorSpaceConversion
                ? WebGLCanvasColorSpaceConversion.None
                : WebGLCanvasColorSpaceConversion.BrowserDefault,
            UploadMode = useTexImage
                ? WebGLCanvasUploadMode.TexImage2D
                : WebGLCanvasUploadMode.TexSubImage2D,
        };
    }

    private InvalidOperationException CreateRenderException(string stage, SkiaTarget target, Exception inner) =>
        new($"WebGL {stage} failed for {target.Width}x{target.Height} RGBA8 " +
            $"using {Diagnostics.UploadPath}; contextLost={_host.IsContextLost}; " +
            $"browserContext='{_host.WebGlVersion}'; cause={inner.GetType().Name}: {inner.Message}", inner);

    private void EnsureThread()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SkiaWebGlBackend));
        if (Environment.CurrentManagedThreadId != _ownerThreadId)
            throw new InvalidOperationException("WebGL backend calls must run on the graphics browser thread.");
    }

    private void OnContextLost(object? sender, EventArgs args) => Diagnostics.ContextLossCount++;
    private void OnContextRestored(object? sender, EventArgs args)
    {
        Diagnostics.SurfaceRecreationCount++;
        _sourceWidth = 0;
        _sourceHeight = 0;
    }

    internal override object CaptureTextureHandle(Texture2D texture) => throw new NotSupportedException();
    internal override (SKSurface surface, GRBackendRenderTarget renderTarget) CreateSurface(
        object textureHandle, Texture2D texture, int width, int height, SKColorType colorType, out object renderState) =>
        throw new NotSupportedException();
    internal override void BindForDrawing(object renderState) => throw new NotSupportedException();
    internal override void UnbindAfterDrawing() => throw new NotSupportedException();
    internal override void DisposeRenderState(object renderState) => throw new NotSupportedException();

    public override void Dispose()
    {
        if (_isDisposed)
            return;
        _isDisposed = true;
        _host.ContextLost -= OnContextLost;
        _host.ContextRestored -= OnContextRestored;
    }

    private sealed class SkiaWebGlTarget : SkiaTarget
    {
        private Texture2D? _texture;

        internal SkiaWebGlTarget(Texture2D texture) => _texture = texture;
        public override Texture2D Texture => _texture
            ?? throw new ObjectDisposedException(nameof(SkiaWebGlTarget));
        public override int Width => Texture.Width;
        public override int Height => Texture.Height;
        public override SurfaceFormat Format => Texture.Format;
        internal override void DisposeSkiaResources() { }
        internal override void DisposeGraphicsResources()
        {
            _texture?.Dispose();
            _texture = null;
        }
    }
}
