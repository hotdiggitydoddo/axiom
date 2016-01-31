using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
// needed for List<>

using System.Linq;
using System.Net.Mime;

//needed for the Select() in the lambda statement

using Microsoft.Xna.Framework;

namespace Axiom.RoguelikeLib
{
	public enum TileType{None, Floor, RoomFloor, Wall, Door, OpenDoor, ClosedDoor, DoorNorth, DoorEast, DoorSouth, DoorWest };

	public class DungeonGenerator {

		//some 2D dungeon Generators converted to C# by Marrt
		//Version 0.31, 2015-10-21
		//Usage:

		//0. Add the Extension.cs (needed for a specific Hashsets conversion) to your Project

		//1. Slap this script onto a GameObject and call one of those from anywhere:
		//- "DungeonGenerator.instance.GenerateHauberkDungeon(int width, int height)" //you can overwrite some or all parameters, but only in order
		//- "DungeonGenerator.instance.GeneratePerlinDungeon(int width, int height)"
		//- "DungeonGenerator.instance.GenerateCaveDungeon(int width, int height)"
		//
		//2. Be sure to have a canvas-GameObject in your Hierarchy for the map to be created and drawn
		//- "CreateMap()" is called in Awake, after that it is only Updated
		//- "UpdateMap(ref _dungeon)" is called on the end of above functions
		//- a button is added to the map that toggles region view mode for the hauberk dungeon
		//- use "DungeonGenerator.instance.ShowTransformOnMap(Transform actor, float gridX, float gridY)" to draw a red point onto the map
		//
		//3. Using the data
		//- The Tilegrid can be accessed by the "Tile[,] dungeon" variable AFTER one of the Generate functios have been called, Tile enum is defined globally at the Top of this script
		//- write your costum instantiate scripts to create the level, i do it like that:
		/*	
	 * 	private	Transform mapParent;
	 * 	public void GenerateByGrid(Tile[,] grid){
	 * 		
	 * 		int xSize = grid.GetLength(0);
	 * 		int ySize = grid.GetLength(1);
	 * 		Vector3 midOff = new Vector3(0F, -yOff/2F, 0F);	 * 				
	 * 		mapParent = new GameObject().transform;
	 * 		mapParent.name = "DynamicBlocks";
	 * 		
	 * 		for(int x = 0; x < xSize; x++){				
	 * 			for(int y = 0; y < ySize; y++){				
	 * 				switch(grid[x,y]){
	 * 					default:																					break;
	 * 					case Tile.Floor:		CreateBlock( new Vector3(xOff *x, yOff * 0F, zOff * y) +midOff);	break;
	 * 					case Tile.RoomFloor:	CreateBlock( new Vector3(xOff *x, yOff * 0F, zOff * y) +midOff);	break;
	 * 					case Tile.Wall:			CreateBlock( new Vector3(xOff *x, yOff * 0F, zOff * y) +midOff);
	 * 											CreateBlock( new Vector3(xOff *x, yOff * 1F, zOff * y) +midOff);	
	 * 																												break;					
	 * 				}			
	 * 			}				
	 * 		}
	 * 	}
	 * 	private	void CreateBlock(Vector3 pos){
	 * 		GameObject block		= Instantiate(dynBlockPrefab, pos, Quaternion.identity) as GameObject;
	 * 		block.transform.parent	= mapParent;
	 * 	}
	*/

		//

		//KNOWN ISSUES:
		//- style: some class variables have underscores - or not
		//- style: Why cram everything into a single script? I don't know, lack of sophisticated coding patterns from my side i guess, i am open to suggestions
		//- Cave: I carelessly replaced randoms with Random.Range, there may be some Range errors since Random.Range(int,int) will always always be lower that the second int
		//- Implementation: i just learned about the more elaborate Datatypes like HashSets and Dictionaries through the Hauberk conversion, errors may be present

		private static DungeonGenerator instance;

		public static DungeonGenerator Instance
		{
			get
			{
				if (instance == null)
					instance = new DungeonGenerator();
				return instance;

			}
		}

		public	static	TileType[,]	_dungeon;	//2D-Array that stores the current type of a tile
		public	static	int[,]	_regions;	//2D-Array
		public	int _dungeonWidth	= 101;
		public	int _dungeonHeight	= 101;

		public	bool _randomSeedOn = false;
		public	int  _seed;					//random seed for map generation

		public	bool addMapToggle = true;	//toggle mapview between _regions and _dungeon

		public void setTile(IntVector2 pos, TileType tileType)	{	_dungeon[pos.x,pos.y] = tileType;			}	//OCCASIONAL ERROR
		public TileType getTile(IntVector2 pos)				{	return _dungeon[pos.x,pos.y];			}

		private Random Random = new Random();


		public DungeonGenerator()
		{
			cardinal = new List<IntVector2>();
			cardinal.Add(new IntVector2(0, 1)); //North
			cardinal.Add(new IntVector2(1, 0)); //East
			cardinal.Add(new IntVector2(0, -1));    //South
			cardinal.Add(new IntVector2(-1, 0));	//West	
		}



		private	void GenerateLevel(){
			//My costum level generation function that places blocks that are skinned by a different algorithm
			//pass the grid to your own algorithm, this class it only creates grids and a map
			//LevelBuilder.instance.Generate3DBlockGrid(_dungeon);
		}

		//███████████████████████████████████████████████████████████████████████████

		#region RoomTemplates generation


		//Dungeon created by placeing room templates connected by doors, by Marrt (expect obvious inanity)

		private	List<RoomTemplate> roomTemplates;
		private	List<Room> placedRooms;

		[Range(1, 250)]
		public	int roomTries	= 250;	//amount of placement tries

		[Range(3, 30)]
		public	int minRoomSize	=  8;	//minimal size of room
		[Range(3, 30)]
		public	int maxRoomSize	= 18;	//maximum size of NON-Overlayed rooms

		[Range(1, 250)]
		public	int randomTemplateCount = 100;		//amount of templates to generate

		[Range(0, 100)]
		public	int wrinkledRoomProbability = 0;	//chance that rooms will get some wrinkling treatment

		[Range(0, 10)]
		public	int overlayCount = 3;

		[Range(0, 100)]
		public	int overlayChance = 50;

		public	bool doors2x2Allowed = false;		//search for potential doors with length 2 where possible

		private	int currentRoom;

		//template of a room
		public class RoomTemplate/*, System.IComparable<RoomTemplate>*/{

			public string name;

			public TileType[,] tilesType;	//Tilegrid of the room
			public int x;			//left line after placement
			public int y;			//bottom line after placement		
			public List<Door> potentialDoors;

			public int width{		//rect width
				get{
					return tilesType.GetLength(0);			
				}			
			}

			public int height{		//rect height
				get{
					return tilesType.GetLength(1);		
				}			
			}

			//size Compare
			/*int System.IComparable<Room>.CompareTo(Room other){
			if (other.x*other.y > this.x*this.y)			return -1;
			else if (other.x*other.y == this.x*this.y)	return 0;
			else 										return 1;
		}*/

			public RoomTemplate(){}

			public RoomTemplate(TileType[,] tilesType, int x, int y, List<Door> potentialDoors){			
				this.tilesType	= tilesType;
				this.x		= x;			
				this.y		= y;
				this.potentialDoors = potentialDoors;
			}
		}

		//Room instance that has been placed, compared to the template it has helper variables for its state and graph
		public class Room : RoomTemplate{

			//State		
			public bool locked;

			//connected Rooms, not used for Graph, Graph consists out of doors
			public List<Room> connected		= new List<Room>();
			public List<Door> doors			= new List<Door>();	//doors that have been created when room is connected

			public Room(){}

			public Room(TileType[,] tilesType, int x, int y, List<Door> potentialDoors){			
				this.tilesType	= tilesType;	
				this.x		= x;			
				this.y		= y;
				this.potentialDoors = potentialDoors;
			}
		}

		public class Door : System.IComparable<Door>{

			public	DoorDirection doorDir;	//direction of door
			public	int doorLength;			//length of door in Tiles
			public	int x;			//left line after placement
			public	int y;			//bottom line after placement

			public Room owner;

			//Graph helper for performing a search
			//public bool active		= false;	//can be passed, not used yet
			//public bool visited	= false;
			public int distance	= 10000;
			public Door prev;
			public List<Edge> connections	= new List<Edge>();

			//distance Compare, needed to get Min in search
			int System.IComparable<Door>.CompareTo(Door other){	//use with .Equals()!
				if (other.distance > this.distance)			return -1;
				else if (other.distance == this.distance)	return 0;
				else 										return 1;
			}

			public Door (DoorDirection dir, int length, int x, int y){
				this.doorDir = dir;
				this.doorLength = length;
				this.x = x;
				this.y = y;
			}

			public Door (DoorDirection dir, int length, int x, int y, Room owner){
				this.doorDir = dir;
				this.doorLength = length;
				this.x = x;
				this.y = y;
				this.owner = owner;
			}
		}

		//Edge, connection between two doors, length is one for doors that are placed to each other
		public	class Edge{
			public Door node2;
			public int length;
			public Edge(){}
			public Edge(Door d2, int l){this.node2 = d2; this.length = l;}
		}

		public enum DoorDirection {North, East, South, West};

		public	class RoomTemplateDungeonPath{
			public List<Door> doors	= new List<Door>();
			public List<Room> rooms	= new List<Room>();
			public int length = 0;
		}




		public	void GenerateRoomTemplateDungeon(	int?	width					= null,
			int?	height					= null){

			_dungeonWidth	= width		?? _dungeonWidth;
			_dungeonHeight	= height	?? _dungeonHeight;

			//if(_randomSeedOn){	Random.seed = this._seed;	}

			_dungeon = new TileType[_dungeonWidth, _dungeonHeight];
			_regions = new int [_dungeonWidth, _dungeonHeight];

			DungeonGenerator.StartTimeLog("generate templates:\t\t");		
			GenerateRoomTemplates();		
			DungeonGenerator.EndTimeLog();

			placedRooms = new List<Room>();
			currentRoom = 0;


			DungeonGenerator.StartTimeLog("room placement:\t\t\t");
			//Placement
			for(int i = 0; i < roomTries; i++){
				int roomNumber = Random.Next(0,roomTemplates.Count);
				TryToPlaceRoom( roomTemplates[roomNumber]);
			}
			DungeonGenerator.EndTimeLog();

			Console.WriteLine(placedRooms.Count+" rooms placed");

			//UpdateMap(ref _dungeon);

			DungeonGenerator.StartTimeLog("dungeon graph:\t\t\t");

			//GRAPH: create Edges between doors of the same room
			foreach(Room room in placedRooms){
				CreateDoorEdgesWithinRoom(room);	//cheap
			}

			//find most distant rooms(only marks on map for now), e.g. to place entrance and exit there fo least sidetracks to minimize backtracking
			FindMostDistantRooms();	//expensive

			DungeonGenerator.EndTimeLog();		
			DungeonGenerator.AddLog("\n");

			//ToggleMap();

		}

