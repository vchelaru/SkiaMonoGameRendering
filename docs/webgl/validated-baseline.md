# Validated WebGL baseline

Updated: 2026-07-10

| Component | Pin |
|---|---|
| .NET target | net8.0 |
| KNI source | `f4f3c5ac99a3f43d6975f8d6aafc42b907c1f6ae` |
| KNI package baseline | 4.2.9001 |
| nkast.Wasm.Canvas | 8.0.11 |
| SkiaSharp | 3.119.4 |
| Gum.SkiaSharp | 2026.5.31.1 |
| Graphics profile | HiDef / WebGL2 |
| Texture format | RGBA8 / `SurfaceFormat.Color` / `SKColorType.Rgba8888` |

Debug and Release native-WASM builds are validated on Windows. A package-consumer Release publish with trimming and full WASM AOT over 103 assemblies also succeeds. Core lifecycle tests pass. Chromium functional tests pass at DPR 1, 1.25, 1.5, and 2, including context loss, page reload, input, backend recreation, and both canvas upload modes. The repository and CI also cover Firefox and WebKit; those two browser binaries were not installed in the local validation environment.

No hardware performance result is asserted by this file yet. Chrome, Edge, and Firefox remain release candidates rather than declared Tier 1 until benchmark JSON reports are checked in from representative hardware. Safari remains Tier 2.

Option A is intentionally not implemented: no measured mandatory-browser failure has triggered its gate.
