using NUnit.Framework;
using System.Linq;

public class TestGameController {
	GameModel GetGameModel(int cols = 5, int rows = 5) {
		var gameModel = new GameModel();
		gameModel.rows = rows;
		gameModel.cols = cols;
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
		//2. Act
		GetGameController(gameModel).MakeField();
		//3. Assert
		Assert.AreEqual(25, gameModel.gemModels.Count);
		Assert.AreEqual(25, gameModel.gemModels.Values.Count(gemModel => gemModel.Type == GemType.Empty));
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
		Assert.AreEqual(25, gameModel.gemModels.Count);
		Assert.AreEqual(0, gameModel.gemModels.Values.Count(gemModel => gemModel.Type == GemType.Empty));
	}

	[Test]
	public void ExistAnyMatches() {
		//1. Arrange
		var gameModel = GetGameModel(3, 3);
		var gameController = GetGameController(gameModel);
		gameController.SetSize();
		gameController.MakeField();

		var emtpyGems = gameModel.gemModels
				.Where(gemModel => gemModel.Value.Type == GemType.Empty)
				.ToDictionary(p => p.Key, p => p.Value);
			
		//2. Act
		emtpyGems[1].Type = GemType.RedGem;
		emtpyGems[2].Type = GemType.RedGem;
		
		emtpyGems[3].Type = GemType.RedGem;
		emtpyGems[4].Type = GemType.RedGem;
		emtpyGems[5].Type = GemType.GreenGem;

		emtpyGems[6].Type = GemType.RedGem;
		emtpyGems[7].Type = GemType.GreenGem;
		emtpyGems[8].Type = GemType.GreenGem;

		//3. Assert
		Assert.AreEqual(GemType.Empty, gameController.GetGemModel(0).Type);
		Assert.AreEqual(GemType.RedGem, gameController.GetGemModel(1).Type);

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