		public	bool TryToPlaceRoom(RoomTemplate roomTemplate){

			//CHANGE! start with placed room and try to add to it

			int xSizeDungeon	= _dungeon.GetLength(0);
			int ySizeDungeon	= _dungeon.GetLength(1);		

			int xSizeRoom		= roomTemplate.tilesType.GetLength(0);
			int ySizeRoom		= roomTemplate.tilesType.GetLength(1);

			int xMax = xSizeDungeon - xSizeRoom;
			int yMax = ySizeDungeon - ySizeRoom;

			bool collision = false;

			//try
			if(xMax < 0 || yMax < 0){
				Console.WriteLine("roomTemplate won't fit");
			}else{

				Room newRoom = null;

				int offX;
				int offY;

				//for first room placement is random, first room needs no collision tests
				if(placedRooms.Count == 0){

					//create new room from template, otherwise we would alter the template itself				
					newRoom = CreateRoomFromTemplate(roomTemplate);

					//random offset
					offX = Random.Next(0, xMax);
					offY = Random.Next(0, yMax);				

				}else{
					//choose a door from a placed room and try to place a room with a fitting door next to it
					Door jackDoor;									//a single door of a placed room
					List<Door> potentialFits = new List<Door>();	//fitting door of the new Room

					Room placedRoom = placedRooms[ Random.Next(0,placedRooms.Count)];	//random already placed room				
					if(	placedRoom.potentialDoors.Count == 0){	return false;	}		//has room remaining potential doors?

					jackDoor = placedRoom.potentialDoors[ Random.Next( 0, placedRoom.potentialDoors.Count ) ];				//random unused door as socket for a plug door

					//get needed direction and set vertical flag
					DoorDirection neededDir;	bool verticalDoor = true;
					if		( jackDoor.doorDir == DoorDirection.North )	{	neededDir = DoorDirection.South;							}
					else if	( jackDoor.doorDir == DoorDirection.East  )	{	neededDir = DoorDirection.West;		verticalDoor = false;	}
					else if	( jackDoor.doorDir == DoorDirection.South )	{	neededDir = DoorDirection.North;							}
					else												{	neededDir = DoorDirection.East;		verticalDoor = false;	}

					//parse all potential doors for fits
					foreach(Door door in roomTemplate.potentialDoors){	if(door.doorDir == neededDir && door.doorLength == jackDoor.doorLength){	potentialFits.Add(door);	}	}				
					if(	potentialFits.Count == 0			){	return false;	}		//no fit

					Door plugDoor = potentialFits[ Random.Next( 0, potentialFits.Count ) ];

					//Doors match, now get the needed room offset to place 1: "door in door" if door lenth = 1, or 2: "door on door" of 2				
					IntVector2 doorOffset = jackDoor.doorLength == 2?
						new IntVector2(	verticalDoor?0:( neededDir == DoorDirection.East?-1:+1 ),	verticalDoor?( neededDir == DoorDirection.North?-1:+1 ):0 ):
						new IntVector2(0,0);

					offX = placedRoom.x +jackDoor.x -plugDoor.x		+doorOffset.x;
					offY = placedRoom.y +jackDoor.y -plugDoor.y		+doorOffset.y;
					//			
					if(	offX < 0 || offX > xMax || offY < 0 || offY > yMax	){return false;}//room outside of dungeon area

					//test if room can fit here, if we start from >0 we essentially allow a overlap
					for(int x = 0; x < xSizeRoom; x++){
						for(int y = 0; y < ySizeRoom; y++){

							//Collision happens when we hit a floor Tile of another room with a Wall or Floor, potential doors are walls at the state, carve them later					
							collision = (_dungeon[x +offX,y +offY] == TileType.Floor) && roomTemplate.tilesType[x,y] != TileType.None;						
							//	collision = (_dungeon[x +offX,y +offY] == Tile.Floor || _dungeon[x +offX,y +offY] == Tile.RoomFloor) && roomTemplate.tiles[x,y] != Tile.None;		//Debug
							if(collision){break;}
						}
						if(collision){break;}
					}

					//doors are carved into the new room directly before placement
					if(!collision){	//create actual room

						newRoom		= CreateRoomFromTemplate(roomTemplate);					

						int plugDoorIndex = roomTemplate.potentialDoors.FindIndex(x => x==plugDoor);					
						plugDoor	= newRoom.potentialDoors[plugDoorIndex];	//refresh: reference the plugdoor in the room, not the roomTemplate

						newRoom.tilesType	[plugDoor.x, plugDoor.y] = TileType.Door;
						placedRoom.tilesType[jackDoor.x, jackDoor.y] = TileType.Door;

						_dungeon[placedRoom.x+jackDoor.x, placedRoom.y+jackDoor.y] = TileType.Door;	//retroactively carve door into dungeon, placedRoom has been carved before

						//2x2 Door
						if(jackDoor.doorLength > 1){
							newRoom.tilesType		[plugDoor.x	+(verticalDoor?1:0), plugDoor.y	+(verticalDoor?0:1)] = TileType.Door;
							placedRoom.tilesType[jackDoor.x	+(verticalDoor?1:0), jackDoor.y	+(verticalDoor?0:1)] = TileType.Door;
							_dungeon[placedRoom.x+jackDoor.x+(verticalDoor?1:0), placedRoom.y+jackDoor.y+(verticalDoor?0:1)] = TileType.Door;												
						}


						//GRAPH, make the rooms know about each other by referencing them in their adjacent lists
						placedRoom.connected.Add(newRoom);
						newRoom.connected.Add(placedRoom);

						//ADD doors to the doorLists
						placedRoom.doors.Add(jackDoor);
						newRoom.doors.Add(plugDoor);
						//REMOVE doors from potentialDoors					
						placedRoom.potentialDoors.Remove(jackDoor);	//door is no longer potential but actual
						newRoom.potentialDoors.Remove(plugDoor);

						//GRAPH, add the door transition to the door graph for weighted pathfinding, distance is 10 if doors placed onto each other, and 10 if next to each other (2x2 doors). 0 distance would break dijkstra!!!
						jackDoor.connections.Add(new Edge( plugDoor, jackDoor.doorLength *10));
						plugDoor.connections.Add(new Edge( jackDoor, jackDoor.doorLength *10));
						jackDoor.owner = placedRoom;
						plugDoor.owner = newRoom;
						//GRAPH, connections between doors within a room are made when placement is finished because we cannot know which potential doors will become actual now

					}				
				}			


				//write room into dungeon if it has not collided
				if(!collision){

					//carve room into _dungeon
					for(int x = 0; x < xSizeRoom; x++){
						for(int y = 0; y < ySizeRoom; y++){						
							if(newRoom.tilesType[x,y] != TileType.None){	//copy everything except Tile.None into dungeon							
								_dungeon[x +offX,y +offY] = newRoom.tilesType[x,y];							
								if(newRoom.tilesType[x,y] == TileType.Floor){	_regions[x +offX,y +offY] = currentRoom;	}	//set region too					
							}
						}
					}
					currentRoom++;

					newRoom.x = offX;
					newRoom.y = offY;

					placedRooms.Add(newRoom);				
					newRoom.name = "Room " +placedRooms.Count.ToString("00");				
				}

			}		

			return !collision;

		}

		private	Room CreateRoomFromTemplate(RoomTemplate roomTemplate){

			int xSizeRoom		= roomTemplate.tilesType.GetLength(0);
			int ySizeRoom		= roomTemplate.tilesType.GetLength(1);

			Room room;

			//copy array ( no reference to old array )
			TileType[,] roomTemplateTilesTypeCopy = new TileType[xSizeRoom, ySizeRoom];		
			System.Array.Copy( roomTemplate.tilesType, roomTemplateTilesTypeCopy, xSizeRoom *ySizeRoom );

			room = new Room(roomTemplateTilesTypeCopy, roomTemplate.x, roomTemplate.y, new List<Door>());		

			//copy potential doors, don't use structs because doors are used as nodes later on and must be passed by ref then		
			foreach(Door door in roomTemplate.potentialDoors){//Door door in roomTemplate.potentialDoors){
				//copy potentialDoor ( no reference to old door )		
				room.potentialDoors.Add( new Door( door.doorDir, door.doorLength, door.x, door.y, room));//copy the door object			
			}

			return room;					
		}




		private	void TryMoreDoors(){

		}

		private	void BuildDungeonGraph(){

		}

		//Graph stuff, find shortest Path between al room pairs and mark the longest
		private void FindMostDistantRooms(){

			Room room1 = null;
			Room room2 = null;

			RoomTemplateDungeonPath longestPath = new RoomTemplateDungeonPath();//inits with length 0

			//check each room pair once
			for(int i1 = 0; i1< placedRooms.Count; i1++){
				for(int i2 = i1+1; i2< placedRooms.Count; i2++){
					RoomTemplateDungeonPath path = FindPath(placedRooms[i1], placedRooms[i2]);
					if(path.length > longestPath.length){
						longestPath = path;
						room1 = placedRooms[i1];
						room2 = placedRooms[i2];
					}
				}
			}

			////color rooms
			//foreach(Room room in placedRooms){

			//	//Debug: different color for each room
			//	//ColorRoomOnMap(room, new Color(Random.Range(0F,1F),Random.Range(0F,1F),Random.Range(0F,1F),1F), 0.5F);continue;

			//	if		( room == room1){						//Entrance
			//		ColorRoomOnMap(room, Color.cyan, 0.3F);
			//	}else if( room == room2){						//Exit
			//		ColorRoomOnMap(room, Color.red, 0.3F);
			//	}else if( longestPath.rooms.Contains(room) ){	//Main Path
			//		ColorRoomOnMap(room, Color.blue, 0.3F);
			//	}else{											//Sidetrack
			//		ColorRoomOnMap(room, Color.black, 0.3F);
			//	}
			//}

			////print path		
			//Door prev = null;
			//foreach(Door door in longestPath.doors){
			//	if(prev != null){				
			//		PrintLineOnMap(prev.owner.x +prev.x, prev.owner.y +prev.y, door.owner.x +door.x, door.owner.y +door.y, Color.green, 0.25F);
			//	}
			//	prev = door;
			//}


			//mapTexture.Apply();

		}

		//create Edge to other doors in the same room
		private void CreateDoorEdgesWithinRoom(Room room){
			foreach(Door door in room.doors){
				foreach(Door door2 in room.doors){
					if(door != door2){//reference compare
						//using a 8 direction heuristic, diagonal counts 14, cardinal 10
						int deltaX = Math.Abs(door.x - door2.x);
						int deltaY = Math.Abs(door.y - door2.y);
						int dist = deltaX > deltaY? ((deltaX-deltaY)*10 + deltaY*14):((deltaY-deltaX)*10 + deltaX*14);//x greater? go x-y straight and y diagonal

						//more exact floating point calculation
						//int dist = (int)(Vector2.Distance(new Vector2((float)door.x,(float)door.y), new Vector2((float)door2.x,(float)door2.y))*10F);

						door.connections.Add(new Edge(door2, dist));//print (dist);					
						//PrintLineOnMap(door.owner.x +door.x, door.owner.y +door.y, door2.owner.x +door2.x, door2.owner.y +door2.y, Color.blue, 0.3F);
					}
				}
			}
		}

		//find path between two rooms, Nodes are Doors	
		public	RoomTemplateDungeonPath FindPath(Room startRoom, Room endRoom){

			//Dijkstra

			//Init helper variables		
			List<Door> unvisited = new List<Door>();

			foreach(Room room in placedRooms){
				foreach(Door door in room.doors){
					unvisited.Add(door);
					//door.visited = false;
					door.distance = 10000;//integer distance
					door.prev = null;
				}
			}

			//every room MUST have doors, except if only one is placed, so check to be sure
			if(startRoom.doors.Count == 0){return new RoomTemplateDungeonPath();}	//empty list

			//start at a random door from starting room, try to find any door of end room
			Door start	= startRoom.doors[0];

			//start.visited = true;
			start.distance = 0;

			//copy list

			Door current = start;
			int alt;// = current.distance;//0

			while(unvisited.Count > 0){			

				current = unvisited.Min();
				if(current.owner == endRoom){ break; }
				unvisited.Remove(current);

				foreach(Edge e in current.connections){		//if(e.node2 == current){print ("edge references itself"); break;}				
					alt = current.distance + e.length;				
					if(alt < e.node2.distance){	//if a shorter path to this noe is discovered, prev is current
						e.node2.distance = alt;
						e.node2.prev	 = current;
					}
				}
			}

			//build path
			RoomTemplateDungeonPath path = new RoomTemplateDungeonPath();
			path.length = current.distance;	//length is known already, its te distance of the last point

			while(current.prev != null){
				path.doors.Add(current);
				if(current.owner != current.prev.owner){path.rooms.Add(current.owner);}	//if room changes in next step, add it
				current = current.prev;

				if(path.doors.Count >1000){Console.WriteLine("looping error, do you have an edge with length 0?"); break;}
			}

			path.doors.Add(current);
			path.rooms.Add(current.owner);
			path.doors.Reverse();
			path.rooms.Reverse();

			return path;
		}

		//Random Template generation... :

		private	void GenerateRoomTemplates(){
			roomTemplates = new List<RoomTemplate>();


			for(int i = 0; i < randomTemplateCount; i++){
				//smallest room = 6 (4x4 for door)
				roomTemplates.Add(	GenerateRandomRoomTemplate()	);
				roomTemplates[roomTemplates.Count-1].name = "Template "+i.ToString("000");
			}
		}

		private	RoomTemplate GenerateRandomRoomTemplate(){

			int xSize = Random.Next(minRoomSize, maxRoomSize +1);
			int ySize = Random.Next(minRoomSize, maxRoomSize +1);

			RoomTemplate roomTemplate = new RoomTemplate();

			//create new room
			roomTemplate.tilesType = CreateUnborderedRectangleRoom(xSize, ySize);

			//chance to overlay a second room for more diversity (only in positive direction, since it doesn't matter which rect came first when we only have 2)
			for(int i = 0; i< overlayCount; i++){
				if(Random.Next(0,101)<overlayChance){	roomTemplate.tilesType = OverlayNewRoom( roomTemplate.tilesType );	}
			}


			//print (roomTemplate.height);

			//chance to ovelay offset / rotate a copy of itself
			if(Random.NextDouble()<0.5F){//50%
				MirrorTiles(roomTemplate.tilesType);
			}

			//chance to ovelay offset / rotate a copy of itself
			if(Random.NextDouble()<0.3F){//30%

			}


			//add Border		
			CreateBorders(roomTemplate.tilesType);

			if(Random.Next(0,101) < wrinkledRoomProbability){
				IngrowRoomBorderTiles( roomTemplate.tilesType );
			}

			FillRoomCavities( roomTemplate );	//if room is has seperated floor tiles, remove all but the biggest connected bunch

			CutRoom( roomTemplate );			//cut away all tiles that have not a single floor as neighbor (in 8 directions)

			MarkPotentialDoors( roomTemplate );	//Mark walls that would allow transitions into other rooms

			return roomTemplate;
		}

		private	TileType[,] CreateBorderedRectangleRoom(int xSize, int ySize){

			TileType[,]	tilesType = new TileType[xSize,ySize];

			//border
			for(int x = 0; x < xSize; x++){
				for(int y = 0; y < ySize; y++){
					if( x == 0 || y == 0 || x == xSize-1 || y == ySize-1){
						tilesType[x,y] = TileType.Wall;
					}
				}
			}

			//insides
			for(int x = 1; x < xSize-1; x++){
				for(int y = 1; y < ySize-1; y++){				
					tilesType[x,y] = TileType.Floor;				
				}
			}

			return tilesType;
		}

		private	TileType[,] CreateUnborderedRectangleRoom(int xSize, int ySize){		
			TileType[,]	tilesType = new TileType[xSize,ySize];						
			//insides
			for(int x = 0; x < xSize; x++){
				for(int y = 0; y < ySize; y++){			
					tilesType[x,y] = TileType.Floor;				
				}
			}		
			return tilesType;
		}

		private	void MirrorTiles( TileType[,] roomTilesType ){

			int xSize = roomTilesType.GetLength(0);
			int ySize = roomTilesType.GetLength(1);

			for(int x = 0; x < xSize; x++){
				if(x < xSize/2){
					for(int y = 0; y < ySize; y++){	//swap
						TileType temp = roomTilesType[x,y];
						int xM = xSize -x -1;	//opposite cell
						roomTilesType[x,y] = roomTilesType[xM,y];
						roomTilesType[xM,y] = temp;
					}
				}
			}
		}

