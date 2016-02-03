#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Input;
using Axiom.RoguelikeLib;

#endregion

namespace Axiom.RoguelikeTest
{
	/// <summary>
	/// This is the main type for your game.
	/// </summary>
	public class RoguelikeGame : Game
	{
		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;

		private Texture2D _floorTex;
		private Texture2D _wallTex;
		private Texture2D _playerTex;
		private Texture2D _doorTex;

		private GameObject _player;

	    private GameObject _testGO;

		private InputState _inputState;
		private Level _level;

	    private RTWorld _world;

		public RoguelikeGame ()
		{
			graphics = new GraphicsDeviceManager (this);
			graphics.PreferredBackBufferWidth = 1280;  // set this value to the desired width of your window
			graphics.PreferredBackBufferHeight = 720;   // set this value to the desired height of your window
			graphics.ApplyChanges ();
			Content.RootDirectory = "Content";	            
			//graphics.IsFullScreen = true;
			_inputState = new InputState ();
            _testGO = new GameObject();
            //_testGO.AttachComponent(new SpatialComponent(Vector2.One));


            
		}

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize ()
		{
			// TODO: Add your initialization logic here
			Level.Camera.ViewportWidth = 1280;
			Level.Camera.ViewportHeight = 720;


			base.Initialize ();

		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent ()
		{
			// Create a new SpriteBatch, which can be used to draw textures.
			spriteBatch = new SpriteBatch (GraphicsDevice);

			//TODO: use this.Content to load your game content here
			_floorTex = Content.Load<Texture2D> ("floor");
			_wallTex = Content.Load<Texture2D> ("wall");
			_playerTex = Content.Load<Texture2D> ("player");
			_doorTex = Content.Load<Texture2D> ("door");

			_level = Level.Generate (101, 101);
			_level.Player = new GameObject ();
			_player = _level.Player;

			var emptyTile = _level.GetRandomEmptyTile ();
			_player.Position = new Vector2 (emptyTile.CellData.X, emptyTile.CellData.Y);

			_level.UpdatePlayerFov ();

			Level.Camera.SpriteWidth = 64;
			Level.Camera.SpriteHeight = 64;

			Level.Camera.CenterOn (emptyTile);

            _world = new RTWorld(5, spriteBatch);
            var entity = _world.CreateEntity();

            _world.EntityMasks[entity].ClearAll();
            _world.SpatialComponents[entity].Position = new Vector2(50, 50);
		    _world.SpriteComponents[entity].Texture = _playerTex;
		    _world.SpriteComponents[entity].Tint = Color.Red;
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update (GameTime gameTime)
		{
			// For Mobile devices, this logic will close the Game when the Back button is pressed
			// Exit() is obsolete on iOS
			#if !__IOS__ &&  !__TVOS__
			if (GamePad.GetState (PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
			    Keyboard.GetState ().IsKeyDown (Keys.Escape)) {
				Exit ();
			}
			#endif


			// Player input handling
			_inputState.Update ();
			Level.Camera.HandleInput( _inputState, PlayerIndex.One );

			float x = 0;
			float y = 0;

			if (_inputState.IsLeft (PlayerIndex.One)) {
				x--;
			} else if (_inputState.IsRight (PlayerIndex.One)) {
				x++;
			} else if (_inputState.IsUp (PlayerIndex.One)) {
				y--;
			} else if (_inputState.IsDown (PlayerIndex.One)) {
				y++;
			}

			if (x != 0 || y != 0)
			{
				var newPos = new Vector2 (_level.Player.Position.X + x, _level.Player.Position.Y + y);
				var tile = _level.GetTile (newPos);

				if (tile.CellData.IsWalkable) {
					_level.Player.Position = newPos;
					_level.UpdatePlayerFov ();
					Level.Camera.CenterOn(tile);
				}
				else if (tile.TileType == TileType.Door)
				{
					_level.SetTileProperties (tile, true, true, true);
					_level.UpdatePlayerFov ();
				}
			}
			base.Update (gameTime);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw (GameTime gameTime)
		{
			graphics.GraphicsDevice.Clear (Color.Black);

			var sizeOfSprites = 64;
			var scale = 1f;
			var multiplier = sizeOfSprites * scale;

			GraphicsDevice.Clear (Color.Black);
		
			spriteBatch.Begin( SpriteSortMode.BackToFront, BlendState.AlphaBlend, 
				null, null, null, null, Level.Camera.TranslationMatrix );

			for (int x = 0; x < _level.Width; x++) {
				for (int y = 0; y < _level.Height; y++) {
					var tilePos = new Vector2 (x, y);
					var tile = _level.GetTile (tilePos);
					var position = new Vector2 (tile.CellData.X * multiplier, tile.CellData.Y * multiplier);

					if (_level.IsInFov (tilePos)) {
						switch (tile.TileType) {
						case TileType.Floor:
							spriteBatch.Draw (_floorTex, position, null, null, null, 0f, Vector2.One, Color.White, SpriteEffects.None, LayerDepth.Cells);
							break;
						case TileType.Wall:
							spriteBatch.Draw (_wallTex, position, null, null, null, 0f, Vector2.One, Color.White, SpriteEffects.None, LayerDepth.Cells);
							break;
						case TileType.RoomFloor:
							spriteBatch.Draw (_floorTex, position, null, null, null, 0f, Vector2.One, Color.Beige, SpriteEffects.None, LayerDepth.Cells);
							break;
						case TileType.Door:
							spriteBatch.Draw (_doorTex, position, null, null, null, 0f, Vector2.One, Color.White, SpriteEffects.None, LayerDepth.Cells);
							break;
						}
					} else if (tile.CellData.IsExplored) {
						switch (tile.TileType) {
						case TileType.Floor:
							spriteBatch.Draw (_floorTex, position, null, null, null, 0f, Vector2.One, Color.Gray, SpriteEffects.None, LayerDepth.Cells);
							break;
						case TileType.Wall:
							spriteBatch.Draw (_wallTex, position, null, null, null, 0f, Vector2.One, Color.Gray, SpriteEffects.None, LayerDepth.Cells);
							break;
						case TileType.RoomFloor:
							spriteBatch.Draw (_floorTex, position, null, null, null, 0f, Vector2.One, Color.Gray, SpriteEffects.None, LayerDepth.Cells);
							break;
						case TileType.Door:
							spriteBatch.Draw (_doorTex, position, null, null, null, 0f, Vector2.One, Color.Gray, SpriteEffects.None, LayerDepth.Cells);
							break;
						}
					}
				}	
			}

			spriteBatch.Draw (_playerTex, new Vector2 (_level.Player.Position.X * multiplier, _level.Player.Position.Y * multiplier), null, null, null, 0f, new Vector2 (scale, scale), Color.White, SpriteEffects.None, LayerDepth.Cells);

            _world.Render();

			spriteBatch.End ();

			base.Draw (gameTime);
		}
	}
}

