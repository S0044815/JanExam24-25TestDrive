using System;
using System.Collections.Generic;
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

        // Q1b: Game state system (Opening vs Playing)
        private enum GameState // This is like a TV: it can be on the menu or on the show. 
        {
            Opening,
            Playing
        }

        private GameState _currentState = GameState.Opening;

        // Q1c: World size (Play screen should be 3000x3000)
        private const int WorldWidth = 3000;
        private const int WorldHeight = 3000;

        // Q1b: Opening screen assets
        private Texture2D _openingScreen;
        private SpriteFont _largeFont;
        private Song _openingMusic;

        // Q1c: Play screen assets (background + play music)
        private Texture2D _background;
        private Song _playMusic;

        // Q1d: Collectable asset
        private Texture2D _collectableTexture;

        // Q1b: One-press Enter detection
        private KeyboardState _previousKeyboard; // Why previousKeyboard? So Enter triggers once when you press it, not every frame while you hold it.

        // Q1d: Random generator for positions + values
        private readonly Random _rng = new Random();

        // Q1d: Collectable data (position + value)
        private struct Collectable
        {
            public Vector2 Position;
            public int Value;

            public Collectable(Vector2 position, int value)
            {
                Position = position;
                Value = value;
            }
        }

        // Q1d: Store 5 collectables
        private List<Collectable> _collectables = new List<Collectable>(5);

        // Q1e: Camera position (lets you view a 3000x3000 world)
        private Vector2 _cameraPosition = Vector2.Zero;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // Q1e: Activity tracker task name
            ActivityAPIClient.Track(
                StudentID: "S00244815",
                StudentName: "Ihor Utochkin",
                activityName: "GP01 Final Exam 2024",
                Task: "Q1e Setting up camera"
            );

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Q1b: Load opening screen + font + opening music
            _openingScreen = Content.Load<Texture2D>("Assets/Opening Screen"); // Load the opening screen image
            _largeFont = Content.Load<SpriteFont>("Assets/Message"); // Load a large font for the message
            _openingMusic = Content.Load<Song>("Assets/Opening Music Track"); // Load the opening music track

            // Q1c: Load play screen background + play music
            // IMPORTANT: Use the exact asset name that appears in MGCB (either "background" or "Assets/background")
            _background = Content.Load<Texture2D>("Assets/background");
            _playMusic = Content.Load<Song>("Assets/Play Track");

            // Q1d: Load collectable texture
            _collectableTexture = Content.Load<Texture2D>("Assets/Collectable");

            // Q1b: Start opening music
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(_openingMusic);
        }

        // Q1d: Create 5 collectables with random values (10..100) and random positions in 3000x3000 world
        private void CreateCollectables()
        {
            _collectables.Clear();

            int maxX = Math.Max(0, WorldWidth - _collectableTexture.Width);
            int maxY = Math.Max(0, WorldHeight - _collectableTexture.Height);

            for (int i = 0; i < 5; i++)
            {
                int value = _rng.Next(10, 101);
                float x = _rng.Next(0, maxX + 1);
                float y = _rng.Next(0, maxY + 1);

                _collectables.Add(new Collectable(new Vector2(x, y), value));
            }
        }

        protected override void Update(GameTime gameTime)
        {
            var keyboard = Keyboard.GetState();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
                Exit();

            // Q1b/Q1c: Enter starts gameplay (switch state, stop opening music, start play music)
            if (_currentState == GameState.Opening) // We check: “Was Enter just pressed this moment?” 
            {
                bool enterPressedNow = keyboard.IsKeyDown(Keys.Enter);
                bool enterWasUpBefore = _previousKeyboard.IsKeyUp(Keys.Enter);

                if (enterPressedNow && enterWasUpBefore)
                {
                    _currentState = GameState.Playing;
                    MediaPlayer.Stop();
                    // gameplay starts now (we’ll do the play screen in Q1c)
                    MediaPlayer.IsRepeating = true;
                    MediaPlayer.Play(_playMusic);

                    // Q1d: Spawn collectables only when we enter Playing state
                    CreateCollectables();

                    // Q1e: Reset camera when we start playing (so we begin at top-left)
                    _cameraPosition = Vector2.Zero;
                }
            }

            // Q1e: Camera moves using ARROW KEYS and is clamped to the 3000x3000 world
            if (_currentState == GameState.Playing)
            {
                float speed = 600f * (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (keyboard.IsKeyDown(Keys.Left))
                    _cameraPosition.X -= speed;
                if (keyboard.IsKeyDown(Keys.Right))
                    _cameraPosition.X += speed;
                if (keyboard.IsKeyDown(Keys.Up))
                    _cameraPosition.Y -= speed;
                if (keyboard.IsKeyDown(Keys.Down))
                    _cameraPosition.Y += speed;

                // Q1e: Clamp camera so it never shows outside the world
                float maxCamX = WorldWidth - GraphicsDevice.Viewport.Width;
                float maxCamY = WorldHeight - GraphicsDevice.Viewport.Height;

                _cameraPosition.X = MathHelper.Clamp(_cameraPosition.X, 0, Math.Max(0, maxCamX));
                _cameraPosition.Y = MathHelper.Clamp(_cameraPosition.Y, 0, Math.Max(0, maxCamY));
            }

            _previousKeyboard = keyboard;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // Q1b: Draw opening screen WITHOUT camera (so it is always centered on the window)
            if (_currentState == GameState.Opening)
            {
                _spriteBatch.Begin();

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

                _spriteBatch.End();
            }

            // Q1c/Q1d/Q1e: Draw play world WITH camera transformMatrix
            if (_currentState == GameState.Playing)
            {
                // Q1e: Camera transform (shifts the view across a large world)
                Matrix cameraMatrix = Matrix.CreateTranslation(-_cameraPosition.X, -_cameraPosition.Y, 0f);

                _spriteBatch.Begin(transformMatrix: cameraMatrix);

                // Q1c: Draw play screen background at 3000x3000
                _spriteBatch.Draw(
                    _background,
                    new Rectangle(0, 0, WorldWidth, WorldHeight),
                    Color.White
                );

                // Q1d: Draw 5 collectables + draw their value above each collectable
                for (int i = 0; i < _collectables.Count; i++)
                {
                    var c = _collectables[i];

                    _spriteBatch.Draw(_collectableTexture, c.Position, Color.White);

                    string valueText = c.Value.ToString();
                    Vector2 textSize = _largeFont.MeasureString(valueText);

                    Vector2 textPos = new Vector2(
                        c.Position.X + (_collectableTexture.Width - textSize.X) / 2f,
                        c.Position.Y - textSize.Y - 4f
                    );

                    _spriteBatch.DrawString(_largeFont, valueText, textPos, Color.Black);
                }

                _spriteBatch.End();
            }

            base.Draw(gameTime);
        }
    }
}