		private	void TransposeTiles( TileType[,] roomTilesType ){

		}

		private	void RotateTiles( TileType[,] roomTilesType ){

		}

		private	void IngrowRoomBorderTiles( TileType[,] roomTilesType ){	//D array is passed by ref

			int xSize = roomTilesType.GetLength(0);
			int ySize = roomTilesType.GetLength(1);

			//chance to add wall if wall is adjacent already
			for(int x = 1; x < xSize-1; x++){
				for(int y = 1; y < ySize-1; y++){				
					if(roomTilesType[x,y] == TileType.Floor){
						int adj = adjacentWalls( roomTilesType, x, y);
						if(adj > 0 &&  (Random.Next(0, 45) / 100)+adj*0.1F > 0.35F){
							roomTilesType[x,y] = TileType.Wall;
						}
					}

				}
			}
		}

		private	TileType[,] OverlayNewRoom(TileType[,] oldTilesType){

			int xSize = oldTilesType.GetLength(0);
			int ySize = oldTilesType.GetLength(1);

			//random offset within old room, -1 because border on border would create unpassabel walls
			int offX = Random.Next(0, xSize -1);
			int offY = Random.Next(0, ySize -1);

			//new room size
			int xSizeOverlay = Random.Next(minRoomSize, maxRoomSize +1);
			int ySizeOverlay = Random.Next(minRoomSize, maxRoomSize +1);

			//ovelay room tiles
			TileType[,] overlay = CreateUnborderedRectangleRoom(xSizeOverlay, ySizeOverlay);

			//new Array containing both rooms, it may happen that new room is completely within old, so use Max
			int newXSize = Math.Max(offX +xSizeOverlay, xSize);
			int newYSize = Math.Max(offY +ySizeOverlay, ySize);
			TileType[,] newTilesType = new TileType[newXSize, newYSize];

			//copy both into it
			for(int x = 0; x < newXSize; x++){
				for(int y = 0; y < newYSize; y++){

					//copy old room
					if(x < xSize && y < ySize){
						newTilesType[x,y] = oldTilesType[x,y];
					}

					//overlay new
					if(x >= offX && y >= offY){									// if on or past the origin of overlay
						if(x < offX +xSizeOverlay && y < offY +ySizeOverlay){	// if room is completely contained we have to check if the overlay array isnt out of bounds
							TileType oldTileType	= newTilesType[x,y];
							TileType overTileType	= overlay[x -offX, y -offY];

							if			( oldTileType == TileType.None ){				//if new is overlaying None: replace
								newTilesType[x,y] = overTileType;
							}else if	( oldTileType == TileType.Floor || overTileType == TileType.Floor){	//floor always wins, overrwriting old walls too if walls already exist on starting room
								newTilesType[x,y] = TileType.Floor;
							}
						}
					}

				}
			}
			// print (oldTiles.GetLength(0)+"|"+offX+"|"+xSize+"|"+xSizeOverlay+"|"+newXSize);
			return newTilesType;
		}

		//don't give border coordinates
		private	int adjacentWalls( TileType[,] tilesType, int x, int y){
			int adj = 0;
			if( tilesType[x-1,y+0] == TileType.Wall ){adj++;}
			if( tilesType[x+0,y+1] == TileType.Wall ){adj++;}
			if( tilesType[x+1,y+0] == TileType.Wall ){adj++;}
			if( tilesType[x+0,y-1] == TileType.Wall ){adj++;}
			return adj;
		}

		private	void FillRoomCavities( RoomTemplate roomTemplate ){

			//i wasn't able to get  4-way Connected-component labeling running so i just use this recursive method...

			int xSize = roomTemplate.tilesType.GetLength(0);
			int ySize = roomTemplate.tilesType.GetLength(1);

			int[,] roomLabels = new int[xSize,ySize];	//defaults to 0

			int regionLabel = 0;
			int largestRegionLabel = -1;
			int largestSize = -1;


			for(int x = 1; x < xSize-1; x++){
				for(int y = 1; y < ySize-1; y++){
					if(roomLabels[x,y] == 0 && roomTemplate.tilesType[x,y] == TileType.Floor){
						regionLabel++;
						int size = FillNeighbors(ref roomTemplate.tilesType, regionLabel, x, y, ref roomLabels, 0);
						if(size > largestSize){
							largestSize = size;
							largestRegionLabel = regionLabel;
						}
					}
				}
			}

			if(regionLabel == 1){return;}//only one region, no need to fill

			for(int x = 1; x < xSize-1; x++){
				for(int y = 1; y < ySize-1; y++){
					if(	roomLabels[x,y] != largestRegionLabel && roomLabels[x,y]!= 0 ){	roomTemplate.tilesType[x,y]= TileType.Wall;	}
				}
			}		
		}

		private int FillNeighbors(ref TileType[,] tilesType, int label, int x, int y, ref int[,] roomLabels, int depth){

			//visited
			roomLabels[x,y] = label;
			int hits = 1;
			depth++; if(depth == 3000){Console.WriteLine("depth error"); return 10000;}
			//edge is wall, so no need to check for out of bounds
			if(roomLabels[x+0, y+1] == 0 && tilesType[x+0, y+1] == TileType.Floor){	hits += FillNeighbors(ref tilesType, label, x+0, y+1, ref roomLabels, depth);	}
			if(roomLabels[x+1, y+0] == 0 && tilesType[x+1, y+0] == TileType.Floor){	hits += FillNeighbors(ref tilesType, label, x+1, y+0, ref roomLabels, depth);	}
			if(roomLabels[x+0, y-1] == 0 && tilesType[x+0, y-1] == TileType.Floor){	hits += FillNeighbors(ref tilesType, label, x+0, y-1, ref roomLabels, depth);	}
			if(roomLabels[x-1, y+0] == 0 && tilesType[x-1, y+0] == TileType.Floor){	hits += FillNeighbors(ref tilesType, label, x-1, y+0, ref roomLabels, depth);	}
			return hits;
		}

		//Cut away adges of a room by testing tiles in all directions if wall is obsolete
		private	void CutRoom( RoomTemplate roomTemplate ){

			//return;

			int xSize = roomTemplate.tilesType.GetLength(0);
			int ySize = roomTemplate.tilesType.GetLength(1);

			for(int x = 0; x < xSize; x++){
				for(int y = 0; y < ySize; y++){	

					bool obsolete = true;

					if( roomTemplate.tilesType[x,y] == TileType.Wall ){

						bool ignoreN = y == ySize-1;
						bool ignoreE = x == xSize-1;
						bool ignoreS = y == 0;
						bool ignoreW = x == 0;

						//if one adjacent block is empty, the block isn't obsolete
						obsolete	= obsolete&& ( ignoreN||				( roomTemplate.tilesType[x+0,y+1] == TileType.Wall || roomTemplate.tilesType[x+0,y+1] == TileType.None) );	//N
						obsolete	= obsolete&& ( ignoreN||	ignoreE||	( roomTemplate.tilesType[x+1,y+1] == TileType.Wall || roomTemplate.tilesType[x+1,y+1] == TileType.None) );	//NE
						obsolete	= obsolete&& ( ignoreE||				( roomTemplate.tilesType[x+1,y+0] == TileType.Wall || roomTemplate.tilesType[x+1,y+0] == TileType.None) );	//E
						obsolete	= obsolete&& ( ignoreS||	ignoreE||	( roomTemplate.tilesType[x+1,y-1] == TileType.Wall || roomTemplate.tilesType[x+1,y-1] == TileType.None) );	//SE
						obsolete	= obsolete&& ( ignoreS||				( roomTemplate.tilesType[x+0,y-1] == TileType.Wall || roomTemplate.tilesType[x+0,y-1] == TileType.None) );	//S
						obsolete	= obsolete&& ( ignoreS||	ignoreW||	( roomTemplate.tilesType[x-1,y-1] == TileType.Wall || roomTemplate.tilesType[x-1,y-1] == TileType.None) );	//SW
						obsolete	= obsolete&& ( ignoreW||				( roomTemplate.tilesType[x-1,y+0] == TileType.Wall || roomTemplate.tilesType[x-1,y+0] == TileType.None) );	//W
						obsolete	= obsolete&& ( ignoreN||	ignoreW||	( roomTemplate.tilesType[x-1,y+1] == TileType.Wall || roomTemplate.tilesType[x-1,y+1] == TileType.None) );	//NW

						if(obsolete){roomTemplate.tilesType[x,y] = TileType.None;}

					}
				}
			}		

			//Remove eventual cavities we have created by only allowing the biggest cavity to exist

		}

		//Convert FloorTiles to make a Border(Wall) on plain Floor-rooms, if a Floor Tile has Tile.None AND Tile.Floor as neighbor (8 dir) it is converted to a border
		private	void CreateBorders( TileType[,] tilesType ){

			int xSize = tilesType.GetLength(0);
			int ySize = tilesType.GetLength(1);		

			for(int x = 0; x < xSize; x++){
				for(int y = 0; y < ySize; y++){	

					bool noneAdj	= false;	//at least 1 None adjacent
					bool flooAdj	= false;	//at least 1 Floor adjacent		

					if( tilesType[x,y] == TileType.Floor ){

						bool ignoreN = y == ySize-1;
						bool ignoreE = x == xSize-1;
						bool ignoreS = y == 0;
						bool ignoreW = x == 0;

						//if one adjacent block is None, condition is met
						noneAdj	= noneAdj|| ( ignoreN||				( tilesType[x+0,y+1] == TileType.None) );	//N
						noneAdj	= noneAdj|| ( ignoreN||	ignoreE||	( tilesType[x+1,y+1] == TileType.None) );	//NE
						noneAdj	= noneAdj|| ( ignoreE||				( tilesType[x+1,y+0] == TileType.None) );	//E
						noneAdj	= noneAdj|| ( ignoreS||	ignoreE||	( tilesType[x+1,y-1] == TileType.None) );	//SE
						noneAdj	= noneAdj|| ( ignoreS||				( tilesType[x+0,y-1] == TileType.None) );	//S
						noneAdj	= noneAdj|| ( ignoreS||	ignoreW||	( tilesType[x-1,y-1] == TileType.None) );	//SW
						noneAdj	= noneAdj|| ( ignoreW||				( tilesType[x-1,y+0] == TileType.None) );	//W
						noneAdj	= noneAdj|| ( ignoreN||	ignoreW||	( tilesType[x-1,y+1] == TileType.None) );	//NW

						//if one adjacent block is Floor, condition is met
						flooAdj	= flooAdj|| ( !ignoreN&&				( tilesType[x+0,y+1] == TileType.Floor));	//N
						flooAdj	= flooAdj|| ( !ignoreN&&	!ignoreE&&	( tilesType[x+1,y+1] == TileType.Floor));	//NE
						flooAdj	= flooAdj|| ( !ignoreE&&				( tilesType[x+1,y+0] == TileType.Floor));	//E
						flooAdj	= flooAdj|| ( !ignoreS&&	!ignoreE&&	( tilesType[x+1,y-1] == TileType.Floor));	//SE
						flooAdj	= flooAdj|| ( !ignoreS&&				( tilesType[x+0,y-1] == TileType.Floor));	//S
						flooAdj	= flooAdj|| ( !ignoreS&&	!ignoreW&&	( tilesType[x-1,y-1] == TileType.Floor));	//SW
						flooAdj	= flooAdj|| ( !ignoreW&&				( tilesType[x-1,y+0] == TileType.Floor));	//W
						flooAdj	= flooAdj|| ( !ignoreN&&	!ignoreW&&	( tilesType[x-1,y+1] == TileType.Floor));	//NW

						if(noneAdj && flooAdj){tilesType[x,y] = TileType.Wall;}

					}
				}
			}		
		}

