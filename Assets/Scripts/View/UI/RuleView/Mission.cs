using System.Collections;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Mission: MonoBehaviour 
{
	public RectTransform Icon;
	public Text label;
	Image image;
	Tween tween;

	void Awake()
	{
		foreach (Transform childOfIcon in Icon.transform)
		{
			if (childOfIcon != Icon) { childOfIcon.gameObject.SetActive(false); }
		}
	}
	
	public void SetMission(MissionModel left)
	{
		var finding = Icon.Find(left.gemID.ToString());
		if (finding != null)
		{
			image = finding.GetComponent<Image>();
			image.gameObject.SetActive(true);
		}
		
		var sb = new StringBuilder();
		sb.AppendFormat("{0}", left.howMany);
		var nextText = sb.ToString();
		if (label.text != nextText) 
		{
			label.text = nextText;

			label.rectTransform.DOKill();
			label.rectTransform.localScale = Vector3.one;
			tween = label.rectTransform.DOScale(1.5f, .8f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutBack);
			var squash = DOTween.Sequence();
			squash.OnStart(() => image.rectTransform.localScale = Vector3.one);
			squash.Append(image.rectTransform.DOScale(new Vector3(1.32f, 0.68f, 1), 0.24f));
			squash.Append(image.rectTransform.DOScale(new Vector3(1, 1, 1), 1.36f).SetEase(Ease.OutElastic));
		}
	}

	public void Hide()
	{
		Icon.gameObject.SetActive(false);
		label.gameObject.SetActive(false);
	}
}
