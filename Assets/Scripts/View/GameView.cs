using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct UpdateResult
{
	public List<MatchedLineInfo> matchResult;
	public List<GemModel> feedResult;
	public List<GemInfo> fallResult;

	public bool HasAnyResult 
	{
		get { return matchResult.Count > 0 || feedResult.Count > 0 || fallResult.Count > 0; }
	}
}

struct MarkedPositionsInfo
{
	public List<Position> positions;
	public bool isNextMovable;
}

struct ReplacedPositionsInfo
{
	public List<Position> positions;
	public int latestCount;
}

public class GameView: BaseView<GameModel, GameController<GameModel>>  
{
	const Int64 FRAME_BY_TURN = 3;
	const float TIME_PER_FRAME = 0.016f;

	Bounds sampleBounds;
	Vector3 gemSize;
	SwipeInput swipeInput;
	GemView gemSelected;
	Dictionary<Int64, GemView> gemViews = new Dictionary<Int64, GemView>();
	Dictionary<Int64, Queue<Action<GOSequence, float>>> actionQueueByTurn = new Dictionary<Int64, Queue<Action<GOSequence, float>>>();

	GOSequence sequence;

	GameObject tiles;
	GameObject gems;
	GameObject gravities;

	public void Awake()
	{
		gravities = new GameObject("Gravities");
		gravities.transform.SetParent(transform);

		tiles = new GameObject("Tiles");
		tiles.transform.SetParent(transform);

		gems = new GameObject("Gems");
		gems.transform.SetParent(transform);
	}

	public override void Start()
	{
		base.Start();
		
		Controller.Init();
		MakeField();
		AlignField();
		SubscribeInput();
		StartCoroutine(StartHello());
	}

	public override void Destroy() 
	{
		base.Destroy();

		UnsubscribeInput();

		gemViews = null;
		actionQueueByTurn = null;
		sequence.Kill();
		sequence = null;
	}

	IEnumerator StartHello() 
	{
		yield return new WaitForSeconds(TIME_PER_FRAME * FRAME_BY_TURN);
		foreach (var gemModel in Controller.GetAll())
		{
			gemViews[gemModel.id].Squash();
		}
	}

	void MakeField() 
	{
		var sampleGem = ResourceCache.Instantiate("RedGem");
		sampleBounds = sampleGem.GetBounds();
		gemSize = sampleBounds.size;
		Destroy(sampleGem);

		foreach (var gemModel in Model.GemModels) 
		{
			if (gemModel.Type == GemType.EmptyGem || gemModel.Type == GemType.BlockedGem) { continue; }
			MakeGemView(gemModel);
		}

		foreach (var tileModel in Model.TileModels)
		{
			if (tileModel.Type == TileType.Immovable) { continue; }
			MakeTileView(tileModel);
		}

		foreach (var gravityModel in Model.GravityModels)
		{
			if (gravityModel.Type == GravityType.Nil) { continue; }
			MakeGravityView(gravityModel);
		}
	}

	GemView MakeGemView(GemModel gemModel) 
	{
		var gemView = ResourceCache.Instantiate<GemView>(gemModel.Name, gems.transform);
		gemView.UpdateModel(gemModel);
		gemView.transform.localPosition
			= new Vector2(gemModel.Position.col * gemSize.x, gemModel.Position.row * gemSize.y);
		gemViews.Add(gemModel.id, gemView);
		return gemView;
	}

	TileView MakeTileView(TileModel tileModel)
	{
		var tileView = ResourceCache.Instantiate<TileView>("Tile", tiles.transform);
		tileView.UpdateModel(tileModel);
		tileView.transform.localPosition 
			= new Vector2(tileModel.Position.col * gemSize.x, tileModel.Position.row * gemSize.y);
		return tileView;
	}

	GravityView MakeGravityView(GravityModel gravityModel)
	{
		Debug.Log(gravityModel.Type);
		var gravityView = ResourceCache.Instantiate<GravityView>(gravityModel.Name, gravities.transform);
		gravityView.UpdateModel(gravityModel);
		gravityView.transform.localPosition 
			= new Vector2(gravityModel.Position.col * gemSize.x, gravityModel.Position.row * gemSize.y);
		return gravityView;
	}

