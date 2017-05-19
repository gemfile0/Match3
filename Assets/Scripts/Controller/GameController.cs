using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class BrokenGemInfo 
{
	public GemModel gemModel;
	public List<GemModel> gemModels;

	public void Clear()
	{
		gemModel = null;
		gemModels.Clear();
	}
}

public class BlockedGemInfo 
{
	public GemModel gemModel;
	public List<GemModel> gemModels;
	public bool isNextBreakable;
	public bool isNextMovable;

	
	public void Clear()
	{
		gemModel = null;
		gemModels.Clear();
		isNextBreakable = false;
		isNextMovable = false;
	}
}

public class ReplacedGemInfo 
{
	public GemModel blockedGemModel;
	public List<GemModel> gemModels;

	public void Clear()
	{
		blockedGemModel = null;
		gemModels.Clear();
	}
}

public class MergedGemInfo
{
	public GemModel mergee;
	public GemModel merger;

	public void Clear()
	{
		mergee = null;
		merger = null;
	}
}

public class MatchableGemInfo
{
	public GemModel sourceGemModel;
	public GemModel nearGemModel;
}

public class MatchableTypeInfo
{
	public Position sourcePosition;
	public GemModel sourceGemModel;
	public List<WhereCanMatch> wheresCanMatch;
	public GemType matchingType;
	public bool isSpecialType;
}

