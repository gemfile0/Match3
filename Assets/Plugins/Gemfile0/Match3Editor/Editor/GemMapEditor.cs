using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;
using System.IO;

[CustomEditor(typeof(GemMap))]
public class GemMapEditor: Editor 
{
	public GemMap map;
	ReorderableList gemList;
	ReorderableList tileList;
	GemBrush brush;
	Vector3 mouseHitPosition;
	bool mouseOnMap 
	{
		get { 
			return mouseHitPosition.x > 0 && mouseHitPosition.x < map.gridSize.x
				&& mouseHitPosition.y < 0 && mouseHitPosition.y > -map.gridSize.y;
		}
	}

	void OnEnable() 
	{
		Tools.current = Tool.View;
		map = target as GemMap;

		CreateGemItems();
		CreateTileItems();
		LoadGemItemSpritesAsCache();
		LoadTileItemSpritesAsCache();
		UpdateCalculations();
		NewBrush();
	}

	void OnDisable() 
	{
		UnsubscribeTextureList();
		DestoryBrush();
	}

	public override void OnInspectorGUI() 
	{
		EditorGUILayout.BeginVertical();

		//
		EditorGUILayout.Space();
		map.mapName = EditorGUILayout.TextField("Map Name:", map.mapName);
		
		//
		EditorGUILayout.Space();
		var oldSize = map.mapSize;
		map.mapSize = EditorGUILayout.Vector2Field("Map Size:", map.mapSize);
		if (map.mapSize != oldSize) 
		{
			UpdateCalculations();
		}

		//
		EditorGUILayout.Space();
		EditorGUILayout.LabelField(
			"Gem Items:", 
			EditorStyle.guiMessageStyle
		);

		//
		var oldItems = map.gemItems;
		gemList.DoLayoutList();
		serializedObject.ApplyModifiedProperties();
		if (oldItems != map.gemItems) 
		{
			LoadGemItemSpritesAsCache();
			UpdateCalculations();
			NewBrush();
		}

		if (GUILayout.Button("Save Gem Items")) {
			WriteData(map.SaveItems(), "Save Gem Items", "gem_items.txt", "txt");
		}
		if (GUILayout.Button("Load Gem Items")) {
			map.LoadItems(ReadTextFromFile("Load Gem Items", "txt"));
		}

		//
		oldItems = map.tileItems;
		tileList.DoLayoutList();
		serializedObject.ApplyModifiedProperties();
		if (oldItems != map.tileItems) 
		{
			LoadGemItemSpritesAsCache();
		}

		//
		if (map.gemItems.Count == 0) 
		{
			EditorGUILayout.HelpBox(
				"You have not selected a texture 2D yet.", 
				MessageType.Warning
			);
		} 
		else 
		{
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Gem Size:", map.gemSize.x + "x" + map.gemSize.y);
			EditorGUILayout.LabelField("Grid Size In Units:", map.gridSize.x + "x" + map.gridSize.y);
			EditorGUILayout.LabelField("Pixels To Units:", map.pixelsToUnits.ToString());
			UpdateBrush(map.currentGemBrush);

			if (GUILayout.Button("Clear Gem Map")) {
				if (EditorUtility.DisplayDialog(
					"Clear map's gems?", 
					"Are you sure?",
					"Clear",
					"Do not clear"
				)) {
					ClearGemMap();
				}
			}

			if (GUILayout.Button("Save Gem Map")) {
				WriteData(SaveGemMap(), "Save Gem Map", map.mapName + ".json", "json");
			}

			if (GUILayout.Button("Load Gem Map")) {
				LoadGemMap(ReadTextFromFile("Load Gem Map", "json"));
			}
		}

		EditorGUILayout.EndVertical();
	}

	void OnSceneGUI()
	{
		if (brush != null) 
		{ 
			UpdateHitPosition(); 
			MoveBrush();

			if (map.gemItems != null && mouseOnMap) 
			{
				Event current = Event.current;
				if (current.shift) {
					Draw();
				} else if (current.alt) {
					RemoveGem();
				}
			}
		}
	}

	void CreateGemItems()
	{
		if (map.gems == null) 
		{
			var go = new GameObject("Gems");
			go.transform.SetParent(map.transform);
			go.transform.position = Vector3.zero;
			map.gems = go;
		}

		gemList = new ReorderableList(
			serializedObject, 
			serializedObject.FindProperty("gemItems"), 
			true, true, true, true
		);
		gemList.onRemoveCallback += OnItemRemove;
		gemList.drawElementCallback += OnGemDraw;
	}

