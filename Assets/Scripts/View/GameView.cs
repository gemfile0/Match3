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

	public override void Awake() {
		base.Awake();
	}

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
			var position = gemModel.Position;
			gemView.transform.localPosition = new Vector2(position.col * gemSize.x, position.row * gemSize.y);
		}
	}

	void RemoveGemView(GemModel gemModel) {
		GemView gemView;
		if (gemViews.TryGetValue(gemModel.id, out gemView)) {
			var gemObject = gemView.gameObject;
			gemObject.SetActive(false);
			Destroy(gemObject);
			gemViews.Remove(gemModel.id);
		}
		
		if (gemModel.specialKey != "") {
			ActBySpecialType(gemModel);
		}
	}

	GemView MakeGemView(GemModel gemModel) {
		var gemView = ResourceCache.Instantiate(gemModel.Name, transform).GetComponent<GemView>();
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
				ActByGemType(gemSelected.Type, swipeInfo.direction);
				gemSelected = null;
			}
		});
	}

	void ActByGemType(GemType gemType, Vector2 direction) {
		switch(gemType) {
			case GemType.ChocoGem:
			StartCoroutine(StartCrunch(gemSelected.Position, direction));
			break;

			case GemType.SuperGem:
			
			break;

			default:
			StartCoroutine(StartSwap(gemSelected.Position, direction));
			break;
		}
	}

	void ActBySpecialType(GemModel gemModel) {
		switch(gemModel.Type) {
			case GemType.SuperGem:
			break;

			case GemType.ChocoGem:
			break;
		}
		
		switch(gemModel.specialKey) {
			case "H":
			StartCoroutine(StartSpread(gemModel.Position, new Vector2{ x = -1, y = 0 }));
			StartCoroutine(StartSpread(gemModel.Position, new Vector2{ x = 1, y = 0 }));
			break;

			case "V":
			StartCoroutine(StartSpread(gemModel.Position, new Vector2{ x = 0, y = -1 }));
			StartCoroutine(StartSpread(gemModel.Position, new Vector2{ x = 0, y = 1 }));
			break;

			case "C":
			break;
		}
	}

	IEnumerator StartSpread(Position sourcePosition, Vector2 direction) {
		var colOffset = (int)direction.x;
		var rowOffset = (int)direction.y;

		var gravity = Model.levelModel.gravity;
		
		while(true) {
			var nearPosition = new Position(sourcePosition.index, colOffset, rowOffset);
			var brokenGemInfo = Controller.Break(nearPosition);
			if (brokenGemInfo.gemModels != null) {
				BreakGems(brokenGemInfo);
			} else {
				break;
			}

			sourcePosition = nearPosition;
		}
		
		yield return null;
	}

	IEnumerator StartCrunch(Position sourcePosition, Vector2 direction) {
		var colOffset = (int)direction.x;
		var rowOffset = (int)direction.y;
		
		var count = 0;
		var gravity = Model.levelModel.gravity;
		var sequence = DOTween.Sequence().SetEase(Ease.Linear);

		SetBlock(sourcePosition, colOffset, rowOffset);
		
		// 1.
		var waiting = true;
		while(waiting) {
			var nearPosition = new Position(sourcePosition.index, colOffset, rowOffset);
			var crunchedGemInfo = Controller.Crunch(sourcePosition, nearPosition);

			if (crunchedGemInfo.gemModels != null) {
				CrunchGems(sequence, count, crunchedGemInfo);
				yield return FeedAndFall(0.36f);
			} else {
				RemoveGemView(crunchedGemInfo.source);
				break;
			}
			
			sourcePosition = nearPosition;
			count++;
		}

		// 2.
		waiting = true;
		while(waiting) {
			waiting = Match();
			yield return FeedAndFall();
		}
	}

	void SetBlock(Position sourcePosition, int colOffset, int rowOffset) {
		var nextCol = colOffset;
		var nextRow = rowOffset;
		while(true) {
			var nearPosition = new Position(sourcePosition.index, nextCol, nextRow);
			var blockedGemModels = Controller.MakeBlock(sourcePosition, nearPosition);
			if (blockedGemModels.Count > 0) {
				blockedGemModels.ForEach(gemModel => {
					var gemView = gemViews[gemModel.id];
					gemView.UpdateModel(gemModel);
					gemView.SetBlock();
				});
			} else {
				break;
			}
			
			nextCol += colOffset;
			nextRow += rowOffset;
		}
	}
	
	void BreakGems(BrokenGemInfo brokenGemInfo) {
		brokenGemInfo.gemModels.ForEach(brokenGemModel => {
			RemoveGemView(brokenGemModel);
		});
	}

	void CrunchGems(Sequence sequence, int count, CrunchedGemInfo crunchedGemInfo) {
		var duration = 0f;
		
		crunchedGemInfo.gemModels.ForEach(crunchedGemModel => {
			var gemObject = gemViews[crunchedGemModel.id].gameObject;
			gemObject.SetActive(false);
			Destroy(gemObject);
			gemViews.Remove(crunchedGemModel.id);
		});

		var gemModel = crunchedGemInfo.source;
		var gemView = gemViews[gemModel.id];
		var position = gemModel.Position;
		var nextPosition = new Vector3(position.col * gemSize.x, position.row * gemSize.y, 0);
		duration = 0.36f * (nextPosition - gemView.transform.localPosition).magnitude / gemSize.y;
		sequence.Insert(duration * count, gemView.gameObject.transform.DOLocalMove(
			nextPosition,
			duration
		).SetEase(Ease.Linear));
	}

	IEnumerator StartSwap(Position sourcePosition, Vector2 direction) {
		var colOffset = (int)direction.x;
		var rowOffset = (int)direction.y;
		var nearPosition = new Position(sourcePosition.index, colOffset, rowOffset);

		// 1. 
		var waiting = true;
		var hasAnyMatch = false;
		yield return SwapGems(Controller.Swap(sourcePosition, nearPosition));

		while(waiting) {
			if (Match()) {
				hasAnyMatch = true;
				yield return FeedAndFall();
			} else {
				waiting = false;
			}
		}

		if (!hasAnyMatch) {
			yield return SwapGems(Controller.Swap(nearPosition, sourcePosition));
		}
	}

	IEnumerator FeedAndFall(float waitForSeconds = 0f) {
		var fallingGemInfosList = new List<List<GemInfo>>();
		while(true) {
			FeedGems(Controller.Feed());

			var fallingGemModels = Controller.Fall();
			fallingGemInfosList.Add(fallingGemModels);

			if (fallingGemModels.Count == 0) {
				FeedGems(Controller.StopFeed());
				break;
			}
		}

		var index = 0;
		var duration = 0.36f;
		var sequence = DOTween.Sequence().SetEase(Ease.InOutQuad);
		for(; index < fallingGemInfosList.Count; index++) {
			var fallingGemInfos = fallingGemInfosList[index];
			FallGems(sequence, fallingGemInfos, index * duration, duration);	

			// Fixed to bounce like jelly when tween finished on each gem.
			if (index < fallingGemInfosList.Count - 1) {
				var nextFallingGemInfos = fallingGemInfosList[index + 1];
				fallingGemInfos.ForEach(fallingGemInfo => {
					if (!nextFallingGemInfos.Any(nextFallingGemInfo => nextFallingGemInfo.id == fallingGemInfo.id)) {
						sequence.InsertCallback(index * duration + duration, () => {
							gemViews[fallingGemInfo.id].Squash();
						});
					}
				});
			}
		}

		yield return new WaitForSeconds((waitForSeconds != 0) ? waitForSeconds : index * duration);
	}

	void FeedGems(List<GemModel> feedingGemModels) {
		feedingGemModels.ForEach(gemModel =>  {
			var gemView = MakeGemView(gemModel);
			var position = gemModel.Position;
			gemView.transform.localPosition = new Vector2(position.col * gemSize.x, position.row * gemSize.y);
			gemView.gameObject.SetActive(false);
		});
	}

	void MatchGems(List<MatchedLineInfo> matchedLineInfos) {
		matchedLineInfos.ForEach(matchedLineInfo => {
			matchedLineInfo.gemModels.ForEach(gemModel => {
				RemoveGemView(gemModel);
			});
			if (matchedLineInfo.newAdded != null) {
				var gemView = MakeGemView(matchedLineInfo.newAdded);
				var position = matchedLineInfo.newAdded.Position;
				gemView.transform.localPosition = new Vector2(position.col * gemSize.x, position.row * gemSize.y);
			}
		});
	}

	void FallGems(Sequence sequence, List<GemInfo> fallingGemInfos, float timeAt, float duration) {
		fallingGemInfos.ForEach(gemInfo => {
			var gemView = gemViews[gemInfo.id];
			var position = gemInfo.position;
			var nextPosition = new Vector3(position.col * gemSize.x, position.row * gemSize.y, 0);
			sequence.InsertCallback(timeAt, ()=>gemView.Open());
			sequence.Insert(timeAt, gemView.gameObject.transform.DOLocalMove(nextPosition, duration).SetEase(Ease.Linear));
		});
	}

	IEnumerator SwapGems(List<GemModel> swappingGemModels) {
		var duration = 0f;
		swappingGemModels.ForEach(gemModel => {
			var gemView = gemViews[gemModel.id];
			var position = gemModel.Position;
			var nextPosition = new Vector3(position.col * gemSize.x, position.row * gemSize.y, 0);
			duration = 0.36f * (nextPosition - gemView.transform.localPosition).magnitude / gemSize.y;
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

	bool Match() {
		var matchedGemModels = Controller.Match();
		if (matchedGemModels.Count > 0) {
			MatchGems(matchedGemModels);
		}

		return matchedGemModels.Count > 0;
	}
}