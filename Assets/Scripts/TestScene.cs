public class TestScene: LevelScene 
{
	public int testingLevel;
	
	protected override void Awake()
	{
		ResourceCache.LoadAll("LevelScene");

		base.Awake();
	}

	protected override int ReadLevelIndex()
	{
		return testingLevel;
	}
}
