using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(BaseMap))]
public class BaseMapEditor: Editor  
{
	protected BaseMap map;
	protected ReorderableList itemList;
	protected BaseBrush brush;
	protected Vector3 mouseHitPosition;
	protected bool mouseOnMap 
	{
		get { 
			return mouseHitPosition.x > 0 && mouseHitPosition.x < map.gridSize.x
				&& mouseHitPosition.y < 0 && mouseHitPosition.y > -map.gridSize.y;
		}
	}

	protected void OnEnable() 
	{
		map = target as BaseMap;
		
		Tools.current = Tool.View;

		CreateGemItems();
		LoadGemItemSpritesAsCache();
		UpdateCalculations();
		NewBrush();
	}

	protected void OnDisable() 
	{
		UnsubscribeTextureList();
		DestoryBrush();
	}

	public override void OnInspectorGUI() 
	{
		EditorGUILayout.BeginVertical();

		//
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Map Size:", map.mapSize.x + "x" + map.mapSize.y);

		//
		EditorGUILayout.Space();
		EditorGUILayout.LabelField(
			"Map Items:", 
			EditorStyle.guiMessageStyle
		);

		//
		var oldItems = map.mapItems;
		itemList.DoLayoutList();
		serializedObject.ApplyModifiedProperties();
		if (oldItems != map.mapItems) 
		{
			LoadGemItemSpritesAsCache();
			UpdateCalculations();
			NewBrush();
		}

		if (GUILayout.Button("Save Map Items")) {
			FileAccessor.WriteData(map.SaveItems(), "Save Gem Items", ReadPostName() + "_items.txt", "txt");
		}
		if (GUILayout.Button("Load Map Items")) {
			map.LoadItems(FileAccessor.ReadTextFromFile("Load Gem Items", "txt"));
		}

		//
		if (map.mapItems.Count == 0) 
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
			EditorGUILayout.LabelField("Gem Size:", map.itemSize.x + "x" + map.itemSize.y);
			EditorGUILayout.LabelField("Grid Size In Units:", map.gridSize.x + "x" + map.gridSize.y);
			EditorGUILayout.LabelField("Pixels To Units:", map.pixelsToUnits.ToString());
			UpdateBrush(map.currentItemBrush);
		}

		EditorGUILayout.EndVertical();
	}

	protected virtual string ReadPostName() 
	{ 
		return "map";
	}

	protected void OnSceneGUI()
	{
		if (brush != null) 
		{ 
			UpdateHitPosition(); 
			MoveBrush();

			if (map.mapItems != null && mouseOnMap) 
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

	public void UpdateCalculations()
	{
		if (map.itemSpriteList.Count > 0)
		{ 
			var sprite = map.itemSpriteList[0].sprite;
			if (sprite == null) { return; }

			var width = sprite.textureRect.width;
			var height = sprite.textureRect.height;

			map.itemSize = new Vector2(width, height);
			map.pixelsToUnits = (int)(sprite.rect.width / sprite.bounds.size.x);
			map.gridSize = new Vector2(
				(width / map.pixelsToUnits) * map.mapSize.x,
				(height / map.pixelsToUnits) * map.mapSize.y
			);
		}
	}

	protected void LoadGemItemSpritesAsCache()
	{
		var gemSpriteList = new List<SpriteReference>();
		var gemSpriteDict = new Dictionary<int, SpriteReference>();

		foreach (var item in map.mapItems)
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
		map.itemSpriteList = gemSpriteList;
		map.itemSpriteDict = gemSpriteDict;
	}

	protected void CreateGemItems()
	{
		if (map.root == null)
		{
			var items = map.transform.Find("Items");
			if (items == null) { 
				items = new GameObject("Items").transform; 
				items.SetParent(map.transform, false);
			}
			map.root = items.gameObject;
		}

		itemList = new ReorderableList(
			serializedObject, 
			serializedObject.FindProperty("mapItems"), 
			true, true, true, true
		);
		itemList.onRemoveCallback += OnItemRemove;
		itemList.drawElementCallback += OnItemDraw;
	}

	protected void UnsubscribeTextureList()
	{
		if (itemList != null) 
		{
			itemList.onRemoveCallback -= OnItemRemove;
			itemList.drawElementCallback -= OnItemDraw;
		}
	}

	void OnItemRemove(ReorderableList list) 
	{
		if (EditorUtility.DisplayDialog("Warning", "Are you sure?", "Yes", "No")) 
		{
			ReorderableList.defaultBehaviours.DoRemoveButton(list);
		}
	}

	void OnItemDraw(Rect rect, int index, bool isActive, bool isFocused)
	{
		var item = itemList.serializedProperty.GetArrayElementAtIndex(index);
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

	void CreateBrush()
	{
		var spriteReference = map.currentItemBrush;
		if (spriteReference != null)
		{
			GameObject go = new GameObject("Brush");
			go.transform.SetParent(map.transform, false);

			brush = go.AddComponent<BaseBrush>();
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

	protected void NewBrush() 
	{
		if (brush == null) { 
			var exist = GameObject.Find("Brush");
			if (exist != null) { brush = exist.GetComponent<BaseBrush>(); }
			else { CreateBrush(); }
		}
	}

	protected void DestoryBrush()
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

	protected void MoveBrush()
	{
		var gemSize = map.itemSize.x / map.pixelsToUnits;
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

	protected void Draw()
	{
		var positionID = brush.positionID.ToString();

		var positionX = brush.transform.position.x;
		var positionY = brush.transform.position.y;

		GameObject gem = GameObject.Find(map.name + "/Items/item_" + positionID);

		if (gem == null) 
		{
			gem = new GameObject("item_" + positionID);
			gem.transform.SetParent(map.root.transform, false);
			gem.transform.localPosition = new Vector3(positionX, positionY);
			gem.AddComponent<SpriteRenderer>();
			gem.AddComponent<ItemInfo>();
		}

		gem.GetComponent<SpriteRenderer>().sprite = brush.renderer2D.sprite;
		gem.GetComponent<ItemInfo>().ID = brush.gemID;
	}

	protected void RemoveGem()
	{
		var positionID = brush.positionID.ToString();

		GameObject gem = GameObject.Find(map.name + "/Items/item_" + positionID);
		if (gem != null)
		{
			DestroyImmediate(gem);
		}
	}
}
