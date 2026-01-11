using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
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
            Playing,
            Success // Q1f: Success state after all collectables collected
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

        // Q1f: Player asset
        private Texture2D _playerTexture;

        // Q1f: Sound when collectable collected (wav)
        private SoundEffect _collectedSound;

        // Q1f: Success sound (mp3)
        private Song _successSound;

        // Q1b: One-press Enter detection
        private KeyboardState _previousKeyboard; // Why previousKeyboard? So Enter triggers once when you press it, not every frame while you hold it.

        // Q1d: Random generator for positions + values
        private readonly Random _rng = new Random();

        // Q1d: Collectable data (position + value)
        private struct Collectable
        {
            public Vector2 Position;
            public int Value;
            public bool IsCollected; // Q1f: disappears after collected

            public Collectable(Vector2 position, int value)
            {
                Position = position;
                Value = value;
                IsCollected = false;
            }

            // Q1f: simple rectangle collision shape
            public Rectangle GetBounds(int width, int height)
            {
                return new Rectangle((int)Position.X, (int)Position.Y, width, height);
            }
        }

        // Q1d: Store 5 collectables
        private List<Collectable> _collectables = new List<Collectable>(5);

        // Q1e: Camera position (lets you view a 3000x3000 world)
        private Vector2 _cameraPosition = Vector2.Zero;

        // Q1f: Player class (moves, clamps, stores score, draws HUD)
        private class Player
        {
            private Texture2D _texture;
            public Vector2 Position;
            public int Score;

            public float Speed = 350f;

            public Player(Texture2D texture, Vector2 startPos)
            {
                _texture = texture;
                Position = startPos;
                Score = 0;
            }

            // Q1f: Player bounding box for collision
            public Rectangle Bounds
            {
                get
                {
                    return new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);
                }
            }

            // Q1f: Player moves using the A, W, S, D keys
            public void Update(GameTime gameTime, KeyboardState keyboard)
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

                Vector2 move = Vector2.Zero;

                if (keyboard.IsKeyDown(Keys.A)) move.X -= 1;
                if (keyboard.IsKeyDown(Keys.D)) move.X += 1;
                if (keyboard.IsKeyDown(Keys.W)) move.Y -= 1;
                if (keyboard.IsKeyDown(Keys.S)) move.Y += 1;

                if (move != Vector2.Zero)
                    move.Normalize();

                Position += move * Speed * dt;
            }

            // Q1f: Player should not be able to move outside world extents
            public void ClampToWorld(int worldWidth, int worldHeight)
            {
                float maxX = worldWidth - _texture.Width;
                float maxY = worldHeight - _texture.Height;

                Position.X = MathHelper.Clamp(Position.X, 0, Math.Max(0, maxX));
                Position.Y = MathHelper.Clamp(Position.Y, 0, Math.Max(0, maxY));
            }

            public void Draw(SpriteBatch spriteBatch)
            {
                spriteBatch.Draw(_texture, Position, Color.White);
            }

            // Q1f: The player score is displayed by the player class in the top righthand corner of the viewport
            public void DrawScoreTopRight(SpriteBatch spriteBatch, SpriteFont font, Viewport viewport, Color color)
            {
                string text = Score.ToString();
                Vector2 size = font.MeasureString(text);

                Vector2 pos = new Vector2(
                    viewport.Width - size.X - 10f,
                    10f
                );

                spriteBatch.DrawString(font, text, pos, color);
            }
        }

        private Player _player;

        // Q1f: We only want to play success sound once
        private bool _successSoundPlayed = false;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // Q1f: Activity tracker task name
            ActivityAPIClient.Track(
                StudentID: "S00244815",
                StudentName: "Ihor Utochkin",
                activityName: "GP01 Final Exam 2024",
                Task: "Q1f Creating Player"
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

            // Q1f: Load player texture
            _playerTexture = Content.Load<Texture2D>("Assets/Player");

            // Q1f: Load collected sound + success sound
            _collectedSound = Content.Load<SoundEffect>("Assets/Collected");
            _successSound = Content.Load<Song>("Assets/Success");

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

        // Q1f: create a player object and place it into the world
        private void CreatePlayer()
        {
            // Start near top-left so you immediately see the player when Playing begins
            _player = new Player(_playerTexture, new Vector2(50, 50));
        }

        // Q1e/Q1f: Camera follows the player and clamps to world extents
        private void UpdateCameraFollowPlayer()
        {
            // Camera tries to keep player in the center of the viewport
            float targetX = _player.Position.X + (_playerTexture.Width / 2f) - (GraphicsDevice.Viewport.Width / 2f);
            float targetY = _player.Position.Y + (_playerTexture.Height / 2f) - (GraphicsDevice.Viewport.Height / 2f);

            float maxCamX = WorldWidth - GraphicsDevice.Viewport.Width;
            float maxCamY = WorldHeight - GraphicsDevice.Viewport.Height;

            _cameraPosition.X = MathHelper.Clamp(targetX, 0, Math.Max(0, maxCamX));
            _cameraPosition.Y = MathHelper.Clamp(targetY, 0, Math.Max(0, maxCamY));
        }

        // Q1f: Handle collisions between player and collectables
        private void CheckCollectableCollisions()
        {
            Rectangle playerRect = _player.Bounds;

            for (int i = 0; i < _collectables.Count; i++)
            {
                if (_collectables[i].IsCollected)
                    continue;

                Rectangle cRect = _collectables[i].GetBounds(_collectableTexture.Width, _collectableTexture.Height);

                if (playerRect.Intersects(cRect))
                {
                    // Q1f: play collected sound
                    _collectedSound.Play();

                    // Q1f: increase score by collectable value
                    _player.Score += _collectables[i].Value;

                    // Q1f: collectable disappears
                    Collectable temp = _collectables[i];
                    temp.IsCollected = true;
                    _collectables[i] = temp;
                }
            }

            // Q1f: When all collectables are collected -> play success sound and show centered score
            bool allCollected = true;
            for (int i = 0; i < _collectables.Count; i++)
            {
                if (!_collectables[i].IsCollected)
                {
                    allCollected = false;
                    break;
                }
            }

            if (allCollected && _currentState == GameState.Playing)
            {
                _currentState = GameState.Success;
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

                    // Q1f: Create the player object when we start playing
                    CreatePlayer();

                    // Q1e: Reset camera when we start playing (so we begin at top-left)
                    _cameraPosition = Vector2.Zero;

                    // Q1f: reset success flag each new run
                    _successSoundPlayed = false;
                }
            }

            // Q1f: Player moves using the A, W, S, D keys. Player clamps to world. Camera follows player.
            if (_currentState == GameState.Playing)
            {
                if (_player != null)
                {
                    _player.Update(gameTime, keyboard);
                    _player.ClampToWorld(WorldWidth, WorldHeight);

                    // Q1f: camera follows player (this replaces arrow-key camera movement)
                    UpdateCameraFollowPlayer();

                    // Q1f: collision with collectables
                    CheckCollectableCollisions();
                }
            }

            // Q1f: Success sound should be played once when all collected
            if (_currentState == GameState.Success && !_successSoundPlayed)
            {
                _successSoundPlayed = true;

                // Stop play music and play success sound (song)
                MediaPlayer.Stop();
                MediaPlayer.IsRepeating = false;
                MediaPlayer.Play(_successSound);
            }

            _previousKeyboard = keyboard;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // Q1b: Opening screen draws in screen space (no camera)
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

                base.Draw(gameTime);
                return;
            }

            // Q1e/Q1c/Q1d/Q1f: World draws in world space using camera transformMatrix
            Matrix cameraMatrix = Matrix.CreateTranslation(-_cameraPosition.X, -_cameraPosition.Y, 0f);

            _spriteBatch.Begin(transformMatrix: cameraMatrix);

            // Q1c: Draw play screen background at 3000x3000
            _spriteBatch.Draw(
                _background,
                new Rectangle(0, 0, WorldWidth, WorldHeight),
                Color.White
            );

            // Q1d/Q1f: Draw collectables (skip collected ones) and draw their value above each collectable
            for (int i = 0; i < _collectables.Count; i++)
            {
                if (_collectables[i].IsCollected)
                    continue;

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

            // Q1f: Draw player in the world
            if (_player != null)
            {
                _player.Draw(_spriteBatch);
            }

            _spriteBatch.End();

            // Q1f: HUD should be drawn in screen space (no camera transform)
            _spriteBatch.Begin();

            if (_player != null)
            {
                // Q1f: score displayed by player class in top right of viewport
                _player.DrawScoreTopRight(_spriteBatch, _largeFont, GraphicsDevice.Viewport, Color.Black);
            }

            // Q1f: When all collectables are collected the player score is displayed in the middle of the viewport
            if (_currentState == GameState.Success && _player != null)
            {
                string scoreText = _player.Score.ToString();
                Vector2 scoreSize = _largeFont.MeasureString(scoreText);

                Vector2 scorePos = new Vector2(
                    (GraphicsDevice.Viewport.Width - scoreSize.X) / 2f,
                    (GraphicsDevice.Viewport.Height - scoreSize.Y) / 2f
                );

                _spriteBatch.DrawString(_largeFont, scoreText, scorePos, Color.Black);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
