using System;
using System.Collections.Generic;
using System.Linq;

public class BrokenGemInfo 
{
	public List<GemModel> gemModels;
	public bool hasNext;
}

public class BlockedGemInfo 
{
	public List<GemModel> gemModels;
	public bool hasNext;
}

public class GameController<M>: BaseController<M>
	where M: GameModel 
{
	public void Init() 
	{
		PutGems();
	}

	public void PutGems() 
	{
		var matchLineModels = Model.allwayMatchLineModels;
		var matchingTypes = Model.MatchingTypes;
		
		var emptyGemModels = 
			from GemModel gemModel in Model.GemModels
			where gemModel is EmptyGemModel && gemModel.Type == GemType.EmptyGem
			select gemModel;

		var random = new System.Random();
		foreach (var emptyGemModel in emptyGemModels) 
		{
			var gemsCantPutIn = new List<GemType>();

			foreach (var matchLineModel in matchLineModels) {
				foreach(var matchingType in matchingTypes) {
					if (ExistAnyMatches(emptyGemModel.Position, matchLineModel, matchingType)) {
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

	public List<GemModel> GetAll()
    {
        var allGemModels = 
			from GemModel gemModel in Model.GemModels
			where gemModel is IMovable && gemModel.Type != GemType.EmptyGem
			select gemModel;

		return allGemModels.ToList();
    }

    public void TurnNext()
    {
        Model.currentTurn += 1;
    }

    public bool ExistAnyMatches(Position sourcePosition, MatchLineModel matchLineModel, GemType matchingType) 
	{
		foreach(var whereCanMatch in matchLineModel.wheresCanMatch) 
		{
			var matchCount = 0;
			foreach(var matchOffset in whereCanMatch.matchOffsets) {
				var matchPosition = new Position(sourcePosition.index, matchOffset[0], matchOffset[1]);
				if (IsMovableTile(matchPosition)
					&& (sourcePosition.index == matchPosition.index
						|| GetGemModel(matchPosition).CanMatch(matchingType))) {
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

	public List<GemModel> Swap(Position sourcePosition, Position nearPosition) 
	{
		var swappingGemModels = new List<GemModel>();

		if (nearPosition.IsAcceptableIndex()) 
		{
			var sourceGemModel = GetGemModel(sourcePosition);
			var nearGemModel = GetGemModel(nearPosition);

			nearGemModel.Position = sourcePosition;
			sourceGemModel.Position = nearPosition;

			SetGemModel(nearGemModel);
			SetGemModel(sourceGemModel);
			sourceGemModel.preservedFromMatch = nearGemModel.preservedFromMatch = Model.currentTurn + 5;
			swappingGemModels = new List<GemModel>{ sourceGemModel, nearGemModel };
		} 

		return swappingGemModels;
    }

	public List<MatchedLineInfo> Match() 
	{
		var matchedLineInfos = new List<MatchedLineInfo>();
		
		var gravity = Model.levelModel.gravity;
		var matchLineModels = Model.positiveMatchLineModels;
		var matchableGemModels = 
			from GemModel gemModel in Model.GemModels
			where gemModel is IMovable 
				&& gemModel.Type != GemType.EmptyGem 
				&& IsMovableTile(gemModel.Position) 
				&& Model.currentTurn > gemModel.preservedFromMatch
				&& !IsFalling(gemModel, gravity)
			select gemModel;

		foreach (var matchableGemModel in matchableGemModels) 
		{
			foreach (var matchLineModel in matchLineModels) {
				var matchedGemModels = GetAnyMatches(matchableGemModel, matchLineModel, gravity);
				if (matchedGemModels.Count > 0) {
					var newMatchLineInfo = new MatchedLineInfo() {
						gemModels = matchedGemModels, 
						matchLineModels = new List<MatchLineModel>(){ matchLineModel }
					};

					// Merge if the matched line has intersection.
					foreach (var matchedLineInfo in matchedLineInfos) {
						if (matchedLineInfo.gemModels.Intersect(newMatchLineInfo.gemModels).Any()) {
							matchedLineInfo.Merge(newMatchLineInfo);
							break;
						}
					}
					
					if (!newMatchLineInfo.isMerged) {
						matchedLineInfos.Add(newMatchLineInfo);
					}
				}
			}
		}

		foreach (var matchedLineInfo in matchedLineInfos) 
		{
			matchedLineInfo.matchLineModels.ForEach(matchLineModel => {
				UnityEngine.Debug.Log(matchLineModel.ToString());
			});
			var latestGemModel = matchedLineInfo.gemModels.OrderByDescending(gemModel => gemModel.sequence).FirstOrDefault();

			foreach (var matchedGemModel in matchedLineInfo.gemModels) {
				var newGemType = GemType.EmptyGem;
				if (matchedGemModel == latestGemModel) {
					newGemType = ReadGemType(
						latestGemModel.Type, 
						ReadSpecialKey(matchedLineInfo.matchLineModels, latestGemModel.PositionVector)
					);
				}
				var newGemModel = GemModelFactory.Get(newGemType, matchedGemModel.Position);
				SetGemModel(newGemModel);

				if (newGemType != GemType.EmptyGem) {
					UnityEngine.Debug.Log("newGemModel : " + newGemModel.ToString());
					matchedLineInfo.newAdded = newGemModel;
					newGemModel.preservedFromBreak = Model.currentTurn + 1;
				}
				newGemModel.preservedFromFall = Model.currentTurn;
			}
		}

		return matchedLineInfos;
	}

	public List<GemModel> GetAnyMatches(GemModel sourceGemModel, MatchLineModel matchLineModel, int[] gravity) 
	{
		var matchedGemModels = new List<GemModel>();
		foreach (var whereCanMatch in matchLineModel.wheresCanMatch) 
		{
			foreach (var matchOffset in whereCanMatch.matchOffsets) {
				var matchingPosition = new Position(sourceGemModel.Position.index, matchOffset[0], matchOffset[1]);
				if (!IsMovableTile(matchingPosition)) {
					continue;
				}

				var matchingGemModel = GetGemModel(matchingPosition);
				if (sourceGemModel.CanMatch(matchingGemModel.Type)
					&& Model.currentTurn > matchingGemModel.preservedFromMatch
					&& !IsFalling(matchingGemModel, gravity)
				) {
					matchedGemModels.Add(matchingGemModel);
				} else {
					break;
				}
			}
			
			if (matchedGemModels.Count == whereCanMatch.matchOffsets.Count) {
				break;
			} else {
				matchedGemModels.Clear();
			}
		}

		return matchedGemModels;
	}

	public GemType ReadGemType(GemType baseGemType, string specialKey) 
	{
		GemType gemType = GemType.EmptyGem;

		int specialGemType = 0;
		switch (specialKey) 
		{
			case "SP": 
				gemType = GemType.SuperGem; 
				break;

			case "SQ":
				gemType = GemType.ChocoGem; 
				break;

			case "C":
				specialGemType = 1;
				break;

			case "H":
				specialGemType = 2;
				break;

			case "V":
				specialGemType = 3;
				break;
		}

		if (specialGemType != 0) 
		{
			gemType = (GemType)((int)baseGemType + specialGemType);
		}
		return gemType;
	}

	public string ReadSpecialKey(List<MatchLineModel> matchLineModels, PositionVector positionVector) 
	{
		var specialKey = "";

		var hasVerticalMatch = false;
		var hasHorizontalMatch = false;
		var hasSquareMatch = false;
		var maxMagnitude = 0;
		foreach (var matchLineModel in matchLineModels)
		{
			hasVerticalMatch = hasVerticalMatch || matchLineModel.type == MatchLineType.V;
			hasHorizontalMatch = hasHorizontalMatch || matchLineModel.type == MatchLineType.H;
			hasSquareMatch = hasSquareMatch || matchLineModel.type == MatchLineType.S;
			maxMagnitude = Math.Max(matchLineModel.magnitude, maxMagnitude);
		}

		if (maxMagnitude == 5)
		{
			specialKey = "SP";
		}
		else if (hasVerticalMatch && hasHorizontalMatch) 
		{
			specialKey = "C";
		}
		else if (maxMagnitude == 4)
		{
			specialKey = positionVector.colOffset != 0 ? "H": "V";
		}
		else if (hasSquareMatch)
		{
			specialKey = "SQ";
		}

		return specialKey;
	}

	bool IsFalling(GemModel gemModel, int[] gravity) 
	{
		bool result = false;

		var sourcePosition = gemModel.Position;
		while (true)
		{
			var nearPosition = new Position(sourcePosition.index, gravity[0], gravity[1]);
			if (!nearPosition.IsAcceptableIndex()) {
				break;
			}

			if (GetGemModel(nearPosition).Type == GemType.EmptyGem) {
				result = true;
				break;
			}
			sourcePosition = nearPosition;
		}
		return result;
	}

	public List<GemInfo> Fall() 
	{
		var fallingGemModels = new List<GemModel>();
		var blockedGemModels = new Stack<GemModel>();
		
		var emptyGemModels = 
			from GemModel gemModel in Model.GemModels
			where gemModel.Type == GemType.EmptyGem && Model.currentTurn > gemModel.preservedFromFall
			select gemModel;

		// Only one of them will be executed between a and b.
		// A: Vertical comparing as bottom up
		var gravity = Model.levelModel.gravity;
		foreach (var emptyGemModel in emptyGemModels) 
		{
			var emptyGemPosition = emptyGemModel.Position;
			var nearPosition = new Position(emptyGemPosition.index, gravity[0], -gravity[1]);
			if (!nearPosition.IsAcceptableIndex()) { continue; }
			
			var nearGemModel = GetGemModel(nearPosition);
			if (Model.currentTurn <= nearGemModel.preservedFromMatch) { continue; }

			if (nearGemModel is IMovable) {
				fallingGemModels.AddRange(Swap(emptyGemPosition, nearPosition));
			} else {
				blockedGemModels.Push(emptyGemModel);
			}
		}

		// B: Diagonal comparing as bottom up
		var setOfColOffsets = new int[][]{ new int[]{ -1, 1 }, new int[]{ 1, -1 } };
		var random = new System.Random();
		foreach (var blockedGemModel in blockedGemModels) 
		{
			var blockedGemPosition = blockedGemModel.Position;
			var colOffsets = setOfColOffsets[random.Next(setOfColOffsets.Length)];

			foreach (var colOffset in colOffsets) {
				var nearPosition = new Position(blockedGemPosition.index, colOffset, -gravity[1]);

				if (!nearPosition.IsAcceptableIndex()) { continue; }

				var nearGemModel = GetGemModel(nearPosition);
				if (IsMovableTile(nearPosition)
					&& !fallingGemModels.Contains(nearGemModel)) {
					fallingGemModels.AddRange(Swap(blockedGemPosition, nearPosition));
					break;
				}
			}
		}

		return fallingGemModels
			.Where(gemModel => gemModel.Type != GemType.EmptyGem)
			.Select(fallingGemModel => {
				return new GemInfo {
					position = fallingGemModel.Position, 
					id = fallingGemModel.id,
					endOfFall = !IsFalling(fallingGemModel, gravity)
				};
			})
			.ToList();
    }

	public List<GemModel> Feed()
	{
		var feedingGemModels = new List<GemModel>();
		
		var matchingTypes = Model.MatchingTypes;

		var random = new System.Random();
		var spawnerGemModels = 
			from GemModel gemModel in Model.GemModels
			where gemModel is SpawnerGemModel
			select gemModel;

		var gravity = Model.levelModel.gravity;
		foreach (SpawnerGemModel spawnerGemModel in spawnerGemModels)
		{
			var spawneePosition = new Position(spawnerGemModel.Position.index, gravity[0], gravity[1]);
			if (GetGemModel(spawneePosition).Type == GemType.EmptyGem) {
				var spawneeGemModel = GemModelFactory.Get(GemType.EmptyGem, spawneePosition);
				SetGemModel(spawneeGemModel);
				spawneeGemModel.Type = matchingTypes[random.Next(matchingTypes.Count)];
				feedingGemModels.Add(spawneeGemModel);
			}
		}

		return feedingGemModels;
    }

	public BlockedGemInfo MarkAsBlock(Position sourcePosition, Position nearPosition, Int64 markerID)
    {
		var blockedGemInfo = new BlockedGemInfo {
			gemModels = new List<GemModel>()
		};
		
		var sourceGemModel = GetGemModel(sourcePosition);
		if (IsMovableTile(sourcePosition)
			&& sourceGemModel is IMovable 
			&& sourceGemModel.Type != GemType.EmptyGem 
			&& Model.currentTurn >= sourceGemModel.preservedFromBreak) 
		{
			blockedGemInfo.gemModels.Add(CopyAsBlock(markerID, sourceGemModel));
		}

		blockedGemInfo.hasNext = IsMovableTile(nearPosition);
		
        return blockedGemInfo;
    }

	public BlockedGemInfo MarkSameGemsAsBlock(Position sourcePosition, GemType gemType, Int64 markerID)
    {
		var blockedGemInfo = new BlockedGemInfo {
			gemModels = new List<GemModel>{ CopyAsBlock(markerID, GetGemModel(sourcePosition)) }
		};
		
		var sameGemModels = 
			from GemModel gemModel in Model.GemModels
			where IsMovableTile(gemModel.Position)
				&& gemModel is IMovable 
				&& gemModel.CanMatch(gemType)
				&& Model.currentTurn >= gemModel.preservedFromBreak
			select gemModel;

		foreach (var gemModel in sameGemModels)
		{
			blockedGemInfo.gemModels.Add(CopyAsBlock(markerID, gemModel));
		}
		
        return blockedGemInfo;
    }

	GemModel CopyAsBlock(Int64 markerID, GemModel targetGemModel)
	{
		var copiedGemModel = GemModelFactory.Get(GemType.BlockedGem, targetGemModel.Position);
		copiedGemModel.id = targetGemModel.id;
		copiedGemModel.markedBy = markerID;
		copiedGemModel.Type = targetGemModel.Type;
		copiedGemModel.specialKey = targetGemModel.specialKey;
		copiedGemModel.endurance = targetGemModel.endurance;
		// copiedGemModel.deadline = targetGemModel.deadline;
		// copiedGemModel.preservedUntil = targetGemModel.preservedUntil;
		SetGemModel(copiedGemModel);
		return copiedGemModel;
	}

	public BrokenGemInfo Break(Position targetPosition, Int64 markerID, int repeat) 
	{
		BrokenGemInfo brokenGemInfo = new BrokenGemInfo();

		if (IsMovableTile(targetPosition) && repeat > 0) 
		{
			brokenGemInfo.hasNext = true;
			
			var targetGemModel = GetGemModel(targetPosition);
			if (targetGemModel.markedBy == markerID) {
				var newGemModel = GemModelFactory.Get(GemType.EmptyGem, targetGemModel.Position);
				SetGemModel(newGemModel);

				brokenGemInfo.gemModels = new List<GemModel>{ targetGemModel };
			}
		}

		return brokenGemInfo;
	}

	public GemModel GetGemModel(Position position) 
	{
		return Model.GemModels[position.row, position.col];
	}

	public void SetGemModel(GemModel gemModel) 
	{
		Model.GemModels[gemModel.Position.row, gemModel.Position.col] = gemModel;
	}

	public bool IsMovableTile(Position position) 
	{
		return position.IsAcceptableIndex() 
			&& Model.TileModels[position.row, position.col].type == TileType.Movable;
	}
}
