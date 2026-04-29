# SkiaMonoGameRendering - Platform Support TODO

This document tracks which framework/platform/backend combinations have been proven to work with SkiaMonoGameRendering.

## Platform Matrix

| Framework       | Version | Backend   | Platform | Sample Project | Status      | Notes |
|-----------------|---------|-----------|----------|----------------|-------------|-------|
| MonoGame        | 3.8.4   | DesktopGL | Desktop  | Sample/, Test/  | Not verified | Updated from 3.8.1; existing samples need to be built and tested on 3.8.4 |
| MonoGame        | 3.8.5   | DirectX   | Desktop  | —              | Not started | |
| MonoGame        | 3.8.5   | Vulkan    | Desktop  | —              | Not started | DX11 vs DX12 TBD |
| KNI             | —       | DesktopGL | Desktop  | —              | Not started | |
| KNI             | —       | DirectX   | Desktop  | —              | Not started | |
| KNI             | —       | —         | Android  | —              | Not started | |
| KNI             | —       | WebGL     | Web      | —              | Not started | Blazor-based |

## Open Questions

- **MonoGame 3.8.5 Vulkan**: DX11 or DX12? Need to decide which DirectX version the Vulkan row targets.
- **KNI versions**: Which KNI package versions to target?
- **Shared sample code**: The sample projects should share as much game logic as possible across platforms. Need example projects to base the multi-platform structure on.

## Next Steps

1. Verify existing Sample/ and Test/ projects build and run on MonoGame 3.8.4.1
2. Create multi-platform sample project structure (pending example projects for reference)
3. Work through each row in the matrix, updating status as each is proven