	void CreateTileItems()
	{
		if (map.tiles == null) 
		{
			var go = new GameObject("Tiles");
			go.transform.SetParent(map.transform);
			go.transform.position = Vector3.zero;
			map.tiles = go;
		}

		tileList = new ReorderableList(
			serializedObject, 
			serializedObject.FindProperty("tileItems"), 
			true, true, true, true
		);
		tileList.onRemoveCallback += OnItemRemove;
		tileList.drawElementCallback += OnTileDraw;
	}

	void UnsubscribeTextureList()
	{
		if (gemList != null) 
		{
			gemList.onRemoveCallback -= OnItemRemove;
			gemList.drawElementCallback -= OnGemDraw;
		}
		if (tileList != null)
		{
			tileList.onRemoveCallback -= OnItemRemove;
			tileList.drawElementCallback -= OnTileDraw;
		}
	}

	void OnItemRemove(ReorderableList list) 
	{
		if (EditorUtility.DisplayDialog("Warning", "Are you sure?", "Yes", "No")) 
		{
			ReorderableList.defaultBehaviours.DoRemoveButton(list);
		}
	}

	void OnGemDraw(Rect rect, int index, bool isActive, bool isFocused)
	{
		OnItemDraw(
			gemList.serializedProperty.GetArrayElementAtIndex(index),
			rect, index, isActive, isFocused
		);
	}

	void OnTileDraw(Rect rect, int index, bool isActive, bool isFocused)
	{
		OnItemDraw(
			tileList.serializedProperty.GetArrayElementAtIndex(index),
			rect, index, isActive, isFocused
		);
	}

	void OnItemDraw(SerializedProperty item, Rect rect, int index, bool isActive, bool isFocused) 
	{
		EditorGUI.PropertyField(
			new Rect(rect.x, rect.y, 20, EditorGUIUtility.singleLineHeight),
			item.FindPropertyRelative("id"),
			GUIContent.none
		);
		EditorGUI.PropertyField(
			new Rect(rect.x+30, rect.y, 90, EditorGUIUtility.singleLineHeight),
			item.FindPropertyRelative("name"),
			GUIContent.none
		);
		EditorGUI.PropertyField(
			new Rect(rect.x+130, rect.y, rect.width-140, EditorGUIUtility.singleLineHeight),
			item.FindPropertyRelative("texture2D"),
			GUIContent.none
		);
	}

