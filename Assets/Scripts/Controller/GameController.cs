using System;
using System.Collections.Generic;
using System.Linq;

public class CrunchedGemInfo 
{
	public List<GemModel> gemModels;
	public bool hasNext;
}

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

    internal List<GemModel> GetAll()
    {
        var allGemModels = 
			from GemModel gemModel in Model.GemModels
			where gemModel is IMovable && gemModel.Type != GemType.EmptyGem
			select gemModel;

		return allGemModels.ToList();
    }

    internal void TurnNext()
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
				if (matchPosition.IsBoundaryIndex() 
					&& (sourcePosition.index == matchPosition.index
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

	public List<GemModel> Swap(Position sourcePosition, Position nearPosition) 
	{
		var swappingGemModels = new List<GemModel>();

		if (nearPosition.IsMovableIndex()) 
		{
			var sourceGemModel = GetGemModel(sourcePosition);
			var nearGemModel = GetGemModel(nearPosition);

			nearGemModel.Position = sourcePosition;
			sourceGemModel.Position = nearPosition;

			SetGemModel(nearGemModel);
			SetGemModel(sourceGemModel);

			swappingGemModels = new List<GemModel>{ sourceGemModel, nearGemModel };
		} 

		return swappingGemModels;
    }

    internal BlockedGemInfo MarkAsBlock(Position sourcePosition, Position nearPosition, Int64 markerID)
    {
		var blockedGemInfo = new BlockedGemInfo {
			gemModels = new List<GemModel>()
		};
		
		var sourceGemModel = GetGemModel(sourcePosition);

		if (sourceGemModel.Type != GemType.EmptyGem 
			&& !(sourceGemModel is IBlockable) 
			&& sourceGemModel.deadline <= Model.currentTurn) 
		{
			blockedGemInfo.gemModels.Add(CopyAsBlock(markerID, sourceGemModel));
		}

		blockedGemInfo.hasNext = nearPosition.IsBoundaryIndex();
		
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
		SetGemModel(copiedGemModel);
		return copiedGemModel;
	}

	internal BrokenGemInfo Break(Position targetPosition, Int64 markerID, int repeat) 
	{
		BrokenGemInfo brokenGemInfo = new BrokenGemInfo();

		if (targetPosition.IsBoundaryIndex() && repeat > 0) 
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
	
	public List<GemModel> StopFeed() 
	{
		var stoppingGemModels = new List<GemModel>();
		
		var spawnerGemModels = 
			from GemModel gemModel in Model.GemModels
			where gemModel is SpawnerGemModel
			select gemModel;

		foreach (SpawnerGemModel spawnerGemModel in spawnerGemModels) 
		{
			var spawnTo = spawnerGemModel.spawnTo;
			var spawneePosition = new Position(spawnerGemModel.Position.index, spawnTo[0], spawnTo[1]);
			if (GetGemModel(spawneePosition).Type != GemType.EmptyGem) {
				var spawneeGemModel = GemModelFactory.Get(GemType.SpawneeGem, spawneePosition);
				SetGemModel(spawneeGemModel);
				stoppingGemModels.Add(spawneeGemModel);
			}
		}

		return stoppingGemModels;
	}

    public List<GemInfo> Fall() 
	{
		var fallingGemModels = new List<GemModel>();
		var blockedGemModels = new Stack<GemModel>();
		
		var emptyGemModels = 
			from GemModel gemModel in Model.GemModels
			where gemModel.Type == GemType.EmptyGem
			select gemModel;

		// Only one of them will be executed between a and b.
		// A: Vertical comparing as bottom up
		var gravity = Model.levelModel.gravity;
		foreach (var emptyGemModel in emptyGemModels) 
		{
			var emptyGemPosition = emptyGemModel.Position;
			var nearPosition = new Position(emptyGemPosition.index, gravity[0], -gravity[1]);
			if (nearPosition.IsMovableIndex()) {
				if (GetGemModel(nearPosition) is IMovable) {
					fallingGemModels.AddRange(Swap(emptyGemPosition, nearPosition));
				} else {
					blockedGemModels.Push(emptyGemModel);
				}
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

				if (nearPosition.IsMovableIndex()) {
					var nearGemModel = GetGemModel(nearPosition);

					if (nearGemModel is IMovable
						&& !fallingGemModels.Contains(nearGemModel)) {
						fallingGemModels.AddRange(Swap(blockedGemPosition, nearPosition));
						break;
					}
				}
			}
		}

		return fallingGemModels
			.Where(gemModel => gemModel.Type != GemType.EmptyGem)
			.Select(fallingGemModel => {
				fallingGemModel.IsFalling = true;
				return new GemInfo {
					position = fallingGemModel.Position, 
					id = fallingGemModel.id 
				};
			})
			.ToList();
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
				&& gemModel.Position.IsBoundaryIndex() 
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
					newGemModel.deadline = Model.currentTurn + 1;
				}
			}
		}

		return matchedLineInfos;
	}

	bool IsFalling(GemModel gemModel, int[] gravity) 
	{
		bool result = false;

		var sourcePosition = gemModel.Position;
		while (true) 
		{
			var nearPosition = new Position(sourcePosition.index, gravity[0], gravity[1]);
			if (!nearPosition.IsBoundaryIndex()) {
				break;
			}

			if (GetGemModel(nearPosition).Type == GemType.EmptyGem) {
				result = true;
				break;
			}
			sourcePosition = nearPosition;
		}
		gemModel.IsFalling = result;
		return result;
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

		switch (matchLineModels[0].magnitude) 
		{
			case 5: specialKey = "SP"; break;
			case 4: specialKey = positionVector.colOffset != 0 ? "H": "V"; break;
			case 2: specialKey = "SQ"; break;
		}

		if (matchLineModels.Count > 1) 
		{
			var matchLineModelA = matchLineModels[0].type;
			var matchLineModelB = matchLineModels[1].type;
			if (matchLineModelA == MatchLineType.V && matchLineModelB == MatchLineType.H
				|| matchLineModelA == MatchLineType.H && matchLineModelB == MatchLineType.V) {
				specialKey = "C";
			} else if (matchLineModels[0].magnitude == 2 || matchLineModels[1].magnitude == 2) {
				specialKey = "SQ";
			}
		}

		return specialKey;
	}

	public List<GemModel> GetAnyMatches(GemModel sourceGemModel, MatchLineModel matchLineModel, int[] gravity) 
	{
		var matchedGemModels = new List<GemModel>();
		foreach (var whereCanMatch in matchLineModel.wheresCanMatch) 
		{
			foreach (var matchOffset in whereCanMatch.matchOffsets) {
				var matchingPosition = new Position(sourceGemModel.Position.index, matchOffset[0], matchOffset[1]);
				if (matchingPosition.IsBoundaryIndex()
					&& sourceGemModel.Type != GemType.ChocoGem
					&& sourceGemModel.Type == GetGemModel(matchingPosition).Type 
					&& !IsFalling(GetGemModel(matchingPosition), gravity)) {
					matchedGemModels.Add(GetGemModel(matchingPosition));
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

	public GemModel GetGemModel(Position position) 
	{
		return Model.GemModels[position.row, position.col];
	}

	public void SetGemModel(GemModel gemModel) 
	{
		Model.GemModels[gemModel.Position.row, gemModel.Position.col] = gemModel;
	}
}
