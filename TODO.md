# SkiaMonoGameRendering - Platform Support TODO

This document tracks which framework/platform/backend combinations have been proven to work with SkiaMonoGameRendering.

## Platform Matrix

| Framework       | Version | Backend   | Platform | Sample Project              | Status      | Notes |
|-----------------|---------|-----------|----------|-----------------------------|-------------|-------|
| MonoGame        | 3.8.4   | DesktopGL | Desktop  | Sample.MonoGame.DesktopGL/  | Working     | SkiaGlBackend. Cross-platform (Windows, Linux, macOS). |
| MonoGame        | 3.8.4   | WindowsDX | Desktop  | Sample.MonoGame.WindowsDX/  | Working     | SkiaAngleBackend. Windows only. Uses ANGLE + D3D11.1 SwapDeviceContextState. |
| MonoGame        | 3.8.5   | DirectX   | Desktop  | —                           | Not started | |
| MonoGame        | 3.8.5   | Vulkan    | Desktop  | —                           | Not started | |
| KNI             | —       | DesktopGL | Desktop  | —                           | Not started | |
| KNI             | —       | DirectX   | Desktop  | —                           | Not started | |
| KNI             | —       | —         | Android  | —                           | Not started | |
| KNI             | —       | WebGL     | Web      | —                           | Blocked     | SkiaSharp WASM has no GPU backend. CPU fallback possible but defeats purpose. See README. |

## Architecture

The library uses a backend abstraction (`SkiaBackend` base class) so each graphics API gets its own implementation. `SkiaRenderer.Initialize(GraphicsDevice)` auto-detects the correct backend at runtime.

- **`SkiaGlBackend`** — OpenGL via SDL context sharing (DesktopGL)
- **`SkiaAngleBackend`** — D3D11 via ANGLE's GL ES translation (WindowsDX, Windows only)

Because MonoGame.Framework.DesktopGL and MonoGame.Framework.WindowsDX are separate NuGet packages that can't coexist, each backend has its own library project. Core source files are shared via linked includes:

- `SkiaMonoGameRendering/` — DesktopGL library (core + GL backend)
- `SkiaMonoGameRendering.WindowsDX/` — WindowsDX library (shares core files + ANGLE backend)

## Known Issues / Cleanup

- **ANGLE DLL packaging**: Currently falls back to Edge WebView's system ANGLE DLLs. The Silk.NET.OpenGLES.ANGLE.Native NuGet package ships 32-bit DLLs mislabeled as x64. Need a reliable x64 ANGLE source — either a correct NuGet package, a manual ANGLE build, or direct bundling.
- **SetData workaround for lazy texture creation**: MonoGame WindowsDX doesn't allocate the D3D11 GPU resource in the Texture2D constructor. The ANGLE backend calls `SetData(new byte[w*h*4])` to force allocation, which is wasteful. Need a cheaper way to trigger D3D11 resource creation.
- **Reflection fragility**: Both backends rely on MonoGame internal field names (`_d3dDevice`, `_d3dContext`, `_texture`, SDL internals). These may change between MonoGame versions. The WindowsDX backend additionally reflects into SharpDX types (`Device1`, `DeviceContext1`). If MonoGame moves away from SharpDX, the ANGLE backend needs updating.
- **glFinish performance**: The ANGLE backend calls `glFinish()` per renderable for GPU sync. Could potentially be relaxed to `glFlush()` if D3D11's internal synchronization is sufficient.

## Open Questions

- **MonoGame 3.8.5 Vulkan**: DX11 or DX12? Need to decide which DirectX version the Vulkan row targets.
- **KNI versions**: Which KNI package versions to target?

## Next Steps

1. Address known issues above (ANGLE DLLs, SetData workaround)
2. Work through remaining platform matrix rows
3. Update Test/ project to match new conventions (or remove if redundant with samples)
