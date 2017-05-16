using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class GemPickerWindow: EditorWindow 
{
	public enum Scale
	{
		Quarter1,
		Quarter2,
		Quarter3,
		FullQaurter,
	}

	Scale scale;
	Vector2 currentSelection = Vector2.zero;
	
	public Vector2 scrollPosition = Vector2.zero;

	[MenuItem("Window/Gem Picker")]
	public static void OpenGemPickerWindow() 
	{
		var window = EditorWindow.GetWindow(typeof(GemPickerWindow));
		var title = new GUIContent();
		title.text = "Gem Picker";
		window.titleContent = title;
	}

	bool gemsVisible = false;
	bool tilesVisible = false;
	void OnGUI() 
	{
		if (Selection.activeGameObject == null)
		{
			return;
		}

		var gemMap = Selection.activeGameObject.GetComponent<GemMap>();
		if (gemMap != null) 
		{
			var gemItems = gemMap.gemItems;
			if (gemItems.Count == 0) { return; }

			scale = (Scale)EditorGUILayout.EnumPopup("Zoom", scale);
			
			var newScale = (((int)scale) + 1) / 4.0f;
			var offsetX = EditorGUIUtility.singleLineHeight;
			var offsetY = EditorGUIUtility.singleLineHeight;

			var firstTexture = gemItems.FirstOrDefault(gemItem => gemItem != null).texture2D;
			var scaledTextureSize = new Vector2(firstTexture.width, firstTexture.height) * newScale;
			
			var maxColumn = (int)((position.width - offsetX) / scaledTextureSize.x);
			var maxRow = (float)Math.Ceiling(((double)gemItems.Count / (double)maxColumn));

			BeginScrollView(scaledTextureSize, offsetY, maxColumn, maxRow);
			DrawGems(gemItems, scaledTextureSize, offsetX, offsetY, maxColumn, maxRow, gemMap);
			
			// tilesVisible = EditorGUILayout.Foldout(tilesVisible, "Tiles");
			// if (tilesVisible)
			// {
			// 	EditorGUI.indentLevel++;

			// 	heightOfGems = DrawGems(gemItems, scaledTextureSize, offsetX, offsetY + heightOfGems);
			// 	DrawSelection(scaledTextureSize, offsetX, offsetY, maxColumn, maxRow, gemMap);
				
			// 	EditorGUI.indentLevel--;
			// 	GUILayout.Space(heightOfGems);
			// }

			EditorGUILayout.EndScrollView();
		}
	}

	void DrawGems(List<GemItem> gemItems, Vector2 scaledTextureSize, float offsetX, float offsetY, float maxColumn, float maxRow, GemMap gemMap)
	{
		gemsVisible = EditorGUILayout.Foldout(gemsVisible, "Gems");
		var heightOfGems = offsetY;
		if (gemsVisible) 
		{
			EditorGUI.indentLevel++;
			
			heightOfGems = DrawGems(gemItems, scaledTextureSize, offsetX, offsetY);
			DrawSelection(scaledTextureSize, offsetX, offsetY, maxColumn, maxRow, gemMap);

			EditorGUI.indentLevel--;
			GUILayout.Space(heightOfGems);
		}
	}

	float DrawGems(List<GemItem> gemItems, Vector2 scaledTextureSize, float offsetX, float offsetY)
	{
		var maxHeightOfLine = 0f;
		var initialOffsetX = offsetX;
		var height = 0f;

		for (var i = 0; i < gemItems.Count; i++)
		{
			var gemItem = gemItems[i];
			if (gemItem.texture2D == null) { continue; }
			
			if ((offsetX + scaledTextureSize.x) > position.width) 
			{
				offsetY += maxHeightOfLine;
				offsetX = initialOffsetX;
				maxHeightOfLine = 0;
			}
			
			GUI.DrawTexture(new Rect(
				offsetX,
				offsetY,
				scaledTextureSize.x, 
				scaledTextureSize.y
			), gemItem.texture2D);

			offsetX += scaledTextureSize.x;
			maxHeightOfLine = Math.Max(maxHeightOfLine, scaledTextureSize.y);
			height = Math.Max(height, offsetY + scaledTextureSize.y);
		}
		return height;
	}

	void DrawSelection(
		Vector2 scaledTextureSize, 
		float offsetX, 
		float offsetY, 
		float maxColumn, 
		float maxRow,
		GemMap gemMap
	) {
		var grid = new Vector2(maxColumn, maxRow);
		
		var selectionPosition = new Vector2(
			scaledTextureSize.x * currentSelection.x + offsetX,
			scaledTextureSize.y * currentSelection.y + offsetY
		);
		var boxTexture = new Texture2D(1, 1);
		boxTexture.SetPixel(0, 0, new Color(0, 0.5f, 1f, 0.4f));
		boxTexture.Apply();

		var style = new GUIStyle(GUI.skin.customStyles[0]);
		style.normal.background = boxTexture;

		GUI.Box(new Rect(
			selectionPosition.x, 
			selectionPosition.y, 
			scaledTextureSize.x, 
			scaledTextureSize.y
		), "", style);

		var currentEvent = Event.current;
		Vector2 mousePosition = new Vector2(currentEvent.mousePosition.x, currentEvent.mousePosition.y);
		if (currentEvent.type == EventType.mouseDown && currentEvent.button == 0) 
		{
			currentSelection.x = Mathf.Floor((mousePosition.x - offsetX) / scaledTextureSize.x);
			currentSelection.y = Mathf.Floor((mousePosition.y - offsetY) / scaledTextureSize.y);

			var currentSelectionID = (int)(currentSelection.x + (currentSelection.y * grid.x));
			currentSelectionID = Mathf.Clamp(currentSelectionID, 0, gemMap.gemSpriteList.Count - 1);
			currentSelection.x = (currentSelectionID) % maxColumn;
			currentSelection.y = (int)((currentSelectionID) / maxColumn);

			gemMap.selectedGemID = currentSelectionID;

			Repaint();
		}
	}

	void BeginScrollView(Vector2 scaledTextureSize, float offsetY, float maxColumn, float maxRow)
	{
		var viewPort = new Rect(
			0, 
			EditorGUIUtility.singleLineHeight, 
			position.width, 
			position.height - EditorGUIUtility.singleLineHeight
		);
		var contentSize = new Rect(
			0, 
			0, 
			maxColumn * scaledTextureSize.x, 
			maxRow * scaledTextureSize.y + offsetY
		);

		scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
	}
}
