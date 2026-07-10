# KNI WebGL quick start

## Prerequisites

- .NET 8 SDK and the `wasm-tools-net8` workload
- WebGL2-capable browser
- PowerShell and Git for the pinned KNI bootstrap

## Build and run

```powershell
dotnet workload install wasm-tools-net8
.\eng\bootstrap-kni-webgl.ps1
dotnet build Sample.Kni.WebGL\Sample.Kni.WebGL.csproj -c Release
dotnet run --project Sample.Kni.WebGL\Sample.Kni.WebGL.csproj -c Release --no-build
```

The library is a Razor class library, so `skia-monogame-webgl.js` is delivered as a static web asset. Consumers do not copy JavaScript manually.

## Application contract

Render `<SkiaMonoGameWebGlHost @ref="host" />`, await `host.Ready`, then construct `SkiaWebGlBackend` explicitly and initialize `SkiaRenderer`. All graphics calls must stay on the browser graphics thread.

`SkiaRenderer.Draw()` must run after any SpriteBatch pass below the UI has ended. The returned renderable texture is a normal KNI `Texture2D` and can be sampled by SpriteBatch, a `RenderTarget2D`, or an effect.

Dispose in this order: stop the game loop, call `SkiaRenderer.Dispose()` to release every target/backend resource, then dispose the host.

## Patched packages

`eng/pack-kni-webgl.ps1` creates the pinned KNI fork packages and `SkiaMonoGameRendering.Kni.WebGL` package in `.artifacts/packages`. KNI uses `4.2.9001-skia-interop.1` and the WebGL library uses `1.0.0-skia-interop.1`, so neither can be confused with binaries lacking `UploadFromCanvas`.
