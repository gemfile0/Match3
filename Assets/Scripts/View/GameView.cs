using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

class UpdateResult
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
	public bool isNextBreakable;
}

struct ReplacedPositionsInfo
{
	public List<Position> positions;
	public int latestCount;
}

public class GemRemovedEvent: UnityEvent<int> {}

public class GameView: BaseView<GameModel, GameController<GameModel>>  
{
	[NonSerializedAttribute]
	public UnityEvent OnPhaseNext = new UnityEvent();
	public GemRemovedEvent OnGemRemoved = new GemRemovedEvent();
	
	const Int64 FRAME_BY_TURN = 3;
	const float TIME_PER_FRAME = 0.012f;
	const Int64 A_QUARTER_OF_SHUFFLE = 25;
	float startOfWaiting = 0f;

	Bounds sampleBounds;
	Vector3 gemSize;
	Bounds sizeOfField;
	SwipeInput swipeInput;
	GemView gemSelected;
	UpdateResult updateResult;
	Dictionary<Int64, GemView> gemViews = new Dictionary<Int64, GemView>();
	Dictionary<Int64, Queue<Action<GOSequence, float>>> actionQueueByTurn = new Dictionary<Int64, Queue<Action<GOSequence, float>>>();

	GOSequence sequence;

	GameObject tiles;
	GameObject gems;
	GameObject gravities;
	bool isPlaying;
	bool isPlayingBefore;

	public void Awake()
	{
		gravities = new GameObject(Literals.Gravities);
		gravities.transform.SetParent(transform);

		tiles = new GameObject(Literals.Tiles);
		tiles.transform.SetParent(transform);

		gems = new GameObject(Literals.Gems);
		gems.transform.SetParent(transform);

		updateResult = new UpdateResult();
	}

	public override void Start()
	{
		base.Start();
		
		MakePoolOfGems();
		MakeField();
		AlignField();
		SubscribeInput();
		GetReady();
	}

	public override void OnDestroy() 
	{
		base.OnDestroy();

		UnsubscribeInput();

		gemViews = null;
		actionQueueByTurn = null;
		if (sequence != null) {
			sequence.Kill();
			sequence = null;
		}
	}

	public void OnModalVisibleChanged(bool visible)
	{
		var curretPosition = transform.localPosition;
		transform.localPosition = new Vector3(curretPosition.x, curretPosition.y, (visible) ? -9 : 0);
	}

	void GetReady()
	{
		UpdateChanges((Int64 passedTurn) => {
			AddAction(Model.currentTurn + passedTurn, (sequence, currentTime) => {
				sequence.InsertCallback(
					currentTime,
					() => {
						// Hello, Player!
						foreach (var gemModel in Controller.GetAll())
						{
							gemViews[gemModel.id].Squash();
						}
					}
				);
			});
		});
		StartCoroutine(StartWatch());
	}

	IEnumerator StartWatch()
	{
		yield return new WaitForSeconds(FRAME_BY_TURN * TIME_PER_FRAME * 25);
		while (true)
		{
			WatchPhase();
			WatchMatchables();
			yield return null;
		}
	}

	void WatchMatchables()
	{
		if (isPlaying) { startOfWaiting = 0f; return; }
		if (startOfWaiting == 0f) { startOfWaiting = Time.time;}
		if ((Time.time - startOfWaiting) > 3f)
		{
			CheckHasAnyMatchableGems();
			startOfWaiting = 0f;
		}
	}

	void WatchPhase()
	{
		if (isPlayingBefore != isPlaying) 
		{
			if (isPlayingBefore == false && isPlaying == true && Controller.HasAnyChange()) {
				OnPhaseNext.Invoke();
			}
		}
		isPlayingBefore = isPlaying;
	}

	void MakePoolOfGems()
	{
		var gemViews = new List<GemView>();
		foreach (GemType gemType in Enum.GetValues(typeof(GemType)))
		{
			if (gemType == GemType.Nil) { continue; }

			var countOfPool = 20;
			while (countOfPool > 0)
			{
				gemViews.Add(ResourceCache.Instantiate<GemView>(gemType.ToString()));
				countOfPool -= 1;
			}
		}

		foreach (var gemView in gemViews)
		{
			gemView.ReturnToPool(false);
		}
	}