	GemView RemoveGemView(GemModel gemModel, bool needToChaining) 
	{
		GemView gemView;
		if (gemViews.TryGetValue(gemModel.id, out gemView)) 
		{
			gemViews.Remove(gemModel.id);
		}
		else
		{
			Debug.Log("Can't remove the gem! " + gemModel.ToString());
		}

		if (needToChaining)
		{
			ActByChaining(
				gemModel.Type, 
				gemModel.specialKey, 
				gemModel.Position, 
				gemModel.endurance, 
				gemModel.id, 
				new Vector2(gemModel.PositionVector.colOffset, gemModel.PositionVector.rowOffset)
			);
		}

		return gemView;
	}

	void AlignField() 
	{
		var sizeOfField = gameObject.GetBounds();
		transform.localPosition = new Vector2(
			sampleBounds.extents.x - sizeOfField.extents.x, 
			sampleBounds.extents.y - sizeOfField.extents.y
		);
	}

	void MatchGems(List<MatchedLineInfo> matchedLineInfos, GOSequence sequence, float currentTime) 
	{
		foreach (var matchedLineInfo in matchedLineInfos)
		{
			foreach (var gemModel in matchedLineInfo.gemModels)
			{
				var gemView = RemoveGemView(gemModel, true);
				if (gemView == null) { continue; }
				sequence.InsertCallback(currentTime, () => {
					gemView.ReturnToPool();
				});
			}

			if (matchedLineInfo.newAdded != null) 
			{
				var gemView = MakeGemView(matchedLineInfo.newAdded);
				gemView.SetActive(false);

				sequence.InsertCallback(currentTime, () => {
					gemView.Reveal();
					gemView.Squash();
				});
			}
		}
	}

	void FallGems(List<GemInfo> fallingGemInfos, GOSequence sequence, float currentTime) 
	{
		foreach (var gemInfo in fallingGemInfos)
		{
			var gemView = gemViews[gemInfo.id];
			var position = gemInfo.position;
			sequence.InsertCallback(currentTime, () => {
				gemView.Reveal();
			});

			var nextPosition = new Vector3(position.col * gemSize.x, position.row * gemSize.y, 0);
			var gapOfTurn = gemView.PreservedFromMatch - Model.currentTurn + 1;
			var duration = gapOfTurn * (TIME_PER_FRAME * FRAME_BY_TURN);
			sequence.Insert(currentTime, gemView.transform.GOLocalMove(
				nextPosition, 
				duration
			));
			if (gemInfo.endOfFall) {
				sequence.InsertCallback(currentTime + duration, () => gemView.Squash());
			}
		};
	}

	void FeedGems(List<GemModel> feedingGemModels, GOSequence sequence, float currentTime) 
	{
		foreach (var gemModel in feedingGemModels)
		{
			var gemView = MakeGemView(gemModel);
			gemView.Hide();
		}
	}