		private	void MarkPotentialDoors(RoomTemplate roomTemplate ){
			//for doorlength 1 and 2, we need at least 3 and 4 adjacent tiles in line with free entrance tiles (Floor or roomend)
			//room consists out of Tiles.Floor/Wall/None as of now

			roomTemplate.potentialDoors = new List<Door>();

			int xSize = roomTemplate.tilesType.GetLength(0);
			int ySize = roomTemplate.tilesType.GetLength(1);

			TileType[,] roomTemplateTilesTypeCopy = new TileType[xSize,ySize];		
			System.Array.Copy( roomTemplate.tilesType, roomTemplateTilesTypeCopy, xSize*ySize );

			//scan for doors in horizontal(EW) and vertical(NS) direction
			for(int x = 0; x < xSize; x++){
				for(int y = 0; y < ySize; y++){
					if( roomTemplate.tilesType[x,y] == TileType.Wall ){
						//on the edges og the room tiles are considered open
						bool noneN = y == ySize-1;
						bool noneS = y == 0;
						bool noneE = x == xSize-1;
						bool noneW = x == 0;

						//if one adjacent block is empty, the block isn't obsolete
						TileType nTileType	= noneN ?	TileType.None:	roomTemplate.tilesType[x+0,y+1];	//N
						TileType sTileType	= noneS ?	TileType.None:	roomTemplate.tilesType[x+0,y-1];	//S
						TileType eTileType	= noneE ?	TileType.None:	roomTemplate.tilesType[x+1,y+0];	//E
						TileType wTileType	= noneW ?	TileType.None:	roomTemplate.tilesType[x-1,y+0];	//W

						//Doordirections, N-door means leaving room in N direction

						if		( nTileType == TileType.None  && sTileType == TileType.Floor && eTileType == TileType.Wall  && wTileType == TileType.Wall	){	roomTemplateTilesTypeCopy[x,y] = TileType.DoorNorth;	}	//door North
						else if	( nTileType == TileType.Floor && sTileType == TileType.None  && eTileType == TileType.Wall  && wTileType == TileType.Wall	){	roomTemplateTilesTypeCopy[x,y] = TileType.DoorSouth;	}	//door South
						else if	( nTileType == TileType.Wall  && sTileType == TileType.Wall  && eTileType == TileType.None  && wTileType == TileType.Floor	){	roomTemplateTilesTypeCopy[x,y] = TileType.DoorEast;		}	//door East
						else if	( nTileType == TileType.Wall  && sTileType == TileType.Wall  && eTileType == TileType.Floor && wTileType == TileType.None	){	roomTemplateTilesTypeCopy[x,y] = TileType.DoorWest;		}	//door West
					}
				}
			}

			//DEBUG: write copy back into Room to see how tiles got assigned
			//System.Array.Copy( roomTilesCopy, room.tiles, xSize*ySize );

			//Add Doors to room		
			for(int x = 0; x < xSize; x++){
				for(int y = 0; y < ySize; y++){

					if(roomTemplateTilesTypeCopy[x,y] != TileType.None && roomTemplateTilesTypeCopy[x,y] != TileType.Wall){	//is door?

						bool noneN = y == ySize-1;
						bool noneS = y == 0;
						bool noneE = x == xSize-1;
						bool noneW = x == 0;

						//if one adjacent block is empty, the block isn't obsolete
						TileType nTileType	= noneN ?	TileType.None:	roomTemplateTilesTypeCopy[x+0,y+1];	//N
						TileType sTileType	= noneS ?	TileType.None:	roomTemplateTilesTypeCopy[x+0,y-1];	//S
						TileType eTileType	= noneE ?	TileType.None:	roomTemplateTilesTypeCopy[x+1,y+0];	//E
						TileType wTileType	= noneW ?	TileType.None:	roomTemplateTilesTypeCopy[x-1,y+0];	//W

						//Doordirections, N-door means leaving room in N direction

						if			( roomTemplateTilesTypeCopy[x,y] == TileType.DoorNorth	|| roomTemplateTilesTypeCopy[x,y] == TileType.DoorSouth) {	//door North

							DoorDirection dir = roomTemplateTilesTypeCopy[x,y] == TileType.DoorNorth? DoorDirection.North: DoorDirection.South;


							//check if we have a door of length 2 by checking if right tile is same door type
							if( doors2x2Allowed && eTileType == roomTemplateTilesTypeCopy[x,y]){
								roomTemplate.potentialDoors.Add(new Door(	dir, 2, x,y	));
								//roomTemplateTilesCopy[x+0,y+1]	= Tile.Wall;	//remove DoorMark
							}else{
								roomTemplate.potentialDoors.Add(new Door(	dir, 1, x,y	));
							}
							roomTemplateTilesTypeCopy[x,y]		= TileType.Wall;	//remove DoorMark


						}else if	( roomTemplateTilesTypeCopy[x,y] == TileType.DoorEast	|| roomTemplateTilesTypeCopy[x,y] == TileType.DoorWest) {	//door South

							DoorDirection dir = roomTemplateTilesTypeCopy[x,y] == TileType.DoorEast? DoorDirection.East: DoorDirection.West;

							//check if we have a door of length 2 by checking if top tile is same door type
							if( doors2x2Allowed && nTileType == roomTemplateTilesTypeCopy[x,y]){
								roomTemplate.potentialDoors.Add(new Door(	dir, 2, x,y	));
								//roomTemplateTilesCopy[x+0,y+1]	= Tile.Wall;	//remove DoorMark
							}else{
								roomTemplate.potentialDoors.Add(new Door(	dir, 1, x,y	));
							}
							roomTemplateTilesTypeCopy[x,y]		= TileType.Wall;	//remove DoorMark								
						}						
					}

				}
			}

		}

		#endregion


		#region HAUBERK (Rooms + Mazes)
		//Gathered from http://journal.stuffwithstuff.com/2014/12/21/rooms-and-mazes/
		//converted to C# for use in Unity by Marrt

		/// The random dungeon generator.
		///
		/// Starting with a stage of solid walls, it works like so:
		///
		/// 1. Place a number of randomly sized and positioned rooms. If a room
		///	overlaps an existing room, it is discarded. Any remaining rooms are
		///	carved out.
		/// 2. Any remaining solid areas are filled in with mazes. The maze generator
		///	will grow and fill in even odd-shaped areas, but will not touch any
		///	rooms.
		/// 3. The result of the previous two steps is a series of unconnected rooms
		///	and mazes. We walk the stage and find every tile that can be a
		///	"connector". This is a solid tile that is adjacent to two unconnected
		///	regions.
		/// 4. We randomly choose connectors and open them or place a door there until
		///	all of the unconnected regions have been joined. There is also a slight
		///	chance to carve a connector between two already-joined regions, so that
		///	the dungeon isn't single connected.
		/// 5. The mazes will have a lot of dead ends. Finally, we remove those by
		///	repeatedly filling in any open tile that's closed on three sides. When
		///	this is done, every corridor in a maze actually leads somewhere.
		///
		/// The end result of this is a multiply-connected dungeon with rooms and lots
		/// of winding corridors.

		//Hauberk variables
		//private	Rectangle bounds					= new Rectangle(0,0,0,0);
		public	int numRoomTries			= 300;					//Room placement tries
		public	int extraConnectorChance	= 5;					//The inverse chance of adding a connector between two regions that have already been joined for more interconnection between regions
		public	int roomExtraSize			= 4;					//Increasing this allows rooms to be larger.
		public	int windingPercent			= 20;					//chance a maze will make a turn which will make it more winding
		public	bool tryRoomsFirst			= false;				//try to make room-to-room connections before making corridor-to-room connections (corridor-to-corridor are impossible)
		public	bool streamLine				= true;				//streamline corridors between branchpoints and doors
		public	List<Rectangle> _rooms;									//list of placed rooms
		private int _currentRegion = -1;							// The index of the current region (=connected carved area) being carved, -1 = default, wall

		public List<IntVector2> cardinal;	//original implementation of Hauberk used Direction.CARDINAL	

		/// <summary>Generate a room and maze dungeon, http://journal.stuffwithstuff.com/2014/12/21/rooms-and-mazes/ </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="numRoomTries">Room placement tries</param>
		/// <param name="extraConnectorChance">The inverse chance of adding a connector between two regions that have already been joined for more interconnection between regions</param>
		/// <param name="roomExtraSize">Increasing this allows rooms to be larger</param>
		/// <param name="windingPercent">chance a maze will make a turn which will make it more winding</param>
		/// <param name="tryRoomsFirst">chance a maze will make a turn which will make it more winding</param>
		/// <param name="seed">use random seed for this generation, ignores _randomSeedOn</param>
		/// 
		public void GenerateHauberkDungeon(	int?	width					= null,
			int?	height					= null,
			int?	numRoomTries			= null,
			int?	extraConnectorChance	= null,
			int?	roomExtraSize			= null,
			int?	windingPercent			= null,
			bool?	tryRoomsFirst			= null,
			bool?	streamLine				= null,
			int?	seed					= null){

			_dungeonWidth	= width		?? _dungeonWidth;
			_dungeonHeight	= height	?? _dungeonHeight;

			//check size
			if (_dungeonWidth % 2 == 0 || _dungeonHeight % 2 == 0) {
				Console.WriteLine("The stage must be odd-sized");
				return;
			}

			//optional parameters to overwrite public ones set in editor
			this.numRoomTries			= numRoomTries			?? this.numRoomTries;
			this.extraConnectorChance	= extraConnectorChance	?? this.extraConnectorChance;
			this.roomExtraSize			= roomExtraSize			?? this.roomExtraSize;
			this.windingPercent			= windingPercent		?? this.windingPercent;
			this.tryRoomsFirst			= tryRoomsFirst			?? this.tryRoomsFirst;
			this.streamLine				= streamLine			?? this.streamLine;
			this._seed					= seed					?? this._seed;

			if(_randomSeedOn || seed != null){	//use random seed if overlaod was used, or if it is enabled in general
				//Random.seed = this._seed;
			}					
			//init grids
			_dungeon		= new TileType[_dungeonWidth, _dungeonHeight];
			_regions		= new int[_dungeonWidth, _dungeonHeight];
			_currentRegion	= -1;	//reset


			//print("size:"+_dungeonHeight+"|"+_dungeonWidth+"\n");


			for (var x = 0; x < _dungeonWidth; x++) {			
				for (var y = 0; y < _dungeonHeight; y++) {
					setTile(new IntVector2(x, y), TileType.Wall);
					_regions[x,y] = _currentRegion;	//-1
				}
			}

			_addRooms();	//randomly place rooms

			// Fill in all of the empty space with mazes.
			for (var x = 1; x < _dungeonWidth; x += 2) {
				for (var y = 1; y < _dungeonHeight; y += 2) {		
					var pos = new IntVector2(x, y);
					if (getTile(pos) != TileType.Wall) continue;	//ignore already carved spaces
					_growMaze(pos);
				}
			}	

			_connectRegions();
			_removeDeadEnds();


			//function Marrt added to streamline corridors
			if(this.streamLine){
				_streamLineCorridors();
			}

			//MultiplyDungeon(2);	//mapPxPerTile = 2F;

			//UpdateMap(ref _dungeon);
			//UpdateMap(ref _regions);

		}


		/// Implementation of the "growing tree" algorithm from here:
		/// http://www.astrolog.org/labyrnth/algrithm.htm.
		private	void _growMaze(IntVector2 start) {
			List<IntVector2> cells = new List<IntVector2>();
			IntVector2 lastDir = null;


			_startRegion();
			_carve(start);

			cells.Add(start);

			while (cells.Count > 0) {

				IntVector2 cell = cells[cells.Count-1];	//last element in list

				// See which adjacent cells are open.
				List<IntVector2> unmadeCells = new List<IntVector2>();

				foreach(IntVector2 dir in cardinal) {
					if (_canCarve(cell, dir)) unmadeCells.Add(dir);
				}

				if(unmadeCells.Count > 0) {
					// Based on how "windy" passages are, try to prefer carving in the
					// same direction.
					IntVector2 dir;
					if (unmadeCells.Contains(lastDir) && Random.Next(0,100) < windingPercent) {
						dir = lastDir;	//keep previous direction
					} else {
						dir = unmadeCells[Random.Next(0,unmadeCells.Count)];	//pick new direction out of possible ones
					}

					_carve(cell + dir );	//carve out wall between the valid cells
					_carve(cell + dir * 2);	//carve out valid cell

					cells.Add(cell + dir * 2);
					lastDir = dir;
				} else {
					// No adjacent uncarved cells.
					cells.RemoveAt(cells.Count-1);	//Remove Last element

					// This path has ended.
					lastDir = null;
				}
			}
		}

		/// Places rooms ignoring the existing maze corridors.
		private	void _addRooms() {

			_rooms = new List<Rectangle>();

			for (var i = 0; i < numRoomTries; i++) {
				// Pick a random room size. The funny math here does two things:
				// - It makes sure rooms are odd-sized to line up with maze.
				// - It avoids creating rooms that are too rectangular: too tall and
				//   narrow or too wide and flat.
				// TODO: This isn't very flexible or tunable. Do something better here.

				int size			= Random.Next(1, 3 + roomExtraSize)	*2 + 1;	//rng.range(1, 3 + roomExtraSize) * 2 + 1;
				int rectangularity	= Random.Next(0, 1 + size/2)			*2;		//rng.range(0, 1 + size ~/ 2) * 2;
				int width			= size;
				int height			= size;

				//print (size +"|"+ rectangularity +"|"+ width +"|"+ height);

				if (Random.Next(0, 2) == 0) {	//50% chance
					width += rectangularity;
				} else {
					height += rectangularity;
				}
				//print (size +"|"+ rectangularity +"|"+ width +"|"+ height);

				int x	= (int)(GetRandomNumber(1F, (_dungeonWidth		- width)  *0.5F	))* 2 + 1;	//rng.range((bounds.width - width) ~/ 2) * 2 + 1;
				int y	= (int)(GetRandomNumber(1F, (_dungeonHeight	- height) *0.5F	))* 2 + 1;	//rng.range((bounds.height - height) ~/ 2) * 2 + 1;

				Rectangle room = new Rectangle(x, y, width, height);

				bool overlaps = false;
				foreach(var other in _rooms) {
					if (room.Intersects(other)) {
						overlaps = true;
						break;	//break this foreach
					}
				}

				if (overlaps) continue;	//don't add room and retry

				//print (room);

				if(x+width > _dungeonWidth || y+height > _dungeonWidth){
					Console.WriteLine("error, Room won't fit, dungeon to small or faulthy room generation");
					return;
				}

				//add non-overlapping room
				_rooms.Add(room);

				_startRegion();


				/*foreach(IntVector2 pos in new Rectangle(x, y, width, height)) {
				_carve(pos);
			}*/

				for(int ix = x; ix < x+width; ix++){
					for(int iy = y; iy < y+height; iy++){
						_carve(new IntVector2(ix,iy), TileType.RoomFloor);
					}	
				}
			}
		}


