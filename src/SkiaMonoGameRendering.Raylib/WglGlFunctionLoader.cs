using System.Runtime.InteropServices;
using SkiaMonoGameRendering.Core.OGL;

namespace SkiaMonoGameRendering.Raylib
{
    /// <summary>
    /// Loads GL entry points via wglGetProcAddress while the Skia-dedicated context is current, so
    /// the function pointers are resolved against the context we'll actually call them on.
    /// </summary>
    internal sealed class WglGlFunctionLoader : IGlFunctionLoader
    {
        private readonly Wgl _wgl;

        public WglGlFunctionLoader(Wgl wgl)
        {
            _wgl = wgl;
        }

        public T Load<T>(string nativeName) where T : Delegate
        {
            var procAddress = _wgl.GetProcAddress(nativeName);
            if (procAddress == IntPtr.Zero)
                throw new InvalidOperationException($"wglGetProcAddress returned null for '{nativeName}'.");

            return Marshal.GetDelegateForFunctionPointer<T>(procAddress);
        }
    }
}
