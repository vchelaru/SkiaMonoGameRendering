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

        internal virtual SkiaTarget CreateTarget(int width, int height, SurfaceFormat format)
        {
            var texture = CreateTexture(width, height, format);

            try
            {
                return new NativeSkiaTarget(this, texture, CaptureTextureHandle(texture));
            }
            catch
            {
                texture.Dispose();
                throw;
            }
        }

        internal virtual void Render(SkiaTarget target, ISkiaRenderable renderable)
        {
            var nativeTarget = target as NativeSkiaTarget
                ?? throw new ArgumentException("The target was not created by this backend.", nameof(target));

            nativeTarget.EnsureSurface(renderable.TargetColorFormat);
            BindForDrawing(nativeTarget.RenderState!);

            try
            {
                if (renderable.ClearCanvasOnRender)
                    nativeTarget.Surface!.Canvas.Clear();

                renderable.DrawToSurface(nativeTarget.Surface!);
                nativeTarget.Surface!.Flush();
            }
            finally
            {
                UnbindAfterDrawing();
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
            public override int Width => Texture.Width;
            public override int Height => Texture.Height;
            public override SurfaceFormat Format => Texture.Format;
            internal SKSurface? Surface => _surface;
            internal object? RenderState => _renderState;

            internal void EnsureSurface(SKColorType colorType)
            {
                if (_surface != null && _backendRenderTarget != null)
                    return;

                var result = _backend.CreateSurface(
                    _textureHandle!, Texture, Width, Height, colorType, out var renderState);
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
