using UnityEngine;

public class BaseView<M, C>: MonoBehaviour 
	where M: BaseModel
	where C: BaseController<M>, new() {
	[SerializeField]
	protected M Model;
	protected C Controller;
	
	public virtual void Awake() {
		Controller = new C();
		Controller.Setup(Model);
	}
}
