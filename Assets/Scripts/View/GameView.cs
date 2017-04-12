using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

class WatcherStatus
{
	Queue<Action<bool>> callbacks;

	public WatcherStatus() 
	{
		callbacks = new Queue<Action<bool>>();	
	}

	public void AddOnce(Action<bool> callback) 
	{
		callbacks.Enqueue(callback);
	}

	public void Update(bool succeed) 
	{
		while (callbacks.Count > 0) 
		{
			callbacks.Dequeue().Invoke(succeed);
		}
	}
}

public class GameView: BaseView<GameModel, GameController<GameModel>> 
{
	Bounds sampleBounds;
	Vector3 gemSize;
	SwipeInput swipeInput;
	GemView gemSelected;
	Dictionary<Int64, GemView> gemViews = new Dictionary<Int64, GemView>();
	Dictionary<string, WatcherStatus> watcherStatuses = new Dictionary<string, WatcherStatus>();
	Queue<Action> syncedAction = new Queue<Action>();
	const float BASE_DURATION = 0.3f;

	public override void Awake() 
	{
		base.Awake();
	}

	void Start() 
	{
		ResourceCache.LoadAll("");
		
		Controller.Init();
		MakeField();
		AlignField();
		ReadInput();
		Watch();
		Hello();
	}

	void Watch() 
	{
		watcherStatuses.Add("match", new WatcherStatus());
		watcherStatuses.Add("feed", new WatcherStatus());
		watcherStatuses.Add("fall", new WatcherStatus());

		StartCoroutine(StartWatch());
	}

	void Hello()
	{
		StartCoroutine(StartHello());
	}

	IEnumerator StartHello() 
	{
		yield return new WaitForSeconds(BASE_DURATION/8);
		Controller.GetAll().ForEach(gemModel => {
			gemViews[gemModel.id].Squash();
		});
	}

	IEnumerator StartWatch() 
	{
		while (true) 
		{
			Controller.TurnNext();
			UpdateWatcher("match", MatchGems(Controller.Match()));
			UpdateWatcher("feed", FeedGems(Controller.Feed()));
			UpdateWatcher("fall", FallGems(Controller.Fall()));
			while(syncedAction.Count > 0) {
				syncedAction.Dequeue().Invoke();
			}
			yield return new WaitForSeconds(BASE_DURATION);
		}
	}