		//Marrt: was the hardest function for me to convert, there maybe errors
		private	void _connectRegions() {
			// Find all of the tiles that can connect two (or more) regions.
			Dictionary<IntVector2, HashSet<int>> connectorRegions = new Dictionary<IntVector2, HashSet<int>>();	//var connectorRegions = <Vec, Set<int>>{};

			//check each wall if it sits between 2 different regions and assign a Hashset to them
			//foreach (IntVector2 pos in bounds.inflate(-1)) {
			for(int ix = 1; ix < _dungeonWidth-1; ix++){
				for(int iy = 1; iy < _dungeonHeight-1; iy++){	

					IntVector2 pos = new IntVector2(ix,iy);

					// Can't already be part of a region.
					if (getTile(pos) != TileType.Wall) continue;

					HashSet<int> regions = new HashSet<int>();

					foreach (IntVector2 dir in cardinal) {
						IntVector2 indexer = (pos + dir);
						var region = _regions[indexer.x, indexer.y];
						//if (region != null) regions.Add(region);
						if (region != -1) regions.Add(region);
					}						

					if (regions.Count < 2) continue;

					connectorRegions[pos] = regions;	//add Hashset to current position
				}
			}

			List<IntVector2> connectors = connectorRegions.Keys.ToList();//var connectors = connectorRegions.keys.toList();

			//Marrt: I think it would make for nicer dungeons if all room-to-room connections would be tried first, therefore sort List
			if(tryRoomsFirst){

				//bring connectors that have two rooms attached, to front		
				connectors.OrderBy(delegate(IntVector2 con) {			
					int connectedRooms = 0;
					foreach (IntVector2 dir in cardinal) {
						IntVector2 indexer = (con + dir);
						if (_dungeon[indexer.x, indexer.y] == TileType.RoomFloor) connectedRooms++;
					}				
					return 2-connectedRooms;
				});

			}

			// Keep track of which regions have been merged. This maps an original
			// region index to the one it has been merged to.
			Dictionary<int, int> merged = new Dictionary<int, int>();
			HashSet<int> openRegions = new HashSet<int>();
			for (int i = 0; i <= _currentRegion; i++) {
				merged[i] = i;
				openRegions.Add(i);
			}

			// Keep connecting regions until we're down to one.
			while (openRegions.Count > 1) {

				//print (openRegions.Count+"|"+connectors.Count);
				IntVector2 connector;
				if(tryRoomsFirst){
					connector = connectors[0];//room-to-room are ordered first in list
				}else{
					connector = connectors[Random.Next( 0, connectors.Count )];//rng.item(connectors);
				}

				// Carve the connection.
				_addJunction(connector);

				// Merge the connected regions. We'll pick one region (arbitrarily) and
				// map all of the other regions to its index.
				//var regions = connectorRegions[connector].map((region) => merged[region]);
				var regions = connectorRegions[connector].Select((region) => merged[region]);

				int dest = regions.First();
				var sources = regions.Skip(1).ToList();

				// Merge all of the affected regions. We have to look at *all* of the
				// regions because other regions may have previously been merged with
				// some of the ones we're merging now.
				for (var i = 0; i <= _currentRegion; i++) {
					if (sources.Contains(merged[i])) {
						merged[i] = dest;
					}
				}

				// The sources are no longer in use.
				//openRegions.removeAll(sources);
				openRegions.RemoveWhere( (source) => sources.Contains(source));

				// Remove any connectors that aren't needed anymore.
				connectors.RemoveAll(delegate( IntVector2 pos) {//	connectors.removeWhere((pos) {

					// If the connector no long spans different regions, we don't need it.
					//var regionss = connectorRegions[pos].map((region) => merged[region]).toSet();
					var regionss = connectorRegions[pos].Select((region) => merged[region]).ToHashSet();	//Extension Method to hashset

					if (regionss.Count > 1) return false;

					// This connecter isn't needed, but connect it occasionally so that the dungeon isn't singly-connected.
					if (Random.Next(0,100) < extraConnectorChance ){

						// Don't allow connectors right next to each other.				
						foreach (IntVector2 dir in cardinal) {
							IntVector2 indexer = (pos + dir);
							if (_dungeon[indexer.x, indexer.y] == TileType.Door) return true;
						}

						//if no connectors are adjacent, add additional connector
						_addJunction(pos);
					}
					return true;
				});
			}
		}

		private	void _addJunction(IntVector2 pos) {

			//open / closedness of a door can be determined in a later manipulations, so i removed it
			/*if (Random.Range(0,4)==0) {	//25%chance
			setTile(pos, Random.Range(0,3)==0 ? Tile.OpenDoor : Tile.Floor);
		} else {
			setTile(pos, Tile.ClosedDoor);
		}*/

			setTile(pos, TileType.Door);
		}


		private	void _removeDeadEnds() {
			bool done = false;

			while (!done) {
				done = true;

				//foreach (IntVector2 pos in bounds.inflate(-1)) {
				for(int ix = 1; ix < _dungeonWidth-1; ix++){
					for(int iy = 1; iy < _dungeonHeight-1; iy++){

						IntVector2 pos = new IntVector2(ix,iy);

						if (getTile(pos) == TileType.Wall) continue;

						// If it only has one exit, it's a dead end.
						var exits = 0;
						foreach(IntVector2 dir in cardinal) {
							if (getTile(pos + dir) != TileType.Wall) exits++;
						}

						if (exits != 1) continue;

						done = false;
						setTile(pos, TileType.Wall);
						_regions[pos.x, pos.y] = -1;
					}
				}
			}
		}

		private	void _streamLineCorridors(){
			/*Added by Marrt taken from this user comment on the source page:
			Peeling • 7 months ago
			As regards the disagreeable windiness between rooms: looking at the output you could get rid of most of it thus:
			Trace each linear corridor section (terminated by branches or rooms)
			Once you have the start and end of a section, retrace your steps. If you find a point where you could dig through one block to make a shortcut to an earlier part of the section, do so, and fill in the unwanted part. Continue until you reach the start of the section.
			Repeat for all linear corridor sections.
		*/				

			//STEP 1: gather all Tiles.Floor, these are all corridor Tiles
			List<IntVector2> corridors = new List<IntVector2>();
			List<List<IntVector2>> traces = new List<List<IntVector2>>();		

			for(int ix = 1; ix < _dungeonWidth-1; ix++){
				for(int iy = 1; iy < _dungeonHeight-1; iy++){
					if(_dungeon[ix,iy] == TileType.Floor){
						corridors.Add(new IntVector2(ix,iy));
					}
				}
			}		

			//STEP 2: gather corridor traces, these are all line segments that are between doors or branching points which themselves are fixed now		
			//extract Line Segments seperated by branching points or doorsteps		

			int failsave = 1000;
			while (corridors.Count > 0 && failsave > 0) {	if(failsave == 1){Console.WriteLine("Marrt didn't expect this to happen");} failsave--;

				// See which adjacent cells are open.
				List<IntVector2> segment = new List<IntVector2>();			
				IntVector2 current = corridors[0];								//arbitrary start		
				buildLineSegment(current, ref corridors, ref segment, 0, true);	//recursive search

				if(segment.Count>4){	//lineSegment has to have at least 5 parts to potentially contain a shortcut
					traces.Add(segment);				
					//debug	//	int g = Random.Range(100,300);	foreach(IntVector2 pos in segment){	_regions[pos.x, pos.y] = g;	}
				}						
			}

			//STEP 3: backtrace traces and check for shortcuts within short range (1 wall in between), then carve a shortcut and uncarve the trace up to that point		
			foreach(List<IntVector2> trace in traces){

				List<IntVector2> finalTrace = new List<IntVector2>();
				int skipIndex = 0;	//shortcut skips iterations			

				for(int i = 0; i< trace.Count; i++){

					if(i < skipIndex){	continue;	}

					finalTrace.Add(trace[i]);	//add current position to final path

					foreach(IntVector2 dir in cardinal) {					
						if(getTile( trace[i] +dir ) == TileType.Wall){			//if we see a wall in test direction

							IntVector2 shortcut = trace[i] +dir +dir;						
							if( trace.Contains( shortcut ) && !finalTrace.Contains(shortcut) ){	//and behind that wall an already visited pos of this trace that has not been removed

								//get index of shortcut so we know how and if to skip
								skipIndex = trace.FindIndex( delegate ( IntVector2 x ) { return x == shortcut;	});//implicit predicate							
								if(i > skipIndex){continue;}	//detected an already obsolete path, we cannot make a shortcut to it
								finalTrace.Add(trace[i]+dir);	//new shortcut connection is added to final sum
								//print ("shortcut"+i+"->"+skipIndex);
							}
						}
					}				
				}

				//uncarve old trace
				foreach(IntVector2 pos in trace){
					setTile(pos, TileType.Wall);				
					_regions[pos.x, pos.y] = -1;
				}

				//recarve trace
				foreach(IntVector2 pos in finalTrace){
					_carve(pos);
					_regions[pos.x, pos.y] = 100;
				}
			}						
		}

		//recursive line builder
		private	int buildLineSegment(IntVector2 current, ref List<IntVector2> source, ref List<IntVector2> target, int currentDepth, bool addAtEnd){

			if(currentDepth>1000){return currentDepth+1;}//failsave

			//check if we are a doorstep or branch, these must not be moved or else
			int exits = 0;
			foreach(IntVector2 dir in cardinal) {
				if (getTile(current + dir) != TileType.Wall){	//if there is anything other than a wall we have an exit or else, doorsteps will have at least 2 non walls (door + path)
					exits++;
				}
			}
			if(exits > 2){			
				source.Remove(current);	//never look at this tile again
				return currentDepth;
			}else{
				if(addAtEnd){
					target.Insert(0,current);	//at least part of a valid lineSegment
				}else{
					target.Add(current);
				}
			}			

			//find adjacent fields, there are only up to 2 directions possible on any lineSegment point, we can only ever find one valid after the first		
			foreach(IntVector2 dir in cardinal) {					
				if(	source.Contains(current + dir) && !target.Contains(current + dir) ){
					//depth first
					currentDepth = buildLineSegment(current + dir, ref source, ref target, currentDepth, addAtEnd);
					//only first call can run twice because it may start in the middle of a segment
					addAtEnd = false;//we want an ordered list, so initial depthsearch will be added at start, the following at the end
				}
			}

			source.Remove(current);		
			return currentDepth+1;
		}


		///<summary>multiply hauberk dungeon tiles to make corridors broader, added by marrt</summary>
		private void MultiplyDungeon(int factor){

			int xSize = _dungeon.GetLength(0);
			int ySize = _dungeon.GetLength(1);

			TileType[,] newDungeon	= new TileType[ xSize *factor, ySize *factor ];
			int[,]  newRegions	= new int [ xSize *factor, ySize *factor ];


			for(int x = 0; x < xSize; x++){
				for(int y = 0; y < ySize; y++){
					int nx = 0;
					int ny = 0;
					for(int i = 0; i < factor*factor; i++){
						newDungeon[x*factor +nx,y*factor +ny] = _dungeon[x,y];
						newRegions[x*factor +nx,y*factor +ny] = _regions[x,y];
						if(nx == factor-1){ny++;}
						nx = (nx+1)%factor;

					}
				}
			}
			_dungeon = newDungeon;
			_regions = newRegions;		
		}


		/// Gets whether or not an opening can be carved from the given starting
		/// [Cell] at [pos] to the adjacent Cell facing [direction]. Returns `true`
		/// if the starting Cell is in bounds and the destination Cell is filled
		/// (or out of bounds).</returns>
		private	bool _canCarve(IntVector2 pos, IntVector2 direction) {
			// Must end in bounds.

			IntVector2 iv2 = pos + direction * 3;
			Vector2 v2 = new Vector2(iv2.x, iv2.y);
			Rectangle bounds	= new Rectangle(0,0,_dungeonWidth, _dungeonHeight);

			if (!bounds.Contains(new Point((int)v2.X, (int)v2.Y))) return false;

			// Destination must not be open.
			return getTile(pos + direction * 2) == TileType.Wall;
		}

		private	void _startRegion() {
			_currentRegion++;
		}

		private	void _carve(IntVector2 pos, TileType? type = null) {

			setTile(pos, type ?? TileType.Floor);	// if non is stated, default is floor

			//print (pos.x +","+ pos.y);
			_regions[pos.x, pos.y] = _currentRegion;
		}
		#endregion


		#region Perlin Noise


		public int		offsetX		= 23;
		public int		offsetY		= 23;		
		public float	scale		= 0.1F;
		public float	threshold	= 0.5F;

		/// <summary>Generate a perlin noise dungeon, warning: no connectivity guaranteed </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="offsetX">x offset on the perlin sample plane</param>
		/// <param name="offsetY">y offset on the perlin sample plane</param>
		/// <param name="scale">bigger values make finer structures</param>
		/// <param name="threshold">0F-1F, threshold on when to draw a wall instead of a floor</param>
		/// 	
		public void GeneratePerlinDungeon(	int?	width		= null,
			int?	height		= null,
			int?	offsetX		= null,
			int?	offsetY		= null,
			float?	scale		= null,
			float?	threshold	= null){

			//print ("Perlin Dungeon");
			var perlinNoise = new PerlinNoise(5);
			_dungeonWidth	= width		?? _dungeonWidth;
			_dungeonHeight	= height	?? _dungeonHeight;

			this.offsetX	= offsetX	?? this.offsetX;
			this.offsetY	= offsetY	?? this.offsetY;
			this.scale		= scale		?? this.scale;
			this.threshold	= threshold	?? this.threshold;

			//init
			_dungeon		= new TileType[_dungeonWidth, _dungeonHeight];
			_regions		= new int[_dungeonWidth, _dungeonHeight];

			for(int x = 0; x < _dungeonWidth; x++){
				for(int y = 0; y < _dungeonHeight; y++){				

					float noise = (float)perlinNoise.Noise((x+this.offsetX) *this.scale, (y+this.offsetY) *this.scale, 1f ) ;	
					if( noise < this.threshold ){
						_dungeon[x,y] = TileType.Floor;				
					}else{
						_dungeon[x,y] = TileType.Wall;	
					}								
				}			
			}


			//	UpdateMap(ref _dungeon);
		}

