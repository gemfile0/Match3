using UnityEngine;
using UnityEngine.SceneManagement;

public class BaseScene: MonoBehaviour
 {
	protected virtual void Awake()
	{
		ResourceCache.LoadAll("Common");
		ResourceCache.LoadAll(SceneManager.GetActiveScene().name);

		if (GameObject.Find("SceneLoader") == null) 
		{
			ResourceCache.Instantiate("SceneLoader");
		}

#if DISABLE_LOG
		if (Application.platform == RuntimePlatform.Android
			|| Application.platform == RuntimePlatform.IPhonePlayer)
		{
			Debug.logger.logEnabled=false;
		}
#endif

#if DISABLE_DEBUG
#else
		var uiView = transform.Find("UIView");
		if (uiView != null)
		{
			ResourceCache.Load("FPSPanel");
			var fpsPanel = ResourceCache.Instantiate("FPSPanel");
			fpsPanel.transform.SetParent(uiView, false);
		}
#endif
	}

	public virtual void Init(object param)
	{
		
	}
}
