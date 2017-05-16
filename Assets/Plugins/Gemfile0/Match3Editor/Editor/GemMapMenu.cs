using UnityEngine;
using UnityEditor;

public class GemMapMenu {

	[MenuItem("GameObject/Gem Map")]
	public static void CreateGemMap() {
		GameObject go = new GameObject("Gem Map");
		go.AddComponent<GemMap>();
	}
}
