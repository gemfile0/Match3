using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public class SpriteReference
{
	public int id;
	public Sprite sprite;
}

public class GemMap: MonoBehaviour 
{
	public Vector2 mapSize = new Vector2(20, 10);
	public string mapName;

	public List<GemItem> gemItems;
	public List<SpriteReference> gemSpriteList;
	public Dictionary<int, SpriteReference> gemSpriteDict;

	public List<GemItem> tileItems;
	public List<SpriteReference> tileSpriteList;

	public Vector2 gemSize = new Vector2();
	public Vector2 gridSize = new Vector2();
	public int pixelsToUnits = 100;
	public int selectedGemID = 0;

	public GameObject gems;
	public GameObject tiles;

	public SpriteReference currentGemBrush
	{
		get { return gemSpriteList[selectedGemID]; }
	}

	public SpriteReference GetGemBrush(int gemID)
	{
		return gemSpriteDict[gemID];
	}
	
	void OnDrawGizmosSelected()
	{
		var pos = transform.position;

		if (gemItems.Count > 0) 
		{
			Gizmos.color = Color.gray;
			var row = 0;
			var maxColumns = mapSize.x;
			var total = mapSize.x * mapSize.y;
			var gem = new Vector3(gemSize.x / pixelsToUnits, gemSize.y / pixelsToUnits);
			var offset = new Vector2(gem.x / 2, gem.y / 2);

			for (var i = 0; i < total; i++) {
				var column = i % maxColumns;
				var newX = (column * gem.x) + offset.x + pos.x;
				var newY = -(row * gem.y) - offset.y + pos.y;
				Gizmos.DrawWireCube(new Vector2(newX, newY), gem);
				if (column == maxColumns - 1) {
					row++;
				}
			}
			
			Gizmos.color = Color.white;
			var centerX = pos.x + (gridSize.x / 2);
			var centerY = pos.y - (gridSize.y / 2);
			Gizmos.DrawWireCube(new Vector2(centerX, centerY), gridSize);
		}
	}

	public string SaveItems()
	{
		var sb = new StringBuilder();
		var total = gemItems.Count;
		for (var i = 0; i < total; i++) 
		{
			sb.Append(gemItems[i].ToString());
			if(i < total - 1) { sb.Append(";"); }
		}
		return sb.ToString();
	}

	public void LoadItems(string data) 
	{
		var itemData = data.Split(';');
		gemItems = new List<GemItem>();
		var total = itemData.Length;
		
		for (var i = 0; i < total; i++) 
		{
			var values = itemData[i].Split(',');
			var item = new GemItem();
			item.id = int.Parse(values[0]);
			item.name = values[1];
			item.path = values[2];
			item.texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>(item.path);
			gemItems.Add(item);
		}
	}
}
