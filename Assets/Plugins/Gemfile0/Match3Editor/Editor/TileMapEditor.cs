using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TileMap))]
public class TileMapEditor: BaseMapEditor 
{
    protected override string ReadPostName() 
	{ 
		return "tile";
	}
}
