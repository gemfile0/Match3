using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

class UpdateInfo
{
	public bool hasAnyMatches = false;
	public Int64 passedTurn = 0;
}

class UpdateResult
{
	public List<MatchedLineInfo> matchResult;
	public List<GemModel> feedResult;
	public List<GemInfo> fallResult;
	public bool hadUpdated = false;

	public bool HasAnyResult 
	{
		get { return matchResult.Count > 0 || feedResult.Count > 0 || fallResult.Count > 0; }
	}
}

public class GameView: BaseView<GameModel, GameController<GameModel>>  
{
	readonly Int64 FRAME_BY_TURN = 3;

	Bounds sampleBounds;
	Vector3 gemSize;
	SwipeInput swipeInput;
	GemView gemSelected;
	Dictionary<Int64, GemView> gemViews = new Dictionary<Int64, GemView>();
	Dictionary<Int64, Queue<Action<Sequence, float>>> actionQueueByTurn = new Dictionary<Int64, Queue<Action<Sequence, float>>>();

	Sequence sequence;

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

		gemViews.Clear();
		gemViews = null;

		actionQueueByTurn.Values.ForEach(actionQueue => {
			actionQueue.Clear();
		});
		actionQueueByTurn.Clear();
		actionQueueByTurn = null;

		sequence.Kill();
		sequence = null;
	}

	IEnumerator StartHello() 
	{
		yield return new WaitForSeconds(0.02f * FRAME_BY_TURN);
		Controller.GetAll().ForEach(gemModel => {
			gemViews[gemModel.id].Squash();
		});
	}

	void MakeField() 
	{
		var sampleGem = ResourceCache.Instantiate("RedGem");
		sampleBounds = sampleGem.GetBounds();
		gemSize = sampleBounds.size;
		Destroy(sampleGem);

		var gemModels = Model.GemModels;
		foreach (var gemModel in gemModels) 
		{
			if (gemModel.Type == GemType.EmptyGem) { continue; }
			var gemView = MakeGemView(gemModel);
			var position = gemModel.Position;

			gemView.SetLocalPosition(new Vector2(position.col * gemSize.x, position.row * gemSize.y));
		}
	}

	GemView MakeGemView(GemModel gemModel) 
	{
		var gemView = ResourceCache.Instantiate(gemModel.Name, transform).GetComponent<GemView>();
		gemView.UpdateModel(gemModel);
		gemViews.Add(gemModel.id, gemView);
		return gemView;
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
			ActBySpecialType(gemModel);
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

	void MatchGems(List<MatchedLineInfo> matchedLineInfos, Sequence sequence, float currentTime) 
	{
		foreach (var matchedLineInfo in matchedLineInfos)
		{
			foreach (var gemModel in matchedLineInfo.gemModels)
			{
				var gemView = RemoveGemView(gemModel, true);
				if (gemView == null) { continue; }
				sequence.InsertCallback(currentTime, () => {
					gemView.gameObject.SetActive(false);
					Destroy(gemView.gameObject);
				});
			}

			if (matchedLineInfo.newAdded != null) 
			{
				var gemView = MakeGemView(matchedLineInfo.newAdded);
				gemView.SetActive(false);
				var position = matchedLineInfo.newAdded.Position;
				gemView.SetLocalPosition(new Vector2(position.col * gemSize.x, position.row * gemSize.y));

				sequence.InsertCallback(currentTime, () => {
					gemView.Reveal();
				});
			}
		}
	}

	void FallGems(List<GemInfo> fallingGemInfos, Sequence sequence, float currentTime) 
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
			var duration = gapOfTurn * (0.02f * FRAME_BY_TURN);
			sequence.Insert(currentTime, gemView.transform.DOLocalMove(
				nextPosition, 
				duration
			).SetEase(Ease.Linear));
			if (gemInfo.endOfFall) {
				sequence.InsertCallback(currentTime + duration, () => gemView.Squash());
			}
		};
	}

	void FeedGems(List<GemModel> feedingGemModels, Sequence sequence, float currentTime) 
	{
		foreach (var gemModel in feedingGemModels)
		{
			var gemView = MakeGemView(gemModel);
			var position = gemModel.Position;
			gemView.SetLocalPosition(new Vector2(position.col * gemSize.x, position.row * gemSize.y));
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
		swipeInput.OnSwipeCancel.AddListener(swipeInfo => {
			gemSelected = null;
		});
		swipeInput.OnSwipeEnd.AddListener(swipeInfo => {
			if (gemSelected != null) {
				ActByGemType(gemSelected.Type, swipeInfo.direction);
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

	void ActByGemType(GemType gemType, Vector2 direction) 
	{
		var selectedPosition = gemSelected.Position;
		var nearPosition = new Position(selectedPosition.index, (int)direction.x, (int)direction.y);

		switch (gemType)
		{
			case GemType.ChocoGem:
			LinedBreaking(
				selectedPosition, 
				direction, 
				gemSelected.Endurance, 
				gemSelected.ID, 
				isChaining: false,
				breakingOffset: 5
			);
			UpdateChanges();
			break;

			case GemType.SuperGem:
			var nearGemModel = Controller.GetGemModel(nearPosition);
			SameColorBreaking(
				nearGemModel.Type,
				gemSelected.ID, 
				isChaining: false
			);
			UpdateChanges();
			break;

			default:
			
			Swap(selectedPosition, nearPosition);
			var updateInfo = UpdateChanges();
			if (!updateInfo.hasAnyMatches) {
				Swap(nearPosition, selectedPosition);
				UpdateChanges(FRAME_BY_TURN * 0.02f * updateInfo.passedTurn);
			}
			break;
		}
	}

	UpdateInfo UpdateChanges(float latestTime = 0f) 
	{
		sequence = DOTween.Sequence().SetEase(Ease.InOutSine);

		var currentFrame = 0;
		var startTurn = Model.currentTurn;
		var noUpdateCount = 0;
		var updateInfo = new UpdateInfo();
		while (true)
		{
			if (currentFrame % FRAME_BY_TURN == 0)
			{
				var passedTurn = Model.currentTurn - startTurn;
				var currentTime = latestTime + FRAME_BY_TURN * 0.02f * passedTurn;
				// Debug.Log("CurrentTurn : " + Model.currentTurn);
				// Debug.Log("currentTime : " + currentTime + ", " + (Model.currentTurn - startTurn));

				Queue<Action<Sequence, float>> actionQueue;
				if (actionQueueByTurn.TryGetValue(Model.currentTurn, out actionQueue)) {
					while (actionQueue.Count > 0) {
						var action = actionQueue.Dequeue();
						action.Invoke(sequence, currentTime);
					}
				}
				
				var updateResult = new UpdateResult {
					matchResult = Controller.Match(),
					feedResult = Controller.Feed(),
					fallResult = Controller.Fall(),
				};

				if (passedTurn == 6 && updateResult.matchResult.Count > 0) {
					updateInfo.hasAnyMatches = true;
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
				if (noUpdateCount > 6) {
					break;
				}
			}

			currentFrame += 1;
		}

		updateInfo.passedTurn = Model.currentTurn - startTurn;
		return updateInfo;
	}

	void ActBySpecialType(GemModel gemModel) 
	{
		switch (gemModel.Type)
		{
			case GemType.SuperGem:
			break;

			case GemType.ChocoGem:
			LinedBreaking(
				gemModel.Position, 
				GetRandomDirection(), 
				gemModel.endurance, 
				gemModel.id, 
				isChaining: true,
				breakingOffset: 5
			);
			break;
		}
		
		switch (gemModel.specialKey)
		{
			case "H":
			LinedBreaking(
				gemModel.Position, 
				new Vector2{ x = -1, y = 0 }, 
				gemModel.endurance, 
				gemModel.id, 
				isChaining: true
			);
			LinedBreaking(
				gemModel.Position, 
				new Vector2{ x = 1, y = 0 }, 
				gemModel.endurance, 
				gemModel.id,
				isChaining: true
			);
			break;

			case "V":
			LinedBreaking(
				gemModel.Position, 
				new Vector2{ x = 0, y = -1 }, 
				gemModel.endurance, 
				gemModel.id,
				isChaining: true
			);
			LinedBreaking(
				gemModel.Position, 
				new Vector2{ x = 0, y = 1 }, 
				gemModel.endurance, 
				gemModel.id,
				isChaining: true
			);
			break;

			case "C":
			RadialBreaking(
				gemModel.Position,
				gemModel.endurance, 
				gemModel.id,
				isChaining: true
			);

			break;
		}
	}

	Vector2 GetRandomDirection()
	{
		var random = new System.Random();
		var allDirections = new Vector2[] {
			new Vector2{ x = 1,  y = 0 }, 
			new Vector2{ x = -1, y = 0 }, 
			new Vector2{ x = 0,  y = 1 }, 
			new Vector2{ x = 0,  y = -1 }
		};
		return allDirections[random.Next(allDirections.Length)];
	}

	void SameColorBreaking(GemType gemType, Int64 markerID, bool isChaining)
	{	
		var markedPositions = SetSameColorBlock(gemType, markerID);
		foreach(var markedPosition in markedPositions)
		{
			AddAction(Model.currentTurn + 1, (Sequence sequence, float currentTime) => {
				var brokenGemInfo = Controller.Break(markedPosition, markerID, int.MaxValue);
				if (brokenGemInfo.gemModels != null) {
					BreakGems(brokenGemInfo, isChaining, sequence, currentTime);
				}
			});
		}
	}

	void RadialBreaking(Position sourcePosition, int repeat, Int64 markerID, bool isChaining)
	{	
		var markedPositions = SetRadialBlock(sourcePosition, repeat, markerID);
		foreach(var markedPosition in markedPositions)
		{
			AddAction(Model.currentTurn + 1, (Sequence sequence, float currentTime) => {
				var brokenGemInfo = Controller.Break(markedPosition, markerID, repeat);
				if (brokenGemInfo.gemModels != null) {
					BreakGems(brokenGemInfo, isChaining, sequence, currentTime);
				}
			});
		}
	}
		
	void LinedBreaking(Position sourcePosition, Vector2 direction, int repeat, Int64 markerID, bool isChaining, int breakingOffset = 1) 
	{
		var colOffset = (int)direction.x;
		var rowOffset = (int)direction.y;

		var markedPositions = SetLinedBlock(sourcePosition, colOffset, rowOffset, repeat, markerID);
		var count = breakingOffset;
		foreach(var markedPosition in markedPositions)
		{
			AddAction(Model.currentTurn + count, (Sequence sequence, float currentTime) => {
				var brokenGemInfo = Controller.Break(markedPosition, markerID, repeat);
				if (brokenGemInfo.gemModels != null) {
					BreakGems(brokenGemInfo, isChaining || markedPosition != sourcePosition, sequence, currentTime);
				} 
			});
			count += breakingOffset;
		}
	}

	void BreakGems(BrokenGemInfo brokenGemInfo, bool needToChaining, Sequence sequence, float currentTime) 
	{
		foreach (var gemModel in brokenGemInfo.gemModels)
		{
			var gemView = RemoveGemView(gemModel, needToChaining);
			if (gemView == null) { continue; }
			sequence.InsertCallback(currentTime, () => {
				gemView.gameObject.SetActive(false);
				Destroy(gemView.gameObject);
			});
		}
	}

	List<Position> SetLinedBlock(Position sourcePosition, int colOffset, int rowOffset, int repeat, Int64 markerID) 
	{
		var markedPositions = new List<Position>();
		while (repeat > 0) 
		{
			var nearPosition = new Position(sourcePosition.index, colOffset, rowOffset);
			var blockedGemInfo = Controller.MarkAsBlock(sourcePosition, nearPosition, markerID);
			markedPositions.Add(sourcePosition);

			blockedGemInfo.gemModels.ForEach(gemModel => {
				GemView gemView;
				if (gemViews.TryGetValue(gemModel.id, out gemView)) {
					gemView.UpdateModel(gemModel);
					gemView.SetBlock();
				}
			});
			
			if (!blockedGemInfo.hasNext) {
				break;
			}
			
			sourcePosition = nearPosition;
			repeat--;
		}

		return markedPositions;
	}

	List<Position> SetRadialBlock(Position sourcePosition, int repeat, Int64 markerID)
	{
		var markedPositions = new List<Position>();
		while (repeat > 0)
		{
			for (var row = -repeat; row <= repeat; row++) {
				for (var col = -repeat; col <= repeat; col++) {
					if (Math.Abs(col) < repeat && Math.Abs(row) < repeat) { continue; }

					var nearPosition = new Position(sourcePosition.index, col, row);
					if (!nearPosition.IsBoundaryIndex()) { continue; }

					var blockedGemInfo = Controller.MarkAsBlock(nearPosition, nearPosition, markerID);
					markedPositions.Add(nearPosition);

					blockedGemInfo.gemModels.ForEach(gemModel => {
						GemView gemView;
						if (gemViews.TryGetValue(gemModel.id, out gemView)) {
							gemView.UpdateModel(gemModel);
							gemView.SetBlock();
						} 
					});
				}
			}

			repeat--;
		}

		return markedPositions;
	}

	List<Position> SetSameColorBlock(GemType gemType, Int64 markerID)
	{
		var markedPositions = new List<Position>();
			
		var blockedGemInfo = Controller.MarkSameGemsAsBlock(gemType, markerID);
		blockedGemInfo.gemModels.ForEach(gemModel => {
			GemView gemView;
			if (gemViews.TryGetValue(gemModel.id, out gemView)) {
				gemView.UpdateModel(gemModel);
				gemView.SetBlock();
			} 

			markedPositions.Add(gemModel.Position);
		});

		return markedPositions;
	}

	void Swap(Position sourcePosition, Position nearPosition) 
	{
		AddAction(Model.currentTurn, (sequence, currentTime) => {
			SwapGems(Controller.Swap(sourcePosition, nearPosition), sequence, currentTime);
		});
	}

	void SwapGems(List<GemModel> swappingGemModels, Sequence sequence, float currentTime) 
	{
		foreach (var gemModel in swappingGemModels)
		{
			var gemView = gemViews[gemModel.id];
			var position = gemModel.Position;
			var nextPosition = new Vector3(position.col * gemSize.x, position.row * gemSize.y, 0);
			var gapOfTurn = gemView.PreservedFromMatch - Model.currentTurn + 1;
			sequence.Insert(currentTime, gemView.transform.DOLocalMove(
				nextPosition, 
				gapOfTurn * (0.02f * FRAME_BY_TURN)
			));
		}
	}

	void AddAction(Int64 turn, Action<Sequence, float> action)
	{
		Queue<Action<Sequence, float>> existingActionQueue;
		if (!actionQueueByTurn.TryGetValue(turn, out existingActionQueue)) 
		{
			existingActionQueue = new Queue<Action<Sequence, float>>();
			actionQueueByTurn.Add(turn, existingActionQueue);
		}
		
		existingActionQueue.Enqueue(action);
	}

    public void PassTheLevelData(TextAsset levelData)
    {
        Model.levelData = levelData;
    }
}
