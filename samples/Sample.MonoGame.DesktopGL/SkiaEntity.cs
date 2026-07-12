using Microsoft.Xna.Framework.Graphics;
using SkiaMonoGameRendering;
using SkiaSharp;

namespace Sample
{
    internal class SkiaEntity
    {
        private readonly SkiaRenderTarget2D _canvas;
        private readonly SKPaint _paint;
        private bool _paintNeedsUpdate;
        private SKColor _color = SKColors.Red;

        public float Radius { get; }
        public Texture2D Texture => _canvas.Texture;
        public SKColor Color
        {
            get => _color;
            set { _color = value; _paintNeedsUpdate = true; }
        }

        public SkiaEntity(GraphicsDevice graphicsDevice, float radius = 100)
        {
            Radius = radius;
            _canvas = new SkiaRenderTarget2D(graphicsDevice, (int)radius * 2, (int)radius * 2);
            _paint = new SKPaint { Color = _color, Style = SKPaintStyle.Fill, IsAntialias = true };
        }

        public void Draw()
        {
            if (_paintNeedsUpdate)
            {
                _paint.Color = Color;
                _paintNeedsUpdate = false;
            }

            _canvas.Begin();
            _canvas.Canvas.DrawCircle(Radius, Radius, Radius, _paint);
            _canvas.End();
        }

        public void Dispose() => _canvas.Dispose();
    }
}
