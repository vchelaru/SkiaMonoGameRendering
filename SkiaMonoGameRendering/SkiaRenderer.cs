using Microsoft.Xna.Framework.Graphics;
using SkiaSharp;
using System.Collections.Generic;
using System;

namespace SkiaMonoGameRendering
{
    public static class SkiaRenderer
    {
        static SkiaBackend _backend;

        public static int TextureCount
        {
            get
            {
                int count = 0;

                foreach (var info in _renderableInfos.Values)
                {
                    if (info.Texture != null)
                        count++;
                }

                return count;
            }
        }

        public static int RenderableCount { get { return _renderables.Count - _renderablesToRemove.Count; } }

        static readonly List<ISkiaRenderable> _renderables = new();
        static readonly List<ISkiaRenderable> _renderablesToRemove = new();
        static readonly Dictionary<ISkiaRenderable, SkiaRenderableInfo> _renderableInfos = new();
        static readonly List<SkiaRenderableInfo> _renderableInfosToClear = new();

        public static void Initialize(SkiaBackend backend, GraphicsDevice graphicsDevice)
        {
            _backend = backend;
            _backend.Initialize(graphicsDevice);
        }

        /// <summary>
        /// Auto-detects the correct backend for the current MonoGame platform.
        /// Looks for SkiaBackend subclasses in all loaded assemblies.
        /// </summary>
        public static void Initialize(GraphicsDevice graphicsDevice)
        {
            var backendType = FindBackendType()
                ?? throw new Exception(
                    "Could not auto-detect a SkiaBackend. Make sure the correct " +
                    "SkiaMonoGameRendering library (DesktopGL or WindowsDX) is referenced.");

            var backend = (SkiaBackend)Activator.CreateInstance(backendType);
            Initialize(backend, graphicsDevice);
        }