	void LoadGemItemSpritesAsCache()
	{
		var gemSpriteList = new List<SpriteReference>();
		var gemSpriteDict = new Dictionary<int, SpriteReference>();;

		foreach (var item in map.gemItems)
		{
			var path = AssetDatabase.GetAssetPath(item.texture2D);
			var spriteReference = new SpriteReference {
				id = item.id,
				sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path)
			};
			gemSpriteList.Add(spriteReference);
			gemSpriteDict.Add(item.id, spriteReference);
			item.path = path;
		}
		map.gemSpriteList = gemSpriteList;
		map.gemSpriteDict = gemSpriteDict;
	}

	void LoadTileItemSpritesAsCache()
	{
		var tileSpriteList = new List<SpriteReference>();

		foreach (var item in map.tileItems)
		{
			var path = AssetDatabase.GetAssetPath(item.texture2D);
			var spriteReference = new SpriteReference {
				id = item.id,
				sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path)
			};
			tileSpriteList.Add(spriteReference);
			item.path = path;
		}
		map.gemSpriteList = tileSpriteList;
	}

	void UpdateCalculations()
	{
		if (map.gemSpriteList.Count > 0)
		{ 
			var sprite = map.gemSpriteList[0].sprite;
			var width = sprite.textureRect.width;
			var height = sprite.textureRect.height;

			map.gemSize = new Vector2(width, height);
			map.pixelsToUnits = (int)(sprite.rect.width / sprite.bounds.size.x);
			map.gridSize = new Vector2(
				(width / map.pixelsToUnits) * map.mapSize.x,
				(height / map.pixelsToUnits) * map.mapSize.y
			);
		}
	}

	void CreateBrush()
	{
		var spriteReference = map.currentGemBrush;
		if (spriteReference != null)
		{
			GameObject go = new GameObject("Brush");
			go.transform.SetParent(map.transform);

			brush = go.AddComponent<GemBrush>();
			brush.renderer2D = go.AddComponent<SpriteRenderer>();
			brush.renderer2D.sortingOrder = 1000;

			var pixelsToUnits = map.pixelsToUnits;
			brush.brushSize = new Vector2(
				spriteReference.sprite.textureRect.width / pixelsToUnits,
				spriteReference.sprite.textureRect.height / pixelsToUnits
			);

			brush.UpdateBrush(spriteReference);
		}
	}

	void NewBrush() 
	{
		if (brush == null) { 
			var exist = GameObject.Find("Brush");
			if (exist != null) { brush = exist.GetComponent<GemBrush>(); }
			else { CreateBrush(); }
		}
	}

	void DestoryBrush()
	{
		if (brush != null) { DestroyImmediate(brush.gameObject); }
	}

	public void UpdateBrush(SpriteReference spriteReferences)
	{
		if (brush != null) { brush.UpdateBrush(spriteReferences); }
	}

	public void UpdateHitPosition()
	{
		var p = new Plane(
			map.transform.TransformDirection(Vector3.forward),
			Vector3.zero
		);
		var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
		var hit = Vector3.zero;
		var distance = 0f;

		if (p.Raycast(ray, out distance)) 
		{ 
			hit = ray.origin + ray.direction.normalized * distance;
		}
		
		mouseHitPosition = map.transform.InverseTransformDirection(hit);
	}

	void MoveBrush()
	{
		var gemSize = map.gemSize.x / map.pixelsToUnits;
		var x = Mathf.Floor(mouseHitPosition.x / gemSize) * gemSize;
		var y = Mathf.Floor(mouseHitPosition.y / gemSize) * gemSize;

		var row = x / gemSize;
		var column = Mathf.Abs(y / gemSize) - 1;

		if (!mouseOnMap) { return; }

		var id = (int)((column * map.mapSize.x) + row);
		brush.positionID = id;

		x += map.transform.position.x + gemSize / 2;
		y += map.transform.position.y + gemSize / 2;

		brush.transform.position = new Vector3(x, y, map.transform.position.z);
	}

	void Draw()
	{
		var positionID = brush.positionID.ToString();

		var positionX = brush.transform.position.x;
		var positionY = brush.transform.position.y;

		GameObject gem = GameObject.Find(map.name + "/Gems/gem_" + positionID);

		if (gem == null) 
		{
			gem = new GameObject("gem_" + positionID);
			gem.transform.SetParent(map.gems.transform);
			gem.transform.position = new Vector3(positionX, positionY, 0);
			gem.AddComponent<SpriteRenderer>();
			gem.AddComponent<Gem>();
		}

		gem.GetComponent<SpriteRenderer>().sprite = brush.renderer2D.sprite;
		gem.GetComponent<Gem>().ID = brush.gemID;
	}

	void RemoveGem()
	{
		var positionID = brush.positionID.ToString();

		GameObject gem = GameObject.Find(map.name + "/Gems/gem_" + positionID);
		if (gem != null)
		{
			DestroyImmediate(gem);
		}
	}

	void ClearGemMap()
	{
		for (var i = 0; i < map.gems.transform.childCount; i++)
		{
			Transform transform = map.gems.transform.GetChild(i);
			DestroyImmediate(transform.gameObject);
			i--;
		}
	}

	void LoadGemMap(string data)
	{
		var level = JsonUtility.FromJson<Level>(data);
		map.mapSize = new Vector2(level.cols, level.rows);
		var gemIDs = level.gems;

		var gemSize = map.gemSize.x / map.pixelsToUnits;
		for (var positionID = 0; positionID < gemIDs.Length; positionID++)
		{
			var positionX = (positionID % level.cols) * gemSize;
			var positionY = -((int)(positionID / level.cols) * gemSize);
			positionX += map.transform.position.x + gemSize / 2;
			positionY -= map.transform.position.y + gemSize / 2;
			
			var gemID = gemIDs[positionID];
			brush.UpdateBrush(map.GetGemBrush(gemID));
			brush.positionID = positionID;
			brush.transform.position = new Vector3(positionX, positionY, map.transform.position.z);
			Draw();
		}
	}

	string SaveGemMap()
	{
		var total = map.gems.transform.childCount;
		var gems = new int[total];
		for (var i = 0; i < total; i++)
		{
			Gem gem = map.gems.transform.Find("gem_" + i).GetComponent<Gem>();
			gems[i] = gem.ID;
		}	
		var cols = (int)map.mapSize.x;
		var rows = (int)map.mapSize.y;
		var level = new Level {
			gems = gems,
			cols = cols,
			rows = rows,
		};
		return JsonUtility.ToJson(level, true);
	}

	void WriteData(string data, string title, string defaultName, string extension) 
	{
		var path = EditorUtility.SaveFilePanel(title, "/Assets", defaultName, extension);
		using (FileStream fs = new FileStream(path, FileMode.Create)) 
		{
			using (StreamWriter writer = new StreamWriter(fs)) {
				writer.Write(data);
			}
		}
		AssetDatabase.Refresh();
	}

	string ReadTextFromFile(string title, string extension) 
	{
		var path = EditorUtility.OpenFilePanel(title, "/Assets", extension);
		var reader = new WWW("file:///" + path);
		while(!reader.isDone) {
		}

		return reader.text;
	}
}
