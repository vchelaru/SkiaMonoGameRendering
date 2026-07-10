# Skia-MonoGame Rendering

A library that lets MonoGame and KNI applications use SkiaSharp's GPU rendering to produce `Texture2D`s — with zero-copy GPU texture sharing. Skia renders anti-aliased vector art, text, and 2D graphics directly into MonoGame textures without any CPU readback.

## Platform Support

| Platform | Backend | Status | How it works |
|----------|---------|--------|--------------|
| MonoGame 3.8.4 DesktopGL | OpenGL | Working | Shared GL context via SDL |
| MonoGame 3.8.4 WindowsDX | D3D11 | Working | ANGLE (GL ES → D3D11 translation) on shared device |
| MonoGame 3.8.5 DirectX | D3D11/D3D12 | Not started | |
| MonoGame 3.8.5 Vulkan | Vulkan | Not started | |
| KNI DesktopGL | OpenGL | Not started | |
| KNI DirectX | D3D11 | Not started | |
| KNI Android | GL ES | Not started | |
| KNI WebGL (Blazor) | WebGL2 | Production candidate | Cross-context `texSubImage2D(canvas)` through a patched public KNI API |

## Requirements

- .NET 8
- Visual Studio 2022
- MonoGame 3.8.4.1 (DesktopGL or WindowsDX)
- SkiaSharp 3.119.4 for WebGL; 3.119.2 for the existing desktop projects

## Quick Start

Reference the appropriate library project for your platform:
- **DesktopGL**: Reference `SkiaMonoGameRendering/SkiaMonoGameRendering.csproj`
- **WindowsDX**: Reference `SkiaMonoGameRendering.WindowsDX/SkiaMonoGameRendering.WindowsDX.csproj`

In your `Initialize()` method:
```cs
SkiaRenderer.Initialize(GraphicsDevice);
```

The correct backend (GL or ANGLE) is auto-detected at runtime. You can also specify one explicitly:
```cs
SkiaRenderer.Initialize(new SkiaGlBackend(), GraphicsDevice);     // DesktopGL
SkiaRenderer.Initialize(new SkiaAngleBackend(), GraphicsDevice);  // WindowsDX
```

In your `Draw()` method, before drawing sprites:
```cs
SkiaRenderer.Draw();
```

## Implement ISkiaRenderable

Create a class that implements `ISkiaRenderable`:

| Member | Description |
|--------|-------------|
| `TargetWidth` | Width of the output texture |
| `TargetHeight` | Height of the output texture |
| `TargetColorFormat` | SkiaSharp color format for the texture |
| `ShouldRender` | Set to `false` when the texture doesn't need updating |
| `ClearCanvasOnRender` | Whether to clear the canvas before drawing |
| `DrawToSurface(SKSurface)` | Your SkiaSharp drawing code goes here |
| `NotifyDrawnTexture(Texture2D)` | Receives the resulting MonoGame texture |

Register your renderable:
```cs
SkiaRenderer.AddRenderable(myRenderable);
```

## Sample Projects

- `Sample.MonoGame.DesktopGL/` — DesktopGL sample (cross-platform: Windows, Linux, macOS)
- `Sample.MonoGame.WindowsDX/` — WindowsDX sample (Windows only)
- `Sample.Kni.WebGL/` — KNI Blazor WebAssembly sample using the patched canvas-upload API
- `Test/` — More comprehensive test with dynamic add/remove, FPS counter, input handling

Game logic is shared between samples — see `Game1.cs` and `SkiaEntity.cs`.

## Architecture

The library uses a backend abstraction (`SkiaBackend` base class) so each graphics API gets its own implementation. Core source files are shared across platform-specific library projects via linked includes:

- `SkiaMonoGameRendering/` — DesktopGL library (core + `SkiaGlBackend`)
- `SkiaMonoGameRendering.WindowsDX/` — WindowsDX library (shared core + `SkiaAngleBackend`)

See `SkiaMonoGame-Rendering-Notes.md` for detailed technical documentation on how each backend works, including the ANGLE integration and D3D11 state management.

## WebGL / WASM Status

The WebGL backend is implemented in `SkiaMonoGameRendering.Kni.WebGL`. It creates a synchronous SkiaSharp WebGL2 source host, flushes the current Skia frame, and uploads that canvas directly into a preallocated KNI `Texture2D` with `texSubImage2D`. Production code has no `readPixels`, managed pixel buffer, or `Texture2D.SetData(byte[])` path.

KNI does not yet expose canvas upload upstream, so the repository pins a KNI commit and carries a small public-API patch. Bootstrap it before building the browser projects:

```powershell
.\eng\bootstrap-kni-webgl.ps1
dotnet build Sample.Kni.WebGL\Sample.Kni.WebGL.csproj -c Release
dotnet run --project Sample.Kni.WebGL\Sample.Kni.WebGL.csproj -c Release --no-build
```

The sample proves SpriteBatch interleaving, render-target consumption, shader sampling, animated Gum/Skia content, pointer/touch/wheel/text input, fractional DPR handling, and backend recreation. See `docs/webgl/quickstart.md`, `docs/webgl/validated-baseline.md`, and `docs/documentation/SkiaWebGlBackend.md` for the exact contract and support status.

## Using SkiaSharp

The `DrawToSurface` callback gives you a full SkiaSharp `SKSurface` with GPU-accelerated canvas. For example, drawing an anti-aliased circle:

```cs
surface.Canvas.DrawCircle(Radius, Radius, Radius, _paint);
```

For more on SkiaSharp drawing, see the [SkiaSharp documentation](https://learn.microsoft.com/en-us/previous-versions/xamarin/xamarin-forms/user-interface/graphics/skiasharp/basics/).

## License

MIT License. See [LICENSE.md](LICENSE.md).

## Credits

Originally created by [Miguel Anxo Figueirido](https://github.com/mfigueirido/SkiaMonoGameRendering). Multi-platform backend abstraction and WindowsDX/ANGLE support added by Victor Chelaru.