		#endregion


		#region Cave
		//https://github.com/AndyStobirski/RogueLike/blob/master/csCaveGenerator.cs

		/// <summary>Generate a cave dungeon, http://www.evilscience.co.uk/a-c-algorithm-to-build-roguelike-cave-systems-part-1/ </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="Neighbours">The number of closed neighbours a cell must have in order to invert it's state</param>
		/// <param name="CloseCellProb">The probability of closing a visited cell, 55 tends to produce 1 cave, 40 few and small caves</param>
		/// <param name="Iterations">The number of times to visit cells</param>
		/// <param name="LowerLimit">Remove rooms smaller than this value</param>
		/// <param name="UpperLimit">Remove rooms larger than this value</param>
		/// <param name="EmptyNeighbours">Removes single cells from cave edges: a cell with this number of empty neighbours is removed</param>
		/// <param name="EmptyCellNeighbours">Fills in holes within caves: an open cell with this number closed neighbours is filled</param>
		/// <param name="seed">use random seed for this generation, ignores _randomSeedOn</param>

		public void GenerateCaveDungeon(int?	width				= null,
			int?	height				= null,
			int?	Neighbours			= null,
			int?	CloseCellProb		= null,
			int?	Iterations			= null,		
			int?	LowerLimit			= null,
			int?	UpperLimit			= null,
			int?	EmptyNeighbours		= null,
			int?	EmptyCellNeighbours	= null,
			int?	seed				= null){

			//print ("CreatingCave");		

			_dungeonWidth	= width		?? _dungeonWidth;
			_dungeonHeight	= height	?? _dungeonHeight;

			this.Neighbours				= Neighbours			?? this.Neighbours;
			this.CloseCellProb			= CloseCellProb			?? this.CloseCellProb;
			this.Iterations				= Iterations			?? this.Iterations;
			this.LowerLimit				= LowerLimit			?? this.LowerLimit;
			this.UpperLimit				= UpperLimit			?? this.UpperLimit;
			this.EmptyNeighbours		= EmptyNeighbours		?? this.EmptyNeighbours;
			this.EmptyNeighbours		= EmptyNeighbours		?? this.EmptyNeighbours;
			this.EmptyCellNeighbours	= EmptyCellNeighbours	?? this.EmptyCellNeighbours;

			this._seed					= seed					?? this._seed;

			if(_randomSeedOn || seed != null){	//use random seed if overlaod was used, or if it is enabled in general
				//Random.seed = this._seed;
			}	

			this.MapSize = new IntVector2(_dungeonWidth, _dungeonHeight);		

			CaveBuild();
			ConnectCaves();

			_dungeon	= new TileType[_dungeonWidth, _dungeonHeight];
			_regions	= new int[_dungeonWidth, _dungeonHeight];	

			for(int x = 0; x < _dungeonWidth; x++){				
				for(int y = 0; y < _dungeonHeight; y++){				
					_dungeon[x,y] = Map[x,y] == 1? TileType.Floor:TileType.Wall;
					_regions[x,y] = Map[x,y] == 1? 0:-1;
				}
			}					
			//UpdateMap(ref _dungeon);
		}



		/// <summary>
		/// csCaveGenerator - generate a cave system and connect the caves together.
		/// 
		/// For more info on it's use see http://www.evilscience.co.uk/?p=624
		/// </summary>
		/// 
		//Marrt: i removed the class distinction to make those parameters visibe in the Editor
		//class csCaveGenerator{

		#region properties
		public int Neighbours			=     4;	//The number of closed neighbours a cell must have in order to invert it's state
		public int CloseCellProb		=    45;	//The probability of closing a visited cell, 55 tends to produce 1 cave, 40 few and small caves
		public int Iterations			= 50000;	//The number of times to visit cells
		public IntVector2 MapSize;					//The size of the map		

		public int LowerLimit			=    16;	//Remove rooms smaller than this value
		public int UpperLimit			=   500;	//Remove rooms larger than this value
		public int EmptyNeighbours		=     3;	//Removes single cells from cave edges: a cell with this number of empty neighbours is removed
		public int EmptyCellNeighbours	=     4;	//Fills in holes within caves: an open cell with this number closed neighbours is filled

		public int Corridor_Min			=     2;	//Minimum corridor length
		public int Corridor_Max			=     5;	//Maximum corridor length
		public int Corridor_MaxTurns	=    10;	//Maximum turns
		public int CorridorSpace		=     2;	//The distance a corridor has to be away from a closed cell for it to be built
		public int BreakOut				= 100000;	//When this value is exceeded, stop attempting to connect caves. Prevents the algorithm from getting stuck.

		private int CaveNumber { get { return Caves == null ? 0 : Caves.Count; } }//Number of caves generated

		#endregion

		#region map structures

		private List<List<IntVector2>> Caves;	// Caves within the map are stored here
		private List<IntVector2> Corridors;		// Corridors within the map stored here
		private int[,] Map;						// Contains the map

		#endregion

		#region lookups

		/// <summary>Generic list of points which contain 4 directions </summary>
		List<IntVector2> Directions = new List<IntVector2>(){
			new IntVector2 (0,-1)		//north
			, new IntVector2 (0,1)		//south
			, new IntVector2 (1,0)		//east
			, new IntVector2 (-1,0)		//west
		};

		List<IntVector2> Directions1 = new List<IntVector2>(){
			new IntVector2 (0,-1)		//north
			, new IntVector2 (0,1)		//south
			, new IntVector2 (1,0)		//east
			, new IntVector2 (-1,0)		//west
			, new IntVector2 (1,-1)		//northeast
			, new IntVector2 (-1,-1)	//northwest
			, new IntVector2 (-1,1)		//southwest
			, new IntVector2 (1,1)		//southeast
			, new IntVector2 (0,0)		//centre
		};

		#endregion
		#region misc

		/// <summary>
		/// Constructor
		/// </summary>
		/*public csCaveGenerator(){
			
			rnd = Random(12345);
			Neighbours = 4;
			Iterations = 50000;
			CloseCellProb = 45;

			LowerLimit = 16;
			UpperLimit = 500;

			MapSize = new IntVector2(40, 20);

			EmptyNeighbours = 3;
			EmptyCellNeighbours = 4;

			CorridorSpace = 2;
			Corridor_MaxTurns = 10;
			Corridor_Min = 2;
			Corridor_Max = 5;

			BreakOut = 100000;
		}*/

		public int CaveBuild(){
			BuildCaves();
			GetCaves();
			return Caves.Count();
		}

		#endregion


		#region cave related

		#region make caves

		/// <summary>
		/// Calling this method will build caves, smooth them off and fill in any holes
		/// </summary>
		private void BuildCaves(){

			Map = new int[MapSize.x, MapSize.y];


			//go through each map cell and randomly determine whether to close it
			//the +5 offsets are to leave an empty border round the edge of the map
			for (int x = 0; x < MapSize.x; x++)
				for (int y = 0; y < MapSize.y; y++)
					if (Random.Next(0,100) < CloseCellProb)
						Map[x, y] = 1;

			IntVector2 cell;

			//Pick cells at random
			for (int x = 0; x <= Iterations; x++)
			{
				cell = new IntVector2(Random.Next(0, MapSize.x), Random.Next(0, MapSize.y));

				//if the randomly selected cell has more closed neighbours than the property Neighbours
				//set it closed, else open it
				if (Neighbours_Get1(cell).Where(n => IntVector2_Get(n) == 1).Count() > Neighbours)
					IntVector2_Set(cell, 1);
				else
					IntVector2_Set(cell, 0);
			}



			//
			//  Smooth of the rough cave edges and any single blocks by making several 
			//  passes on the map and removing any cells with 3 or more empty neighbours
			//
			for (int ctr = 0; ctr < 5; ctr++)
			{
				//examine each cell individually
				for (int x = 0; x < MapSize.x; x++)
					for (int y = 0; y < MapSize.y; y++)
					{
						cell = new IntVector2(x, y);

						if (
							IntVector2_Get(cell) > 0
							&& Neighbours_Get(cell).Where(n => IntVector2_Get(n) == 0).Count() >= EmptyNeighbours
						)
							IntVector2_Set(cell, 0);
					}
			}

			//
			//  fill in any empty cells that have 4 full neighbours
			//  to get rid of any holes in an cave
			//
			for (int x = 0; x < MapSize.x; x++)
				for (int y = 0; y < MapSize.y; y++)
				{
					cell = new IntVector2(x, y);

					if (
						IntVector2_Get(cell) == 0
						&& Neighbours_Get(cell).Where(n => IntVector2_Get(n) == 1).Count() >= EmptyCellNeighbours
					)
						IntVector2_Set(cell, 1);
				}
		}

		#endregion

		#region locate caves
		/// <summary>
		/// Locate the edge of the specified cave
		/// </summary>
		/// <param name="pCaveNumber">Cave to examine</param>
		/// <param name="pCavePoint">Point on the edge of the cave</param>
		/// <param name="pDirection">Direction to start formting the tunnel</param>
		/// <returns>Boolean indicating if an edge was found</returns>
		private void Cave_GetEdge(List<IntVector2> pCave, ref IntVector2 pCavePoint, ref IntVector2 pDirection)
		{
			do
			{

				//random point in cave
				pCavePoint = pCave.ToList()[Random.Next(0, pCave.Count())];

				pDirection = Direction_Get(pDirection);

				do
				{
					pCavePoint += pDirection;

					if (!IntVector2_Check(pCavePoint))
						break;
					else if (IntVector2_Get(pCavePoint) == 0)
						return;

				} while (true);



			} while (true);
		}

		/// <summary>Locate all the caves within the map and place each one into the generic list Caves</summary>
		private void GetCaves()
		{
			Caves = new List<List<IntVector2>>();

			List<IntVector2> Cave;
			IntVector2 cell;

			//examine each cell in the map...
			for (int x = 0; x < MapSize.x; x++)
				for (int y = 0; y < MapSize.y; y++)
				{
					cell = new IntVector2(x, y);
					//if the cell is closed, and that cell doesn't occur in the list of caves..
					if (IntVector2_Get(cell) > 0 && Caves.Count(s => s.Contains(cell)) == 0)
					{
						Cave = new List<IntVector2>();

						//launch the recursive
						LocateCave(cell, Cave);

						//check that cave falls with the specified property range size...
						if (Cave.Count() <= LowerLimit | Cave.Count() > UpperLimit)
						{
							//it does, so bin it
							foreach (IntVector2 p in Cave)
								IntVector2_Set(p, 0);
						}
						else
							Caves.Add(Cave);
					}
				}

		}

		/// <summary>
		/// Recursive method to locate the cells comprising a cave, 
		/// based on flood fill algorithm
		/// </summary>
		/// <param name="cell">Cell being examined</param>
		/// <param name="current">List containing all the cells in the cave</param>
		private void LocateCave(IntVector2 pCell, List<IntVector2> pCave)
		{
			foreach (IntVector2 p in Neighbours_Get(pCell).Where(n => IntVector2_Get(n) > 0))
			{
				if (!pCave.Contains(p))
				{
					pCave.Add(p);
					LocateCave(p, pCave);
				}
			}
		}

		#endregion

		#region connect caves

		/// <summary>
		/// Attempt to connect the caves together
		/// </summary>
		public bool ConnectCaves(){


			if (Caves.Count() == 0)
				return false;



			List<IntVector2> currentcave;
			List<List<IntVector2>> ConnectedCaves = new List<List<IntVector2>>();
			IntVector2 cor_point = new IntVector2(0,0);
			IntVector2 cor_direction = new IntVector2(0,0);
			List<IntVector2> potentialcorridor = new List<IntVector2>();
			int breakoutctr = 0;

			Corridors = new List<IntVector2>(); //corridors built stored here

			//get started by randomly selecting a cave..
			currentcave = Caves[Random.Next(0, Caves.Count())];
			ConnectedCaves.Add(currentcave);
			Caves.Remove(currentcave);



			//starting builder
			do
			{

				//no corridors are present, sp build off a cave
				if (Corridors.Count() == 0)
				{
					currentcave = ConnectedCaves[Random.Next(0, ConnectedCaves.Count())];
					Cave_GetEdge(currentcave, ref cor_point, ref cor_direction);
				}
				else
					//corridors are presnt, so randomly chose whether a get a start
					//point from a corridor or cave
					if (Random.Next(0, 100) > 49)
					{
						currentcave = ConnectedCaves[Random.Next(0, ConnectedCaves.Count())];
						Cave_GetEdge(currentcave, ref cor_point, ref cor_direction);
					}
					else
					{
						currentcave = null;
						Corridor_GetEdge(ref cor_point, ref cor_direction);
					}



				//using the points we've determined above attempt to build a corridor off it
				potentialcorridor = Corridor_Attempt(cor_point
					, cor_direction
					, true);


				//if not null, a solid object has been hit
				if (potentialcorridor != null)
				{

					//examine all the caves
					for (int ctr = 0; ctr < Caves.Count(); ctr++)
					{

						//check if the last point in the corridor list is in a cave
						if (Caves[ctr].Contains(potentialcorridor.Last()))
						{
							if (
								currentcave == null //we've built of a corridor
								| currentcave != Caves[ctr] //or built of a room
							)
							{
								//the last corridor point intrudes on the room, so remove it
								potentialcorridor.Remove(potentialcorridor.Last());
								//add the corridor to the corridor collection
								Corridors.AddRange(potentialcorridor);
								//write it to the map
								foreach (IntVector2 p in potentialcorridor)
									IntVector2_Set(p, 1);


								//the room reached is added to the connected list...
								ConnectedCaves.Add(Caves[ctr]);
								//...and removed from the Caves list
								Caves.RemoveAt(ctr);

								break;

							}
						}
					}
				}

				//breakout
				if (breakoutctr++ > BreakOut)
					return false;

			} while (Caves.Count() > 0);

			Caves.AddRange(ConnectedCaves);
			ConnectedCaves.Clear();
			return true;
		}

