using UnityEngine;
using UnityEditor;

public class EditorTimeScaler: EditorWindow
{
	static float timeScale = 1.0f;
	static EditorTimeScaler window;

	static EditorTimeScaler()
	{
	}

	[MenuItem("Tools/TimeScaler")]
	public static void OpenWindow()
	{
		window = (EditorTimeScaler)EditorWindow.GetWindow(typeof(EditorTimeScaler));
		window.titleContent = new GUIContent("Time Scaler");
	}

	void OnGUI()
	{
		if (window == null) { OpenWindow(); }
		timeScale = EditorGUILayout.Slider("Time Scale", timeScale, 0.0f, 1.0f);
		ChangeTimeScale(timeScale);
	}

	static void ChangeTimeScale(float value)
	{
		if (timeScale != Time.timeScale) { Time.timeScale = timeScale; }
	}
}