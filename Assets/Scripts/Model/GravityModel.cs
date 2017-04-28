[System.Serializable]
public enum GravityType 
{
    UP = 1, DOWN, LEFT, RIGHT
}

[System.Serializable]
public class GravityModel 
{
    public GravityType type;
    public int[] vector;
    public GravityModel(GravityType type) 
    {
        this.type = type;

        switch (type)
        {
            case GravityType.UP:
            vector = new int[]{ 0, 1 };
            break;

            case GravityType.DOWN:
            vector = new int[]{ 0, -1 };
            break;
            
            case GravityType.LEFT:
            vector = new int[]{ -1, 0 };
            break;
            
            case GravityType.RIGHT:
            vector = new int[]{ 1, 0 };
            break;
        }
    }
}

static class GravityModelFactory 
{
    public static GravityModel Get(GravityType gravityType) 
    {
        var gravityModel = new GravityModel(gravityType);
        return gravityModel;
    }
}