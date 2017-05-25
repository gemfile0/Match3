using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollContentCanSnap: MonoBehaviour 
{
	public RectTransform panel;
	public RectTransform[] items;
	public RectTransform center;
	public int itemIndex = 3;

	float[] positiveDistances;
	float[] distances;
	bool dragging;
	int itemDistance;

	SwipeInput swipeInput;
	Coroutine sticking;

	public void Setup(RectTransform[] items, int latestLevelIndex = -1)
	{
		this.items = items;
		int itemLength = items.Length;

		positiveDistances = new float[itemLength];
		distances = new float[itemLength];
		itemDistance = (int)Mathf.Abs(items[1].anchoredPosition.x - items[0].anchoredPosition.x);

		if (latestLevelIndex >= 0) { itemIndex = latestLevelIndex; }
		panel.anchoredPosition = new Vector2((itemIndex - 1) * -itemDistance, 0f);

		SubscribeInput();
	}

	void Update()
	{
		Rearrange();
	}
	
	public void StartDrag()
	{
		dragging = true;
	}

	public void EndDrag()
	{
		dragging = false;
	}

	void SubscribeInput()
	{
		swipeInput = GetComponent<SwipeInput>();
		swipeInput.allowedInGUI = true;
		swipeInput.threshold = int.MaxValue;
		swipeInput.isCancelable = false;

		swipeInput.OnSwipeEnd.AddListener(swipeInfo => {
			if (sticking != null) { StopCoroutine(sticking); }

			if (swipeInfo.direction == Vector2.right) {
				sticking = StartCoroutine(StartStickTo(itemIndex - 1));
			} else if (swipeInfo.direction == Vector2.left) {
				sticking = StartCoroutine(StartStickTo(itemIndex + 1));
			} else {
				sticking = StartCoroutine(StartStickTo(itemIndex));
			}
		});
	}

	void Rearrange()
	{
		for (int i = 0; i < items.Length; i++)
		{
			distances[i] = center.position.x - items[i].position.x;
			positiveDistances[i] = Mathf.Abs(distances[i]);

			if (distances[i] > 50)
			{
				float currentX = items[i].anchoredPosition.x;
				float currentY = items[i].anchoredPosition.y;

				Vector2 newAnchoredPosition = new Vector2(currentX + (items.Length * itemDistance), currentY);
				items[i].anchoredPosition = newAnchoredPosition;
			}

			if (distances[i] < -50)
			{
				float currentX = items[i].anchoredPosition.x;
				float currentY = items[i].anchoredPosition.y;

				Vector2 newAnchoredPosition = new Vector2(currentX - (items.Length * itemDistance), currentY);
				items[i].anchoredPosition = newAnchoredPosition;
			}
		}
	}

	IEnumerator StartStickTo(int itemIndex = 0)
	{
		if (itemIndex > items.Length) {
			itemIndex = 1;
		} else if (itemIndex < 1) {
			itemIndex = items.Length;
		}
		this.itemIndex = itemIndex;

		float targetPosition = -items[itemIndex - 1].anchoredPosition.x;
		float currentTime = 0f;
		float duration = .395f;
		while (true)
		{
			currentTime += Time.deltaTime;
			float newX = Mathf.Lerp(panel.anchoredPosition.x, targetPosition, currentTime/duration);
			panel.anchoredPosition = new Vector2(newX, panel.anchoredPosition.y);

			if (currentTime >= duration) { yield break; }
			yield return null;
		}
	}
}
