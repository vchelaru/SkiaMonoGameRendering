using Microsoft.Xna.Framework.Graphics;
using SkiaSharp;
using static SkiaMonoGameRendering.GlConstants;
using static SkiaMonoGameRendering.GlWrapper;

namespace SkiaMonoGameRendering
{
    internal class GlTextureState
    {
        internal int TextureId;
        internal int FramebufferId;
        internal int RenderbufferId;
    }

    public class SkiaGlBackend : SkiaBackend
    {
        IntPtr _windowId;
        IntPtr _mgContextId;
        IntPtr _skContextId;
        GRContext _grContext;

        public override GRContext GRContext => _grContext;

        public override void Initialize(GraphicsDevice graphicsDevice)
        {
            GraphicsDevice = graphicsDevice;

            _windowId = GetMgWindowId(graphicsDevice);
            _mgContextId = SDL_GL_GetCurrentContext();

            MgGlFunctions.LoadFunctions();

            var setAttributeResult = SDL_GL_SetAttribute(SDL_GL_SHARE_WITH_CURRENT_CONTEXT, 1);
            if (setAttributeResult < 0)
                throw new Exception("SDL_GL_SetAttribute failed.");

            _skContextId = SDL_GL_CreateContext(_windowId);
            if (_skContextId == IntPtr.Zero)
                throw new Exception("SDL_GL_CreateContext failed.");

            MakeSkiaContextCurrent();
            SkGlFunctions.LoadFunctions();
            _grContext = GRContext.CreateGl();
            MakeEngineContextCurrent();
        }

        void MakeSkiaContextCurrent()
        {
            var result = MakeCurrent(_windowId, _skContextId);
            if (result < 0)
                throw new Exception("SDL_GL_MakeCurrent failed.");
        }

        void MakeEngineContextCurrent()
        {
            var result = MakeCurrent(_windowId, _mgContextId);
            if (result < 0)
                throw new Exception("SDL_GL_MakeCurrent failed.");
        }

        internal override void BeginDraw()
        {
            MakeSkiaContextCurrent();
            _grContext.ResetContext();
        }

        internal override void EndDraw()
        {
            MakeEngineContextCurrent();
        }

        internal override object CaptureTextureHandle(Texture2D texture)
        {
            MgGlFunctions.GetInteger(GL_TEXTURE_BINDING_2D, out var textureId);
            return new GlTextureState { TextureId = textureId };
        }

        internal override (SKSurface surface, GRBackendRenderTarget renderTarget) CreateSurface(
            object textureHandle, Texture2D texture, int width, int height, SKColorType colorType, out object renderState)
        {
            var state = (GlTextureState)textureHandle;

            SkGlFunctions.GetInteger(GL_SAMPLES, out var samples);
            var maxSamples = _grContext.GetMaxSurfaceSampleCount(colorType);
            if (samples > maxSamples)
                samples = maxSamples;

            SkGlFunctions.GenRenderbuffers(1, out var renderbufferId);
            SkGlFunctions.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderbufferId);
            SkGlFunctions.RenderbufferStorage(RenderbufferTarget.Renderbuffer,
                RenderbufferStorage.Depth24Stencil8, width, height);
            SkGlFunctions.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

            SkGlFunctions.GenFramebuffers(1, out var framebufferId);
            SkGlFunctions.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferId);

            SkGlFunctions.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, state.TextureId, 0);
            SkGlFunctions.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, renderbufferId);
            SkGlFunctions.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                FramebufferAttachment.StencilAttachment, RenderbufferTarget.Renderbuffer, renderbufferId);

            var framebufferStatus = SkGlFunctions.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (framebufferStatus != FramebufferErrorCode.FramebufferComplete &&
                framebufferStatus != FramebufferErrorCode.FramebufferCompleteExt)
                throw new Exception("Skia framebuffer creation failed.");

            SkGlFunctions.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            state.FramebufferId = framebufferId;
            state.RenderbufferId = renderbufferId;

            var skiaFramebufferInfo = new GRGlFramebufferInfo((uint)framebufferId, colorType.ToGlSizedFormat());
            var backendRenderTarget = new GRBackendRenderTarget(width, height, samples, 8, skiaFramebufferInfo);
            var surface = SKSurface.Create(_grContext, backendRenderTarget, GRSurfaceOrigin.TopLeft, colorType);

            renderState = state;
            return (surface, backendRenderTarget);
        }

        internal override void BindForDrawing(object renderState)
        {
            var state = (GlTextureState)renderState;
            SkGlFunctions.BindFramebuffer(FramebufferTarget.Framebuffer, state.FramebufferId);
        }

        internal override void UnbindAfterDrawing()
        {
            SkGlFunctions.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        internal override void DisposeRenderState(object renderState)
        {
            var state = (GlTextureState)renderState;

            if (state.FramebufferId > 0)
            {
                SkGlFunctions.BindFramebuffer(FramebufferTarget.Framebuffer, state.FramebufferId);
                SkGlFunctions.InvalidateFramebuffer(FramebufferTarget.Framebuffer, 3, SkGlFunctions.FramebufferAttachements);
                SkGlFunctions.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                    FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, 0, 0);
                SkGlFunctions.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                    FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, 0);
                SkGlFunctions.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                    FramebufferAttachment.StencilAttachment, RenderbufferTarget.Renderbuffer, 0);
                SkGlFunctions.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                SkGlFunctions.DeleteFramebuffers(1, ref state.FramebufferId);
            }

            if (state.RenderbufferId > 0)
                SkGlFunctions.DeleteRenderbuffers(1, ref state.RenderbufferId);
        }

        public override void Dispose()
        {
            _grContext?.Dispose();
        }
    }
}
