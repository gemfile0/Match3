using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ShowBounds))]
class ShowBoundsEditor: Editor 
{
	void OnSceneGUI() 
	{
		ShowBounds t = target as ShowBounds;

		if(t == null || t.edges == null || t.edges.Length == 0) { return; }

		Handles.color = Color.white;

		Vector3 endPoint = t.edges[0];
		t.edges.ForEach(edge => {
			Handles.DrawLine(endPoint, edge);
			endPoint = edge;
		});
	}
}
