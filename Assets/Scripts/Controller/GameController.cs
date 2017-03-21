using System;
using System.Collections.Generic;

public class GameController<M>: BaseController<M>
	where M: GameModel {
	public void Set() {
		Position.Cols = Model.rows;
		Position.Cols = Model.rows;
		MakeGems();
	}

	void MakeGems() {
		var rows = Model.rows;
		var cols = Model.rows;
		var random = new Random();
		var gemNames = new List<string> {"RedGem", "BlueGem", "GreenGem", "PurpleGem", "CyanGem", "YellowGem"};
		var gemModels = new Dictionary<int, GemModel>();

		var count = 0;
		for(var row = 0; row < rows; row++) {
			for(var col = 0; col < cols; col++) {
				int index = random.Next(gemNames.Count);
				gemModels.Add(count, new GemModel(gemNames[index], new Position(count)));
				count++;
			}
		}

		Model.gemModels = gemModels;
	}
}
