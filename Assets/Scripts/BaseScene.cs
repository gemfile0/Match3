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
		if (GameObject.Find(Literals.SceneLoader) == null) 
		{
			ResourceCache.Instantiate(Literals.SceneLoader);
		}
		if (GameObject.Find(Literals.Toast) == null) 
		{
			ResourceCache.Instantiate(Literals.Toast);
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
		var uiView = transform.Find(Literals.UIView);
		if (uiView != null)
		{
			ResourceCache.Load(Literals.FPSPanel);
			var fpsPanel = ResourceCache.Instantiate(Literals.FPSPanel);
			fpsPanel.transform.SetParent(uiView, false);
		}
#endif
	}

	public virtual void Init(object param)
	{
		
	}
}