	void MakeField() 
	{
		var sampleGem = ResourceCache.Instantiate(Literals.RedGem);
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
			if (tileModel.Type == TileType.Movable) {
				MakeTileView(tileModel);
			}
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
		var tileView = ResourceCache.Instantiate<TileView>(Literals.Tile, tiles.transform);
		tileView.UpdateModel(tileModel);
		tileView.transform.localPosition 
			= new Vector2(tileModel.Position.col * gemSize.x, tileModel.Position.row * gemSize.y);
		return tileView;
	}

	GravityView MakeGravityView(GravityModel gravityModel)
	{
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
			// Debug.Log("Can't remove the gem! " + gemModel.ToString());
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
		sizeOfField = gameObject.GetBounds();
		transform.localPosition = new Vector2(
			sampleBounds.extents.x - sizeOfField.extents.x, 
			sampleBounds.extents.y - sizeOfField.extents.y
		);
	}

	void MatchGems(List<MatchedLineInfo> matchedLineInfos, GOSequence sequence, float currentTime) 
	{
		foreach (var matchedLineInfo in matchedLineInfos)
		{
			var newAdded = matchedLineInfo.newAdded;
			var combindedLocation = default(Vector2);
			if (newAdded != null) 
			{
				combindedLocation = new Vector2(newAdded.Position.col * gemSize.x, newAdded.Position.row * gemSize.y);
			}

			foreach (var gemModel in matchedLineInfo.gemModels)
			{
				var gemView = RemoveGemView(gemModel, true);
				if (gemView == null) { continue; }
				
				sequence.InsertCallback(currentTime, () => {
					gemView.ReturnToPool(true, combindedLocation);
					OnGemRemoved.Invoke((int)gemModel.Type);
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
			MakeGemView(gemModel).Hide();
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
				gemSelected.Squash();
			}
		});
		swipeInput.OnSwipeCancel.AddListener(() => {
			gemSelected = null;
		});
		swipeInput.OnSwipeEnd.AddListener(swipeInfo => {
			if (isPlaying) { 
				Toast.Show("A coroutine is still running.", .8f);
				return; 
			}
			
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
		var colOffset = (int)direction.x;
		var rowOffset = (int)direction.y;
		if (!Controller.IsAcceptableIndex(sourcePosition, colOffset, rowOffset)) { return; }
		var nearPosition = Position.Get(sourcePosition, colOffset, rowOffset);
		if (!Controller.IsMovableTile(nearPosition)) { return; }

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
				breakingOffset: 5,
				comparingToMoveon: CompareIsMovable,
				isChaining: false
			);
			UpdateChanges();
		}
		else 
		{
			Swap(sourcePosition, nearPosition);
			UpdateChanges((Int64 passedTurn) => {
				Swap(nearPosition, sourcePosition, 1);
			});
		}
	}

	IEnumerator StartUpdateChanges(Action<Int64> OnNoAnyMatches)
	{
		isPlaying = true;
		
		sequence = GOTween.Sequence().SetAutoKill(false);

		var currentFrame = 0;
		var startTurn = Model.currentTurn;
		var noUpdateCount = 0;
		var sumOfNoUpdate = 0;
		while (true)
		{
			if (currentFrame % FRAME_BY_TURN == 0)
			{
				var passedTurn = Model.currentTurn - startTurn;
				var currentTime = FRAME_BY_TURN * TIME_PER_FRAME * passedTurn;
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
				
				updateResult.matchResult = Controller.Match();
				updateResult.feedResult = Controller.Feed();
				updateResult.fallResult = Controller.Fall();
				
				if (passedTurn == 6) {
					if (updateResult.matchResult.Count == 0 && OnNoAnyMatches != null) {
						OnNoAnyMatches(passedTurn);
					} 
				}
				
				if (updateResult.HasAnyResult) {
					noUpdateCount = 0;

					MatchGems(updateResult.matchResult, sequence, currentTime);
					FeedGems(updateResult.feedResult, sequence, currentFrame);
					FallGems(updateResult.fallResult, sequence, currentTime);
				} else {
					noUpdateCount++;
					sumOfNoUpdate++;
				}

				Controller.TurnNext();

				// Debug.Log(noUpdateCount + ", " + sequence.IsComplete + ", " + countOfAction);
				if (passedTurn > 20) { yield return null; }
				if (noUpdateCount > 20 && sequence.IsComplete && countOfAction == 0) { break; }
			}

			currentFrame += 1;
		}

		sequence.Kill();
		isPlaying = false;
	}

	void CheckHasAnyMatchableGems()
	{
		var matchableGems = Controller.GetMatchableGems();
		if (matchableGems.Count == 0) 
		{
			AddAction(Model.currentTurn, (sequence, currentTime) => {
				Toast.Show("There's doesn't have any match.", 3f);
			});
			AddAction(Model.currentTurn + A_QUARTER_OF_SHUFFLE, (sequence, currentTime) => {
				foreach (var gemModel in Controller.Shuffle(Model.currentTurn + A_QUARTER_OF_SHUFFLE * 3)) {
					var gemView = gemViews[gemModel.id];
					var aQuarterOfTime = A_QUARTER_OF_SHUFFLE * (TIME_PER_FRAME * FRAME_BY_TURN);
					sequence.Insert(currentTime, gemView.transform.GOMove(Vector3.zero, aQuarterOfTime).SetEase(GOEase.EaseIn));
					sequence.Insert(currentTime + aQuarterOfTime, gemView.transform.GOLocalMove(
						new Vector2(gemModel.Position.col * gemSize.x, gemModel.Position.row * gemSize.y), 
						aQuarterOfTime
					).SetEase(GOEase.EaseOut));
				}
			});
			UpdateChanges();
		} 
		else 
		{
			var matchableGem = matchableGems[UnityEngine.Random.Range(0, matchableGems.Count)];
			if (matchableGem.sourceGemModel != null) { gemViews[matchableGem.sourceGemModel.id].Highlight(); }
			if (matchableGem.nearGemModel != null) { gemViews[matchableGem.nearGemModel.id].Highlight(); }
		}
	}

	void UpdateChanges(Action<Int64> OnNoAnyMatches = null) 
	{
		Controller.TakeSnapshot();
		StartCoroutine(StartUpdateChanges(OnNoAnyMatches));
	}

	void ActByChaining(
		GemType gemType, 
		string specialKey, 
		Position sourcePosition, 
		int repeat, 
		Int64 markerID, 
		Vector2 direction
	) {
		// Debug.Log("ActByChaining : " + gemType + ", " + specialKey + ", " + repeat + ", " + direction);
		switch (gemType)
		{
			case GemType.SuperGem:
			SameTypeBreaking(sourcePosition, GetRandomType(), markerID, isChaining: true);
			break;

			case GemType.ChocoGem:
			LinedBreaking(
				sourcePosition, GetRandomDirection(), repeat, markerID, breakingOffset: 5, comparingToMoveon: CompareIsMovable, isChaining: true
			);
			break;
		}
		
		switch (specialKey)
		{
			case Literals.SP:
			EmptyBlockBreaking(1, markerID);
			SameTypeBreaking(sourcePosition, gemType, markerID, isChaining: true);
			break;

			case Literals.H:
			LinedBreaking(sourcePosition, new Vector2{ x = -1, y = 0 }, repeat, markerID, comparingToMoveon: CompareIsBreakable, isChaining: true);
			LinedBreaking(sourcePosition, new Vector2{ x = 1, y = 0 }, repeat, markerID, comparingToMoveon: CompareIsBreakable, isChaining: true);
			break;

			case Literals.V:
			LinedBreaking(sourcePosition, new Vector2{ x = 0, y = -1 }, repeat, markerID, comparingToMoveon: CompareIsBreakable, isChaining: true);
			LinedBreaking(sourcePosition, new Vector2{ x = 0, y = 1 }, repeat, markerID, comparingToMoveon: CompareIsBreakable, isChaining: true);
			break;

			case Literals.C:
			RadialBreaking(sourcePosition, repeat, markerID, isChaining: true);
			break;

			case Literals.HH:
			case Literals.VV:
			case Literals.HV:
			case Literals.VH:
			LinedBreaking(sourcePosition, new Vector2{ x = -1, y = 0 }, repeat, markerID, comparingToMoveon: CompareIsBreakable, isChaining: true);
			LinedBreaking(sourcePosition, new Vector2{ x = 1, y = 0 }, repeat, markerID, comparingToMoveon: CompareIsBreakable, isChaining: true);
			LinedBreaking(sourcePosition, new Vector2{ x = 0, y = -1 }, repeat, markerID, comparingToMoveon: CompareIsBreakable, isChaining: true);
			LinedBreaking(sourcePosition, new Vector2{ x = 0, y = 1 }, repeat, markerID, comparingToMoveon: CompareIsBreakable, isChaining: true);
			break;

			case Literals.CC:
			RadialBreaking(sourcePosition, repeat, markerID, isChaining: true);
			break;

			case Literals.SQSQ:
			LinedRadialBreaking(
				sourcePosition, direction, repeat, markerID, isChaining: true, breakingOffset: 5, comparingToMoveon: CompareIsMovable
			);
			break;

			case Literals.SPSP:
			AllTypeBreaking(sourcePosition, markerID, isChaining: true);
			break;

			case Literals.CH:
			case Literals.HC:
			LinedRadialBreaking(
				sourcePosition, new Vector2{ x = -1, y = 0 }, repeat, markerID, isChaining: true, comparingToMoveon: CompareIsBreakable
			);
			LinedRadialBreaking(
				sourcePosition, new Vector2{ x = 1, y = 0 }, repeat, markerID, isChaining: true, comparingToMoveon: CompareIsBreakable
			);
			break;

			case Literals.CV:
			case Literals.VC:
			LinedRadialBreaking(
				sourcePosition, new Vector2{ x = 0, y = -1 }, repeat, markerID, isChaining: true, comparingToMoveon: CompareIsBreakable
			);
			LinedRadialBreaking(
				sourcePosition, new Vector2{ x = 0, y = 1 }, repeat, markerID, isChaining: true, comparingToMoveon: CompareIsBreakable
			);
			break;

			case Literals.HSQ:
			case Literals.SQH:
			case Literals.VSQ:
			case Literals.SQV:
			EmptyBlockBreaking(1, markerID);
			LinedBreaking(
				sourcePosition, 
				direction, 
				repeat, 
				markerID, 
				isChaining: true, 
				breakingOffset: 5, 
				comparingToMoveon: CompareIsMovable, 
				onComplete: (latestPosition) => {
					LinedBreaking(
						latestPosition, new Vector2{ x = -1, y = 0 }, int.MaxValue, markerID, comparingToMoveon: CompareIsBreakable, isChaining: true
					);
					LinedBreaking(
						latestPosition, new Vector2{ x = 1, y = 0 }, int.MaxValue, markerID, comparingToMoveon: CompareIsBreakable, isChaining: true
					);
					LinedBreaking(
						latestPosition, new Vector2{ x = 0, y = -1 }, int.MaxValue, markerID, comparingToMoveon: CompareIsBreakable, isChaining: true
					);
					LinedBreaking(
						latestPosition, new Vector2{ x = 0, y = 1 }, int.MaxValue, markerID, comparingToMoveon: CompareIsBreakable, isChaining: true
					);
				}
			);
			break;

			case Literals.CSQ:
			case Literals.SQC:
			EmptyBlockBreaking(1, markerID);
			LinedBreaking(
				sourcePosition, 
				direction, 
				repeat, 
				markerID, 
				isChaining: true, 
				breakingOffset: 5, 
				comparingToMoveon: CompareIsMovable,
				onComplete: (latestPosition) => {
					RadialBreaking(latestPosition, 1, markerID, isChaining: true);
				}
			);
			break;

			case Literals.HSP:
			case Literals.SPH:
			case Literals.VSP:
			case Literals.SPV:
			SameTypeReplacing(
				sourcePosition, gemType, markerID, new string[]{ Literals.H, Literals.V }, repeat, isChaining: true
			);
			break;

			case Literals.CSP:
			case Literals.SPC:
			SameTypeReplacing(
				sourcePosition, gemType, markerID, new string[]{ Literals.C }, repeat, isChaining: true
			);
			break;

			case Literals.SPSQ:
			case Literals.SQSP:
			SameTypeReplacing(
				sourcePosition, GetRandomType(), markerID, new string[]{ Literals.SQ }, repeat, isChaining: true
			);
			break;
		}
	}

	bool CompareIsBreakable(BlockedGemInfo blockedGemInfo)
	{
		return blockedGemInfo.isNextBreakable;
	} 

	bool CompareIsBreakable(MarkedPositionsInfo markedPositionsInfo)
	{
		return markedPositionsInfo.isNextBreakable;
	} 

	bool CompareIsMovable(BlockedGemInfo blockedGemInfo)
	{
		return blockedGemInfo.isNextMovable;
	} 

	bool CompareIsMovable(MarkedPositionsInfo markedPositionsInfo)
	{
		return markedPositionsInfo.isNextMovable;
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
		int breakingOffset = 1,
		Func<MarkedPositionsInfo, bool> comparingToMoveon = null
	) {	
		var colOffset = (int)direction.x;
		var rowOffset = (int)direction.y;

		var count = breakingOffset;
		while (repeat > 0)
		{
			var markedPositionsInfo = SetRadialBlock(sourcePosition, 1, markerID, colOffset, rowOffset);
			AddAction(Model.currentTurn + count, (GOSequence sequence, float currentTime) => {
				foreach (var markedPosition in markedPositionsInfo.positions)
				{
					var brokenGemInfo = Controller.Break(markedPosition, markerID);
					BreakGems(brokenGemInfo.gemModel, isChaining, sequence, currentTime);
				}
			});

			if (!comparingToMoveon(markedPositionsInfo)) { break; }
			
			sourcePosition = Position.Get(sourcePosition, colOffset, rowOffset);
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
		Func<BlockedGemInfo, bool> comparingToMoveon = null,
		Action<Position> onComplete = null
	) {
		var colOffset = (int)direction.x;
		var rowOffset = (int)direction.y;

		var markedPositions = SetLinedBlock(sourcePosition, repeat, markerID, colOffset, rowOffset, comparingToMoveon);
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
			OnGemRemoved.Invoke((int)gemModel.Type);
		});
	}

