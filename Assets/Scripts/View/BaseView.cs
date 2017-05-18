using UnityEngine;

public class BaseView<M, C>: PooledObject
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

	public virtual void OnDestroy()
	{
		if (Controller == null) { return; }
		
		Controller.Kill();
		Controller = null;
	}

	public virtual void UpdateModel(M model) 
    {
        Model = model;
    }
}
