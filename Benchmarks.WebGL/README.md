# WebGL upload benchmark

Serve this directory over HTTP and open `index.html`. Each run performs 120 warm-up frames and 600 measured frames for 1, 2, or 4 sequential renderables, then emits a versioned JSON report with CPU/GPU percentiles, frame misses, renderer, context attributes, DPR, upload settings, and context-loss count.

The `readPixels` path is a negative baseline only. Production code never selects it.