        static Type FindBackendType()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (!type.IsAbstract && type.IsSubclassOf(typeof(SkiaBackend)))
                            return type;
                    }
                }
                catch (System.Reflection.ReflectionTypeLoadException) { }
            }
            return null;
        }

        public static bool IsManaging(ISkiaRenderable renderable)
        {
            return _renderables.Contains(renderable) && !_renderablesToRemove.Contains(renderable);
        }

        public static void AddRenderable(ISkiaRenderable renderable)
        {
            if (IsManaging(renderable))
                throw new ArgumentException("The renderable is already being managed.", nameof(renderable));

            _renderables.Add(renderable);
        }

        public static void RemoveRenderable(ISkiaRenderable renderable)
        {
            if (!IsManaging(renderable))
                throw new ArgumentException("Can't remove the renderable because it isn't managed.", nameof(renderable));

            if (!_renderablesToRemove.Contains(renderable))
                _renderablesToRemove.Add(renderable);
        }

        static SurfaceFormat SkColorFormatToMgColorFormat(SKColorType color)
        {
            switch (color)
            {
                case SKColorType.Rgba1010102:
                    return SurfaceFormat.Rgba1010102;
                case SKColorType.Rgba16161616:
                    return SurfaceFormat.Rgba64;
                case SKColorType.Alpha8:
                    return SurfaceFormat.Alpha8;
#if !FNA
                case SKColorType.Bgra8888:
                    return SurfaceFormat.Bgra32;
#endif
                case SKColorType.Rg1616:
                    return SurfaceFormat.Rg32;
                default:
                    return SurfaceFormat.Color;
            }
        }

        static SkiaRenderableInfo CreateNewTextureAndInfo(ISkiaRenderable renderable, SkiaRenderableInfo? oldInfo)
        {
            if (oldInfo.HasValue)
                _renderableInfosToClear.Add(oldInfo.Value);

            var texture = _backend.CreateTexture(renderable.TargetWidth, renderable.TargetHeight,
                SkColorFormatToMgColorFormat(renderable.TargetColorFormat));

            var textureHandle = _backend.CaptureTextureHandle(texture);

            return new SkiaRenderableInfo(textureHandle, texture);
        }

        public static void Draw()
        {
            var doAnyNeedToRender = false;

            for (int i = 0; i < _renderables.Count; i++)
            {
                var renderable = _renderables[i];

                if (renderable.ShouldRender && renderable.TargetWidth > 0 && renderable.TargetHeight > 0)
                {
                    if (_renderableInfos.TryGetValue(renderable, out SkiaRenderableInfo info))
                    {
                        if (info.Texture == null || renderable.TargetWidth != info.Texture.Width || renderable.TargetHeight != info.Texture.Height
                            || SkColorFormatToMgColorFormat(renderable.TargetColorFormat) != info.Texture.Format)
                        {
                            _renderableInfos[renderable] = CreateNewTextureAndInfo(renderable, info);
                        }
                    }
                    else
                    {
                        _renderableInfos.Add(renderable, CreateNewTextureAndInfo(renderable, null));
                    }
                }
                doAnyNeedToRender = doAnyNeedToRender || renderable.ShouldRender;
            }

            if (!doAnyNeedToRender)
                return;

            _backend.BeginDraw();

            for (int i = 0; i < _renderables.Count; i++)
            {
                var renderable = _renderables[i];

                if (!renderable.ShouldRender)
                    continue;

                int textureWidth = renderable.TargetWidth;
                int textureHeight = renderable.TargetHeight;
                var skColor = renderable.TargetColorFormat;
                var info = _renderableInfos[renderable];

                if (info.Surface == null || info.BackendRenderTarget == null)
                {
                    var (surface, backendRT) = _backend.CreateSurface(
                        info.TextureHandle, info.Texture, textureWidth, textureHeight, skColor, out var renderState);

                    _renderableInfos[renderable] = new SkiaRenderableInfo(
                        info.TextureHandle, info.Texture, surface, backendRT, renderState);
                    info = _renderableInfos[renderable];
                }

                _backend.BindForDrawing(info.RenderState);

                if (renderable.ClearCanvasOnRender)
                    info.Surface.Canvas.Clear();

                renderable.DrawToSurface(info.Surface);
                info.Surface.Flush();

                _backend.UnbindAfterDrawing();

                renderable.NotifyDrawnTexture(info.Texture);
            }

            ManageSkiaDataToClear();

            _backend.EndDraw();

            ManageMonoGameDataToClear();
        }

        private static void ManageSkiaDataToClear()
        {
            for (int i = 0; i < _renderablesToRemove.Count; i++)
            {
                var renderable = _renderablesToRemove[i];
                if (_renderableInfos.TryGetValue(renderable, out var info))
                {
                    info.Surface?.Dispose();
                    info.BackendRenderTarget?.Dispose();
                    if (info.RenderState != null)
                        _backend.DisposeRenderState(info.RenderState);
                }
            }

            for (int i = 0; i < _renderableInfosToClear.Count; i++)
            {
                var info = _renderableInfosToClear[i];
                info.Surface?.Dispose();
                info.BackendRenderTarget?.Dispose();
                if (info.RenderState != null)
                    _backend.DisposeRenderState(info.RenderState);
            }
        }

        private static void ManageMonoGameDataToClear()
        {
            for (int i = 0; i < _renderablesToRemove.Count; i++)
            {
                var renderable = _renderablesToRemove[i];
                if (_renderableInfos.TryGetValue(renderable, out var info))
                {
                    info.Texture?.Dispose();
                    info.ClearReferences();
                    _renderableInfos.Remove(renderable);
                }

                _renderables.Remove(renderable);
            }

            for (int i = 0; i < _renderableInfosToClear.Count; i++)
            {
                var info = _renderableInfosToClear[i];
                info.Texture?.Dispose();
                info.ClearReferences();
            }

            _renderablesToRemove.Clear();
            _renderableInfosToClear.Clear();
        }
    }
}
