using System;
using System.Collections.Generic;
using RogueSharp;
using Microsoft.Xna.Framework;

namespace Axiom.RoguelikeLib
{
	public class Level
	{
		public GameObject Player { get; set; }

		public int Width { get; protected set; }
		public int Height { get; protected set; }

		public static readonly Camera Camera = new Camera();

		protected IMap _map;
		protected Tile[,] _tiles;

		protected List<GameObject> _gameObjects;

		private Level(IMap map, Tile[,] tiles)
		{
			_map = map;
			_tiles = tiles;
			_gameObjects = new List<GameObject> ();
			Width = map.Width;
			Height = map.Height;
			Level.Camera.Level = this;
		}

		public static Level Generate(int width, int height)
		{
			DungeonGenerator.Instance.GenerateHauberkDungeon(width, height, 195000, 5, 5, 50, false, true);

			var tileData = DungeonGenerator._dungeon;
			var map = new Map (width, height);
			var tiles = new Tile[width, height];

			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					switch (tileData[x, y])
					{
						case TileType.Floor: 
							map.SetCellProperties (x, y, true, true); 
							break;
						case TileType.RoomFloor: 
							map.SetCellProperties(x, y, true, true); 
							break;
						case TileType.Wall: 
							map.SetCellProperties(x, y, false, false); 
							break;
						case TileType.Door: 
							map.SetCellProperties (x, y, false, false); 
							break;
					}
					tiles [x, y] = new Tile (tileData [x, y]);
				}
			}

			return new Level (map, tiles);
		}

		public Tile GetTile(Vector2 position)
		{
			var tile = _tiles [(int)position.X, (int)position.Y];
			tile.CellData = _map.GetCell ((int)position.X, (int)position.Y);
			return tile;
		}

		public Tile GetTile(int x, int y)
		{
			var tile = _tiles [x, y];
			tile.CellData = _map.GetCell (x, y);
			return tile;
		}

		public Tile SetTileProperties(Tile tile, bool isWalkable, bool isTransparent, bool isExplored)
		{
			_map.SetCellProperties (tile.CellData.X, tile.CellData.Y, isTransparent, isWalkable, isExplored);
			return GetTile (tile.CellData.X, tile.CellData.Y);
		}

		public void UpdatePlayerFov()
		{
			_map.ComputeFov ((int)Player.Position.X, (int)Player.Position.Y, 30, true);

			foreach ( var cell in _map.GetAllCells() )
			{
				if ( _map.IsInFov( cell.X, cell.Y ) )
				{
					_map.SetCellProperties( cell.X, cell.Y, cell.IsTransparent, cell.IsWalkable, true );
				}
			}
		}


		public Tile GetRandomEmptyTile()
		{
			var random = new Random();

			while( true )
			{
				int x = random.Next( _map.Width );
				int y = random.Next( _map.Height );

				var tile = GetTile (x, y);
				if (tile.CellData.IsWalkable)
					return tile;
			}
		}

		public Tile[,] GetAllTiles()
		{
			return _tiles;
		}

		public bool IsInFov(Vector2 position)
		{
			return _map.IsInFov ((int)position.X, (int)position.Y);
		}
	}
}

