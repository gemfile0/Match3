using NUnit.Framework;
using System.Linq;

public class TestGameController {
	GameModel GetGameModel(int cols = 5, int rows = 5) {
		var gameModel = new GameModel();
		gameModel.levelModel = new LevelModel() { 
			moves = 10,
			cols = 5,
			rows = 7,
			tiles = new int[] {
				0, 1, 1, 1, 0,
				0, 1, 1, 1, 0,
				0, 1, 1, 1, 0,
				1, 1, 1, 1, 1,
				1, 1, 1, 1, 1,
				1, 1, 1, 1, 1,
				1, 1, 1, 1, 1
			}
		};
		return gameModel;
	}

	GameController<GameModel> GetGameController(GameModel gameModel) {
		var gameController = new GameController<GameModel>();
		gameController.Setup(gameModel);
		return gameController;
	}

	[Test]
	public void MakeField() {
		//1. Arrange
		var gameModel = GetGameModel();
		var gameController = GetGameController(gameModel);
		//2. Act
		gameController.MakeField();
		//3. Assert
		var gemModels = gameModel.GemModels;
		Assert.AreEqual(25, gemModels.GetLength(0) * gemModels.GetLength(1));

		var emtpyGemModels = 
			from GemModel gemModel in gemModels
			where gemModel.Type == GemType.Empty
			select gemModel;
		Assert.AreEqual(25, emtpyGemModels.Count());
	}

	[Test]
	public void PutGems() {
		//1. Arrange
		var gameModel = GetGameModel(5, 5);
		var gameController = GetGameController(gameModel);
		
		//2. Act
		gameController.MakeField();
		gameController.PutGems();

		//3. Assert
		var gemModels = gameModel.GemModels;
		Assert.AreEqual(25, gemModels.GetLength(0) * gemModels.GetLength(1));
		
		var emtpyGemModels = 
			from GemModel gemModel in gemModels
			where gemModel.Type == GemType.Empty
			select gemModel;
		Assert.AreEqual(0, emtpyGemModels.Count());
	}

	[Test]
	public void ExistAnyMatches() {
		//1. Arrange
		var gameModel = GetGameModel(3, 3);
		var gameController = GetGameController(gameModel);
		gameController.SetSize();
		gameController.MakeField();

		var gemModels = gameModel.GemModels;
		var emtpyGemModels = 
			(from GemModel gemModel in gemModels
			where gemModel.Type == GemType.Empty
			select gemModel).ToList();

		//2. Act
		emtpyGemModels[1].Type = GemType.RedGem;
		emtpyGemModels[2].Type = GemType.RedGem;
		
		emtpyGemModels[3].Type = GemType.RedGem;
		emtpyGemModels[4].Type = GemType.RedGem;
		emtpyGemModels[5].Type = GemType.GreenGem;

		emtpyGemModels[6].Type = GemType.RedGem;
		emtpyGemModels[7].Type = GemType.GreenGem;
		emtpyGemModels[8].Type = GemType.GreenGem;

		//3. Assert
		Assert.AreEqual(GemType.Empty, gameController.GetGemModel(new Position(0, 0)).Type);
		Assert.AreEqual(GemType.RedGem, gameController.GetGemModel(new Position(0, 1)).Type);

		//1. Arrange
		var horizontalMatch = new MatchLineModel(3, 1);
		var verticalMatch = new MatchLineModel(1, 3);
		var sqaureMatch = new MatchLineModel(2, 2);

		//2. Act & Assert
		Assert.AreEqual(3, horizontalMatch.wheresCanMatch[0].matchOffsets.Count);

		Assert.AreEqual(true, gameController.ExistAnyMatches(0, horizontalMatch, GemType.RedGem));
		Assert.AreEqual(true, gameController.ExistAnyMatches(0, verticalMatch, GemType.RedGem));
		Assert.AreEqual(true, gameController.ExistAnyMatches(0, sqaureMatch, GemType.RedGem));

		Assert.AreEqual(false, gameController.ExistAnyMatches(0, horizontalMatch, GemType.GreenGem));
		Assert.AreEqual(false, gameController.ExistAnyMatches(0, verticalMatch, GemType.PurpleGem));
		Assert.AreEqual(false, gameController.ExistAnyMatches(0, sqaureMatch, GemType.BlueGem));
	}
}
