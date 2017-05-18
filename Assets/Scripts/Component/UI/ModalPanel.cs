using System.Text;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ModalPanel: MonoBehaviour 
{
	[SerializeField]
	Text titleLabel;
	
	[SerializeField]
	Text yesLabel;
	[SerializeField]
	Button yesButton;
	[SerializeField]
	Button cancelButton;

	void Awake()
	{
		gameObject.SetActive(false);
		yesButton.gameObject.SetActive(false);
		cancelButton.gameObject.SetActive(false);		
	}

	void OnDestroy()
	{
		yesButton.onClick.RemoveAllListeners();
		cancelButton.onClick.RemoveAllListeners();
	}

	void OnEnable()
	{
		var sequence = DOTween.Sequence();
		sequence.Insert(0, transform.DOScale(new Vector2(.2f, .2f), .295f).From().SetEase(Ease.OutBack));
		sequence.InsertCallback(.2f, () => {
			yesButton.gameObject.SetActive(true);
			cancelButton.gameObject.SetActive(true);

			yesButton.transform.DOScale(new Vector2(.2f, .2f), .295f).From().SetEase(Ease.OutBack);
			cancelButton.transform.DOScale(new Vector2(.2f, .2f), .295f).From().SetEase(Ease.OutBack);
		});
	}

	void OnDisable()
	{
		yesButton.gameObject.SetActive(false);
		cancelButton.gameObject.SetActive(false);		
	}

	public void Choice(string titleText, string yesText, UnityAction yesEvent, UnityAction cancelEvent = null)
	{
		titleLabel.text = titleText;
		yesLabel.text = yesText;

		gameObject.SetActive(true);

		yesButton.onClick.RemoveAllListeners();
		yesButton.onClick.AddListener(yesEvent);
		yesButton.onClick.AddListener(ClosePanel);

		cancelButton.onClick.RemoveAllListeners();
		cancelButton.onClick.AddListener(cancelEvent);
		cancelButton.onClick.AddListener(ClosePanel);
	}

	void ClosePanel()
	{
		gameObject.SetActive(false);
	}
}