		#endregion

		#endregion

		#region corridor related

		/// <summary>
		/// Randomly get a point on an existing corridor
		/// </summary>
		/// <param name="Location">Out: location of point</param>
		/// <returns>Bool indicating success</returns>
		private void Corridor_GetEdge(ref IntVector2 pLocation, ref IntVector2 pDirection)
		{
			List<IntVector2> validdirections = new List<IntVector2>();

			do
			{
				//the modifiers below prevent the first of last point being chosen
				pLocation = Corridors[Random.Next(0, Corridors.Count - 1)];

				//attempt to locate all the empy map points around the location
				//using the directions to offset the randomly chosen point
				foreach (IntVector2 p in Directions)
					if (IntVector2_Check(new IntVector2(pLocation.x + p.x, pLocation.y + p.y)))
					if (IntVector2_Get(new IntVector2(pLocation.x + p.x, pLocation.y + p.y)) == 0)
						validdirections.Add(p);


			} while (validdirections.Count == 0);

			pDirection = validdirections[Random.Next(0, validdirections.Count)];
			pLocation += pDirection;

		}

		/// <summary>
		/// Attempt to build a corridor
		/// </summary>
		/// <param name="pStart"></param>
		/// <param name="pDirection"></param>
		/// <param name="pPreventBackTracking"></param>
		/// <returns></returns>
		private List<IntVector2> Corridor_Attempt(IntVector2 pStart, IntVector2 pDirection, bool pPreventBackTracking)
		{

			List<IntVector2> lPotentialCorridor = new List<IntVector2>();
			lPotentialCorridor.Add(pStart);

			int corridorlength;
			IntVector2 startdirection = new IntVector2(pDirection.x, pDirection.y);

			int pTurns = Corridor_MaxTurns;

			while (pTurns >= 0)
			{
				pTurns--;

				corridorlength = Random.Next(Corridor_Min, Corridor_Max);
				//build corridor
				while (corridorlength > 0)
				{
					corridorlength--;

					//make a point and offset it
					pStart += pDirection;

					if (IntVector2_Check(pStart) && IntVector2_Get(pStart) == 1)
					{
						lPotentialCorridor.Add(pStart);
						return lPotentialCorridor;
					}

					if (!IntVector2_Check(pStart))
						return null;
					else if (!Corridor_IntVector2Test(pStart, pDirection))
						return null;

					lPotentialCorridor.Add(pStart);

				}

				if (pTurns > 1)
				if (!pPreventBackTracking)
					pDirection = Direction_Get(pDirection);
				else
					pDirection = Direction_Get(pDirection, startdirection);
			}

			return null;
		}

		private bool Corridor_IntVector2Test(IntVector2 pPoint, IntVector2 pDirection)
		{

			//using the property corridor space, check that number of cells on
			//either side of the point are empty
			foreach (int r in Enumerable.Range(-CorridorSpace, 2 * CorridorSpace + 1).ToList())
			{
				if (pDirection.x == 0)//north or south
				{
					if (IntVector2_Check(new IntVector2(pPoint.x + r, pPoint.y)))
					if (IntVector2_Get(new IntVector2(pPoint.x + r, pPoint.y)) != 0)
						return false;
				}
				else if (pDirection.y == 0)//east west
				{
					if (IntVector2_Check(new IntVector2(pPoint.x, pPoint.y + r)))
					if (IntVector2_Get(new IntVector2(pPoint.x, pPoint.y + r)) != 0)
						return false;
				}

			}

			return true;
		}

		#endregion

		#region direction related

		/// <summary>
		/// Return a list of the valid neighbouring cells of the provided point
		/// using only north, south, east and west
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		private List<IntVector2> Neighbours_Get(IntVector2 p)
		{
			return Directions.Select(d => new IntVector2(p.x + d.x, p.y + d.y))
				.Where(d => IntVector2_Check(d)).ToList();
		}

		/// <summary>
		/// Return a list of the valid neighbouring cells of the provided point
		/// using north, south, east, ne,nw,se,sw
		private List<IntVector2> Neighbours_Get1(IntVector2 p)
		{
			return Directions1.Select(d => new IntVector2(p.x + d.x, p.y + d.y))
				.Where(d => IntVector2_Check(d)).ToList();
		}

		/// <summary>
		/// Get a random direction, provided it isn't equal to the opposite one provided
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		private IntVector2 Direction_Get(IntVector2 p)
		{
			IntVector2 newdir;
			do
			{
				newdir = Directions[Random.Next(0, Directions.Count())];

			} while (newdir.x != -p.x & newdir.y != -p.y);

			return newdir;
		}

		/// <summary>
		/// Get a random direction, excluding the provided directions and the opposite of 
		/// the provided direction to prevent a corridor going back on it's self.
		/// 
		/// The parameter pDirExclude is the first direction chosen for a corridor, and
		/// to prevent it from being used will prevent a corridor from going back on 
		/// it'self
		/// </summary>
		/// <param name="dir">Current direction</param>
		/// <param name="pDirectionList">Direction to exclude</param>
		/// <param name="pDirExclude">Direction to exclude</param>
		/// <returns></returns>
		private IntVector2 Direction_Get(IntVector2 pDir, IntVector2 pDirExclude)
		{
			IntVector2 NewDir;
			do
			{
				NewDir = Directions[Random.Next(0, Directions.Count())];
			} while (
				Direction_Reverse(NewDir) == pDir
				| Direction_Reverse(NewDir) == pDirExclude
			);


			return NewDir;
		}

		private IntVector2 Direction_Reverse(IntVector2 pDir)
		{
			return new IntVector2(-pDir.x, -pDir.y);
		}

		#endregion

		#region cell related

		/// <summary>
		/// Check if the provided point is valid
		/// </summary>
		/// <param name="p">Point to check</param>
		/// <returns></returns>
		private bool IntVector2_Check(IntVector2 p)
		{
			return p.x >= 0 & p.x < MapSize.x & p.y >= 0 & p.y < MapSize.y;
		}

		/// <summary>
		/// Set the map cell to the specified value
		/// </summary>
		/// <param name="p"></param>
		/// <param name="val"></param>
		private void IntVector2_Set(IntVector2 p, int val)
		{
			Map[p.x, p.y] = val;
		}

		/// <summary>
		/// Get the value of the provided point
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		private int IntVector2_Get(IntVector2 p)
		{
			return Map[p.x, p.y];
		}

		#endregion
		//}	

		#endregion

		#region Performance

		public	static string	levelGenLog = "";
		public	static float	lastTime	= 0F;

		//Time Logging
		public	static void ResetLog(){
			levelGenLog = "";
		}

		public	static void StartTimeLog(string message){
			//	lastTime = Time.realtimeSinceStartup;
			levelGenLog += message;
		}

		public	static void EndTimeLog(){
			//levelGenLog += (Time.realtimeSinceStartup-lastTime).ToString("F4")+"\n";
		}

		public	static void AddLog(string message){
			levelGenLog += message+"\n";
		}

		public	static string GetLog(){
			return levelGenLog;
		}
		#endregion

		#region MAP

		public void ShowMap()
		{
			DungeonGenerator.instance.GenerateHauberkDungeon();
			var tiles = DungeonGenerator._dungeon;


		}





		//needs a canvas in the hierarchy, otherwise it wont work


		//update map by region grid
		//private void UpdateMap(ref int[,] grid){

		//	if(mapTexture == null){return;}

		//	int xSize = grid.GetLength(0);
		//	int ySize = grid.GetLength(1);

		//	mapTexture.Resize(xSize, ySize, TextureFormat.RGBA32, false );
		//	mapTexture.Apply();

		//	//generate random Colors
		//	Dictionary<int, Color> colors = new Dictionary<int, Color>();
		//	for(int x = 0; x < xSize; x++){		
		//		for(int y = 0; y < ySize; y++){
		//			if(!colors.ContainsKey(grid[x,y])){
		//				colors.Add(grid[x,y], new Color(Random.Range(0F,1F),Random.Range(0F,1F),Random.Range(0F,1F)) );
		//			}
		//		}
		//	}

		//	for(int x = 0; x < xSize; x++){		
		//		for(int y = 0; y < ySize; y++){
		//			mapTexture.SetPixel(x, y, grid[x,y] == -1? Color.black:colors[grid[x,y]]);

		//			//special Debug case, -100
		//			if(grid[x,y] == -100)mapTexture.SetPixel(x, y, Color.white);
		//		}
		//	}

		//	mapTexture.Apply();
		//	RefreshMap(new Vector2(xSize, ySize));		
		//}

		//update map by Tilegrid
		//public void UpdateMap(ref Tile[,] grid){
		//	int xSize = grid.GetLength(0);
		//	int ySize = grid.GetLength(1);

		//	mapTexture.Resize(xSize, ySize, TextureFormat.RGBA32, false );
		//	mapTexture.Apply();

		//	for(int x = 0; x < xSize; x++){		
		//		for(int y = 0; y < ySize; y++){
		//			switch(grid[x,y]){
		//				default:				mapTexture.SetPixel(x, y, Color.clear);		break;
		//				case Tile.Floor:		mapTexture.SetPixel(x, y, Color.grey);		break;
		//				case Tile.RoomFloor:	mapTexture.SetPixel(x, y, Color.white);		break;
		//				case Tile.Wall:			mapTexture.SetPixel(x, y, Color.black);		break;
		//				case Tile.Door:			mapTexture.SetPixel(x, y, Color.magenta);	break;

		//				//DEBUG for room Template Dungeon
		//				case Tile.DoorNorth:	mapTexture.SetPixel(x, y, Color.yellow);	break;
		//				case Tile.DoorEast:		mapTexture.SetPixel(x, y, Color.blue);		break;
		//				case Tile.DoorSouth:	mapTexture.SetPixel(x, y, Color.green);		break;
		//				case Tile.DoorWest:		mapTexture.SetPixel(x, y, Color.cyan);		break;
		//			}
		//		}
		//	}

		//	mapTexture.Apply();
		//	RefreshMap(new Vector2(xSize, ySize));
		//}

		//http://wiki.unity3d.com/index.php?title=TextureDrawLine
		//private void PrintLineOnMap(int x0, int y0, int x1, int y1, Color color, float blendFactor){
		//	int dy = (int)(y1-y0);
		//	int dx = (int)(x1-x0);
		//	int stepx, stepy;

		//	if(dy < 0)	{dy = -dy; stepy = -1;}
		//	else		{stepy = 1;}
		//	if(dx < 0)	{dx = -dx; stepx = -1;}
		//	else		{stepx = 1;}
		//	dy <<= 1;
		//	dx <<= 1;

		//	float fraction = 0;

		//	BlendMapPixel(x0, y0, color, blendFactor);
		//	if (dx > dy) {
		//		fraction = dy - (dx >> 1);
		//		while (Mathf.Abs(x0 - x1) > 1) {
		//			if (fraction >= 0) {
		//				y0 += stepy;
		//				fraction -= dx;
		//			}
		//			x0 += stepx;
		//			fraction += dy;
		//			BlendMapPixel(x0, y0, color, blendFactor);
		//		}
		//	}else{
		//		fraction = dx - (dy >> 1);
		//		while (Mathf.Abs(y0 - y1) > 1) {
		//			if (fraction >= 0) {
		//				x0 += stepx;
		//				fraction -= dy;
		//			}
		//			y0 += stepy;
		//			fraction += dx;
		//			BlendMapPixel(x0, y0, color, blendFactor);
		//		}
		//	}				

		//	mapTexture.Apply();
		//}

		//public	void ColorRoomOnMap(Room room, Color color, float blendFactor){
		//	int xSize = room.tiles.GetLength(0);
		//	int ySize = room.tiles.GetLength(1);		
		//	for(int x = 0; x < xSize; x++){
		//		for(int y = 0; y < ySize; y++){
		//			if( room.tiles[x,y] == Tile.Floor){
		//				BlendMapPixel(x +room.x, y +room.y, color, blendFactor);
		//			}
		//		}
		//	}
		//}

		//public	void ColorPixelOnMap(int x, int y, Color color){
		//	mapTexture.SetPixel(x, y, color);
		//}

		//public	void BlendMapPixel(int x, int y, Color color, float factor){
		//	Color color1 = mapTexture.GetPixel(x,y);
		//	mapTexture.SetPixel(x, y, Color.Lerp(color1, color, factor));
		//}

		//private float mapPxPerTile = 3F;
		//private	void RefreshMap(Vector2 sizePx){

		//	//keep order of this changes:		
		//	mapImage.rectTransform.anchorMin		= new Vector2(1F,0F);
		//	mapImage.rectTransform.anchorMax		= new Vector2(1F,0F);
		//	mapImage.rectTransform.pivot			= new Vector2(1F,0F);
		//	mapImage.rectTransform.offsetMin		= Vector2.zero;
		//	mapImage.rectTransform.offsetMax		= sizePx *mapPxPerTile;	//1 tile = 2x2 px		
		//	mapImage.rectTransform.anchoredPosition	= new Vector2(-3,+3);//small dist from corner	

		//	Sprite sprite	= Sprite.Create( mapTexture, new Rectangle(0, 0, mapTexture.width, mapTexture.height), new Vector2(0.5F, 0.5F));		
		//	mapImage.sprite	= sprite;
		//	mapImage.enabled = true;
		//}