	List<Position> SetLinedBlock(
		Position sourcePosition, 
		int repeat, 
		Int64 markerID,
		int colOffset, 
		int rowOffset, 
		Func<BlockedGemInfo, bool> comparingToMoveon = null
	)  {
		var markedPositions = new List<Position>();
		while (repeat > 0) 
		{
			var blockedGemInfo = Controller.MarkAsBlock(sourcePosition, markerID);
			var gemModel = blockedGemInfo.gemModel;
			if (gemModel != null)
			{
				GemView gemView;
				if (gemViews.TryGetValue(gemModel.id, out gemView)) {
					gemView.UpdateModel(gemModel);
					gemView.SetBlock(markerID);
				}
			}
			
			// All positions on the same line must be included cause it would use as a timing of tweening.
			markedPositions.Add(sourcePosition); 

			var isNextAcceptable = Controller.IsAcceptableIndex(sourcePosition, colOffset, rowOffset);
			if (isNextAcceptable)
			{
				var nextPosition = Position.Get(sourcePosition, colOffset, rowOffset);
				blockedGemInfo.isNextMovable = Controller.IsMovableTile(nextPosition);
				blockedGemInfo.isNextBreakable = Controller.IsBreakableTile(nextPosition);

				sourcePosition = nextPosition;
			}

			if (!isNextAcceptable || !comparingToMoveon(blockedGemInfo)) { break; } 
			repeat -= 1;
		}

		return markedPositions;
	}

