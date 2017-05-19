public class BaseController<M> 
    where M: BaseModel 
{
    protected M Model;
    
    public virtual void Setup(M model) 
    {
        Model = model;
        Model.Setup();
    }

    public virtual void Kill()
    {
        Model.Kill();
        Model = null;
    }
}
