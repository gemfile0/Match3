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
	Sequence squash;
	Tween scale;

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

			squash = DOTween.Sequence();
			squash.OnStart(() => image.rectTransform.localScale = Vector3.one);
			squash.Append(image.rectTransform.DOScale(new Vector3(1.28f, 0.72f, 1), 0.24f));
			squash.Append(image.rectTransform.DOScale(new Vector3(1, 1, 1), 1.36f).SetEase(Ease.OutElastic));
			squash.Pause();
        	squash.SetAutoKill(false);

			scale = label.rectTransform.DOScale(1.25f, .5f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutBack);
			scale.Pause();
			scale.SetAutoKill(false);
		}
		
		var sb = new StringBuilder();
		sb.AppendFormat("{0}", left.howMany);
		var nextText = sb.ToString();
		if (label.text != nextText) 
		{
			label.text = nextText;

			label.rectTransform.localScale = Vector3.one;
			scale.Restart();
			squash.Restart();
		}
	}

	public void Hide()
	{
		Icon.gameObject.SetActive(false);
		label.gameObject.SetActive(false);
		squash = null;
		scale = null;
	}
}
