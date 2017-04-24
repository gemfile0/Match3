using UnityEngine;
using UnityEditor;

public class TimeScaleWindow: EditorWindow
{
	float timeScale = 1.0f;
	float timeScaleBefore = 0f;

	[MenuItem("Tools/Time Scale Window")]
	public static void OpenWindow()
	{
		var window = EditorWindow.GetWindow(typeof(TimeScaleWindow));
		window.titleContent = new GUIContent("Time Scale Window");
	}

	void OnEnable()
	{
		EditorApplication.playmodeStateChanged += OnPlaymodeStateChange;
	}

	void OnDiable()
	{
		EditorApplication.playmodeStateChanged -= OnPlaymodeStateChange;
	}

	void OnGUI()
	{
		timeScale = EditorGUILayout.Slider("Time Scale", timeScale, 0.0f, 1.0f);
		ChangeTimeScale(timeScale);
	}

	void OnPlaymodeStateChange()
	{
		if (EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying)
		{
			timeScaleBefore = timeScale;
		}
		else if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
		{
			timeScale = timeScaleBefore;
			ChangeTimeScale(timeScale);
			Repaint();
		}
	}

	void ChangeTimeScale(float value)
	{
		if (timeScale != Time.timeScale) { Time.timeScale = timeScale; }
	}
}