using Microsoft.Xna.Framework.Graphics;
using SkiaSharp;

namespace SkiaMonoGameRendering
{
    public static class SkiaRenderer
    {
        private static SkiaBackend? _backend;
        private static GraphicsDevice? _graphicsDevice;
        private static readonly List<ISkiaRenderable> _renderables = new();
        private static readonly HashSet<ISkiaRenderable> _renderablesToRemove = new();
        private static readonly Dictionary<ISkiaRenderable, SkiaTarget> _targets = new();
        private static readonly List<SkiaTarget> _targetsToDispose = new();

        public static int TextureCount => _targets.Count;
        public static int RenderableCount => _renderables.Count - _renderablesToRemove.Count;
        public static bool IsInitialized => _backend != null;

        public static void Initialize(SkiaBackend backend, GraphicsDevice graphicsDevice)
        {
            ArgumentNullException.ThrowIfNull(backend);
            ArgumentNullException.ThrowIfNull(graphicsDevice);

            if (_backend != null)
            {
                if (ReferenceEquals(_backend, backend) && ReferenceEquals(_graphicsDevice, graphicsDevice))
                    return;

                throw new InvalidOperationException(
                    "SkiaRenderer is already initialized. Call SkiaRenderer.Dispose() before changing the backend or GraphicsDevice.");
            }

            backend.Initialize(graphicsDevice);
            _backend = backend;
            _graphicsDevice = graphicsDevice;
        }

        /// <summary>
        /// Auto-detects the correct backend for the current MonoGame platform.
        /// Explicit initialization is recommended for trimmed applications.
        /// </summary>
        public static void Initialize(GraphicsDevice graphicsDevice)
        {
            var backendType = FindBackendType()
                ?? throw new InvalidOperationException(
                    "Could not auto-detect a SkiaBackend. Reference the platform package or initialize an explicit backend.");

            var backend = (SkiaBackend?)Activator.CreateInstance(backendType)
                ?? throw new InvalidOperationException($"Could not create backend '{backendType.FullName}'.");
            Initialize(backend, graphicsDevice);
        }

        private static Type? FindBackendType()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (!type.IsAbstract && type.IsSubclassOf(typeof(SkiaBackend)))
                            return type;
                    }
                }
                catch (System.Reflection.ReflectionTypeLoadException)
                {
                }
            }

            return null;
        }

        public static bool IsManaging(ISkiaRenderable renderable)
        {
            ArgumentNullException.ThrowIfNull(renderable);
            return _renderables.Contains(renderable) && !_renderablesToRemove.Contains(renderable);
        }

        public static void AddRenderable(ISkiaRenderable renderable)
        {
            ArgumentNullException.ThrowIfNull(renderable);
            EnsureInitialized();

            if (IsManaging(renderable))
                throw new ArgumentException("The renderable is already being managed.", nameof(renderable));

            _renderables.Add(renderable);
        }

        public static void RemoveRenderable(ISkiaRenderable renderable)
        {
            ArgumentNullException.ThrowIfNull(renderable);

            if (!IsManaging(renderable))
                throw new ArgumentException("The renderable is not managed.", nameof(renderable));

            _renderablesToRemove.Add(renderable);
        }

        public static void Draw()
        {
            var backend = EnsureInitialized();

            PrepareTargets(backend);
            DisposeSkiaResources(backend);

            var beganDraw = false;
            try
            {
                foreach (var renderable in _renderables)
                {
                    if (_renderablesToRemove.Contains(renderable) || !CanRender(renderable))
                        continue;

                    if (!beganDraw)
                    {
                        backend.BeginDraw();
                        beganDraw = true;
                    }

                    var target = _targets[renderable];
                    backend.Render(target, renderable);
                    renderable.NotifyDrawnTexture(target.Texture);
                }
            }
            finally
            {
                try
                {
                    if (beganDraw)
                        backend.EndDraw();
                }
                finally
                {
                    try
                    {
                        DisposeGraphicsResources();
                    }
                    finally
                    {
                        CompleteRemovals();
                    }
                }
            }
        }

        public static void Dispose()
        {
            if (_backend == null)
            {
                ClearCollections();
                return;
            }

            var backend = _backend;
            _targetsToDispose.AddRange(_targets.Values);
            _targets.Clear();

            try
            {
                try
                {
                    DisposeSkiaResources(backend);
                }
                finally
                {
                    DisposeGraphicsResources();
                }
            }
            finally
            {
                try
                {
                    backend.Dispose();
                }
                finally
                {
                    _backend = null;
                    _graphicsDevice = null;
                    ClearCollections();
                }
            }
        }

        private static void PrepareTargets(SkiaBackend backend)
        {
            foreach (var renderable in _renderables)
            {
                if (_renderablesToRemove.Contains(renderable))
                {
                    QueueTargetForDisposal(renderable);
                    continue;
                }

                if (!CanRender(renderable))
                    continue;

                var format = SkColorFormatToMgColorFormat(renderable.TargetColorFormat);
                if (_targets.TryGetValue(renderable, out var existing) &&
                    (existing.Width != renderable.TargetWidth ||
                     existing.Height != renderable.TargetHeight ||
                     existing.Format != format))
                {
                    QueueTargetForDisposal(renderable);
                }

                if (!_targets.ContainsKey(renderable))
                    _targets.Add(renderable, backend.CreateTarget(renderable.TargetWidth, renderable.TargetHeight, format));
            }
        }

        private static void QueueTargetForDisposal(ISkiaRenderable renderable)
        {
            if (_targets.Remove(renderable, out var target))
                _targetsToDispose.Add(target);
        }

        private static void DisposeSkiaResources(SkiaBackend backend)
        {
            if (_targetsToDispose.Count == 0)
                return;

            backend.BeginDraw();
            try
            {
                foreach (var target in _targetsToDispose)
                    target.DisposeSkiaResources();
            }
            finally
            {
                backend.EndDraw();
            }
        }

        private static void DisposeGraphicsResources()
        {
            foreach (var target in _targetsToDispose)
                target.DisposeGraphicsResources();
            _targetsToDispose.Clear();
        }

        private static void CompleteRemovals()
        {
            if (_renderablesToRemove.Count == 0)
                return;

            _renderables.RemoveAll(_renderablesToRemove.Contains);
            _renderablesToRemove.Clear();
        }

        private static bool CanRender(ISkiaRenderable renderable) =>
            renderable.ShouldRender && renderable.TargetWidth > 0 && renderable.TargetHeight > 0;

        private static SkiaBackend EnsureInitialized() => _backend
            ?? throw new InvalidOperationException("SkiaRenderer.Initialize must be called before using the renderer.");

        private static void ClearCollections()
        {
            _renderables.Clear();
            _renderablesToRemove.Clear();
            _targets.Clear();
            _targetsToDispose.Clear();
        }

        private static SurfaceFormat SkColorFormatToMgColorFormat(SKColorType color)
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
    }
}
