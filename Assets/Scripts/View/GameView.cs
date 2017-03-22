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
		var sampleGem = ResourceCache.Instantiate(Model.gemModels[0].name);
		sampleBounds = sampleGem.GetBounds();
		Destroy(sampleGem);

		gemSize = sampleBounds.size;

		Model.gemModels.Values.ForEach(gemModel => {
			var name = gemModel.name;
			var position = gemModel.position;
			var id = gemModel.id;

			var gemObject = ResourceCache.Instantiate(name, transform);
			gemObject.transform.localPosition = new Vector2(position.col * gemSize.x, position.row * gemSize.y);

			var gemView = gemObject.GetComponent<GemView>();
			gemView.UpdateModel(gemModel);
			gemViews.Add(id, gemView);
		});
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
		yield return SwapGems(Controller.Swap(sourcePosition, nearPosition));
		var matchedGemModels = Controller.Match();
		if (matchedGemModels.Count > 0) {
			yield return MatchGems(matchedGemModels);
		} else {
			yield return SwapGems(Controller.Swap(nearPosition, sourcePosition));
		}
	}

	IEnumerator MatchGems(List<GemModel> matchedGemModels) {
		matchedGemModels.ForEach(gemModel => {
			var gemObject = gemViews[gemModel.id].gameObject;
			gemObject.SetActive(false);
			Destroy(gemObject);
		});
		yield return null;
	}

	IEnumerator SwapGems(List<GemModel> swappingGemModels) {
		var duration = .395f;
		swappingGemModels.ForEach(gemModel => {
			var gemView = gemViews[gemModel.id];
			var position = gemModel.position;
			gemView.gameObject.transform.DOLocalMove(
				new Vector2(position.col * gemSize.x, position.row * gemSize.y),
				duration
			);
		});
		yield return new WaitForSeconds(duration);
	}

	void Destroy() {
		swipeInput.OnSwipeStart.RemoveAllListeners();
	}
}