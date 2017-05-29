using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level: MonoBehaviour 
{
	public string Name;
	public Vector2 mapSize = new Vector2(20, 10);
	public TileMap tileMap;
	public GravityMap gravityMap;
	public GemMap gemMap;
	public int moves;	
	public MissionModel[] missions;
    public int[] gemTypesAvailable;
}
