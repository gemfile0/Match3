using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Linq;

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
		var sampleGem = ResourceCache.Instantiate("RedGem");
		sampleBounds = sampleGem.GetBounds();
		gemSize = sampleBounds.size;
		Destroy(sampleGem);

		var gemModels = Model.GemModels;
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
				yield return FallAndFeed();
			} else {
				break;
			}
		}

		// 3.
		if (!hasAnyMatch) {
			yield return SwapGems(Controller.Swap(nearPosition, sourcePosition));
		}
	}

	IEnumerator FallAndFeed() {
		var fallingGemInfosList = new List<List<GemInfo>>();
		while(true) {
			FeedGems(Controller.Feed());
			var fallingGemModels = Controller.Fall();
			fallingGemInfosList.Add(
				fallingGemModels.Select(fallingGemModel => new GemInfo(){ 
					position = fallingGemModel.position, 
					id = fallingGemModel.id
				}).ToList()
			);
			
			if (fallingGemModels.Count == 0) {
				break;
			}
		}

		var index = 0;
		var duration = 0.395f;
		var sequence = DOTween.Sequence().SetEase(Ease.InOutQuad);
		for(; index < fallingGemInfosList.Count; index++) {
			var fallingGemInfos = fallingGemInfosList[index];
			FallGems(sequence, fallingGemInfos, index * duration, duration);	

			// Fixed to bounce like jelly when tween finished on each gem.
			if (index < fallingGemInfosList.Count - 1) {
				var nextFallingGemInfos = fallingGemInfosList[index + 1];
				fallingGemInfos.ForEach(fallingGemInfo => {
					if (!nextFallingGemInfos.Any(nextFallingGemInfo => nextFallingGemInfo.id == fallingGemInfo.id)) {
						sequence.InsertCallback(index * duration + 0.36f, () => gemViews[fallingGemInfo.id].Squash());
					}
				});
			}
		}

		yield return new WaitForSeconds(index * duration);
	}

	void FeedGems(List<GemModel> feedingGemModels) {
		feedingGemModels.ForEach(gemModel =>  {
			var gemView = MakeGemView(gemModel);
			var position = gemModel.position;
			gemView.transform.localPosition = new Vector2(position.col * gemSize.x, position.row * gemSize.y);
			gemView.gameObject.SetActive(false);
		});
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

	void FallGems(Sequence sequence, List<GemInfo> fallingGemInfos, float timeAt, float duration) {
		fallingGemInfos.ForEach(gemInfo => {
			// Debug.Log(gemInfo.id + ": " + gemInfo.position.ToString());
			var gemView = gemViews[gemInfo.id];
			var position = gemInfo.position;
			var nextPosition = new Vector3(position.col * gemSize.x, position.row * gemSize.y, 0);
			sequence.InsertCallback(timeAt, ()=>gemView.gameObject.SetActive(true));
			sequence.Insert(timeAt, gemView.gameObject.transform.DOLocalMove(nextPosition, duration).SetEase(Ease.Linear));
		});
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