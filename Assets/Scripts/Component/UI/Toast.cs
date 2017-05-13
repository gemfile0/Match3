using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Toast: MonoBehaviour 
{
	public CanvasGroup panel;
	public Text label;
	Sequence sequence;

	static Toast instance;

	void Awake()
	{
		gameObject.SetActive(false);
		instance = this;
	}

	public static void Show(string message, float duration)
	{
		instance.ShowMessage(message, duration);
	}

	public void ShowMessage(string message, float duration)
	{
		if (sequence != null && sequence.IsPlaying()) { return; }

		sequence = DOTween.Sequence()
			.OnStart(() => {
				gameObject.SetActive(true);
				panel.alpha = 0;
				label.text = message;
			})
			.OnComplete(() => {
				gameObject.SetActive(false);
			});
		sequence.Append(panel.DOFade(1, .295f).SetEase(Ease.OutCirc));
		sequence.AppendInterval(duration);
		sequence.Append(panel.DOFade(0, .295f).SetEase(Ease.InCirc));
	}
}
