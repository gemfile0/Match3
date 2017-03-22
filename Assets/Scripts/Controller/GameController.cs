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
		Position.Cols = Model.cols;
		Position.Rows = Model.rows;
	}

	public void Update() {
		
	}

	public void MakeField() {
		var rows = Model.rows;
		var cols = Model.rows;
		
		var gemModels = new Dictionary<int, GemModel>();

		var count = 0;
		for(var row = 0; row < rows; row++) {
			for(var col = 0; col < cols; col++) {
				gemModels.Add(count, new GemModel(GemType.Empty, new Position(count)));
				count++;
			}
		}

		Model.gemModels = gemModels;
	}

	public void PutGems() {
		var matchingTypes = new List<GemType> {
			GemType.RedGem, GemType.GreenGem, GemType.BlueGem, 
			GemType.CyanGem, GemType.PurpleGem, GemType.YellowGem
		};
		
		var emtpyGems = Model.gemModels
				.Where(gemModel => gemModel.Value.Type == GemType.Empty)
				.ToDictionary(p => p.Key, p => p.Value);
		var matchLineModels = Model.matchLineModels;

		var random = new System.Random();
		emtpyGems.ForEach(gemModel => {
			var sourceIndex = gemModel.Key;
			var gemsCantPutIn = new List<GemType>();
			matchLineModels.ForEach(matchLineModel => {
				matchingTypes.ForEach(matchingType => {
					if (ExistAnyMatches(sourceIndex, matchLineModel, matchingType)) {
						gemsCantPutIn.Add(matchingType);
					}
				});
			});

			if (gemsCantPutIn.Count == matchingTypes.Count) {
				throw new InvalidOperationException("That should not happen.");
			}

			var gemsCanPutIn = matchingTypes.Except(gemsCantPutIn).ToList();

			int index = random.Next(gemsCanPutIn.Count);
			var gemTypeChoosen = gemsCanPutIn[index];
			gemModel.Value.Type = gemTypeChoosen;
		});
	}

    public bool ExistAnyMatches(int sourceIndex, MatchLineModel matchLineModel, GemType matchingType) {
		foreach(var whereCanMatch in matchLineModel.wheresCanMatch) {
			var matchCount = 0;
			foreach(var matchOffset in whereCanMatch.matchOffsets) {
				var matchPosition = new Position(sourceIndex, matchOffset[0], matchOffset[1]);
				if (matchPosition.IsAcceptableIndex() 
					&& (sourceIndex == matchPosition.index
						|| matchingType == GetGemModel(matchPosition.index).Type)) {
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
		var gemModels = Model.gemModels;
		var swappingGemModels = new List<GemModel>();
		if (nearPosition.IsAcceptableIndex()) {
			var sourceGemModel = GetGemModel(sourcePosition.index);
			var nearGemModel = GetGemModel(nearPosition.index);
			nearGemModel.position = sourcePosition;
			sourceGemModel.position = nearPosition;

			gemModels[nearGemModel.position.index] = nearGemModel;
			gemModels[sourceGemModel.position.index] = sourceGemModel;

			swappingGemModels = new List<GemModel>{ sourceGemModel, nearGemModel };
		}

		return swappingGemModels;
    }

	public List<GemModel> Match() {
		var gemModels = Model.gemModels;
		var matchedGemModels = new List<GemModel>();
		Model.gemModels.Values.ForEach(gemModel => {
			Model.matchLineModels.ForEach(matchLineModel => {
				if (ExistAnyMatches(gemModel.position.index, matchLineModel, gemModel.Type)) {
					matchedGemModels.Add(gemModel);
				}
			});
		});

		matchedGemModels.ForEach(matchedGemModel => {
			gemModels[matchedGemModel.position.index] = new GemModel(GemType.Empty, matchedGemModel.position);
		});
		return matchedGemModels;
	}

	public GemModel GetGemModel(int index) {
		if (index >= 0 && index < Model.gemModels.Count) {
			return Model.gemModels[index];
		} else {
			return null;
		}
	}
}
