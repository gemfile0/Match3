using UnityEngine;

public class BaseView<M, C>: MonoBehaviour 
	where M: BaseModel
	where C: BaseController<M>, new() 
{
	[SerializeField]
	protected M Model;
	protected C Controller;
	
	public virtual void Start() 
	{
		Controller = new C();
		Controller.Setup(Model);
	}

	public virtual void Destroy()
	{
		Controller.Destroy();
		Controller = null;
	}
}