		//private bool regView = false;
		//public	void ToggleMap(){
		//	regView = !regView;
		//	if(regView){
		//		UpdateMap(ref _regions);
		//	}else{
		//		UpdateMap(ref _dungeon);
		//	}
		//}

		////gridX/Y is the gameUnit dimension of your Tile, made for top-down default = XZ-Plane
		//public	void ShowTransformOnMap(Transform actor, float gridX, float gridY, float? mapOrigX = null, float? mapOrigY = null){

		//	float offX = mapOrigX	?? default(float);
		//	float offY = mapOrigY	?? default(float);

		//	StartCoroutine(ShowOnMap(actor, gridX, gridY, offX, offY));
		//}

		//public	int actorPosX = 0;
		//public	int actorPosY = 0;
		//private	bool mapPointerActive = false;

		//private	IEnumerator ShowOnMap(Transform actor, float gridX, float gridY, float mapOffX, float mapOffY){

		//	if(mapPointerActive){mapPointerActive = false; yield return null;}	//finish current coroutine

		//	mapPointerActive = true;

		//	while(mapPointerActive){
		//		int actorPixelX = (int)( (actor.position.x -mapOffX +gridX/2F) /gridX ) ;
		//		int actorPixelY = (int)( (actor.position.z -mapOffY +gridY/2F) /gridY ) ;

		//		//only update if needed
		//		if(actorPosX != actorPixelX || actorPosY != actorPixelY){
		//			//mapTexture.SetPixel(actorPosX, actorPosY, Color.yellow);//yellow, or reset it to your Color coding
		//			actorPosX = actorPixelX;
		//			actorPosY = actorPixelY;
		//			//mapTexture.SetPixel(actorPosX, actorPosY, Color.red);
		//			BlendMapPixel(actorPosX, actorPosY, Color.yellow, 0.3F);
		//			mapTexture.Apply();

		//		}
		//		yield return null;
		//	}
		//}
		#endregion

		public double GetRandomNumber(double minimum, double maximum)
		{
			Random random = new Random();
			return random.NextDouble() * (maximum - minimum) + minimum;
		}

	}


	#region DATATYPES

	public class IntVector2 {
		public int x;
		public int y;

		public IntVector2(int x, int y) {
			this.x = x;
			this.y = y;
		}

		public int sqrMagnitude{
			get { return x * x + y * y; }
		}

		//Casts
		public static implicit operator Vector2(IntVector2 From){
			return new Vector2(From.x, From.y);
		}

		public static implicit operator IntVector2(Vector2 From){
			return new IntVector2((int)From.X, (int)From.Y);
		}

		public static implicit operator string(IntVector2 From){
			return "(" + From.x + ", " + From.y + ")";
		}

		//Operators
		public static IntVector2 operator +(IntVector2 a, IntVector2 b) {
			return new IntVector2(a.x + b.x, a.y + b.y);
		}

		public static IntVector2 operator +(IntVector2 a, Vector2 b) {
			return new IntVector2(a.x + (int)b.X, a.y + (int)b.Y);
		}

		public static IntVector2 operator -(IntVector2 a, IntVector2 b) {
			return new IntVector2(a.x - b.x, a.y - b.y);
		}

		public static IntVector2 operator -(IntVector2 a, Vector2 b) {
			return new IntVector2(a.x - (int)b.X, a.y - (int)b.Y);
		}

		public static IntVector2 operator *(IntVector2 a, int b) {
			return new IntVector2(a.x * b, a.y * b);
		}		

		public static bool operator == (IntVector2 iv1, IntVector2 iv2){
			return (iv1.x == iv2.x && iv1.y == iv2.y);
		}

		public static bool operator != (IntVector2 iv1, IntVector2 iv2){
			return (iv1.x != iv2.x || iv1.y != iv2.y);
		}

		public override bool Equals(System.Object obj){
			// If parameter is null return false.
			if (obj == null){
				return false;
			}

			// If parameter cannot be cast to Point return false.
			IntVector2 p = obj as IntVector2;
			if ((System.Object)p == null){
				return false;
			}

			// Return true if the fields match:
			return (x == p.x) && (y == p.y);
		}

		public bool Equals(IntVector2 p){
			// If parameter is null return false:
			if ((object)p == null){
				return false;
			}

			// Return true if the fields match:
			return (x == p.x) && (y == p.y);
		}

		public override int GetHashCode(){
			return x ^ y;
		}	
	}


	/** Integer Rectangleangle.
	 * Works almost like UnityEngine.Rectangle but with integer coordinates , gathered from aarons pathfinding stuff
	 */
	public struct IntRectangle {
		public int xmin, ymin, xmax, ymax;

		public IntRectangle (int xmin, int ymin, int xmax, int ymax) {
			this.xmin = xmin;
			this.xmax = xmax;
			this.ymin = ymin;
			this.ymax = ymax;
		}

		public bool Contains (int x, int y) {
			return !(x < xmin || y < ymin || x > xmax || y > ymax);
		}

		public int Width {
			get {
				return xmax-xmin+1;
			}
		}

		public int Height {
			get {
				return ymax-ymin+1;
			}
		}

		/** Returns if this rectangle is valid.
		 * An invalid rect could have e.g xmin > xmax.
		 * Rectangleamgles with a zero area area invalid.
		 */
		public bool IsValid () {
			return xmin <= xmax && ymin <= ymax;
		}

		public static bool operator == (IntRectangle a, IntRectangle b) {
			return a.xmin == b.xmin && a.xmax == b.xmax && a.ymin == b.ymin && a.ymax == b.ymax;
		}

		public static bool operator != (IntRectangle a, IntRectangle b) {
			return a.xmin != b.xmin || a.xmax != b.xmax || a.ymin != b.ymin || a.ymax != b.ymax;
		}

		public override bool Equals (System.Object _b) {
			IntRectangle b = (IntRectangle)_b;
			return xmin == b.xmin && xmax == b.xmax && ymin == b.ymin && ymax == b.ymax;
		}

		public override int GetHashCode () {
			return xmin*131071 ^ xmax*3571 ^ ymin*3109 ^ ymax*7;
		}

		/** Returns the intersection rect between the two rects.
		 * The intersection rect is the area which is inside both rects.
		 * If the rects do not have an intersection, an invalid rect is returned.
		 * \see IsValid
		 */
		public static IntRectangle Intersection (IntRectangle a, IntRectangle b) {
			IntRectangle r = new IntRectangle(
				System.Math.Max(a.xmin,b.xmin),
				System.Math.Max(a.ymin,b.ymin),
				System.Math.Min(a.xmax,b.xmax),
				System.Math.Min(a.ymax,b.ymax)
			);

			return r;
		}

		/** Returns if the two rectangles intersect each other
		 */
		public static bool Intersects (IntRectangle a, IntRectangle b) {
			return !(a.xmin > b.xmax || a.ymin > b.ymax || a.xmax < b.xmin || a.ymax < b.ymin);
		}

		/** Returns a new rect which contains both input rects.
		 * This rectangle may contain areas outside both input rects as well in some cases.
		 */
		public static IntRectangle Union (IntRectangle a, IntRectangle b) {
			IntRectangle r = new IntRectangle(
				System.Math.Min(a.xmin,b.xmin),
				System.Math.Min(a.ymin,b.ymin),
				System.Math.Max(a.xmax,b.xmax),
				System.Math.Max(a.ymax,b.ymax)
			);

			return r;
		}

		/** Returns a new IntRectangle which is expanded to contain the point */
		public IntRectangle ExpandToContain (int x, int y) {
			IntRectangle r = new IntRectangle(
				System.Math.Min(xmin,x),
				System.Math.Min(ymin,y),
				System.Math.Max(xmax,x),
				System.Math.Max(ymax,y)
			);
			return r;
		}

		/** Returns a new rect which is expanded by \a range in all directions.
		 * \param range How far to expand. Negative values are permitted.
		 */
		public IntRectangle Expand (int range) {
			return new IntRectangle(xmin-range,
				ymin-range,
				xmax+range,
				ymax+range
			);
		}

		/** Matrices for rotation.
		 * Each group of 4 elements is a 2x2 matrix.
		 * The XZ position is multiplied by this.
		 * So
		 * \code
		 * //A rotation by 90 degrees clockwise, second matrix in the array
		* (5,2) * ((0, 1), (-1, 0)) = (2,-5)
			* \endcode
			*/
			private static readonly int[] Rotations = {
			1, 0, //Identity matrix
			0, 1,

			0, 1,
			-1, 0,

			-1, 0,
			0,-1,

			0,-1,
			1, 0
		};

		/** Returns a new rect rotated around the origin 90*r degrees.
		 * Ensures that a valid rect is returned.
		 */
		public IntRectangle Rotate ( int r ) {
			int mx1 = Rotations[r*4+0];
			int mx2 = Rotations[r*4+1];
			int my1 = Rotations[r*4+2];
			int my2 = Rotations[r*4+3];

			int p1x = mx1*xmin + mx2*ymin;
			int p1y = my1*xmin + my2*ymin;

			int p2x = mx1*xmax + mx2*ymax;
			int p2y = my1*xmax + my2*ymax;

			return new IntRectangle (
				System.Math.Min ( p1x, p2x ),
				System.Math.Min ( p1y, p2y ),
				System.Math.Max ( p1x, p2x ),
				System.Math.Max ( p1y, p2y )
			);
		}

		/** Returns a new rect which is offset by the specified amount.
		 */
		public IntRectangle Offset ( Int2 offset ) {
			return new IntRectangle ( xmin+offset.x, ymin + offset.y, xmax + offset.x, ymax + offset.y );
		}

		/** Returns a new rect which is offset by the specified amount.
		 */
		public IntRectangle Offset ( int x, int y ) {
			return new IntRectangle ( xmin+x, ymin + y, xmax + x, ymax + y );
		}

		public override string ToString () {
			return "[x: "+xmin+"..."+xmax+", y: " + ymin +"..."+ymax+"]";
		}

		/** Draws some debug lines representing the rect */
		//public void DebugDraw (Matrix4x4 matrix, Color col) {
		//	Vector3 p1 = matrix.MultiplyPoint3x4 (new Vector3(xmin,0,ymin));
		//	Vector3 p2 = matrix.MultiplyPoint3x4 (new Vector3(xmin,0,ymax));
		//	Vector3 p3 = matrix.MultiplyPoint3x4 (new Vector3(xmax,0,ymax));
		//	Vector3 p4 = matrix.MultiplyPoint3x4 (new Vector3(xmax,0,ymin));

		//	Debug.DrawLine (p1,p2,col);
		//	Debug.DrawLine (p2,p3,col);
		//	Debug.DrawLine (p3,p4,col);
		//	Debug.DrawLine (p4,p1,col);
		//}
	}

	/** Two Dimensional Integer Coordinate Pair , gathered from aarons pathfinding stuff*/
	public struct Int2 {
		public int x;
		public int y;

		public Int2 (int x, int y) {
			this.x = x;
			this.y = y;
		}

		public int sqrMagnitude {
			get {
				return x*x+y*y;
			}
		}

		public long sqrMagnitudeLong {
			get {
				return (long)x*(long)x+(long)y*(long)y;
			}
		}

		public static Int2 operator + (Int2 a, Int2 b) {
			return new Int2 (a.x+b.x, a.y+b.y);
		}

		public static Int2 operator - (Int2 a, Int2 b) {
			return new Int2 (a.x-b.x, a.y-b.y);
		}

		public static bool operator == (Int2 a, Int2 b) {
			return a.x == b.x && a.y == b.y;
		}

		public static bool operator != (Int2 a, Int2 b) {
			return a.x != b.x || a.y != b.y;
		}

		public static int Dot (Int2 a, Int2 b) {
			return a.x*b.x + a.y*b.y;
		}

		public static long DotLong (Int2 a, Int2 b) {
			return (long)a.x*(long)b.x + (long)a.y*(long)b.y;
		}

		public override bool Equals (System.Object o) {
			if (o == null) return false;
			Int2 rhs = (Int2)o;

			return x == rhs.x && y == rhs.y;
		}

		public override int GetHashCode () {
			return x*49157+y*98317;
		}

		/** Matrices for rotation.
	 * Each group of 4 elements is a 2x2 matrix.
	 * The XZ position is multiplied by this.
	 * So
	 * \code
	 * //A rotation by 90 degrees clockwise, second matrix in the array
		* (5,2) * ((0, 1), (-1, 0)) = (2,-5)
			* \endcode
			*/
			private static readonly int[] Rotations = {
			1, 0, //Identity matrix
			0, 1,

			0, 1,
			-1, 0,

			-1, 0,
			0,-1,

			0,-1,
			1, 0
		};

		/** Returns a new Int2 rotated 90*r degrees around the origin. */
		public static Int2 Rotate ( Int2 v, int r ) {
			r = r % 4;
			return new Int2 ( v.x*Rotations[r*4+0] + v.y*Rotations[r*4+1], v.x*Rotations[r*4+2] + v.y*Rotations[r*4+3] );
		}

		public static Int2 Min (Int2 a, Int2 b) {
			return new Int2 (System.Math.Min (a.x,b.x), System.Math.Min (a.y,b.y));
		}

		public static Int2 Max (Int2 a, Int2 b) {
			return new Int2 (System.Math.Max (a.x,b.x), System.Math.Max (a.y,b.y));
		}

		/*public static Int2 FromInt3XZ (Int3 o) {
		return new Int2 (o.x,o.z);
	}*/

		/*public static Int3 ToInt3XZ (Int2 o) {
		return new Int3 (o.x,0,o.y);
	}*/

	public override string ToString ()
	{
		return "("+x+", " +y+")";
	}
}

#endregion
}

