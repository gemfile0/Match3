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
	int countLeft;
	int countTarget;
	Coroutine chainging;
	Sequence squash;

	void Awake()
	{
		foreach (Transform childOfIcon in Icon.transform)
		{
			if (childOfIcon != Icon) { childOfIcon.gameObject.SetActive(false); }
		}
	}

	void OnDestroy()
	{
	}
	
	public void SetMission(MissionModel left)
	{
		var finding = Icon.Find(left.gemID.ToString());
		if (finding != null && image == null)
		{
			image = finding.GetComponent<Image>();
			image.gameObject.SetActive(true);

			squash = DOTween.Sequence();
			squash.OnStart(() => image.rectTransform.localScale = Vector3.one);
			squash.Append(image.rectTransform.DOScale(new Vector3(1.08f, 0.92f, 1), 0.24f));
			squash.Append(image.rectTransform.DOScale(Vector3.one, 1.36f).SetEase(Ease.OutElastic));
			squash.SetAutoKill(false);
			squash.Pause();
		}
		
		if (countLeft > left.howMany)
		{
			countTarget = left.howMany;
			squash.Restart();

			if (chainging == null) { chainging = StartCoroutine(StartTextChanging()); }
		}
		else if (countLeft < left.howMany)
		{
			var sb = new StringBuilder();
			sb.AppendFormat("{0}", left.howMany);
			label.text = sb.ToString();
			countLeft = left.howMany;
		}
	}

	IEnumerator StartTextChanging()
	{
		label.rectTransform.localScale = Vector3.one;

		while (countLeft > countTarget)
		{
			countLeft -= 1;

			var sb = new StringBuilder();
			sb.AppendFormat("{0}", countLeft);
			label.text = sb.ToString();

			yield return new WaitForSeconds(0.06f);
		}
		chainging = null;
	}

	public void Hide()
	{
		Icon.gameObject.SetActive(false);
		label.gameObject.SetActive(false);
	}
}
