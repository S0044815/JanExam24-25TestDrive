using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Tracker.WebAPIClient;

namespace TestDrive
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private enum GameState // This is like a TV: it can be on the menu or on the show. 
        {
            Opening,
            Playing
        }

        private GameState _currentState = GameState.Opening;

        private Texture2D _openingScreen;
        private SpriteFont _largeFont;
        private Song _openingMusic;

        private KeyboardState _previousKeyboard; // Why previousKeyboard? So Enter triggers once when you press it, not every frame while you hold it.

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            ActivityAPIClient.Track(
                StudentID: "S00244815",
                StudentName: "Ihor Utochkin",
                activityName: "GP01 Final Exam 2024",
                Task: "Q1b Opening Screen"
            );

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _openingScreen = Content.Load<Texture2D>("Assets/Opening Screen"); // Load the opening screen image
            _largeFont = Content.Load<SpriteFont>("Assets/Message"); // Load a large font for the message
            _openingMusic = Content.Load<Song>("Assets/Opening Music Track"); // Load the opening music track

            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(_openingMusic);
        }

        protected override void Update(GameTime gameTime)
        {
            var keyboard = Keyboard.GetState();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
                Exit();

            if (_currentState == GameState.Opening) // We check: “Was Enter just pressed this moment?” 
            {
                bool enterPressedNow = keyboard.IsKeyDown(Keys.Enter);
                bool enterWasUpBefore = _previousKeyboard.IsKeyUp(Keys.Enter);

                if (enterPressedNow && enterWasUpBefore)
                {
                    _currentState = GameState.Playing;
                    MediaPlayer.Stop();
                    // gameplay starts now (we’ll do the play screen in Q1c)
                }
            }

            _previousKeyboard = keyboard;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();

            if (_currentState == GameState.Opening)
            {
                _spriteBatch.Draw(
                    _openingScreen,
                    new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height),
                    Color.White
                );

                string text = "Press Enter to Start";
                Vector2 size = _largeFont.MeasureString(text);
                Vector2 pos = new Vector2(
                    (GraphicsDevice.Viewport.Width - size.X) / 2f,
                    (GraphicsDevice.Viewport.Height - size.Y) / 2f
                );

                _spriteBatch.DrawString(_largeFont, text, pos, Color.Black);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
