using NUnit.Framework;
using System.Linq;
using UnityEngine;

public class TestGameController 
{
	GameModel GetGameModel() 
	{
		var gameModel = new GameModel();
		gameModel.levelData = Resources.Load<TextAsset>("level_test");
		return gameModel;
	}

	GameController<GameModel> GetGameController(GameModel gameModel) 
	{
		var gameController = new GameController<GameModel>();
		gameController.Setup(gameModel);
		return gameController;
	}

	[Test]
	public void MakeField() 
	{
		//1. Arrange
		var gameModel = GetGameModel();
		//2. Act
		GetGameController(gameModel);
		//3. Assert
		var gemModels = gameModel.GemModels;
		Assert.AreEqual(15, gemModels.GetLength(0) * gemModels.GetLength(1));

		var normalGemModels = 
			from GemModel gemModel in gemModels
			where gemModel is EmptyGemModel
			select gemModel;
		Assert.AreEqual(9, normalGemModels.Count());
	}

	[Test]
	public void PutGems() 
	{
		//1. Arrange
		var gameModel = GetGameModel();
		var gameController = GetGameController(gameModel);
		
		//2. Act
		gameController.PutGems();

		//3. Assert
		var gemModels = gameModel.GemModels;
		Assert.AreEqual(15, gemModels.GetLength(0) * gemModels.GetLength(1));
		
		var emtpyGemModels = 
			from GemModel gemModel in gemModels
			where gemModel.Type == GemType.EmptyGem
			select gemModel;
		Assert.AreEqual(3, emtpyGemModels.Count());
	}

	[Test]
	public void ExistAnyMatches() 
	{
		//1. Arrange
		var gameModel = GetGameModel();
		var gameController = GetGameController(gameModel);
		
		var gemModels = gameModel.GemModels;
		var normalGemModels = 
			(from GemModel gemModel in gemModels
			where gemModel is EmptyGemModel
			select gemModel).ToList();

		//2. Act
		var sampleGemTypes = new GemType[] {
			GemType.RedGem, GemType.RedGem, GemType.RedGem,
			GemType.RedGem, GemType.RedGem, GemType.GreenGem,
			GemType.RedGem, GemType.GreenGem, GemType.GreenGem
		};

		var index = 0;
		foreach(var sampleGemType in sampleGemTypes) {
			normalGemModels[index].Type = sampleGemTypes[index];
			index++;
		}

		//3. Assert
		Assert.AreEqual(GemType.EmptyGem, gameController.GetGemModel(Position.Get(0, 0)).Type);
		Assert.AreEqual(GemType.RedGem, gameController.GetGemModel(Position.Get(0, 1)).Type);

		//1. Arrange
		var horizontalMatch = new MatchLineModel(-2, 0, 3, 1);
		var verticalMatch = new MatchLineModel(-2, 0, 1, 3);
		var sqaureMatch = new MatchLineModel(-1, -1, 2, 2);

		//2. Act & Assert
		Assert.AreEqual(3, horizontalMatch.wheresCanMatch[0].MatchOffsets.Count);

		var position = Position.Get(0, 0);
		Assert.AreEqual(true, gameController.GetWheresCanMatch(position, horizontalMatch, GemType.RedGem));
		Assert.AreEqual(true, gameController.GetWheresCanMatch(position, verticalMatch, GemType.RedGem));
		Assert.AreEqual(true, gameController.GetWheresCanMatch(position, sqaureMatch, GemType.RedGem));

		Assert.AreEqual(false, gameController.GetWheresCanMatch(position, horizontalMatch, GemType.GreenGem));
		Assert.AreEqual(false, gameController.GetWheresCanMatch(position, verticalMatch, GemType.PurpleGem));
		Assert.AreEqual(false, gameController.GetWheresCanMatch(position, sqaureMatch, GemType.BlueGem));
	}

	[Test]
	public void Swap() 
	{
		//1. Arrange
		var gameModel = GetGameModel();
		GetGameController(gameModel);

		var gemModels = gameModel.GemModels;
		var normalGemModels = 
			(from GemModel gemModel in gemModels
			where gemModel is EmptyGemModel
			select gemModel).ToList();

		var sampleGemTypes = new GemType[] {
			GemType.YellowGem, 	GemType.GreenGem, 	GemType.RedGem,
			GemType.BlueGem, 	GemType.RedGem, 	GemType.GreenGem,
			GemType.GreenGem, 	GemType.GreenGem, 	GemType.OrangeGem
		};

		var index = 0;
		foreach(var sampleGemType in sampleGemTypes) {
			normalGemModels[index].Type = sampleGemTypes[index];
			index++;
		}

		//2. Act & Assert
		
	}

	[Test]
	public void Match() 
	{

	}

	[Test]
	public void Feed() 
	{

	}

	[Test]
	public void Fall() 
	{
		
	}
}