public class GameController<M>: BaseController<M>
	where M: GameModel 
{
	readonly int[][] SET_OF_RANDOM_DIRECTIONS;
	readonly Random RANDOM;
	List<MatchedLineInfo> matchedLineInfos;
	List<GemModel> feedingGemModels;
	List<GemModel> fallingGemModels;
	Stack<GemModel> blockedGemModels;
	List<GemModel> swappingGemModels;
	BrokenGemInfo brokenGemInfo;
	BlockedGemInfo blockedGemInfo;
	List<GemModel> matchedGemModels;
	ReplacedGemInfo replacedGemInfo;
	MergedGemInfo mergedGemInfo;
	List<MatchableGemInfo> matchableGemInfos;
	List<WhereCanMatch> wheresCanMatch;
	List<MatchableTypeInfo> matchableTypeInfos;
	
	public GameController()
	{
		SET_OF_RANDOM_DIRECTIONS = new int[][]{ new int[]{ -1, 1 }, new int[]{ 1, -1 } };
		RANDOM = new Random();
		matchedLineInfos = new List<MatchedLineInfo>();
		feedingGemModels = new List<GemModel>();
		fallingGemModels = new List<GemModel>();
		blockedGemModels = new Stack<GemModel>();
		matchedGemModels = new List<GemModel>();
		swappingGemModels = new List<GemModel>();
		brokenGemInfo = new BrokenGemInfo() {
			gemModels = new List<GemModel>()
		};
		blockedGemInfo = new BlockedGemInfo {
			gemModels = new List<GemModel>()
		};
		replacedGemInfo = new ReplacedGemInfo { 
			gemModels = new List<GemModel>()
		};
		mergedGemInfo = new MergedGemInfo();
		matchableGemInfos = new List<MatchableGemInfo>();
		wheresCanMatch = new List<WhereCanMatch>();
		matchableTypeInfos = new List<MatchableTypeInfo>();
	}

	public override void Kill() 
	{
		base.Kill();

		matchedLineInfos = null;
		feedingGemModels = null;
		fallingGemModels = null;
		blockedGemModels = null;
		matchedGemModels = null;
		swappingGemModels = null;
		brokenGemInfo = null;
		blockedGemInfo = null;
		replacedGemInfo = null;
		mergedGemInfo = null;
		matchableGemInfos = null;
		wheresCanMatch = null;
		matchableTypeInfos = null;
	}

	public void TakeSnapshot()
	{
		Model.HistoryOfGemModels.Add((GemModel[,])Model.GemModels.Clone());
	}

	public bool HasAnyChange()
	{
		var gemModelsCurrent = Model.GemModels;
		var gemModelsBefore = Model.HistoryOfGemModels[Model.HistoryOfGemModels.Count - 1];
		return !gemModelsCurrent.ContentEquals<GemModel>(gemModelsBefore);
	}

	public bool HasAnySpecialGems()
	{
		var specialGemModels = 
			from GemModel gemModel in Model.GemModels
			where IsMovableTile(gemModel.Position)
				&& gemModel.IsSpecialType()
			select gemModel;

		return specialGemModels.Any();
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
					if (GetWheresCanMatch(emptyGemModel.Position, matchLineModel, matchingType).Count == 0) { continue; }
					gemsCantPutIn.Add(matchingType);
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
			where gemModel is IMovable && gemModel.Type != GemType.EmptyGem && IsMovableTile(gemModel.Position)
			select gemModel;

		return allGemModels;
    }
	
    public void TurnNext()
    {
        Model.currentTurn += 1;
    }

    public List<WhereCanMatch> GetWheresCanMatch(Position sourcePosition, MatchLineModel matchLineModel, GemType matchingType) 
	{
		wheresCanMatch.Clear();
		
		foreach(var whereCanMatch in matchLineModel.wheresCanMatch) 
		{
			var matchCount = 0;
			foreach(var matchOffset in whereCanMatch.MatchOffsets) {
				if (!IsAcceptableIndex(sourcePosition, matchOffset[0], matchOffset[1])) { break; }

				var matchPosition = Position.Get(sourcePosition, matchOffset[0], matchOffset[1]);
				if (IsMovableTile(matchPosition)
					&& (sourcePosition.index == matchPosition.index || GetGemModel(matchPosition).CanMatch(matchingType))) {
					matchCount++;
				} else {
					break;
				}
			}
			
			if (matchCount == whereCanMatch.MatchOffsets.Count) {
				wheresCanMatch.Add(whereCanMatch);
			}
		}

		return wheresCanMatch;
	}

	public List<MatchableGemInfo> GetMatchableGems()
	{
		matchableGemInfos.Clear();
		matchableTypeInfos.Clear();

		foreach (var gemModel in GetAll()) 
		{
			var sourcePosition = gemModel.Position;
			var sourceGemModel = GetGemModel(sourcePosition);
			
			// Add to targets either if it is special gem
			if (sourceGemModel.IsSpecialType()) {
				matchableTypeInfos.Add(new MatchableTypeInfo {
					sourcePosition = sourcePosition,
					sourceGemModel = sourceGemModel,
					wheresCanMatch = new List<WhereCanMatch> {
						new WhereCanMatch(new List<int[]>{ new int[] {0, 0} }) 
					},
					matchingType = sourceGemModel.Type,
					isSpecialType = true
				});
			}

			// Or if it has a matchable line.
			foreach (var matchLineModel in Model.allwayMatchLineModels) {
				foreach(var matchingType in Model.MatchingTypes) {
					var wheresCanMatch = GetWheresCanMatch(sourcePosition, matchLineModel, matchingType);
					if (wheresCanMatch.Count == 0) { continue; }

					matchableTypeInfos.Add(new MatchableTypeInfo {
						sourcePosition = sourcePosition,
						wheresCanMatch = wheresCanMatch.ToList(),
						matchingType = matchingType
					});
				}
			}
		}

		foreach (var matchableTypeInfo in matchableTypeInfos) 
		{
			var sourcePosition = matchableTypeInfo.sourcePosition;
			if (matchableTypeInfo.isSpecialType) {
				var sourceGemModel = matchableTypeInfo.sourceGemModel;
				if (sourceGemModel.Type == GemType.ChocoGem || sourceGemModel.Type == GemType.SuperGem) {
					matchableGemInfos.Add(new MatchableGemInfo { 
						sourceGemModel = GetGemModel(sourcePosition)
					});
				}
				
				foreach (var offsetCanSwap in Model.offsetsCanSwap) {
					if (!IsAcceptableIndex(sourcePosition, offsetCanSwap[0], offsetCanSwap[1])) { continue; }
					var nearPosition = Position.Get(sourcePosition, offsetCanSwap[0], offsetCanSwap[1]);

					if (!IsMovableTile(nearPosition)
						|| !GetGemModel(nearPosition).IsSpecialType()) { continue; }

					matchableGemInfos.Add(new MatchableGemInfo { 
						sourceGemModel = GetGemModel(sourcePosition), nearGemModel = GetGemModel(nearPosition) 
					});
					// UnityEngine.Debug.Log("Possible Match Found : " + sourcePosition + " <=> " + nearPosition);
				}
				continue;
			}
			
			foreach(var whereCanMatch in matchableTypeInfo.wheresCanMatch) {
				foreach (var offsetCanSwap in Model.offsetsCanSwap) {
					if (!IsAcceptableIndex(sourcePosition, offsetCanSwap[0], offsetCanSwap[1])) { continue; }
					var nearPosition = Position.Get(sourcePosition, offsetCanSwap[0], offsetCanSwap[1]);

					if (!IsMovableTile(nearPosition)
						|| GetGemModel(nearPosition).Type != matchableTypeInfo.matchingType
						|| IsNearPositionOnMatchLine(nearPosition, sourcePosition, whereCanMatch)) { continue; }

					matchableGemInfos.Add(new MatchableGemInfo { 
						sourceGemModel = GetGemModel(sourcePosition), nearGemModel = GetGemModel(nearPosition) 
					});
					// UnityEngine.Debug.Log("Possible Match Found : " + sourcePosition + " <=> " + nearPosition);
				}
			}
		}

		return matchableGemInfos;
	}
	
	bool IsNearPositionOnMatchLine(Position nearPosition, Position sourcePosition, WhereCanMatch whereCanMatch)
	{
		var result = false;
		foreach(var matchOffset in whereCanMatch.MatchOffsets) 
		{
			var matchPosition = Position.Get(sourcePosition, matchOffset[0], matchOffset[1]);
			if (matchPosition.index == nearPosition.index) { result = true; break; }
		}
			
		return result;
	}

	public IEnumerable<GemModel> Shuffle(Int64 preservedFromMatch)
	{
		var allGems = GetAll().ToList();
		var random = new Random();
        for (int i = 0; i < allGems.Count; i++)
        {
            int randomIndex = random.Next(i, allGems.Count);
			var randomPosition = allGems[randomIndex].Position;
			allGems[randomIndex].Position = allGems[i].Position;
			allGems[i].Position = randomPosition;
        }

		foreach (var gemModel in allGems)
		{
			gemModel.preservedFromMatch = preservedFromMatch;
			SetGemModel(gemModel);
		}
		
		return GetAll();
	}

	public List<GemModel> Swap(Position sourcePosition, Position nearPosition) 
	{
		swappingGemModels.Clear();

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

		return swappingGemModels;
    }

	public MergedGemInfo Merge(Position sourcePosition, Position nearPosition)
	{
		mergedGemInfo.Clear();

		var sourceGemModel = GetGemModel(sourcePosition);
		var nearGemModel = GetGemModel(nearPosition);

		nearGemModel.specialKey = MergeSpecialKey(nearGemModel, sourceGemModel);
		nearGemModel.endurance = MergeEndurance(nearGemModel, sourceGemModel);
		nearGemModel.positionBefore = sourcePosition;

		sourceGemModel.preservedFromMatch = nearGemModel.preservedFromMatch = Model.currentTurn + 5;

		mergedGemInfo.merger = nearGemModel;
		mergedGemInfo.mergee = sourceGemModel;

		return mergedGemInfo;
	}

	public List<MatchedLineInfo> Match() 
	{
		matchedLineInfos.Clear();
		
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
					gemModels = matchedGemModels.ToList(), 
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
			// foreach (var matchLineModel in matchedLineInfo.matchLineModels)
			// {
				// UnityEngine.Debug.Log(matchLineModel.ToString());
			// }
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
					// UnityEngine.Debug.Log("newGemModel : " + newGemModel.ToString());
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
		matchedGemModels.Clear();
		foreach (var whereCanMatch in matchLineModel.wheresCanMatch) 
		{
			foreach (var matchOffset in whereCanMatch.MatchOffsets) {
				if (!IsAcceptableIndex(sourceGemModel.Position, matchOffset[0], matchOffset[1])) { continue; }

				var matchingPosition = Position.Get(sourceGemModel.Position, matchOffset[0], matchOffset[1]);
				if (!IsMovableTile(matchingPosition)) { continue; }

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
			
			if (matchedGemModels.Count == whereCanMatch.MatchOffsets.Count) {
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
			case Literals.SP: 
				gemType = GemType.SuperGem; 
				break;

			case Literals.SQ:
				gemType = GemType.ChocoGem; 
				break;

			case Literals.C:
				specialGemType = 1;
				break;

			case Literals.H:
				specialGemType = 2;
				break;

			case Literals.V:
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
		merger.specialKey = mergee.specialKey = Literals.Nil;

		var sb = new StringBuilder();
		sb.Append(ReadMergingKey(mergerType, mergerKey));
		sb.Append(ReadMergingKey(mergeeType, mergeeKey));
		return sb.ToString();
	}

	int MergeEndurance(GemModel merger, GemModel mergee)
	{
		if (merger.specialKey == Literals.CC) { return 2; }
		if (merger.specialKey == Literals.HSQ || merger.specialKey == Literals.SQH) { return 5; }
		return Math.Max(merger.endurance, mergee.endurance);
	}

	string ReadMergingKey(GemType gemType, string specialKey)
	{
		if (gemType == GemType.SuperGem) { return Literals.SP; }
		else if (gemType == GemType.ChocoGem) { return Literals.SQ; }
		return specialKey;
	}

	public string ReadSpecialKey(IEnumerable<MatchLineModel> matchLineModels, PositionVector positionVector) 
	{
		var specialKey = Literals.Nil;

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
			specialKey = Literals.SP;
		}
		else if (hasVerticalMatch && hasHorizontalMatch) 
		{
			specialKey = Literals.C;
		}
		else if (maxMagnitude == 4)
		{
			specialKey = positionVector.colOffset != 0 ? Literals.H: Literals.V;
		}
		else if (hasSquareMatch)
		{
			specialKey = Literals.SQ;
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
			if (!IsAcceptableIndex(sourcePosition, gravity[0], gravity[1])) { break; }

			var nearPosition = Position.Get(sourcePosition, gravity[0], gravity[1]);
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
		fallingGemModels.Clear();
		blockedGemModels.Clear();
		
		var emptyGemModels = 
			from GemModel gemModel in Model.GemModels
			where IsMovableTile(gemModel.Position)
				&& gemModel is IMovable 
				&& gemModel.Type == GemType.EmptyGem 
				&& Model.currentTurn > gemModel.preservedFromFall
			select gemModel;

		// Only one of them will be executed between a and b.
		// A: Vertical comparing as bottom up
		foreach (var emptyGemModel in emptyGemModels) 
		{
			var emptyGemPosition = emptyGemModel.Position;
			var gravity = GetGravityModel(emptyGemPosition).vector;

			if (!IsAcceptableIndex(emptyGemPosition, gravity[0], -gravity[1])) { continue; }
			var nearPosition = Position.Get(emptyGemPosition, gravity[0], -gravity[1]);
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

			foreach (var randomDirection in randomDirections) 
			{
				int directionX = 0;
				int directionY = 0;
				if (gravity[0] != 0) {
					directionX = -gravity[0];
					directionY = randomDirection;
				} else if (gravity[1] != 0) {
					directionX = randomDirection;
					directionY = -gravity[1];
				}

				if (!IsAcceptableIndex(blockedGemPosition, directionX, directionY)) { continue; }
				Position nearPosition = Position.Get(blockedGemPosition, directionX, directionY);

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
		feedingGemModels.Clear();
		
		var matchingTypes = Model.MatchingTypes;

		var spawnerGemModels = 
			from GemModel gemModel in Model.GemModels
			where gemModel is SpawnerGemModel
			select gemModel;

		foreach (SpawnerGemModel spawnerGemModel in spawnerGemModels)
		{
			var gravity = GetGravityModel(spawnerGemModel.Position).vector;
			var spawneePosition = Position.Get(spawnerGemModel.Position, gravity[0], gravity[1]);
			if (GetGemModel(spawneePosition).Type == GemType.EmptyGem) {
				var spawneeGemModel = GemModelFactory.Get(GemType.EmptyGem, spawneePosition);
				SetGemModel(spawneeGemModel);
				spawneeGemModel.Type = matchingTypes[RANDOM.Next(matchingTypes.Count)];
				feedingGemModels.Add(spawneeGemModel);
			}
		}

		return feedingGemModels;
    }

	public BlockedGemInfo MarkAsBlock(Position sourcePosition, Int64 markerID)
    {
		blockedGemInfo.Clear();
		
		var sourceGemModel = GetGemModel(sourcePosition);
		var isMovable = IsMovableTile(sourcePosition);
		if (isMovable
			&& sourceGemModel is IMovable 
			&& sourceGemModel.Type != GemType.EmptyGem 
			&& Model.currentTurn >= sourceGemModel.preservedFromBreak)
			// && Model.currentTurn >= sourceGemModel.preservedFromMatch) 
		{
			blockedGemInfo.gemModel = CopyAsBlock(markerID, sourceGemModel);
		}

        return blockedGemInfo;
    }

	public ReplacedGemInfo ReplaceSameTypeAsSpecial(
		Position sourcePosition, 
		GemType gemType, 
		Int64 replacerID, 
		string[] specialKeys, 
		int endurance
	) {
		replacedGemInfo.Clear();
		replacedGemInfo.blockedGemModel = CopyAsBlock(replacerID, GetGemModel(sourcePosition));
		
		var sameGemModels = 
			from GemModel gemModel in Model.GemModels
			where IsMovableTile(gemModel.Position)
				&& gemModel is IMovable 
				&& gemModel.CanMatch(gemType)
				&& Model.currentTurn >= gemModel.preservedFromBreak
			select gemModel;

		foreach (var gemModel in sameGemModels)
		{
			replacedGemInfo.gemModels.Add(
				CopyAsSpecial(replacerID, gemModel, specialKeys[RANDOM.Next(specialKeys.Length)], endurance)
			);
		}
		
        return replacedGemInfo;
    }

	public ReplacedGemInfo ReplaceAnyTypeAsSpecial(
		Int64 replacerID, 
		string[] specialKeys, 
		int endurance
	) {
		replacedGemInfo.Clear();
		
		var chosenGemModels = 
			from GemModel gemModel in Model.GemModels
			where IsMovableTile(gemModel.Position)
				&& gemModel is IMovable 
				&& gemModel.IsPrimitiveType()
				&& Model.currentTurn >= gemModel.preservedFromBreak
			select gemModel;

		chosenGemModels = chosenGemModels.OrderBy(s => Guid.NewGuid()).Take(endurance);
		foreach (var gemModel in chosenGemModels)
		{
			replacedGemInfo.gemModels.Add(
				CopyAsSpecial(replacerID, gemModel, specialKeys[RANDOM.Next(specialKeys.Length)], int.MaxValue)
			);
		}
		
        return replacedGemInfo;
    }

	public BlockedGemInfo MarkSameTypeAsBlock(Position sourcePosition, GemType gemType, Int64 markerID)
    {
		blockedGemInfo.Clear();
		blockedGemInfo.gemModels.Add(CopyAsBlock(markerID, GetGemModel(sourcePosition)));
		
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

	public BlockedGemInfo MarkSpecialTypeAsBlock(Int64 markerID)
    {
		blockedGemInfo.Clear();
		
		var specialGemModels = 
			from GemModel gemModel in Model.GemModels
			where IsMovableTile(gemModel.Position)
				&& gemModel.IsSpecialType()
			select gemModel;

		foreach (var gemModel in specialGemModels)
		{
			blockedGemInfo.gemModels.Add(CopyAsBlock(markerID, gemModel));
		}
		
        return blockedGemInfo;
    }

	public BlockedGemInfo MarkAllGemsAsBlock(Position sourcePosition, Int64 markerID)
	{
		blockedGemInfo.Clear();
		
		var sameGemModels = 
			from GemModel gemModel in Model.GemModels
			where IsMovableTile(gemModel.Position)
				&& gemModel is IMovable 
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
		SetGemModel(copiedGemModel);
		return copiedGemModel;
	}

	public BrokenGemInfo Break(Position targetPosition, Int64 markerID) 
	{
		brokenGemInfo.Clear();

		if (IsMovableTile(targetPosition))
		{
			var targetGemModel = GetGemModel(targetPosition);
			if (targetGemModel.markedBy == markerID || targetGemModel.replacedBy == markerID) {
				var newGemModel = GemModelFactory.Get(GemType.EmptyGem, targetGemModel.Position);
				newGemModel.preservedFromFall = Model.currentTurn + 1;
				SetGemModel(newGemModel);

				brokenGemInfo.gemModel = targetGemModel;
			}
		}

		return brokenGemInfo;
	}

	public BrokenGemInfo BreakEmptyBlocks(Int64 markerID) 
	{
		brokenGemInfo.Clear();
		
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

	public bool IsAcceptableIndex(Position position, int colOffset, int rowOffset)
	{
		return (position.col + colOffset) >= 0
			&& (position.col + colOffset) <  Model.Cols
			&& (position.row + rowOffset) >= 0
			&& (position.row + rowOffset) <  Model.Rows;
	}

	public void SetGemModel(GemModel gemModel) 
	{
		Model.GemModels[gemModel.Position.row, gemModel.Position.col] = gemModel;
	}

	public bool IsMovableTile(Position position) 
	{
		return Model.TileModels[position.row, position.col].Type == TileType.Movable;
	}

	public bool IsBreakableTile(Position position) 
	{
		return Model.TileModels[position.row, position.col].Type != TileType.Immovable;
	}

}
