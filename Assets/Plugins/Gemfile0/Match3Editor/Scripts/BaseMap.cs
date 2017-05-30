#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public class SpriteReference
{
	public int id;
	public Sprite sprite;
}

public class BaseMap: MonoBehaviour 
{
	public Vector2 mapSize = new Vector2(20, 10);
	public List<BaseMapItem> mapItems;
	public GameObject root;
	public Vector2 itemSize = new Vector2();
	public Vector2 gridSize = new Vector2();
	public int pixelsToUnits = 100;
	public int selectedItemID = 0;
	public List<SpriteReference> itemSpriteList;
	public Dictionary<int, SpriteReference> itemSpriteDict;

	public SpriteReference currentItemBrush
	{
		get { return itemSpriteList[selectedItemID]; }
	}

	public string SaveItems()
	{
		var sb = new StringBuilder();
		var total = mapItems.Count;
		for (var i = 0; i < total; i++) 
		{
			sb.Append(mapItems[i].ToString());
			if(i < total - 1) { sb.Append(";"); }
		}
		return sb.ToString();
	}

	public void LoadItems(string data) 
	{
		var itemData = data.Split(';');
		mapItems = new List<BaseMapItem>();
		var total = itemData.Length;
		
		for (var i = 0; i < total; i++) 
		{
			var values = itemData[i].Split(',');
			var item = new BaseMapItem();
			item.id = int.Parse(values[0]);
			item.name = values[1];
			item.path = values[2];
			item.texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>(item.path);
			mapItems.Add(item);
		}
	}

	void OnDrawGizmosSelected()
	{
		var pos = transform.position;

		if (mapItems.Count > 0) 
		{
			// Draw a guideline
			Gizmos.color = Color.gray;
			var row = 0;
			var maxColumns = mapSize.x;
			var total = mapSize.x * mapSize.y;
			var item = new Vector3(itemSize.x / pixelsToUnits, itemSize.y / pixelsToUnits);
			var offset = new Vector2(item.x / 2, item.y / 2);

			for (var i = 0; i < total; i++) {
				var column = i % maxColumns;
				var newX = (column * item.x) + offset.x + pos.x;
				var newY = -(row * item.y) - offset.y + pos.y;
				Gizmos.DrawWireCube(new Vector2(newX, newY), item);
				if (column == maxColumns - 1) {
					row++;
				}
			}
			
			Gizmos.color = Color.white;
			var centerX = pos.x + (gridSize.x / 2);
			var centerY = pos.y - (gridSize.y / 2);
			Gizmos.DrawWireCube(new Vector2(centerX, centerY), gridSize);

			// Draw each items
			Gizmos.color = Color.cyan;
			foreach (var child in root.GetComponentsInChildren<ItemInfo>())
			{
				Gizmos.DrawWireCube(child.transform.localPosition, item);
			}
		}
	}
}
#endif