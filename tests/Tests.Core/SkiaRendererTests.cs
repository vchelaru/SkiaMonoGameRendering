using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Graphics;
using SkiaMonoGameRendering;
using SkiaSharp;
using Xunit;

namespace Tests.Core;

public sealed class SkiaRendererTests : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice =
        (GraphicsDevice)RuntimeHelpers.GetUninitializedObject(typeof(GraphicsDevice));

    public SkiaRendererTests() => SkiaRenderer.Dispose();

    [Fact]
    public void Initialize_IsIdempotentOnlyForSameConfiguration()
    {
        var backend = new FakeBackend();

        SkiaRenderer.Initialize(backend, _graphicsDevice);
        SkiaRenderer.Initialize(backend, _graphicsDevice);

        Assert.Equal(1, backend.InitializeCount);
        Assert.Throws<InvalidOperationException>(() =>
            SkiaRenderer.Initialize(new FakeBackend(), _graphicsDevice));
    }

    [Fact]
    public void Draw_ReusesTargetAndRecreatesItOnResize()
    {
        var backend = InitializeBackend();
        var renderable = new FakeRenderable();
        SkiaRenderer.AddRenderable(renderable);

        SkiaRenderer.Draw();
        SkiaRenderer.Draw();
        renderable.Width = 64;
        SkiaRenderer.Draw();

        Assert.Equal(2, backend.CreatedTargets.Count);
        Assert.Equal(3, backend.RenderCount);
        Assert.Equal(3, renderable.NotifyCount);
        Assert.Equal(1, backend.CreatedTargets[0].SkiaDisposeCount);
        Assert.Equal(1, backend.CreatedTargets[0].GraphicsDisposeCount);
        Assert.Equal(1, SkiaRenderer.TextureCount);
    }

    [Fact]
    public void RemoveRenderable_DisposesEvenWhenNoRenderableIsDirty()
    {
        var backend = InitializeBackend();
        var renderable = new FakeRenderable();
        SkiaRenderer.AddRenderable(renderable);
        SkiaRenderer.Draw();
        renderable.ShouldRenderValue = false;

        SkiaRenderer.RemoveRenderable(renderable);
        SkiaRenderer.Draw();

        Assert.Equal(0, SkiaRenderer.RenderableCount);
        Assert.Equal(0, SkiaRenderer.TextureCount);
        Assert.Equal(1, backend.CreatedTargets[0].SkiaDisposeCount);
        Assert.Equal(1, backend.CreatedTargets[0].GraphicsDisposeCount);
    }

    [Fact]
    public void Draw_DoesNotNotifyAndRestoresBackendWhenRenderFails()
    {
        var backend = InitializeBackend();
        backend.RenderException = new InvalidOperationException("expected");
        var renderable = new FakeRenderable();
        SkiaRenderer.AddRenderable(renderable);

        Assert.Throws<InvalidOperationException>(SkiaRenderer.Draw);

        Assert.Equal(0, renderable.NotifyCount);
        Assert.Equal(backend.BeginCount, backend.EndCount);
    }

    [Fact]
    public void Dispose_ReleasesTargetsBackendAndStaticState()
    {
        var backend = InitializeBackend();
        SkiaRenderer.AddRenderable(new FakeRenderable());
        SkiaRenderer.Draw();

        SkiaRenderer.Dispose();

        Assert.True(backend.IsDisposed);
        Assert.Equal(1, backend.CreatedTargets[0].SkiaDisposeCount);
        Assert.Equal(1, backend.CreatedTargets[0].GraphicsDisposeCount);
        Assert.False(SkiaRenderer.IsInitialized);
        Assert.Equal(0, SkiaRenderer.RenderableCount);
    }

    [Fact]
    public void Dispose_ClearsStaticStateWhenBackendDisposeFails()
    {
        var backend = InitializeBackend();
        backend.DisposeException = new InvalidOperationException("expected");
        SkiaRenderer.AddRenderable(new FakeRenderable());
        SkiaRenderer.Draw();

        Assert.Throws<InvalidOperationException>(SkiaRenderer.Dispose);

        Assert.False(SkiaRenderer.IsInitialized);
        Assert.Equal(0, SkiaRenderer.RenderableCount);
        Assert.Equal(1, backend.CreatedTargets[0].GraphicsDisposeCount);
    }

    public void Dispose() => SkiaRenderer.Dispose();

    private FakeBackend InitializeBackend()
    {
        var backend = new FakeBackend();
        SkiaRenderer.Initialize(backend, _graphicsDevice);
        return backend;
    }

    private sealed class FakeRenderable : ISkiaRenderable
    {
        public int Width { get; set; } = 32;
        public int Height { get; set; } = 32;
        public bool ShouldRenderValue { get; set; } = true;
        public int NotifyCount { get; private set; }
        public int TargetWidth => Width;
        public int TargetHeight => Height;
        public SKColorType TargetColorFormat => SKColorType.Rgba8888;
        public bool ShouldRender => ShouldRenderValue;
        public bool ClearCanvasOnRender => true;
        public void DrawToSurface(SKSurface surface) { }
        public void NotifyDrawnTexture(Texture2D texture) => NotifyCount++;
    }

    private sealed class FakeBackend : SkiaBackend
    {
        public int InitializeCount { get; private set; }
        public int BeginCount { get; private set; }
        public int EndCount { get; private set; }
        public int RenderCount { get; private set; }
        public bool IsDisposed { get; private set; }
        public Exception? RenderException { get; set; }
        public Exception? DisposeException { get; set; }
        public List<FakeTarget> CreatedTargets { get; } = new();
        public override GRContext GRContext => null!;

        public override void Initialize(GraphicsDevice graphicsDevice)
        {
            GraphicsDevice = graphicsDevice;
            InitializeCount++;
        }

        internal override void BeginDraw() => BeginCount++;
        internal override void EndDraw() => EndCount++;

        internal override SkiaTarget CreateTarget(int width, int height, SurfaceFormat format)
        {
            var target = new FakeTarget(width, height, format);
            CreatedTargets.Add(target);
            return target;
        }

        internal override void Render(SkiaTarget target, ISkiaRenderable renderable)
        {
            RenderCount++;
            if (RenderException != null)
                throw RenderException;
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
            IsDisposed = true;
            if (DisposeException != null)
                throw DisposeException;
        }
    }

    private sealed class FakeTarget : SkiaTarget
    {
        private static readonly Texture2D DummyTexture =
            (Texture2D)RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));

        public FakeTarget(int width, int height, SurfaceFormat format)
        {
            WidthValue = width;
            HeightValue = height;
            FormatValue = format;
        }

        private int WidthValue { get; }
        private int HeightValue { get; }
        private SurfaceFormat FormatValue { get; }
        public int SkiaDisposeCount { get; private set; }
        public int GraphicsDisposeCount { get; private set; }
        public override Texture2D Texture => DummyTexture;
        public override int Width => WidthValue;
        public override int Height => HeightValue;
        public override SurfaceFormat Format => FormatValue;
        internal override void DisposeSkiaResources() => SkiaDisposeCount++;
        internal override void DisposeGraphicsResources() => GraphicsDisposeCount++;
    }
}
