public class GemController<M> : BaseController<M>
where M : GemModel
{
	internal void Start() {
		Model.isMoving = true;
	}

    internal void Stop() {
        Model.isMoving = false;
    }
}
