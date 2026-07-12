using Microsoft.Xna.Framework.Graphics;

namespace SkiaMonoGameRendering
{
    /// <summary>
    /// Owns all resources used to render one <see cref="ISkiaRenderable"/>.
    /// </summary>
    internal abstract class SkiaTarget : IDisposable
    {
        public abstract Texture2D Texture { get; }
        public abstract int Width { get; }
        public abstract int Height { get; }
        public abstract SurfaceFormat Format { get; }

        internal abstract void DisposeSkiaResources();
        internal abstract void DisposeGraphicsResources();

        public void Dispose()
        {
            DisposeSkiaResources();
            DisposeGraphicsResources();
            GC.SuppressFinalize(this);
        }
    }
}
