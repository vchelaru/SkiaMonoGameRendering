using Microsoft.Xna.Framework.Graphics;
using SkiaSharp;

namespace SkiaMonoGameRendering
{
    internal struct SkiaRenderableInfo
    {
        internal object TextureHandle;
        internal Texture2D Texture;
        internal SKSurface Surface;
        internal GRBackendRenderTarget BackendRenderTarget;
        internal object RenderState;

        internal SkiaRenderableInfo(object textureHandle, Texture2D texture)
        {
            TextureHandle = textureHandle;
            Texture = texture;
            Surface = null;
            BackendRenderTarget = null;
            RenderState = null;
        }

        internal SkiaRenderableInfo(object textureHandle, Texture2D texture,
            SKSurface surface, GRBackendRenderTarget backendRenderTarget, object renderState)
        {
            TextureHandle = textureHandle;
            Texture = texture;
            Surface = surface;
            BackendRenderTarget = backendRenderTarget;
            RenderState = renderState;
        }

        internal void ClearReferences()
        {
            Texture = null;
            BackendRenderTarget = null;
            Surface = null;
            RenderState = null;
            TextureHandle = null;
        }
    }
}
