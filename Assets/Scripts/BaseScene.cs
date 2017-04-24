using UnityEngine;
using UnityEngine.SceneManagement;

public class BaseScene: MonoBehaviour
 {
	protected virtual void Awake()
	{
		ResourceCache.LoadAll(SceneManager.GetActiveScene().name);
	}

	public virtual void Init(object param)
	{
		
	}
}
