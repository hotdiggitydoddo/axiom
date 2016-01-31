using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RogueSharp;
using Microsoft.Xna.Framework;

namespace Axiom.RogueLikeTools
{
    public class Tile : Cell 
    {
        public Vector2 Position { get; set; }
        public TileType TileType { get; set; }
    }

	public class Dungeon : Map
    {
        public int Width { get; }
        public int Height { get; }

		private Tile[,] _tiles;

        private Dungeon(TileType[,] tiles, RogueSharp.Map map)
        {
            Width = map.Width;
            Height = map.Height;
			_tiles = new Tile[Width, Height];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _tiles[x,y] = new Tile
                    {
                        Position = new Vector2(x, y),
                        TileType = tiles[x, y],
                    };
					switch (tiles [x, y]) {
					case TileType.Floor:
						_tiles [x, y].IsTransparent = true;
						_tiles [x, y].IsWalkable = true;
						break;
					}
                }
            }
        }

        public static Dungeon Generate(int width, int height)
        {
            DungeonGenerator.Instance.GenerateHauberkDungeon(width, height, 195000, 5, 5, 50, false, true);
            var tiles = DungeonGenerator._dungeon;
            var map = new RogueSharp.Map(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    switch (tiles[x, y])
                    {
                        case TileType.Floor: map.SetCellProperties(x, y, true, true); break;
                        case TileType.RoomFloor: map.SetCellProperties(x, y, true, true); break;
                        case TileType.Wall: map.SetCellProperties(x, y, false, false); break;
                        case TileType.Door: map.SetCellProperties(x, y, false, false); break;
                    }
                }
            }
            return new Map(tiles, map);
        }

		public Tile[,] GetAllTiles()
		{
			return _tiles;
		}

		public bool IsInFov(Vector2 cellPosition)
		{
			return _map.IsInFov ((int)cellPosition.X, (int)cellPosition.Y);
		}

		public void UpdatePlayerFov()
		{
			_map.ComputeFov( (int)PlayerPosition.X, (int)PlayerPosition.Y, 30, true );
			foreach ( RogueSharp.Cell cell in _map.GetAllCells() )
			{
				if( _map.IsInFov( cell.X, cell.Y ) )
				{
					_map.SetCellProperties( cell.X, cell.Y, cell.IsTransparent, cell.IsWalkable, true );
					this._cells [cell.X, cell.Y].IsExplored = true;
				}
			}
		}

		public Cell GetCell(Vector2 position)
		{
			return _cells [(int)position.X, (int)position.Y];
		}

        public void Update()
        {
        }

		private Cell GetRandomEmptyCell()
		{
			var random = new Random();

			while( true )
			{
				int x = random.Next( _map.Width );
				int y = random.Next( _map.Height );
				if ( this._cells[x, y].IsWalkable )
				{
					return _cells[x, y];
				}
			}
		}
    }
}
