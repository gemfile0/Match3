using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Collections;

public class GameView: BaseView<GameModel, GameController<GameModel>> {
	Bounds sampleBounds;
	Vector3 gemSize;
	SwipeInput swipeInput;
	GemView gemSelected;
	Dictionary<Int64, GemView> gemViews = new Dictionary<Int64, GemView>();
	
	void Start() {
		ResourceCache.LoadAll("");
		
		Controller.Init();
		MakeField();
		AlignField();
		ReadInput();
	}

	void MakeField() {
		var gemModels = Model.GemModels;
		var sampleGem = ResourceCache.Instantiate(gemModels[0, 0].name);
		sampleBounds = sampleGem.GetBounds();
		Destroy(sampleGem);

		gemSize = sampleBounds.size;

		foreach(var gemModel in gemModels) {
			var gemView = MakeGemView(gemModel);
			var position = gemModel.position;
			gemView.transform.localPosition = new Vector2(position.col * gemSize.x, position.row * gemSize.y);
		}
	}

	GemView MakeGemView(GemModel gemModel) {
		var gemView = ResourceCache.Instantiate(gemModel.name, transform).GetComponent<GemView>();
		gemView.UpdateModel(gemModel);
		gemViews.Add(gemModel.id, gemView);
		return gemView;
	}

	void AlignField() {
		var sizeOfField = gameObject.GetBounds();
		transform.localPosition = new Vector2(
			sampleBounds.extents.x-sizeOfField.extents.x, 
			sampleBounds.extents.y-sizeOfField.extents.y
		);
	}

	void ReadInput() {
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
				StartCoroutine(StartSwap(gemSelected, swipeInfo.direction));
				gemSelected = null;
			}
		});
	}

	IEnumerator StartSwap(GemView gemSelected, Vector2 direction) {
		var sourcePosition = gemSelected.Position;
		var nearPosition = new Position(sourcePosition.index, (int)direction.x, (int)direction.y);

		// 1. 
		yield return SwapGems(Controller.Swap(sourcePosition, nearPosition));
		
		// 2.
		var hasAnyMatch = false;
		while(true) {
			var matchedGemModels = Controller.Match();
			if (matchedGemModels.Count > 0) {
				hasAnyMatch = true;

				yield return MatchGems(matchedGemModels);
				DropGems(Controller.Drop());
				yield return new WaitForSeconds(
					FeedGems(Controller.Feed())
				);
			} else {
				break;
			}
		}

		// 3.
		if (!hasAnyMatch) {
			yield return SwapGems(Controller.Swap(nearPosition, sourcePosition));
		}
	}

	float FeedGems(Queue<List<GemModel>> feedingListQueue) {
		var count = 0;
		var maxDuration = 0f;
		while(feedingListQueue.Count > 0) {
			var feedingList = feedingListQueue.Dequeue();
			feedingList.ForEach(gemModel => {
				var gemView = MakeGemView(gemModel);
				var position = gemModel.position;
				gemView.transform.localPosition = new Vector2(position.col * gemSize.x, (Model.rows + count) * gemSize.y);
			});
			count++;
			var duration = DropGems(feedingList);
			maxDuration = Math.Max(maxDuration, duration);
		}

		return maxDuration;
	}

	IEnumerator MatchGems(List<GemModel> matchedGemModels) {
		matchedGemModels.ForEach(gemModel => {
			var gemObject = gemViews[gemModel.id].gameObject;
			gemObject.SetActive(false);
			Destroy(gemObject);
			gemViews.Remove(gemModel.id);
		});
		yield return null;
	}

	float DropGems(List<GemModel> movingGemModels) {
		var duration = 0f;
		movingGemModels.ForEach(gemModel => {
			var gemView = gemViews[gemModel.id];
			var position = gemModel.position;
			var nextPosition = new Vector3(position.col * gemSize.x, position.row * gemSize.y, 0);
			duration = 1.395f * (nextPosition - gemView.transform.localPosition).magnitude / gemSize.y;
			var sequence = DOTween.Sequence();
			sequence.Append(gemView.gameObject.transform.DOLocalMove(nextPosition, duration));
			sequence.InsertCallback(duration - 0.155f, () => gemView.Squash());
			sequence.SetEase(Ease.InOutCubic);
		});
		return duration;
	}

	IEnumerator SwapGems(List<GemModel> swappingGemModels) {
		var duration = 0f;
		swappingGemModels.ForEach(gemModel => {
			var gemView = gemViews[gemModel.id];
			var position = gemModel.position;
			var nextPosition = new Vector3(position.col * gemSize.x, position.row * gemSize.y, 0);
			duration = 0.395f * (nextPosition - gemView.transform.localPosition).magnitude / gemSize.y;
			gemView.gameObject.transform.DOLocalMove(
				nextPosition,
				duration
			);
		});
		yield return new WaitForSeconds(duration);
	}

	void Destroy() {
		swipeInput.OnSwipeStart.RemoveAllListeners();
	}
}