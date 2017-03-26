using System;
using System.Collections.Generic;
using System.Linq;

public class GameController<M>: BaseController<M>
	where M: GameModel {
		
	public void Init() {
		SetSize();
		MakeField();
		PutGems();
	}

	public void SetSize() {
		Position.Cols = Model.Cols;
		Position.Rows = Model.Rows;
	}

	public void Update() {
		
	}

	public void MakeField() {
		var rows = Model.Rows;
		var cols = Model.Cols;
		var gemModels = Model.GemModels = new GemModel[rows, cols];
		var tileModels = Model.tileModels;

		for (var row = 0; row < rows; row++) {
			for (var col = 0; col < cols; col++) {
				gemModels[row, col] = GemModelFactory.Get(tileModels[row, col].type, new Position(col, row));
			}
		}
	}

	public void PutGems() {
		var matchLineModels = Model.matchLineModels;
		var matchingTypes = Model.MatchingTypes;
		
		var emptyGemModels = 
			from GemModel gemModel in Model.GemModels
			where gemModel is NormalGemModel
			select gemModel;

		var random = new System.Random();
		foreach(var emptyGemModel in emptyGemModels) {
			var gemsCantPutIn = new List<GemType>();

			foreach(var matchLineModel in matchLineModels) {
				foreach(var matchingType in matchingTypes) {
					if (ExistAnyMatches(emptyGemModel.position.index, matchLineModel, matchingType)) {
						gemsCantPutIn.Add(matchingType);
					}
				}
			}

			if (gemsCantPutIn.Count == matchingTypes.Count) {
				throw new InvalidOperationException("That should not happen.");
			}

			var gemsCanPutIn = matchingTypes.Except(gemsCantPutIn).ToList();
			emptyGemModel.Type = gemsCanPutIn[random.Next(gemsCanPutIn.Count)];
		}
	}

    public bool ExistAnyMatches(int sourceIndex, MatchLineModel matchLineModel, GemType matchingType) {
		foreach(var whereCanMatch in matchLineModel.wheresCanMatch) {
			var matchCount = 0;
			foreach(var matchOffset in whereCanMatch.matchOffsets) {
				var matchPosition = new Position(sourceIndex, matchOffset[0], matchOffset[1]);
				if (matchPosition.IsAcceptableIndex() 
					&& (sourceIndex == matchPosition.index
						|| matchingType == GetGemModel(matchPosition).Type)) {
					matchCount++;
				} else {
					break;
				}
			}
			
			if (matchCount == whereCanMatch.matchOffsets.Count) {
				return true;
			}
		}

		return false;
	}

	public List<GemModel> Swap(Position sourcePosition, Position nearPosition) {
		var gemModels = Model.GemModels;
		var swappingGemModels = new List<GemModel>();
		if (nearPosition.IsAcceptableIndex()) {
			var sourceGemModel = GetGemModel(sourcePosition);
			var nearGemModel = GetGemModel(nearPosition);

			nearGemModel.position = sourcePosition;
			sourceGemModel.position = nearPosition;

			gemModels[nearGemModel.position.row, nearGemModel.position.col] = nearGemModel;
			gemModels[sourceGemModel.position.row, sourceGemModel.position.col] = sourceGemModel;

			swappingGemModels = new List<GemModel>{ sourceGemModel, nearGemModel };
		}

		return swappingGemModels;
    }

    public List<GemModel> Feed() {
		var feedingGemModels = new List<GemModel>();
		
		var gemModels = Model.GemModels;
		var matchingTypes = Model.MatchingTypes;

		var random = new System.Random();
		var spawnerGemModels = 
			from GemModel gemModel in Model.GemModels
			where gemModel is SpawnerGemModel
			select gemModel;

		foreach(SpawnerGemModel spawnerGemModel in spawnerGemModels) {
			var spawnTo = spawnerGemModel.spawnTo;
			var spawneePosition = new Position(spawnerGemModel.position.index, spawnTo[0], spawnTo[1]);
			if (GetGemModel(spawneePosition).Type == GemType.Empty) {
				var spawneeGemModel 
					= gemModels[spawneePosition.row, spawneePosition.col] 
					= GemModelFactory.Get(TileType.Normal, spawneePosition);

				spawneeGemModel.Type = matchingTypes[random.Next(matchingTypes.Count)];
				feedingGemModels.Add(spawneeGemModel);
			}
		}

		return feedingGemModels;
    }

    public List<GemModel> Fall() {
		var fallingGemModels = new List<GemModel>();
		var blockedGemModels = new List<GemModel>();
		
		var gemModels = Model.GemModels;
		var emptyGemModels = 
			from GemModel gemModel in Model.GemModels
			where gemModel.Type == GemType.Empty
			select gemModel;
        
		// Only one of them will be executed between a and b.
		// A: Vertical comparing as bottom up
		foreach(var emptyGemModel in emptyGemModels) {
			var emptyGemPosition = emptyGemModel.position;
			var nearPosition = new Position(emptyGemPosition.index, 0, 1);

			if (nearPosition.IsAcceptableIndex()) {
				if (GetGemModel(nearPosition) is IMovable) {
					fallingGemModels.AddRange(Swap(emptyGemPosition, nearPosition));
				} else {
					blockedGemModels.Add(emptyGemModel);
				}
			}
		}

		// B: Diagonal comparing as bottom up
		var setOfColOffsets = new int[][]{ new int[]{ -1, 1 }, new int[]{ 1, -1} };
		var random = new System.Random();
		foreach(var blockedGemModel in blockedGemModels) {
			var blockedGemPosition = blockedGemModel.position;
			var colOffsets = setOfColOffsets[random.Next(setOfColOffsets.Length)];

			foreach(var colOffset in colOffsets) {
				var nearPosition = new Position(blockedGemPosition.index, colOffset, 1);

				if (nearPosition.IsAcceptableIndex()) {
					var nearGemModel = GetGemModel(nearPosition);

					if (nearGemModel is IMovable
						&& !fallingGemModels.Contains(nearGemModel)) {
						fallingGemModels.AddRange(Swap(blockedGemPosition, nearPosition));
					}
				}
			}
		}

		return fallingGemModels.Where(gemModel => gemModel.Type != GemType.Empty).ToList();
    }

    public List<GemModel> Match() {
		var matchedGemModels = new List<GemModel>();
		var matchedLineModels = new List<MatchLineModel>();
		
		var gemModels = Model.GemModels;
		var matchLineModels = Model.matchLineModels;

		var matchableGemModels = 
			from GemModel gemModel in Model.GemModels
			where gemModel is IMovable && gemModel.Type != GemType.Empty
			select gemModel;

		foreach(var matchbleGemModel in matchableGemModels) {
			foreach(var matchLineModel in matchLineModels) {
				if (ExistAnyMatches(matchbleGemModel.position.index, matchLineModel, matchbleGemModel.Type)) {
					matchedGemModels.Add(matchbleGemModel);
					matchedLineModels.Add(matchLineModel);
				}
			}
		}
		
		matchedGemModels = matchedGemModels.Distinct().ToList();
		foreach(var matchedGemModel in matchedGemModels) {
			var newGemModel = GemModelFactory.Get(TileType.Normal, matchedGemModel.position);
			gemModels[matchedGemModel.position.row, matchedGemModel.position.col] = newGemModel;
		}

		return matchedGemModels;
	}

	public GemModel GetGemModel(Position position) {
		return Model.GemModels[position.row, position.col];
	}
}
