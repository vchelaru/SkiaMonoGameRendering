# SkiaWebGlBackend

## Definition

`SkiaWebGlBackend` is the browser-specific `SkiaBackend` that renders an `ISkiaRenderable` synchronously into the hidden GPU canvas owned by `SkiaMonoGameWebGlHost`, flushes Skia, and uploads that canvas into an allocated KNI `Texture2D`.

Namespace: `SkiaMonoGameRendering.Kni.WebGL`

Assembly/package: `SkiaMonoGameRendering.Kni.WebGL`

```csharp
[SupportedOSPlatform("browser")]
public sealed class SkiaWebGlBackend : SkiaBackend
```

## Constructor

| Signature | Description |
| --- | --- |
| `SkiaWebGlBackend(SkiaMonoGameWebGlHost host, SkiaWebGlOptions? options = null)` | Creates a backend for a ready host. The backend subscribes to the host's context lifecycle until disposed. |

## Members

| Member | Description |
| --- | --- |
| `Diagnostics` | Read-only counters and latest CPU timing for rendering, upload, resize, dropped frames, and context loss. |
| `GRContext` | The Skia GPU context owned by the host. It is available after the host has created its first surface. |
| `Initialize(GraphicsDevice)` | Attaches the backend to the KNI graphics device. `host.Ready` must already have completed. |
| `Dispose()` | Unsubscribes context events. Targets and their KNI textures are disposed by `SkiaRenderer.Dispose()`. |

## Options

`SkiaWebGlOptions` controls WebGL2 validation, direct `texSubImage2D` versus diagnostic `texImage2D`, Y orientation, premultiplied alpha, and color-space conversion. The production default path is direct canvas `texSubImage2D`; `DiagnosticTexImage2D` reallocates storage and is intended only for comparison.

## Example

```razor
<SkiaMonoGameWebGlHost @ref="host" RequireWebGl2="true" />

@code {
    private SkiaMonoGameWebGlHost? host;

    private async Task StartAsync(GraphicsDevice graphicsDevice, ISkiaRenderable ui)
    {
        await host!.Ready;

        var backend = new SkiaWebGlBackend(host, new SkiaWebGlOptions
        {
            RequireWebGl2 = true,
            FlipY = false,
            PremultiplyAlpha = true,
            DisableColorSpaceConversion = true,
        });

        SkiaRenderer.Initialize(backend, graphicsDevice);
        SkiaRenderer.AddRenderable(ui);
    }
}
```

Call `SkiaRenderer.Draw()` only after ending any lower `SpriteBatch`. The notified texture is current when `Draw()` returns and can immediately be sampled by another `SpriteBatch`, a `RenderTarget2D` pass, or a KNI effect.

## Remarks

- Rendering and upload are synchronous and must remain on the browser graphics thread.
- Only `SurfaceFormat.Color` / RGBA8 targets are supported.
- The normal upload path has no `readPixels`, managed pixel buffer, or per-frame `Texture2D` allocation.
- During `webglcontextlost`, pause `SkiaRenderer.Draw()` until the host reports restoration.
- Dispose `SkiaRenderer` before disposing the host or replacing the backend/graphics device.

See also [WebGL quick start](../webgl/quickstart.md) and [troubleshooting](../webgl/troubleshooting.md).
