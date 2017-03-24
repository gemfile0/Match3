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
		Position.Cols = Model.cols;
		Position.Rows = Model.rows;
	}

	public void Update() {
		
	}

	public void MakeField() {
		var rows = Model.rows;
		var cols = Model.cols;
		var gemModels = Model.GemModels = new GemModel[rows, cols];

		for(var row = 0; row < rows; row++) {
			for(var col = 0; col < cols; col++) {
				gemModels[row, col] = new GemModel(GemType.Empty, new Position(col, row));
			}
		}
	}

	public void PutGems() {
		var matchLineModels = Model.matchLineModels;
		var matchingTypes = Model.MatchingTypes;
		Debug.Log("PutGems : " + matchingTypes.Count);
		
		var emtpyGemModels = 
			from GemModel gemModel in Model.GemModels
			where gemModel.Type == GemType.Empty
			select gemModel;


		var random = new System.Random();
		foreach(var emptyGemModel in emtpyGemModels) {
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

    internal Queue<List<GemModel>> Feed() {
		var feedingListQueue = new Queue<List<GemModel>>();

		var matchingTypes = Model.MatchingTypes;
		var gemModels = Model.GemModels;
		var random = new System.Random();

		for(var row = 0; row < gemModels.GetLength(0); row++) {
			var feedingList = new List<GemModel>();
			for(var col = 0; col < gemModels.GetLength(1); col++) {
				var gemModel = gemModels[row, col];
				if (gemModel.Type == GemType.Empty) {
					gemModel.Type = matchingTypes[random.Next(matchingTypes.Count)];

					feedingList.Add(gemModel);
				}
			}
			if (feedingList.Count > 0) {
				feedingListQueue.Enqueue(feedingList);
			}
		}
		
		return feedingListQueue;
    }

    public List<GemModel> Drop() {
		var droppedGemModels = new List<GemModel>();
		
		var gemModels = Model.GemModels;
		var emptyGemModels = Model.EmtpyGemModels;
        
		foreach(var emptyGemModel in emptyGemModels) {
			while (true) {
				var emptyGemPosition = emptyGemModel.position;
				var nearPosition = new Position(emptyGemPosition.index, 0, 1);
				if (!nearPosition.IsAcceptableIndex()) {
					break;
				}

				droppedGemModels.AddRange(Swap(emptyGemPosition, nearPosition));
			}
		}

		return droppedGemModels
			.Distinct()
			.Where(gemModel => gemModel.Type != GemType.Empty).ToList();
    }

    public List<GemModel> Match() {
		var matchedGemModels = new List<GemModel>();
		var matchedLineModels = new List<MatchLineModel>();
		
		var gemModels = Model.GemModels;
		var emptyGemModels = Model.EmtpyGemModels;
		var matchLineModels = Model.matchLineModels;

		foreach(var gemModel in gemModels) {
			foreach(var matchLineModel in matchLineModels) {
				if (ExistAnyMatches(gemModel.position.index, matchLineModel, gemModel.Type)) {
					matchedGemModels.Add(gemModel);
					matchedLineModels.Add(matchLineModel);
				}
			}
		}
		
		matchedGemModels = matchedGemModels.Distinct().ToList();
		emptyGemModels = new List<GemModel>();
		foreach(var matchedGemModel in matchedGemModels) {
			// Debug.Log("matchedGemModel : " + matchedGemModel.position.index + ", " + matchedGemModel.name);
			var newGemModel = new GemModel(GemType.Empty, matchedGemModel.position);
			gemModels[matchedGemModel.position.row, matchedGemModel.position.col] = newGemModel;
			emptyGemModels.Add(newGemModel);
		}

		Model.EmtpyGemModels = emptyGemModels;
		return matchedGemModels;
	}

	public GemModel GetGemModel(Position position) {
		return Model.GemModels[position.row, position.col];
	}
}
