using UnityEngine;

public class ScrollRectSnap: MonoBehaviour 
{
	public RectTransform panel;
	public RectTransform[] items;
	public RectTransform center;
	public int startItem = 3;

	float[] positiveDistances;
	float[] distances;
	bool dragging = false;
	int itemDistance;
	int minItemIndex;
	bool hasMessageSended;
	bool targetNearestItem = true;

	public void Setup(RectTransform[] items, int latestLevelIndex = -1)
	{
		this.items = items;
		int itemLength = items.Length;
		positiveDistances = new float[itemLength];
		distances = new float[itemLength];
		itemDistance = (int)Mathf.Abs(
			items[1].anchoredPosition.x - items[0].anchoredPosition.x
		);

		if (latestLevelIndex >= 0) {
			startItem = latestLevelIndex;
		}

		panel.anchoredPosition = new Vector2((startItem - 1) * -itemDistance, 0f);
	}
	
	public void StartDrag()
	{
		dragging = true;
		hasMessageSended = false;
		targetNearestItem = true;
	}

	public void EndDrag()
	{
		dragging = false;
		hasMessageSended = true;
	}

	void Update()
	{
		if (items.Length == 0) { return; }

		for (int i = 0; i < items.Length; i++)
		{
			distances[i] = center.position.x - items[i].position.x;
			Debug.Log(distances[i]);
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

		if (targetNearestItem)
		{
			float minDistance = Mathf.Min(positiveDistances);

			for (int j = 0; j < items.Length; j++)
			{
				if (minDistance == positiveDistances[j]) {
					minItemIndex = j;
				}
			}

			if (!dragging)
			{
				LerpToItem(-items[minItemIndex].anchoredPosition.x);
			}
		}
	}

	void LerpToItem(float position)
	{
		float newX = Mathf.Lerp(panel.anchoredPosition.x, position, Time.deltaTime * 5f);
		Vector2 newPosition = new Vector2(newX, panel.anchoredPosition.y);
		if (Mathf.Abs(position - newX) < 3)
		{
			newX = position;
		}

		var positiveNewX = Mathf.Abs(newX);
		var positivePosition = Mathf.Abs(position);
		if (positiveNewX >= positivePosition - 1
			&& positiveNewX <= positivePosition + 1 
			&& !hasMessageSended) 
		{
			hasMessageSended = true;
			SendMessageFromItem(minItemIndex);
		}

		panel.anchoredPosition = newPosition;
	}

	void SendMessageFromItem(int itemIndex)
	{
		if (itemIndex - 1 == 3) {

		}
	}

}
