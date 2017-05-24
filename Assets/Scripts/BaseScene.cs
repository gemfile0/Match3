using UnityEngine;
using UnityEngine.SceneManagement;

public class BaseScene: MonoBehaviour
 {
	public int targetFrameRate = 60;

	protected virtual void Awake()
	{
		Application.targetFrameRate = targetFrameRate;

		ResourceCache.LoadAll(Literals.Common);
		ResourceCache.LoadAll(SceneManager.GetActiveScene().name);
		if (GameObject.Find(Literals.RootCanvas) == null) 
		{
			ResourceCache.Instantiate(Literals.RootCanvas);
		}

		if (GameObject.Find(Literals.MatchSound) == null) 
		{
			ResourceCache.Instantiate(Literals.MatchSound);
		}

#if DISABLE_LOG
		if (Application.platform == RuntimePlatform.Android
			|| Application.platform == RuntimePlatform.IPhonePlayer)
		{
			Debug.logger.logEnabled=false;
		}
#endif

#if DISABLE_FPS
#else
		var rootCanvas = GameObject.Find(Literals.RootCanvas);
		if (rootCanvas != null)
		{
			ResourceCache.Load(Literals.FPSPanel);
			var fpsPanel = ResourceCache.Instantiate(Literals.FPSPanel);
			fpsPanel.transform.SetParent(rootCanvas.transform, false);
		}
#endif
	}
}
