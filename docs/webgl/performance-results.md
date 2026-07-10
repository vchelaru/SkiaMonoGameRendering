# WebGL performance results

No acceptance numbers are checked in yet. Populate this document only from JSON exported by `Benchmarks.WebGL` on identified physical hardware.

Required runs: 1920x1080, 2560x1440, and 3840x2160; direct canvas `texSubImage2D`, diagnostic `texImage2D`, ImageBitmap, OffscreenCanvas, and readPixels negative baseline; Chrome, Edge, Firefox, and Safari where support is claimed.

CI headless results are correctness signals only and must not be used for GPU budget claims.
