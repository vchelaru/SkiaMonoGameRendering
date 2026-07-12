using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SkiaMonoGameRendering;

namespace Sample
{
    /// <summary>
    /// Shared game logic for all platform samples. The correct SkiaBackend is
    /// auto-detected at runtime based on which library assembly is referenced.
    /// </summary>
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SkiaEntity _entity;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 800;

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _entity = new SkiaEntity(GraphicsDevice);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);

            _entity.Draw();
            DrawSkiaEntity();

            base.Draw(gameTime);
        }

        void DrawSkiaEntity()
        {
            var destinationRectangle = new Rectangle(0, 0, _entity.Texture.Width, _entity.Texture.Height);

            _spriteBatch.Begin(SpriteSortMode.Deferred);
            _spriteBatch.Draw(_entity.Texture, destinationRectangle, Color.White);
            _spriteBatch.End();
        }
    }
}