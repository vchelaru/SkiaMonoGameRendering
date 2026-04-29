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
| KNI WebGL (Blazor) | WebGL | Blocked | See [WebGL / WASM Status](#webgl--wasm-status) |

## Requirements

- .NET 8
- Visual Studio 2022
- MonoGame 3.8.4.1 (DesktopGL or WindowsDX)
- SkiaSharp 3.119.2

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
- `Test/` — More comprehensive test with dynamic add/remove, FPS counter, input handling

Game logic is shared between samples — see `Game1.cs` and `SkiaEntity.cs`.

## Architecture

The library uses a backend abstraction (`SkiaBackend` base class) so each graphics API gets its own implementation. Core source files are shared across platform-specific library projects via linked includes:

- `SkiaMonoGameRendering/` — DesktopGL library (core + `SkiaGlBackend`)
- `SkiaMonoGameRendering.WindowsDX/` — WindowsDX library (shared core + `SkiaAngleBackend`)

See `SkiaMonoGame-Rendering-Notes.md` for detailed technical documentation on how each backend works, including the ANGLE integration and D3D11 state management.

## WebGL / WASM Status

**GPU-accelerated Skia rendering in the browser is currently not possible from .NET.**

This may be surprising since Skia is Chrome's rendering engine and is GPU-accelerated there. The difference:

- **Chrome's Skia** runs as native C++ code *inside* the browser, with direct access to the GPU via the OS graphics API. It's not sandboxed.
- **SkiaSharp in Blazor WASM** runs as WebAssembly *inside* the browser sandbox. The SkiaSharp NuGet package (`SkiaSharp.NativeAssets.WebAssembly`) only ships the **CPU rasterizer** — it does not wire up Skia's GL backend to the browser's WebGL context.
- **Google's CanvasKit** is a separate WASM build of Skia that *does* include WebGL GPU support (used by Flutter Web, Google Docs, etc.), but it's a JavaScript library with no .NET wrapper.

The Skia C++ code fully supports rendering via GL ES 2.0 (which is what WebGL is). The gap is purely in the .NET packaging — nobody has built the bridge between SkiaSharp's managed WASM build and the browser's WebGL context.

A CPU-only fallback (Skia renders to a bitmap, then `SetData()` uploads to a WebGL texture) is feasible but defeats the zero-copy GPU purpose of this library.

**If you have experience with SkiaSharp internals, WASM native interop, or CanvasKit, and are interested in helping bridge this gap, please open an issue.** Even a proof-of-concept showing `GRContext.CreateGl()` working against a browser WebGL context from .NET WASM would be a major contribution.

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
