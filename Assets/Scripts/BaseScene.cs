using UnityEngine;
using UnityEngine.SceneManagement;

public class BaseScene: MonoBehaviour
 {
	protected virtual void Awake()
	{
		ResourceCache.LoadAll("Common");
		ResourceCache.LoadAll(SceneManager.GetActiveScene().name);

		if (GameObject.Find("SceneLoader") == null) {
			ResourceCache.Instantiate("SceneLoader");
		}
	}

	public virtual void Init(object param)
	{
		
	}
}
