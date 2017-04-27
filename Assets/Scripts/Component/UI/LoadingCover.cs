using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class LoadingCover: MonoBehaviour 
{
	readonly float DURATION = 0.395f;
	[SerializeField]
	Text title;
	[SerializeField]
	CanvasGroup canvasGroup;
	[SerializeField]
	bl_ProgressBar progressBar;
	
	public IEnumerator Show(string text) 
	{
		title.text = text;

		canvasGroup.DOFade(1, DURATION);
		yield return new WaitForSeconds(DURATION);
	}

	public IEnumerator Hide()
	{
		canvasGroup.DOFade(0, DURATION);
		yield return new WaitForSeconds(DURATION);
		Destroy(gameObject);
	}

	public IEnumerator Progress(float progress)
    {
		progressBar.Value = progress * 100;
        yield return null;
    }
}
