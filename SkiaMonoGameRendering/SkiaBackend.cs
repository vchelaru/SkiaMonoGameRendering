using Microsoft.Xna.Framework.Graphics;
using SkiaSharp;

namespace SkiaMonoGameRendering
{
    public abstract class SkiaBackend : IDisposable
    {
        public GraphicsDevice GraphicsDevice { get; protected set; }
        public abstract GRContext GRContext { get; }

        public abstract void Initialize(GraphicsDevice graphicsDevice);

        internal abstract void BeginDraw();
        internal abstract void EndDraw();

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
    }
}
