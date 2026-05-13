# webgl-blit-v0 — cross-context `texImage2D(canvas)` spike

Standalone static HTML page. No build, no Blazor, no MonoGame. Open `index.html` directly in a browser (or serve it with any static server) and watch the HUD.

## What it tests

The load-bearing question for SkiaMonoGameRendering Option D (see section 8 of `../../SkiaMonoGame-Rendering-Notes.md`): is `gl.texImage2D(TEXTURE_2D, 0, RGBA, RGBA, UNSIGNED_BYTE, sourceCanvas)` cheap enough when source and destination are *different* WebGL2 contexts on different canvases?

If yes (sub-millisecond GPU, low CPU), Option D is the path. If no (multi-millisecond), we revisit Option A (shared WebGL context).

## How to run

```
# pick one
start index.html
# or
python -m http.server 8000   # then http://localhost:8000/
```

Either works. No `file://` security issues — there are no fetches.

## What to look at

The HUD reports:
- **Upload CPU**: wall-clock cost of the `texImage2D` JS call. Includes IPC enqueue, validation, no GPU wait.
- **Upload GPU**: actual GPU-side time via `EXT_disjoint_timer_query_webgl2` (precision varies by browser; some browsers quantize to 100 µs or worse for security).
- **Frame time avg**: end-to-end frame time. Should be near-vsync (~16.7 ms at 60 Hz) if everything's healthy.

Visual check: the destination canvas should show the same animated rotating triangle as the source. If the destination is black or static, the upload isn't working.

## Upload paths in the dropdown

- **`texImage2D(canvas)`** — the default. Fast on Chrome/Edge (Blink + ANGLE/D3D11 on Windows, GPU blit). Slow on Firefox per measurements: ~25 ms at 1080p, consistent with an internal CPU readback.
- **`texSubImage2D(canvas)`** — avoids re-allocating destination storage every frame. After frame 1 the destination texture exists; `texSubImage2D` just overwrites. Usually a touch faster.
- **`createImageBitmap → texImage2D(bitmap)`** — async. The explicit "this is going to a texture" intent may dodge Firefox's readback heuristic. CPU time includes the `createImageBitmap` cost since you pay it in production.
- **`OffscreenCanvas + transferToImageBitmap → texImage2D(bitmap)`** — the ownership-transfer fast path. Source becomes an `OffscreenCanvas`; `transferToImageBitmap` is synchronous and zero-copy on supporting browsers. Most likely candidate to rescue Firefox.
- **`readPixels → texImage2D(Uint8Array)`** — Option B baseline. Pulls pixels to CPU explicitly, uploads as typed array. If this matches the "slow" canvas path on a browser, that browser is already doing readback under the hood for the canvas variant.

## Other toggles

- **Source canvas size** — bump 1080p → 1440p → 4K and watch how cost scales. Linear in pixel count is the fast-path signature; sub-linear means the cost is dominated by fixed overhead (IPC, sync); super-linear means readback-style behavior.
- **`UNPACK_PREMULTIPLY_ALPHA_WEBGL` / `UNPACK_FLIP_Y_WEBGL`** — flipping these can push the browser off the direct-blit fast path and into a fullscreen conversion-shader pass.
- **Flush source before upload** — forces `gl.flush()` on the source context. Tests whether implicit ordering is enough.

## Cross-browser

Run in Chrome, Firefox, Edge. Safari if you're feeling thorough.

**Observed v0 result (initial run):**
- Chrome / Edge: ~0.25 ms at 1080p with `texImage2D(canvas)`. Fast path confirmed.
- Firefox: ~25 ms at 1080p with `texImage2D(canvas)`. ~100× slower — almost certainly an internal CPU readback. Run the alternative upload paths (`OffscreenCanvas + transferToImageBitmap` especially) to see if any of them rescue Firefox.

## Status / where to pick up

- **Done:** the default `texImage2D(canvas)` path measured across Chrome/Edge/Firefox at 1080p.
- **Next:** in Firefox, cycle through the four alternative upload-path dropdown entries at 1080p / 1440p / 4K and record numbers. Particularly want `OffscreenCanvas + transferToImageBitmap` — that's the spec-blessed zero-copy path and the most likely candidate to drop Firefox to sub-millisecond. Also worth running `readPixels` baseline on Firefox: if it matches the canvas-path 25 ms, Firefox is already doing readback internally for `texImage2D(canvas)`.
- **After v0 conclusive:** move to v1 — repeat the measurement with a real KNI MG canvas as the destination. See section 8 of `../../SkiaMonoGame-Rendering-Notes.md` for the broader plan and v1/v2 scope.

## What this *doesn't* test

- KNI's WebGL context specifically — that's v1.
- SkiaSharp's WASM Skia as the source — using a hand-written WebGL2 source instead. Skia adds its own per-frame draw cost, but that's parallel to the upload cost, not part of it.
- Interleaving with MG's frame structure — v2.
- `OffscreenCanvas` + `ImageBitmap` fast path. Could add a toggle for this if v0 results are borderline.

## Success criteria (rough)

At 1920×1080 RGBA, on Chrome with a mid-range GPU:
- Upload CPU < 500 µs
- Upload GPU < 1 ms

If both hit, Option D is viable. If GPU times are multi-millisecond regardless of toggles, the fast path is unavailable in this browser/driver combo and we either restrict targeting or revisit Option A.