	MarkedPositionsInfo SetRadialBlock(Position sourcePosition, int repeat, Int64 markerID, int colOffset = 0, int rowOffset = 0)
	{
		var markedPositions = new List<Position>();
		while (repeat > 0)
		{
			for (var row = -repeat; row <= repeat; row++) 
			{
				for (var col = -repeat; col <= repeat; col++) 
				{
					if (Math.Abs(col) < repeat && Math.Abs(row) < repeat) { continue; }
					if (!Controller.IsAcceptableIndex(sourcePosition, col, row)) { continue; }

					var nextPosition = Position.Get(sourcePosition, col, row);
					var blockedGemInfo = Controller.MarkAsBlock(nextPosition, markerID);
					
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

		var isNextAcceptable = Controller.IsAcceptableIndex(sourcePosition, colOffset, rowOffset);
		var markedPositionInfo = new MarkedPositionsInfo {
			positions = markedPositions,
			isNextMovable = isNextAcceptable && Controller.IsMovableTile(Position.Get(sourcePosition, colOffset, rowOffset)),
			isNextBreakable = isNextAcceptable && Controller.IsBreakableTile(Position.Get(sourcePosition, colOffset, rowOffset))
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
					OnGemRemoved.Invoke((int)gemModel.Type);
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

	void Swap(Position sourcePosition, Position nearPosition, int turnOffset = 0) 
	{
		AddAction(Model.currentTurn + turnOffset, (sequence, currentTime) => {
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
			));
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
		var blockedGemInfo = Controller.MarkAsBlock(sourcePosition, markerID);
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

    public void PassTheLevelModel(LevelModel levelModel)
    {
        Model.levelModel = levelModel;
    }
}
