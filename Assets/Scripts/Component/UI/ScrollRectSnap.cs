using UnityEngine;

public class ScrollRectSnap: MonoBehaviour 
{
	public RectTransform panel;
	public RectTransform[] items;
	public RectTransform center;
	public int startButton = 3;

	private float[] positiveDistances;
	private float[] distances;
	private bool dragging = false;
	private int itemDistance;
	private int minButtonIndex;
	private bool hasMessageSended;
	private bool targetNearestButton = true;

	void Start()
	{
		int itemLength = items.Length;
		positiveDistances = new float[itemLength];
		distances = new float[itemLength];
		
		itemDistance = (int)Mathf.Abs(
			items[1].anchoredPosition.x - items[0].anchoredPosition.x
		);

		panel.anchoredPosition = new Vector2((startButton - 1) * -itemDistance, 0f);
	}

	void Update()
	{
		for (int i = 0; i < items.Length; i++)
		{
			distances[i] = center.position.x - items[i].position.x;
			positiveDistances[i] = Mathf.Abs(distances[i]);

			if (distances[i] > 2000)
			{
				float currentX = items[i].anchoredPosition.x;
				float currentY = items[i].anchoredPosition.y;

				Vector2 newAnchoredPosition = new Vector2(currentX + (items.Length * itemDistance), currentY);
				items[i].anchoredPosition = newAnchoredPosition;
			}

			if (distances[i] < -2000)
			{
				float currentX = items[i].anchoredPosition.x;
				float currentY = items[i].anchoredPosition.y;

				Vector2 newAnchoredPosition = new Vector2(currentX - (items.Length * itemDistance), currentY);
				items[i].anchoredPosition = newAnchoredPosition;
			}
		}

		if (targetNearestButton)
		{
			float minDistance = Mathf.Min(positiveDistances);

			for (int j = 0; j < items.Length; j++)
			{
				if (minDistance == positiveDistances[j]) {
					minButtonIndex = j;
				}
			}

			if (!dragging)
			{
				LerpToButton(-items[minButtonIndex].anchoredPosition.x);
			}
		}
	}

	void LerpToButton(float position)
	{
		float newX = Mathf.Lerp(panel.anchoredPosition.x, position, Time.deltaTime * 5f);
		Vector2 newPosition = new Vector2(newX, panel.anchoredPosition.y);
		if (Mathf.Abs(position - newX) < 3f)
		{
			newX = position;
		}

		var positiveNewX = Mathf.Abs(newX);
		var positivePosition = Mathf.Abs(position);
		if (positiveNewX >= positivePosition - 1f 
			&& positiveNewX <= positivePosition + 1 
			&& !hasMessageSended) 
		{
			hasMessageSended = true;
			SendMessageFromButton(minButtonIndex);
		}

		panel.anchoredPosition = newPosition;
	}

	void SendMessageFromButton(int buttonIndex)
	{
		if (buttonIndex - 1 == 3) {

		}
	}

	public void StartDrag()
	{
		dragging = true;
		hasMessageSended = false;
		targetNearestButton = true;
	}

	public void EndDrag()
	{
		dragging = false;
		hasMessageSended = true;
	}

	public void GoToButton(int buttonIndex)
	{
		targetNearestButton = false;
		minButtonIndex = buttonIndex - 1;
	}
}
