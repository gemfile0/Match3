using System;
using System.Collections.Generic;
using System.Linq;

public struct BrokenGemInfo 
{
	public GemModel gemModel;
	public List<GemModel> gemModels;
	public bool isNextMovable;
}

public struct BlockedGemInfo 
{
	public GemModel gemModel;
	public List<GemModel> gemModels;
	public bool isNextMovable;
}

public struct ReplacedGemInfo 
{
	public GemModel blockedGemModel;
	public List<GemModel> gemModels;
}

public struct MergedGemInfo
{
	public GemModel mergee;
	public GemModel merger;
}

public class GameController<M>: BaseController<M>
	where M: GameModel 
{
	readonly int[][] SET_OF_RANDOM_DIRECTIONS;
	readonly Random RANDOM;

	public GameController()
	{
		SET_OF_RANDOM_DIRECTIONS = new int[][]{ new int[]{ -1, 1 }, new int[]{ 1, -1 } };
		RANDOM = new Random();
	}
	
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
			emptyGemModel.Type = gemsCanPutIn[RANDOM.Next(gemsCanPutIn.Count)];
		}
	}

	public IEnumerable<GemModel> GetAll()
    {
        var allGemModels = 
			from GemModel gemModel in Model.GemModels
			where gemModel is IMovable && gemModel.Type != GemType.EmptyGem
			select gemModel;

		return allGemModels;
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
			sourceGemModel.preservedFromMatch = nearGemModel.preservedFromMatch 
				= Model.currentTurn + 5;
			swappingGemModels.Add(sourceGemModel);
			swappingGemModels.Add(nearGemModel);
		} 

		return swappingGemModels;
    }

	public MergedGemInfo Merge(Position sourcePosition, Position nearPosition)
	{
		var mergedGemInfo = new MergedGemInfo();

		if (nearPosition.IsAcceptableIndex()) 
		{
			var sourceGemModel = GetGemModel(sourcePosition);
			var nearGemModel = GetGemModel(nearPosition);

			nearGemModel.specialKey = MergeSpecialKey(nearGemModel, sourceGemModel);
			nearGemModel.endurance = MergeEndurance(nearGemModel, sourceGemModel);
			nearGemModel.positionBefore = sourcePosition;

			sourceGemModel.preservedFromMatch = nearGemModel.preservedFromMatch = Model.currentTurn + 5;

			mergedGemInfo.merger = nearGemModel;
			mergedGemInfo.mergee = sourceGemModel;
		} 

		return mergedGemInfo;
	}

	public List<MatchedLineInfo> Match() 
	{
		var matchedLineInfos = new List<MatchedLineInfo>();
		
		var matchLineModels = Model.positiveMatchLineModels;
		var matchableGemModels = 
			from GemModel gemModel in Model.GemModels
			where gemModel is IMovable 
				&& gemModel.Type != GemType.EmptyGem 
				&& IsMovableTile(gemModel.Position) 
				&& Model.currentTurn > gemModel.preservedFromMatch
				&& !IsFalling(gemModel)
			select gemModel;

		foreach (var matchableGemModel in matchableGemModels) 
		{
			foreach (var matchLineModel in matchLineModels) 
			{
				var matchedGemModels = GetAnyMatches(matchableGemModel, matchLineModel);

				if (matchedGemModels.Count == 0) { continue; }

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

		foreach (var matchedLineInfo in matchedLineInfos) 
		{
			matchedLineInfo.matchLineModels.ForEach(matchLineModel => {
				UnityEngine.Debug.Log(matchLineModel.ToString());
			});
			var latestGemModel = matchedLineInfo.gemModels.OrderByDescending(gemModel => gemModel.sequence).FirstOrDefault();

			foreach (var matchedGemModel in matchedLineInfo.gemModels) 
			{
				var newGemType = GemType.EmptyGem;
				if (matchedGemModel == latestGemModel) {
					newGemType = ReadGemType(
						latestGemModel.Type, 
						ReadSpecialKey(matchedLineInfo.matchLineModels, latestGemModel.PositionVector)
					);
				}

				// Create a new gem model either empty or merged.
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

	public List<GemModel> GetAnyMatches(GemModel sourceGemModel, MatchLineModel matchLineModel) 
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
					&& !IsFalling(matchingGemModel)
				) {
					matchedGemModels.Add(matchingGemModel);
				} else {
					// Need to match about all offsets.
					break;
				}
			}
			
			if (matchedGemModels.Count == whereCanMatch.matchOffsets.Count) {
				// Finally found!
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

	string MergeSpecialKey(GemModel merger, GemModel mergee)
	{
		var mergerType = merger.Type;
		var mergerKey = merger.specialKey;

		var mergeeType = mergee.Type;
		var mergeeKey = mergee.specialKey;
		var maxOfGemtype = Math.Max((int)merger.Type, (int)mergee.Type);
		merger.Type = (maxOfGemtype >= 10) ? (GemType)maxOfGemtype : GemType.Nil;
		mergee.Type = GemType.Nil;
		merger.specialKey = mergee.specialKey = "";
		return ReadMergingKey(mergerType, mergerKey) + ReadMergingKey(mergeeType, mergeeKey);
	}

	int MergeEndurance(GemModel merger, GemModel mergee)
	{
		if (merger.specialKey == "CC") { return 2; }
		if (merger.specialKey == "HSQ" || merger.specialKey == "SQH") { return 5; }
		return Math.Max(merger.endurance, mergee.endurance);
	}

	string ReadMergingKey(GemType gemType, string specialKey)
	{
		if (gemType == GemType.SuperGem) { return "SP"; }
		else if (gemType == GemType.ChocoGem) { return "SQ"; }
		return specialKey;
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

	bool IsFalling(GemModel gemModel) 
	{
		bool result = false;

		var sourcePosition = gemModel.Position;
		while (true)
		{
			var gravity = GetGravityModel(sourcePosition).vector;
			var nearPosition = new Position(sourcePosition.index, gravity[0], gravity[1]);
			if (!IsMovableTile(nearPosition)) {
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
			where gemModel is IMovable && gemModel.Type == GemType.EmptyGem && Model.currentTurn > gemModel.preservedFromFall
			select gemModel;

		// Only one of them will be executed between a and b.
		// A: Vertical comparing as bottom up
		foreach (var emptyGemModel in emptyGemModels) 
		{
			var emptyGemPosition = emptyGemModel.Position;
			var gravity = GetGravityModel(emptyGemPosition).vector;
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
		foreach (var blockedGemModel in blockedGemModels) 
		{
			var blockedGemPosition = blockedGemModel.Position;
			var gravity = GetGravityModel(blockedGemPosition).vector;
			var randomDirections = SET_OF_RANDOM_DIRECTIONS[RANDOM.Next(SET_OF_RANDOM_DIRECTIONS.Length)];

			foreach (var randomDirection in randomDirections) {
				int directionX = 0;
				int directionY = 0;
				if (gravity[0] != 0) {
					directionX = -gravity[0];
					directionY = randomDirection;
				} else if (gravity[1] != 0) {
					directionX = randomDirection;
					directionY = -gravity[1];
				}

				Position nearPosition = new Position(blockedGemPosition.index, directionX, directionY);
				if (!nearPosition.IsAcceptableIndex()) { continue; }

				var nearGemModel = GetGemModel(nearPosition);
				if (Model.currentTurn <= nearGemModel.preservedFromMatch) { continue; }
				
				if (IsMovableTile(nearPosition)
					&& !fallingGemModels.Contains(nearGemModel)
					&& nearGemModel is IMovable) {
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
					endOfFall = !IsFalling(fallingGemModel)
				};
			})
			.ToList();
    }

	public List<GemModel> Feed()
	{
		var feedingGemModels = new List<GemModel>();
		
		var matchingTypes = Model.MatchingTypes;

		var spawnerGemModels = 
			from GemModel gemModel in Model.GemModels
			where gemModel is SpawnerGemModel
			select gemModel;

		foreach (SpawnerGemModel spawnerGemModel in spawnerGemModels)
		{
			var gravity = GetGravityModel(spawnerGemModel.Position).vector;
			var spawneePosition = new Position(spawnerGemModel.Position.index, gravity[0], gravity[1]);
			if (GetGemModel(spawneePosition).Type == GemType.EmptyGem) {
				var spawneeGemModel = GemModelFactory.Get(GemType.EmptyGem, spawneePosition);
				SetGemModel(spawneeGemModel);
				spawneeGemModel.Type = matchingTypes[RANDOM.Next(matchingTypes.Count)];
				feedingGemModels.Add(spawneeGemModel);
			}
		}

		return feedingGemModels;
    }

	public BlockedGemInfo MarkAsBlock(Position sourcePosition, Position nearPosition, Int64 markerID)
    {
		var blockedGemInfo = new BlockedGemInfo();
		
		if (!sourcePosition.IsAcceptableIndex()) { return blockedGemInfo; }

		var sourceGemModel = GetGemModel(sourcePosition);
		var isMovableTile = IsMovableTile(sourcePosition);
		if (isMovableTile
			&& sourceGemModel is IMovable 
			&& sourceGemModel.Type != GemType.EmptyGem 
			&& Model.currentTurn >= sourceGemModel.preservedFromBreak)
			// && Model.currentTurn >= sourceGemModel.preservedFromMatch) 
		{
			blockedGemInfo.gemModel = CopyAsBlock(markerID, sourceGemModel);
		}

		blockedGemInfo.isNextMovable = isMovableTile;
		
        return blockedGemInfo;
    }

	public ReplacedGemInfo ReplaceSameTypeAsSpecial(
		Position sourcePosition, 
		GemType gemType, 
		Int64 replacerID, 
		string[] specialKeys, 
		int endurance
	) {
		var replacedGemInfo = new ReplacedGemInfo { 
			blockedGemModel = CopyAsBlock(replacerID, GetGemModel(sourcePosition)),
			gemModels = new List<GemModel>{}
		};
		
		var sameGemModels = 
			from GemModel gemModel in Model.GemModels
			where IsMovableTile(gemModel.Position)
				&& gemModel is IMovable 
				&& gemModel.CanMatch(gemType)
				&& Model.currentTurn >= gemModel.preservedFromBreak
				// && Model.currentTurn >= gemModel.preservedFromMatch
			select gemModel;

		foreach (var gemModel in sameGemModels)
		{
			replacedGemInfo.gemModels.Add(
				CopyAsSpecial(replacerID, gemModel, specialKeys[RANDOM.Next(specialKeys.Length)], endurance)
			);
		}
		
        return replacedGemInfo;
    }

	public BlockedGemInfo MarkSameTypeAsBlock(Position sourcePosition, GemType gemType, Int64 markerID)
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
				// && Model.currentTurn >= gemModel.preservedFromMatch
			select gemModel;

		foreach (var gemModel in sameGemModels)
		{
			blockedGemInfo.gemModels.Add(CopyAsBlock(markerID, gemModel));
		}
		
        return blockedGemInfo;
    }

	public BlockedGemInfo MarkAllGemsAsBlock(Position sourcePosition, Int64 markerID)
	{
		var blockedGemInfo = new BlockedGemInfo {
			gemModels = new List<GemModel>()
		};
		
		var sameGemModels = 
			from GemModel gemModel in Model.GemModels
			where IsMovableTile(gemModel.Position)
				&& gemModel is IMovable 
				&& Model.currentTurn >= gemModel.preservedFromBreak
				// && Model.currentTurn >= gemModel.preservedFromMatch
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
		copiedGemModel.Position = targetGemModel.Position;
		copiedGemModel.positionBefore = targetGemModel.positionBefore;
		SetGemModel(copiedGemModel);
		return copiedGemModel;
	}

	GemModel CopyAsSpecial(Int64 replacerID, GemModel targetGemModel, string specialKey, int endurance)
	{
		var copiedGemModel = GemModelFactory.Get(
			ReadGemType(targetGemModel.Type, specialKey), 
			targetGemModel.Position
		);
		copiedGemModel.id = targetGemModel.id;
		copiedGemModel.replacedBy = replacerID;
		copiedGemModel.Type = targetGemModel.Type;
		copiedGemModel.specialKey = specialKey;
		copiedGemModel.endurance = endurance;
		SetGemModel(copiedGemModel);
		return copiedGemModel;
	}

	public BrokenGemInfo Break(Position targetPosition, Int64 markerID) 
	{
		BrokenGemInfo brokenGemInfo = new BrokenGemInfo();

		var isNextMovable = IsMovableTile(targetPosition);
		if (isNextMovable) 
		{
			var targetGemModel = GetGemModel(targetPosition);
			if (targetGemModel.markedBy == markerID || targetGemModel.replacedBy == markerID) {
				var newGemModel = GemModelFactory.Get(GemType.EmptyGem, targetGemModel.Position);
				newGemModel.preservedFromFall = Model.currentTurn + 1;
				SetGemModel(newGemModel);

				brokenGemInfo.gemModel = targetGemModel;
			}
		}

		brokenGemInfo.isNextMovable = isNextMovable;

		return brokenGemInfo;
	}

	public BrokenGemInfo BreakEmptyBlocks(Int64 markerID) 
	{
		BrokenGemInfo brokenGemInfo = new BrokenGemInfo() {
			gemModels = new List<GemModel>()
		};
		
		var emptyBlockGemModels = 
			from GemModel gemModel in Model.GemModels
			where gemModel is IBlockable
				&& (gemModel.Type == GemType.EmptyGem || gemModel.Type == GemType.Nil)
				&& gemModel.markedBy == markerID
			select gemModel;

		foreach (var gemModel in emptyBlockGemModels)
		{
			var newGemModel = GemModelFactory.Get(GemType.EmptyGem, gemModel.Position);
			newGemModel.preservedFromFall = Model.currentTurn;
			SetGemModel(newGemModel);

			brokenGemInfo.gemModels.Add(gemModel);
		}

		return brokenGemInfo;
	}

	public GemModel GetGemModel(Position position) 
	{
		return Model.GemModels[position.row, position.col];
	}

	public GravityModel GetGravityModel(Position position)
	{
		return Model.GravityModels[position.row, position.col];
	}

	public void SetGemModel(GemModel gemModel) 
	{
		Model.GemModels[gemModel.Position.row, gemModel.Position.col] = gemModel;
	}

	public bool IsMovableTile(Position position) 
	{
		return position.IsAcceptableIndex() 
			&& Model.TileModels[position.row, position.col].Type != TileType.Immovable;
	}

	public bool IsSpecialType(GemModel gemModel)
	{
		// "H": "H", "V", "C", Choco, Super
		// "V': "H", "V", "C", Choco, Super
		// "C": "H", "V", "C", Choco, Super
		// Choco: "H", "V", "C", Choco, Super
		// Super: "H", "V", "C", Choco, Super
		var gemType = gemModel.Type;
		var specialKey = gemModel.specialKey;
		return (
			gemType == GemType.SuperGem 
			|| gemType == GemType.ChocoGem 
			|| specialKey == "H"
			|| specialKey == "V"
			|| specialKey == "C"
		);
	}
}
