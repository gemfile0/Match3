using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Canvas))]
public class SceneLoader: MonoBehaviour 
{
	public void Load(string sceneName)
	{
		StartCoroutine(StartSceneLoading(sceneName));
	}

	IEnumerator StartSceneLoading(string sceneName)
	{
		var loadingCoverObject = ResourceCache.Instantiate("LoadingCover");
		loadingCoverObject.transform.SetParent(transform, false);
		
		var loadingCover = loadingCoverObject.GetComponent<LoadingCover>();
		yield return loadingCover.Show(sceneName);

		AsyncOperation asyncOpeartion = SceneManager.LoadSceneAsync(sceneName);
		while (!asyncOpeartion.isDone)
		{
			yield return loadingCover.Progress(asyncOpeartion.progress);
		}

		yield return loadingCover.Progress(1);
		yield return loadingCover.Hide();
	}
}
