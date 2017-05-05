using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher: MonoBehaviour 
{
	public void SwitchScene()
	{
		int nextLevel = (SceneManager.GetActiveScene().buildIndex + 1) % SceneManager.sceneCountInBuildSettings;
		SceneManager.LoadScene(nextLevel);
	}
}
