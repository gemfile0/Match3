using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader: MonoBehaviour
{
	public LoadingCover loadingCoverPrefab;
	
	public void Load(string sceneName)
	{
		StartCoroutine(StartSceneLoading(sceneName));
	}

	IEnumerator StartSceneLoading(string sceneName)
	{
		var loadingCover = Object.Instantiate<LoadingCover>(loadingCoverPrefab);
		loadingCover.transform.SetParent(transform, false);
		
		yield return loadingCover.Show(sceneName);

		AsyncOperation asyncOpeartion = SceneManager.LoadSceneAsync(sceneName);
		while (!asyncOpeartion.isDone)
		{
			yield return loadingCover.Progress(asyncOpeartion.progress);
		}

		DOTween.Clear();
		System.GC.Collect();

		yield return loadingCover.Progress(1);
		yield return loadingCover.HideAndKill();
	}
}
