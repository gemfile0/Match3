using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

		for(var row = 0; row < rows; row++) {
			for(var col = 0; col < cols; col++) {
				var gemType = (tileModels[row, col].type == TileType.Normal) ? GemType.Empty : GemType.Blocked;
				Debug.Log(row + ", " + col + ": " + gemType);
				gemModels[row, col] = new GemModel(gemType, new Position(col, row));
			}
		}
	}

	public void PutGems() {
		var matchLineModels = Model.matchLineModels;
		var matchingTypes = Model.MatchingTypes;
		
		var emptyGemModels = 
			from GemModel gemModel in Model.GemModels
			where gemModel.Type == GemType.Empty
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

    public Queue<List<GemModel>> Feed() {
		var feedingListQueue = new Queue<List<GemModel>>();

		var matchingTypes = Model.MatchingTypes;
		var gemModels = Model.GemModels;
		var random = new System.Random();

		var blockedGemModels = new List<GemModel>();
		for(var row = 0; row < gemModels.GetLength(0); row++) {
			var feedingList = new List<GemModel>();
			for(var col = 0; col < gemModels.GetLength(1); col++) {
				var gemModel = gemModels[row, col];
				if (gemModel.Type == GemType.Empty) {
					if (!IsFeedingBlocked(row, col)) {
						gemModel.Type = matchingTypes[random.Next(matchingTypes.Count)];
						feedingList.Add(gemModel);
					} else {
						blockedGemModels.Add(gemModel);
					}
				}
			}
			if (feedingList.Count > 0) {
				feedingListQueue.Enqueue(feedingList);
			}
		}

		Model.BlockedGemModels = blockedGemModels;
		
		return feedingListQueue;
    }

	public List<GemModel> DropDiagonally() {
		var droppedGemModels = new List<GemModel>();
		
		var blockedGemModels = Model.BlockedGemModels;
        var colOffsets = new int[]{ 0, -1, 1 };
		foreach(var blockedGemModel in blockedGemModels) {
			while (true) {
				var blockedGemPosition = blockedGemModel.position;
				var failCount = 0;

				foreach(var colOffset in colOffsets) {
					var nearPosition = new Position(blockedGemPosition.index, colOffset, 1);
					if (nearPosition.IsAcceptableIndex() && GetGemModel(nearPosition).Type != GemType.Blocked) {
						droppedGemModels.AddRange(Swap(blockedGemPosition, nearPosition));
					} else {
						failCount++;
					}
				}

				if (failCount == colOffsets.Length) {
					break;
				}
			}
		}

		return droppedGemModels
			.Distinct()
			.Where(gemModel => gemModel.Type != GemType.Empty).ToList();
	}

	bool IsFeedingBlocked(int sourceRow, int sourceCol) {
		var gemModels = Model.GemModels;
		
		while(true) {
			sourceRow += 1;
			var position = new Position(sourceCol, sourceRow);
			
			if (position.IsAcceptableIndex()) {
				if (GetGemModel(position).Type == GemType.Blocked) {
					return true;
				}
			} else {
				break;
			}
		}

		return false;
	}

    public List<GemModel> Fall() {
		var fallingGemModels = new List<GemModel>();
		var blockedGemModels = new List<GemModel>();
		
		var gemModels = Model.GemModels;
		var emptyGemModels = 
			from GemModel gemModel in Model.GemModels
			where gemModel.Type == GemType.Empty
			select gemModel;
        
		// Vertical
		foreach(var emptyGemModel in emptyGemModels) {
			var emptyGemPosition = emptyGemModel.position;
			var nearPosition = new Position(emptyGemPosition.index, 0, 1);

			if (nearPosition.IsAcceptableIndex()) {
				if (GetGemModel(nearPosition).Type != GemType.Blocked) {
					fallingGemModels.AddRange(Swap(emptyGemPosition, nearPosition));
				} else {
					blockedGemModels.Add(emptyGemModel);
				}
			}
		}

		// Diagonal
		var setOfColOffsets = new int[][]{ new int[]{ -1, 1 }, new int[]{ 1, -1} };
		var random = new System.Random();
		foreach(var blockedGemModel in blockedGemModels) {
			var blockedGemPosition = blockedGemModel.position;
			var colOffsets = setOfColOffsets[random.Next(setOfColOffsets.Length)];

			foreach(var colOffset in colOffsets) {
				var nearPosition = new Position(blockedGemPosition.index, colOffset, 1);

				if (nearPosition.IsAcceptableIndex()) {
					var nearGemModel = GetGemModel(nearPosition);

					if (nearGemModel.Type != GemType.Blocked
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
			where gemModel.Type != GemType.Blocked && gemModel.Type != GemType.Empty
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
			var newGemModel = new GemModel(GemType.Empty, matchedGemModel.position);
			gemModels[matchedGemModel.position.row, matchedGemModel.position.col] = newGemModel;
		}

		return matchedGemModels;
	}

	public GemModel GetGemModel(Position position) {
		return Model.GemModels[position.row, position.col];
	}
}