	void UpdateWatcher(string act, bool succeed) 
	{
		var watcherStatus = watcherStatuses[act];
		watcherStatus.Update(succeed);
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
			var gemView = MakeGemView(gemModel);
			var position = gemModel.Position;
			gemView.SetLocalPosition(new Vector2(position.col * gemSize.x, position.row * gemSize.y));
		}
	}

	void RemoveGemView(GemModel gemModel, bool needToChaining) 
	{
		GemView gemView;
		if (gemViews.TryGetValue(gemModel.id, out gemView)) 
		{
			gemViews.Remove(gemModel.id);
			var gemObject = gemView.gameObject;
			gemObject.SetActive(false);
			Destroy(gemObject);
		}
		else
		{
			Debug.Log("Can't remove the gem! " + gemModel.ToString());
		}

		if (needToChaining)
		{
			ActBySpecialType(gemModel);
		}
	}

	GemView MakeGemView(GemModel gemModel) 
	{
		var gemView = ResourceCache.Instantiate(gemModel.Name, transform).GetComponent<GemView>();
		gemView.UpdateModel(gemModel);
		gemViews.Add(gemModel.id, gemView);
		return gemView;
	}

	void AlignField() 
	{
		var sizeOfField = gameObject.GetBounds();
		transform.localPosition = new Vector2(
			sampleBounds.extents.x-sizeOfField.extents.x, 
			sampleBounds.extents.y-sizeOfField.extents.y
		);
	}

	void ReadInput() 
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

	void ActByGemType(GemType gemType, Vector2 direction) 
	{
		switch (gemType)
		{
			case GemType.ChocoGem:
			StartCoroutine(StartLinedBreaking(
				gemSelected.Position, 
				direction, 
				gemSelected.Endurance, 
				gemSelected.ID, 
				BASE_DURATION, 
				needToChaining: false
			));
			break;

			case GemType.SuperGem:
			break;

			default:
			StartCoroutine(StartSwap(gemSelected.Position, direction));
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

	/* needToProtect */void ActBySpecialType(GemModel gemModel) 
	{
		switch (gemModel.Type)
		{
			case GemType.SuperGem:
			break;

			case GemType.ChocoGem:
			StartCoroutine(StartLinedBreaking(
				gemModel.Position, 
				GetRandomDirection(), 
				gemModel.endurance, 
				gemModel.id, 
				BASE_DURATION, 
				needToChaining: false
			));
			break;
		}
		
		switch (gemModel.specialKey)
		{
			case "H":
			StartCoroutine(StartLinedBreaking(
				gemModel.Position, 
				new Vector2{ x = -1, y = 0 }, 
				gemModel.endurance, 
				gemModel.id, 
				BASE_DURATION/4,
				needToChaining: true
			));
			StartCoroutine(StartLinedBreaking(
				gemModel.Position, 
				new Vector2{ x = 1, y = 0 }, 
				gemModel.endurance, 
				gemModel.id,
				BASE_DURATION/4,
				needToChaining: true
			));
			break;

			case "V":
			StartCoroutine(StartLinedBreaking(
				gemModel.Position, 
				new Vector2{ x = 0, y = -1 }, 
				gemModel.endurance, 
				gemModel.id,
				BASE_DURATION/4,
				needToChaining: true
			));
			StartCoroutine(StartLinedBreaking(
				gemModel.Position, 
				new Vector2{ x = 0, y = 1 }, 
				gemModel.endurance, 
				gemModel.id,
				BASE_DURATION/4,
				needToChaining: true
			));
			break;

			case "C":
			StartCoroutine(StartRadialBreaking(
				gemModel.Position,
				gemModel.endurance, 
				gemModel.id,
				needToChaining: true
			));

			break;
		}
	}

	IEnumerator StartRadialBreaking(Position sourcePosition, int repeat, Int64 markerID, bool needToChaining)
	{	
		var markedPositions = SetRadialBlock(sourcePosition, repeat, markerID, needToChaining);
		foreach(var markedPosition in markedPositions)
		{
			var brokenGemInfo = Controller.Break(markedPosition, markerID, repeat);
			if (brokenGemInfo.gemModels != null) {
				BreakGems(brokenGemInfo, needToChaining);
			} 
		}
		yield return null;
	}
		
	IEnumerator StartLinedBreaking(Position sourcePosition, Vector2 direction, int repeat, Int64 markerID, float duration, bool needToChaining) 
	{
		var colOffset = (int)direction.x;
		var rowOffset = (int)direction.y;

		var initialPosition = sourcePosition;
		var markedPositions = SetLinedBlock(sourcePosition, colOffset, rowOffset, repeat, markerID);
		foreach(var markedPosition in markedPositions)
		{
			var brokenGemInfo = Controller.Break(markedPosition, markerID, repeat);
			if (brokenGemInfo.gemModels != null) {
				BreakGems(brokenGemInfo, needToChaining || initialPosition != sourcePosition);
			} 

			yield return new WaitForSeconds(duration);
		}
	}

	List<Position> SetRadialBlock(Position sourcePosition, int repeat, Int64 markerID, bool needToChaining)
	{
		var markedPositions = new List<Position>();
		while (repeat > 0)
		{
			List<int[]> positionVectors = new List<int[]>();
			for (var row = -repeat; row <= repeat; row++) {
				for (var col = -repeat; col <= repeat; col++) {
					if (Math.Abs(col) < repeat && Math.Abs(row) < repeat) continue;

					var nearPosition = new Position(sourcePosition.index, col, row);
					var blockedGemInfo = Controller.MarkAsBlock(nearPosition, nearPosition, markerID);
					markedPositions.Add(nearPosition);

					if (blockedGemInfo.gemModels.Count > 0) {
						blockedGemInfo.gemModels.ForEach(gemModel => {
							GemView gemView;
							if (gemViews.TryGetValue(gemModel.id, out gemView)) {
								gemView.UpdateModel(gemModel);
								gemView.SetBlock();
							} 
						});
					} 
				}
			}

			repeat--;
		}

		return markedPositions;
	}

	List<Position> SetLinedBlock(Position sourcePosition, int colOffset, int rowOffset, int repeat, Int64 markerID) 
	{
		var markedPositions = new List<Position>();
		while (repeat > 0) 
		{
			var nearPosition = new Position(sourcePosition.index, colOffset, rowOffset);
			var blockedGemInfo = Controller.MarkAsBlock(sourcePosition, nearPosition, markerID);
			markedPositions.Add(sourcePosition);

			if (blockedGemInfo.gemModels.Count > 0) {
				blockedGemInfo.gemModels.ForEach(gemModel => {
					GemView gemView;
					if (gemViews.TryGetValue(gemModel.id, out gemView)) {
						gemView.UpdateModel(gemModel);
						gemView.SetBlock();
					}
				});
			}
			
			if (!blockedGemInfo.hasNext) {
				break;
			}
			
			sourcePosition = nearPosition;
			repeat--;
		}

		return markedPositions;
	}
	
	void BreakGems(BrokenGemInfo brokenGemInfo, bool needToChaining) 
	{
		brokenGemInfo.gemModels.ForEach(brokenGemModel => {
			RemoveGemView(brokenGemModel, needToChaining);
		});
	}

	IEnumerator StartSwap(Position sourcePosition, Vector2 direction) 
	{
		var colOffset = (int)direction.x;
		var rowOffset = (int)direction.y;
		var nearPosition = new Position(sourcePosition.index, colOffset, rowOffset);

		// 1. 
		AddSyncedAction(() => {
			SwapGems(Controller.Swap(sourcePosition, nearPosition));
			SubscribeWatcher("match", succeed => {
				if (!succeed) {
					SwapGems(Controller.Swap(nearPosition, sourcePosition));
				}
			});
		});
		yield return null;
	}

	void SubscribeWatcher(string act, Action<bool> action) 
	{
		watcherStatuses[act].AddOnce(action);
	}

	bool FeedGems(List<GemModel> feedingGemModels) 
	{
		feedingGemModels.ForEach(gemModel => {
			var gemView = MakeGemView(gemModel);
			var position = gemModel.Position;
			gemView.SetLocalPosition(new Vector2(position.col * gemSize.x, position.row * gemSize.y));
			gemView.SetActive(false);
		});

		return feedingGemModels.Count > 0;
	}

	bool MatchGems(List<MatchedLineInfo> matchedLineInfos) 
	{
		matchedLineInfos.ForEach(matchedLineInfo => {
			matchedLineInfo.gemModels.ForEach(gemModel => {
				RemoveGemView(gemModel, true);
			});
			
			if (matchedLineInfo.newAdded != null) {
				var gemView = MakeGemView(matchedLineInfo.newAdded);
				var position = matchedLineInfo.newAdded.Position;
				gemView.SetLocalPosition(new Vector2(position.col * gemSize.x, position.row * gemSize.y));
			}
		});

		return matchedLineInfos.Count > 0;
	}

	bool FallGems(List<GemInfo> fallingGemInfos) 
	{
		fallingGemInfos.ForEach(gemInfo => {
			var gemView = gemViews[gemInfo.id];
			var position = gemInfo.position;
			var nextPosition = new Vector3(position.col * gemSize.x, position.row * gemSize.y, 0);
			gemView.Open();
			gemView.DoLocalMove(nextPosition, BASE_DURATION);
		});

		return fallingGemInfos.Count > 0;
	}

	bool SwapGems(List<GemModel> swappingGemModels) 
	{
		swappingGemModels.ForEach(gemModel => {
			var gemView = gemViews[gemModel.id];
			var position = gemModel.Position;
			var nextPosition = new Vector3(position.col * gemSize.x, position.row * gemSize.y, 0);
			gemView.DoLocalMove(
				nextPosition,
				BASE_DURATION * (nextPosition - gemView.transform.localPosition).magnitude / gemSize.y
			);
		});

		return swappingGemModels.Count > 0;
	}

	void AddSyncedAction(Action action) 
	{
		syncedAction.Enqueue(action);
	}

	void Destroy() 
	{
		swipeInput.OnSwipeStart.RemoveAllListeners();
		swipeInput.OnSwipeCancel.RemoveAllListeners();
		swipeInput.OnSwipeEnd.RemoveAllListeners();
	}
}