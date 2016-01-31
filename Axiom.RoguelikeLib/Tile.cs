using System;
using RogueSharp;

namespace Axiom.RoguelikeLib
{
	public class Tile
	{
		public TileType TileType { get; set; }
		public Cell CellData { get; set; }

		public Tile (TileType type)
		{
			TileType = type;
		}
	}
}

