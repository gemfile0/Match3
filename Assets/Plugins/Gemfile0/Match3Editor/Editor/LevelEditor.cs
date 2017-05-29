using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Level))]
public class LevelEditor: Editor   
{
	Level level;

	protected void OnEnable() 
	{
		level = target as Level;
		
		Tools.current = Tool.View;
	}

	public override void OnInspectorGUI()
	{
		EditorGUILayout.Space();
		level.Name = EditorGUILayout.TextField("Level Name:", level.Name);

		//
		EditorGUILayout.Space();
		level.tileMap = (TileMap)EditorGUILayout.ObjectField("Tile Map: ", level.tileMap, typeof(TileMap), true);

		level.gravityMap = (GravityMap)EditorGUILayout.ObjectField("Gravity Map: ", level.gravityMap, typeof(GravityMap), true);

		level.gemMap = (GemMap)EditorGUILayout.ObjectField("Gem Map: ", level.gemMap, typeof(GemMap), true);

		//
		EditorGUILayout.Space();
		var oldSize = level.mapSize;
		level.mapSize = EditorGUILayout.Vector2Field("Map Size:", level.mapSize);
		if (level.mapSize != oldSize) 
		{
			level.tileMap.mapSize 
				= level.gravityMap.mapSize 
				= level.gemMap.mapSize 
				= level.mapSize;
		}

		//
		EditorGUILayout.Space();
		level.moves = EditorGUILayout.IntField("Moves:", level.moves);

		//
		EditorGUILayout.Space();
		var property = serializedObject.FindProperty("gemTypesAvailable");
        serializedObject.Update();
        EditorGUILayout.PropertyField(property, true);
        serializedObject.ApplyModifiedProperties();

		//
		EditorGUILayout.Space();
		property = serializedObject.FindProperty("missions");
        serializedObject.Update();
        EditorGUILayout.PropertyField(property, true);
        serializedObject.ApplyModifiedProperties();
		
		// if (GUILayout.Button("Clear Gem Map")) {
		// 	if (EditorUtility.DisplayDialog(
		// 		"Clear map's gems?", 
		// 		"Are you sure?",
		// 		"Clear",
		// 		"Do not clear"
		// 	)) {
		// 		ClearGemMap();
		// 	}
		// }

		//
		EditorGUILayout.Space();
		if (GUILayout.Button("Save Level")) 
		{
			FileAccessor.WriteData(SaveGemMap(), "Save Level", level.Name + ".json", "json");
		}

		if (GUILayout.Button("Load Level")) 
		{
			LoadGemMap(FileAccessor.ReadTextFromFile("Load Level", "json"));
		}
	}

	// void ClearGemMap()
	// {
	// 	for (var i = 0; i < map.gems.transform.childCount; i++)
	// 	{
	// 		Transform transform = map.gems.transform.GetChild(i);
	// 		DestroyImmediate(transform.gameObject);
	// 		i--;
	// 	}
	// }

	void LoadGemMap(string data)
	{
		// var level = JsonUtility.FromJson<Level>(data);
		// map.mapSize = new Vector2(level.cols, level.rows);
		// var gemIDs = level.gems;

		// var gemSize = map.gemSize.x / map.pixelsToUnits;
		// for (var positionID = 0; positionID < gemIDs.Length; positionID++)
		// {
		// 	var positionX = (positionID % level.cols) * gemSize;
		// 	var positionY = -((int)(positionID / level.cols) * gemSize);
		// 	positionX += map.transform.position.x + gemSize / 2;
		// 	positionY -= map.transform.position.y + gemSize / 2;
			
		// 	var gemID = gemIDs[positionID];
		// 	brush.UpdateBrush(map.GetGemBrush(gemID));
		// 	brush.positionID = positionID;
		// 	brush.transform.position = new Vector3(positionX, positionY, map.transform.position.z);
		// 	Draw();
		// }
	}

	string SaveGemMap()
	{
		var tileMap = level.tileMap;
		var gravityMap = level.gravityMap;
		var gemMap = level.gemMap;

		int cols = (int)level.mapSize.x;
		int rows = (int)level.mapSize.y;
		
		var countOfItem = cols * rows;
		var tiles = new int[countOfItem];
		var gravities = new int[countOfItem];
		var gems = new int[countOfItem];
		for (var i = 0; i < countOfItem; i++)
		{
			tiles[i] = tileMap.root.transform.Find("item_" + i).GetComponent<ItemInfo>().ID;
			gravities[i] = gravityMap.root.transform.Find("item_" + i).GetComponent<ItemInfo>().ID;
			gems[i] = gemMap.root.transform.Find("item_" + i).GetComponent<ItemInfo>().ID;
		}

		var levelModel = new LevelModel {
			cols = cols,
			rows = rows,
			moves = level.moves,
			gems = gems,
			tiles = tiles,
			gravities = gravities,
			gemTypesAvailable = level.gemTypesAvailable,
			missions = level.missions
		};
		return JsonUtility.ToJson(levelModel, true);
	}
}