	void SubscribeInput() 
	{
		swipeInput = GetComponent<SwipeInput>();
		swipeInput.OnSwipeStart.AddListener(swipeInfo => {
			Vector3 pos = Camera.main.ScreenToWorldPoint(swipeInfo.touchBegin);
			RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.zero);
			if (hit && hit.collider != null) {
				gemSelected = hit.collider.gameObject.GetComponent<GemView>();
				gemSelected.Highlight();
				gemSelected.Squash();
			}
		});
		swipeInput.OnSwipeCancel.AddListener(() => {
			gemSelected = null;
		});
		swipeInput.OnSwipeEnd.AddListener(swipeInfo => {
			if (gemSelected != null) {
				var sourceGemModel = Controller.GetGemModel(gemSelected.Position);
				ActBySwipe(sourceGemModel, swipeInfo.direction);
				gemSelected = null;
			}
		});
	}

	void UnsubscribeInput()
	{
		swipeInput.OnSwipeStart.RemoveAllListeners();
		swipeInput.OnSwipeCancel.RemoveAllListeners();
		swipeInput.OnSwipeEnd.RemoveAllListeners();
	}

	void ActBySwipe(GemModel sourceGemModel, Vector2 direction) 
	{
		var sourcePosition = sourceGemModel.Position;
		var nearPosition = new Position(sourcePosition.index, (int)direction.x, (int)direction.y);
		var nearGemModel = Controller.GetGemModel(nearPosition);
		if ((Controller.IsSpecialType(sourceGemModel) && Controller.IsSpecialType(nearGemModel))
			|| sourceGemModel.Type == GemType.SuperGem || nearGemModel.Type == GemType.SuperGem
		) {
			Merge(sourcePosition, nearPosition);
			UpdateChanges();
		}
		else if (sourceGemModel.Type == GemType.ChocoGem) 
		{
			LinedBreaking(
				sourcePosition, 
				direction, 
				sourceGemModel.endurance, 
				sourceGemModel.id, 
				isChaining: false,
				breakingOffset: 5
			);
			UpdateChanges();
		}
		else 
		{
			Swap(sourcePosition, nearPosition);
			UpdateChanges(0, (Int64 passedTurn) => {
				Swap(nearPosition, sourcePosition);
				UpdateChanges(FRAME_BY_TURN * TIME_PER_FRAME * passedTurn);
			});
		}
	}

	IEnumerator StartUpdateChanges(float timeOffset, Action<Int64> OnNoAnyMatches)
	{
		sequence = GOTween.Sequence().SetEase(GOEase.SmoothStep);

		var currentFrame = 0;
		var startTurn = Model.currentTurn;
		var noUpdateCount = 0;
		while (true)
		{
			if (currentFrame % FRAME_BY_TURN == 0)
			{
				var passedTurn = Model.currentTurn - startTurn;
				var currentTime = timeOffset + FRAME_BY_TURN * TIME_PER_FRAME * passedTurn;
				// Debug.Log("currentTime : " + Model.currentTurn + ", " + currentTime);
				var countOfAction = actionQueueByTurn.Count;

				Queue<Action<GOSequence, float>> actionQueue;
				if (actionQueueByTurn.TryGetValue(Model.currentTurn, out actionQueue)) {
					while (actionQueue.Count > 0) {
						var action = actionQueue.Dequeue();
						action.Invoke(sequence, currentTime);
					}
					actionQueueByTurn.Remove(Model.currentTurn);
				}
				
				var updateResult = new UpdateResult {
					matchResult = Controller.Match(),
					feedResult = Controller.Feed(),
					fallResult = Controller.Fall(),
				};

				if (passedTurn == 6 && updateResult.matchResult.Count == 0 && OnNoAnyMatches != null) {
					OnNoAnyMatches(Model.currentTurn - startTurn);
					break;
				}

				if (updateResult.HasAnyResult) {
					noUpdateCount = 0;

					MatchGems(updateResult.matchResult, sequence, currentTime);
					FeedGems(updateResult.feedResult, sequence, currentFrame);
					FallGems(updateResult.fallResult, sequence, currentTime);
				} else {
					noUpdateCount++;
				}

				Controller.TurnNext();

				if (passedTurn >= 12) { yield return null; }
				if (noUpdateCount >= 18 && sequence.IsComplete && countOfAction == 0) { break; }
			}

			currentFrame += 1;
		}

		yield return null;
	}

	void UpdateChanges(float latestTime = 0f, Action<Int64> OnNoAnyMatches = null) 
	{
		StartCoroutine(StartUpdateChanges(latestTime, OnNoAnyMatches));
	}

	void ActByChaining(
		GemType gemType, 
		string specialKey, 
		Position sourcePosition, 
		int repeat, 
		Int64 markerID, 
		Vector2 direction
	) {
		Debug.Log("ActByChaining : " + gemType + ", " + specialKey + ", " + repeat + ", " + direction);
		switch (gemType)
		{
			case GemType.SuperGem:
			SameTypeBreaking(sourcePosition, GetRandomType(), markerID, isChaining: true);
			break;

			case GemType.ChocoGem:
			LinedBreaking(sourcePosition, GetRandomDirection(), repeat, markerID, isChaining: true, breakingOffset: 5);
			break;
		}
		
		switch (specialKey)
		{
			case "SP":
			EmptyBlockBreaking(1, markerID);
			SameTypeBreaking(sourcePosition, gemType, markerID, isChaining: true);
			break;

			case "H":
			LinedBreaking(sourcePosition, new Vector2{ x = -1, y = 0 }, repeat, markerID, isChaining: true);
			LinedBreaking(sourcePosition, new Vector2{ x = 1, y = 0 }, repeat, markerID, isChaining: true);
			break;

			case "V":
			LinedBreaking(sourcePosition, new Vector2{ x = 0, y = -1 }, repeat, markerID, isChaining: true);
			LinedBreaking(sourcePosition, new Vector2{ x = 0, y = 1 }, repeat, markerID, isChaining: true);
			break;

			case "C":
			RadialBreaking(sourcePosition, repeat, markerID, isChaining: true);
			break;

			case "HH":
			case "VV":
			case "HV":
			case "VH":
			LinedBreaking(sourcePosition, new Vector2{ x = -1, y = 0 }, repeat, markerID, isChaining: true);
			LinedBreaking(sourcePosition, new Vector2{ x = 1, y = 0 }, repeat, markerID, isChaining: true);
			LinedBreaking(sourcePosition, new Vector2{ x = 0, y = -1 }, repeat, markerID, isChaining: true);
			LinedBreaking(sourcePosition, new Vector2{ x = 0, y = 1 }, repeat, markerID, isChaining: true);
			break;

			case "CC":
			RadialBreaking(sourcePosition, repeat, markerID, isChaining: true);
			break;

			case "SQSQ":
			LinedRadialBreaking(sourcePosition, direction, repeat, markerID, isChaining: true, breakingOffset: 5);
			break;

			case "SPSP":
			AllTypeBreaking(sourcePosition, markerID, isChaining: true);
			break;

			case "CH":
			case "HC":
			LinedRadialBreaking(sourcePosition, new Vector2{ x = -1, y = 0 }, repeat, markerID, isChaining: true);
			LinedRadialBreaking(sourcePosition, new Vector2{ x = 1, y = 0 }, repeat, markerID, isChaining: true);
			break;

			case "CV":
			case "VC":
			LinedRadialBreaking(sourcePosition, new Vector2{ x = 0, y = -1 }, repeat, markerID, isChaining: true);
			LinedRadialBreaking(sourcePosition, new Vector2{ x = 0, y = 1 }, repeat, markerID, isChaining: true);
			break;

			case "HSQ":
			case "SQH":
			case "VSQ":
			case "SQV":
			EmptyBlockBreaking(1, markerID);
			LinedBreaking(
				sourcePosition, 
				direction, 
				repeat, 
				markerID, 
				isChaining: true, 
				breakingOffset: 5, 
				onComplete: (latestPosition) => {
					LinedBreaking(latestPosition, new Vector2{ x = -1, y = 0 }, int.MaxValue, markerID, isChaining: true);
					LinedBreaking(latestPosition, new Vector2{ x = 1, y = 0 }, int.MaxValue, markerID, isChaining: true);
					LinedBreaking(latestPosition, new Vector2{ x = 0, y = -1 }, int.MaxValue, markerID, isChaining: true);
					LinedBreaking(latestPosition, new Vector2{ x = 0, y = 1 }, int.MaxValue, markerID, isChaining: true);
				}
			);
			break;

			case "CSQ":
			case "SQC":
			EmptyBlockBreaking(1, markerID);
			LinedBreaking(
				sourcePosition, 
				direction, 
				repeat, 
				markerID, 
				isChaining: true, 
				breakingOffset: 5, 
				onComplete: (latestPosition) => {
					RadialBreaking(latestPosition, 1, markerID, isChaining: true);
				}
			);
			break;

			case "HSP":
			case "SPH":
			case "VSP":
			case "SPV":
			SameTypeReplacing(sourcePosition, gemType, markerID, new string[]{ "H", "V" }, repeat, isChaining: true);
			break;

			case "CSP":
			case "SPC":
			SameTypeReplacing(sourcePosition, gemType, markerID, new string[]{ "C" }, repeat, isChaining: true);
			break;

			case "SPSQ":
			case "SQSP":
			break;
		}
	}

	Vector2 GetRandomDirection()
	{
		return SwipeInput.ALL_DIRECTIONS[UnityEngine.Random.Range(0, SwipeInput.ALL_DIRECTIONS.Length)];
	}

	GemType GetRandomType()
	{
		return Model.MatchingTypes[UnityEngine.Random.Range(0, Model.MatchingTypes.Count)];
	}

	void EmptyBlockBreaking(int turnOffset, Int64 markerID)
	{
		AddAction(Model.currentTurn + turnOffset, (GOSequence sequence, float currentTime) => {
			var brokenGemInfo = Controller.BreakEmptyBlocks(markerID);
			foreach (var gemModel in brokenGemInfo.gemModels) {
				BreakGems(gemModel, false, sequence, currentTime);
			}
		});
	}

	void SameTypeBreaking(Position sourcePosition, GemType gemType, Int64 markerID, bool isChaining)
	{	
		var markedPositions = SetSameTypeAsBlock(sourcePosition, gemType, markerID);
		var count = 1;
		
		foreach (var markedPosition in markedPositions)
		{
			AddAction(Model.currentTurn + count, (GOSequence sequence, float currentTime) => {
				var brokenGemInfo = Controller.Break(markedPosition, markerID);
				BreakGems(brokenGemInfo.gemModel, isChaining, sequence, currentTime);
			});
			count += 1;
		}
	}

	void SameTypeReplacing(
		Position sourcePosition, 
		GemType gemType, 
		Int64 replacerID, 
		string[] specialKeys, 
		int endurance, 
		bool isChaining
	) {	
		var replacedPositionsInfo = SetSameTypeAsSpecial(sourcePosition, gemType, replacerID, specialKeys, endurance);

		var count = replacedPositionsInfo.latestCount;
		EmptyBlockBreaking(count, replacerID);
		AddAction(Model.currentTurn + count, (GOSequence sequence, float currentTime) => {
			foreach (var replacedPosition in replacedPositionsInfo.positions)
			{
				var brokenGemInfo = Controller.Break(replacedPosition, replacerID);
				BreakGems(brokenGemInfo.gemModel, isChaining, sequence, currentTime);
			}
		});
	}

	void AllTypeBreaking(Position sourcePosition, Int64 markerID, bool isChaining)
	{
		var markedPositions = SetAllTypeAsBlock(sourcePosition, markerID);
		var count = 1;
		foreach (var markedPosition in markedPositions)
		{
			AddAction(Model.currentTurn + count, (GOSequence sequence, float currentTime) => {
				var brokenGemInfo = Controller.Break(markedPosition, markerID);
				BreakGems(brokenGemInfo.gemModel, isChaining, sequence, currentTime);
			});
			count += 1;
		}
	}

	void RadialBreaking(Position sourcePosition, int repeat, Int64 markerID, bool isChaining)
	{
		var markedPositionsInfo = SetRadialBlock(sourcePosition, repeat, markerID);
		AddAction(Model.currentTurn + 1, (GOSequence sequence, float currentTime) => {
			foreach (var markedPosition in markedPositionsInfo.positions)
			{
				var brokenGemInfo = Controller.Break(markedPosition, markerID);
				BreakGems(brokenGemInfo.gemModel, isChaining, sequence, currentTime);
			}
		});
	}

	void LinedRadialBreaking(
		Position sourcePosition, 
		Vector2 direction, 
		int repeat, 
		Int64 markerID, 
		bool isChaining, 
		int breakingOffset = 1
	) {	
		var colOffset = (int)direction.x;
		var rowOffset = (int)direction.y;

		var count = breakingOffset;
		while (repeat > 0)
		{
			var nearPosition = new Position(sourcePosition.index, colOffset, rowOffset);
			var markedPositionsInfo = SetRadialBlock(sourcePosition, 1, markerID);
			AddAction(Model.currentTurn + count, (GOSequence sequence, float currentTime) => {
				foreach (var markedPosition in markedPositionsInfo.positions)
				{
					var brokenGemInfo = Controller.Break(markedPosition, markerID);
					BreakGems(brokenGemInfo.gemModel, isChaining, sequence, currentTime);
				}
			});

			if (!markedPositionsInfo.isNextMovable) { break; }
			
			sourcePosition = nearPosition;
			count += breakingOffset;
			repeat -= 1;
		}
	}
		
	void LinedBreaking(
		Position sourcePosition, 
		Vector2 direction, 
		int repeat, 
		Int64 markerID, 
		bool isChaining, 
		int breakingOffset = 1,
		Action<Position> onComplete = null
	) {
		var colOffset = (int)direction.x;
		var rowOffset = (int)direction.y;

		var markedPositions = SetLinedBlock(sourcePosition, colOffset, rowOffset, repeat, markerID);
		var count = breakingOffset;
		for (var i = 0; i < markedPositions.Count; i++)
		{
			var markedPosition = markedPositions[i];
			var turn = Model.currentTurn + count;
			AddAction(turn, (GOSequence sequence, float currentTime) => {
				var brokenGemInfo = Controller.Break(markedPosition, markerID);
				BreakGems(brokenGemInfo.gemModel, isChaining || markedPosition != sourcePosition, sequence, currentTime);
			});
			if (i == markedPositions.Count - 1 && onComplete != null) {
				AddAction(turn, (GOSequence sequence, float currentTime) => {
					onComplete(markedPosition);
				});
			}
			count += breakingOffset;
		}
	}

	void BreakGems(GemModel gemModel, bool needToChaining, GOSequence sequence, float currentTime) 
	{
		if (gemModel == null) { return; }

		var gemView = RemoveGemView(gemModel, needToChaining);
		if (gemView == null) { return; }

		sequence.InsertCallback(currentTime, () => {
			gemView.ReturnToPool();
		});
	}

	List<Position> SetLinedBlock(Position sourcePosition, int colOffset, int rowOffset, int repeat, Int64 markerID) 
	{
		var markedPositions = new List<Position>();
		while (repeat > 0) 
		{
			var nearPosition = new Position(sourcePosition.index, colOffset, rowOffset);
			var blockedGemInfo = Controller.MarkAsBlock(sourcePosition, nearPosition, markerID);
			var gemModel = blockedGemInfo.gemModel;
			if (gemModel != null)
			{
				GemView gemView;
				if (gemViews.TryGetValue(gemModel.id, out gemView)) {
					gemView.UpdateModel(gemModel);
					gemView.SetBlock(markerID);
				}
			}
			
			if (!blockedGemInfo.isNextMovable) { 
				break;
			} else { 
				// All positions on the same line must be included cause it would use as a timing of tweening.
				markedPositions.Add(sourcePosition); 
			}
			
			sourcePosition = nearPosition;
			repeat--;
		}

		return markedPositions;
	}

	MarkedPositionsInfo SetRadialBlock(Position sourcePosition, int repeat, Int64 markerID)
	{
		var markedPositions = new List<Position>();
		var isNextMovable = false;
		while (repeat > 0)
		{
			for (var row = -repeat; row <= repeat; row++) 
			{
				for (var col = -repeat; col <= repeat; col++) 
				{
					if (Math.Abs(col) < repeat && Math.Abs(row) < repeat) { continue; }

					var nextPosition = new Position(sourcePosition.index, col, row);
					var blockedGemInfo = Controller.MarkAsBlock(nextPosition, nextPosition, markerID);
					if (!isNextMovable && blockedGemInfo.isNextMovable) { isNextMovable = true; }
					
					var gemModel = blockedGemInfo.gemModel;
					if (gemModel == null) { continue; }
					
					GemView gemView;
					if (gemViews.TryGetValue(gemModel.id, out gemView)) {
						gemView.UpdateModel(gemModel);
						gemView.SetBlock(markerID);
					}
					markedPositions.Add(nextPosition);
				}
			}

			repeat--;
		}

		var markedPositionInfo = new MarkedPositionsInfo {
			positions = markedPositions,
			isNextMovable = isNextMovable
		};

		return markedPositionInfo;
	}

	ReplacedPositionsInfo SetSameTypeAsSpecial(
		Position sourcePosition, 
		GemType gemType, 
		Int64 replacerID, 
		string[] specialKeys, 
		int endurance
	) {
		var replacedPositions = new List<Position>();
		
		var count = 3;
		var blockedGemInfo = Controller.ReplaceSameTypeAsSpecial(sourcePosition, gemType, replacerID, specialKeys, endurance);
		foreach (var gemModel in blockedGemInfo.gemModels)
		{
			var gemViewRemoving = RemoveGemView(gemModel, false);
			var gemViewCreating = MakeGemView(gemModel);
			gemViewCreating.SetActive(false);
			AddAction(Model.currentTurn + count, (GOSequence sequence, float currentTime) => {
				sequence.InsertCallback(currentTime, () => {
					gemViewRemoving.ReturnToPool();
					gemViewCreating.SetActive(true);
				});
			});
			
			replacedPositions.Add(gemModel.Position);
			count += 3;
		}

		var replacedPositionInfo = new ReplacedPositionsInfo {
			positions = replacedPositions,
			latestCount = count
		};

		return replacedPositionInfo;
	}

	List<Position> SetSameTypeAsBlock(Position sourcePosition, GemType gemType, Int64 markerID)
	{
		var markedPositions = new List<Position>();

		var blockedGemInfo = Controller.MarkSameTypeAsBlock(sourcePosition, gemType, markerID);
		foreach (var gemModel in blockedGemInfo.gemModels)
		{
			GemView gemView;
			if (gemViews.TryGetValue(gemModel.id, out gemView)) {
				gemView.UpdateModel(gemModel);
				gemView.SetBlock(markerID);
			} 

			markedPositions.Add(gemModel.Position);
		}

		return markedPositions;
	}

	List<Position> SetAllTypeAsBlock(Position sourcePosition, Int64 markerID)
	{
		var markedPositions = new List<Position>();
		
		var blockedGemInfo = Controller.MarkAllGemsAsBlock(sourcePosition, markerID);
		foreach (var gemModel in blockedGemInfo.gemModels)
		{
			GemView gemView;
			if (gemViews.TryGetValue(gemModel.id, out gemView)) {
				gemView.UpdateModel(gemModel);
				gemView.SetBlock(markerID);
			} 

			markedPositions.Add(gemModel.Position);
		}

		return markedPositions;
	}

	void Swap(Position sourcePosition, Position nearPosition) 
	{
		AddAction(Model.currentTurn, (sequence, currentTime) => {
			SwapGems(Controller.Swap(sourcePosition, nearPosition), sequence, currentTime);
		});
	}

	void Merge(Position sourcePosition, Position nearPosition)
	{
		AddAction(Model.currentTurn, (sequence, currentTime) => {
			MergeGems(Controller.Merge(sourcePosition, nearPosition), sequence, currentTime);
		});
	}

	void SwapGems(List<GemModel> swappingGemModels, GOSequence sequence, float currentTime) 
	{
		foreach (var gemModel in swappingGemModels)
		{
			var gemView = gemViews[gemModel.id];
			var position = gemModel.Position;
			var nextPosition = new Vector3(position.col * gemSize.x, position.row * gemSize.y, 0);
			var gapOfTurn = gemView.PreservedFromMatch - Model.currentTurn + 1;
			sequence.Insert(currentTime, gemView.transform.GOLocalMove(
				nextPosition, 
				gapOfTurn * (TIME_PER_FRAME * FRAME_BY_TURN)
			).SetEase(GOEase.EaseOut));
		}
	}

	void MergeGems(MergedGemInfo mergedGemInfo, GOSequence sequence, float currentTime)
	{
		var mergerGemModel = mergedGemInfo.merger;
		var mergeeGemModel = mergedGemInfo.mergee;

		var mergerPosition = mergerGemModel.Position;
		var mergeePosition = mergeeGemModel.Position;

		var mergeeGemView = gemViews[mergeeGemModel.id];
		var mergeeNextPosition = new Vector3(mergerPosition.col * gemSize.x, mergerPosition.row * gemSize.y, 0);
		var gapOfTurn = mergeeGemView.PreservedFromMatch - (Model.currentTurn + 1);
		sequence.Insert(currentTime, mergeeGemView.transform.GOLocalMove(
			mergeeNextPosition, 
			gapOfTurn * (TIME_PER_FRAME * FRAME_BY_TURN)
		).SetEase(GOEase.EaseOut));
		
		var markerID = mergerGemModel.id;
		
		AddAction((mergeeGemView.PreservedFromMatch + 1), (GOSequence innerSequence, float innerCurrentTime) => {
			SetBlock(mergerPosition, markerID);
			var brokenGemInfo = Controller.Break(mergerPosition, markerID);
			BreakGems(brokenGemInfo.gemModel, true, innerSequence, innerCurrentTime);

			SetBlock(mergeePosition, markerID);
		});
	}

	void SetBlock(Position sourcePosition, Int64 markerID)
	{
		var blockedGemInfo = Controller.MarkAsBlock(sourcePosition, sourcePosition, markerID);
		var gemModel = blockedGemInfo.gemModel;
		if (gemModel != null)
		{
			GemView gemView;
			if (gemViews.TryGetValue(gemModel.id, out gemView)) {
				gemView.UpdateModel(gemModel);
				gemView.SetBlock(markerID);
			}
		}
	}

	void AddAction(Int64 turn, Action<GOSequence, float> action)
	{
		Queue<Action<GOSequence, float>> existingActionQueue;
		if (!actionQueueByTurn.TryGetValue(turn, out existingActionQueue)) 
		{
			existingActionQueue = new Queue<Action<GOSequence, float>>();
			actionQueueByTurn.Add(turn, existingActionQueue);
		}
		
		existingActionQueue.Enqueue(action);
	}

    public void PassTheLevelData(TextAsset levelData)
    {
        Model.levelData = levelData;
    }
}
