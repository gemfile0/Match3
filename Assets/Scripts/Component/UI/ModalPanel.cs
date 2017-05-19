using System.Text;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ModalVisibleChangedEvent: UnityEvent<bool> {}

public class ModalPanel: MonoBehaviour 
{
	public ModalVisibleChangedEvent OnVisbileChanged = new ModalVisibleChangedEvent();

	[SerializeField]
	Text titleLabel;
	
	[SerializeField]
	Text yesLabel;
	[SerializeField]
	Button yesButton;
	[SerializeField]
	Button cancelButton;
	Sequence pop;

	void Awake()
	{
		gameObject.SetActive(false);
		yesButton.gameObject.SetActive(false);
		cancelButton.gameObject.SetActive(false);	

		pop = DOTween.Sequence();
		pop.Insert(0, transform.DOScale(new Vector2(.2f, .2f), .295f).From().SetEase(Ease.OutBack));
		pop.InsertCallback(.2f, () => {
			yesButton.gameObject.SetActive(true);
			cancelButton.gameObject.SetActive(true);

			yesButton.transform.DOScale(new Vector2(.2f, .2f), .295f).From().SetEase(Ease.OutBack);
			cancelButton.transform.DOScale(new Vector2(.2f, .2f), .295f).From().SetEase(Ease.OutBack);
		});	
		pop.SetAutoKill(false);
		pop.Pause();
	}

	void OnDestroy()
	{
		yesButton.onClick.RemoveAllListeners();
		cancelButton.onClick.RemoveAllListeners();
		pop = null;
	}

	void OnEnable()
	{
		pop.Restart();
		OnVisbileChanged.Invoke(true);
	}

	void OnDisable()
	{
		yesButton.gameObject.SetActive(false);
		cancelButton.gameObject.SetActive(false);
		OnVisbileChanged.Invoke(false);
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