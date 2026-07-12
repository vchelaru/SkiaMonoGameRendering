using Microsoft.Xna.Framework.Graphics;
using SkiaSharp;

namespace SkiaMonoGameRendering
{
    public abstract class SkiaBackend : IDisposable
    {
        public GraphicsDevice GraphicsDevice { get; protected set; } = null!;
        public abstract GRContext GRContext { get; }

        public abstract void Initialize(GraphicsDevice graphicsDevice);

        internal abstract void BeginDraw();
        internal abstract void EndDraw();

        /// <summary>
        /// Eagerly allocates the texture and GPU render-target resources for a fixed-size target.
        /// </summary>
        internal virtual SkiaTarget CreateTarget(int width, int height, SKColorType colorType)
        {
            var texture = CreateTexture(width, height, ToSurfaceFormat(colorType));

            try
            {
                var target = new NativeSkiaTarget(this, texture, CaptureTextureHandle(texture));
                BeginDraw();
                try
                {
                    target.EnsureSurface(colorType);
                }
                finally
                {
                    EndDraw();
                }
                return target;
            }
            catch
            {
                texture.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Begins a render pass into <paramref name="target"/>: switches to the Skia GPU context,
        /// binds the target, and returns the canvas to draw on.
        /// </summary>
        internal virtual SKCanvas BeginRender(SkiaTarget target, bool clear)
        {
            var nativeTarget = target as NativeSkiaTarget
                ?? throw new ArgumentException("The target was not created by this backend.", nameof(target));

            BeginDraw();
            BindForDrawing(nativeTarget.RenderState!);

            if (clear)
                nativeTarget.Surface!.Canvas.Clear();

            return nativeTarget.Surface!.Canvas;
        }

        /// <summary>
        /// Ends the render pass started by <see cref="BeginRender"/>: flushes Skia's queued GPU
        /// work, unbinds the target, and switches back to the host engine's GPU context.
        /// </summary>
        internal virtual void EndRender(SkiaTarget target)
        {
            var nativeTarget = target as NativeSkiaTarget
                ?? throw new ArgumentException("The target was not created by this backend.", nameof(target));

            try
            {
                nativeTarget.Surface!.Flush();
            }
            finally
            {
                UnbindAfterDrawing();
                EndDraw();
            }
        }

        internal virtual Texture2D CreateTexture(int width, int height, SurfaceFormat format)
        {
            return new Texture2D(GraphicsDevice, width, height, false, format);
        }

        internal abstract object CaptureTextureHandle(Texture2D texture);
        internal abstract (SKSurface surface, GRBackendRenderTarget renderTarget) CreateSurface(
            object textureHandle, Texture2D texture, int width, int height, SKColorType colorType, out object renderState);
        internal abstract void BindForDrawing(object renderState);
        internal abstract void UnbindAfterDrawing();
        internal abstract void DisposeRenderState(object renderState);

        public abstract void Dispose();

        internal static SurfaceFormat ToSurfaceFormat(SKColorType color)
        {
            return color switch
            {
                SKColorType.Rgba1010102 => SurfaceFormat.Rgba1010102,
                SKColorType.Rgba16161616 => SurfaceFormat.Rgba64,
                SKColorType.Alpha8 => SurfaceFormat.Alpha8,
#if !FNA
                SKColorType.Bgra8888 => SurfaceFormat.Bgra32,
#endif
                SKColorType.Rg1616 => SurfaceFormat.Rg32,
                _ => SurfaceFormat.Color,
            };
        }

        private sealed class NativeSkiaTarget : SkiaTarget
        {
            private readonly SkiaBackend _backend;
            private Texture2D? _texture;
            private object? _textureHandle;
            private SKSurface? _surface;
            private GRBackendRenderTarget? _backendRenderTarget;
            private object? _renderState;

            internal NativeSkiaTarget(SkiaBackend backend, Texture2D texture, object textureHandle)
            {
                _backend = backend;
                _texture = texture;
                _textureHandle = textureHandle;
            }

            public override Texture2D Texture => _texture
                ?? throw new ObjectDisposedException(nameof(NativeSkiaTarget));
            internal SKSurface? Surface => _surface;
            internal object? RenderState => _renderState;

            internal void EnsureSurface(SKColorType colorType)
            {
                if (_surface != null && _backendRenderTarget != null)
                    return;

                var result = _backend.CreateSurface(
                    _textureHandle!, Texture, Texture.Width, Texture.Height, colorType, out var renderState);
                _surface = result.surface;
                _backendRenderTarget = result.renderTarget;
                _renderState = renderState;
            }

            internal override void DisposeSkiaResources()
            {
                _surface?.Dispose();
                _surface = null;
                _backendRenderTarget?.Dispose();
                _backendRenderTarget = null;

                if (_renderState != null)
                {
                    _backend.DisposeRenderState(_renderState);
                    _renderState = null;
                }

                _textureHandle = null;
            }

            internal override void DisposeGraphicsResources()
            {
                _texture?.Dispose();
                _texture = null;
            }
        }
    }
}
