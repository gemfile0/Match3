using System.Collections;
using UnityEngine;

public class Cube: MonoBehaviour 
{
	GOSequence sequence;

	void Start () 
	{
		sequence = GOTween.Sequence().SetEase(GOEase.SmoothStep);
		sequence.Insert(1, transform.GOLocalMove(
			new Vector3(0, 5, 0),
			1f
		));

		StartCoroutine(StartAddingPosition());
	}
	
	IEnumerator StartAddingPosition()
	{
		var count = 2;
		while (count <= 5)
		{
			var random = new System.Random();
			var randomX = random.Next(-5, 5);
			var randomY = random.Next(-5, 5);

			Debug.Log(count + ": " + randomX + ", " + randomY);
			
			sequence.Insert(count, transform.GOLocalMove(
				new Vector3(randomX, randomY, 0),
				1f
			));
			sequence.InsertCallback(count, () => {
				// Debug.Log(count);
			});
			yield return null;
			count++;
		}
	}
}
