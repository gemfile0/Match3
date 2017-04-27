using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ModalPanel: MonoBehaviour 
{
	[SerializeField]
	Button yesButton;
	[SerializeField]
	Button noButton;
	[SerializeField]
	Button cancelButton;
	
	// private static ModalPanel modalPanel;
	// public static ModalPanel Instance() 
	// {
	// 	if (!modalPanel) {
	// 		modalPanel = FindObjectOfType(typeof(ModalPanel)) as ModalPanel;
	// 	}

	// 	return modalPanel;
	// }

	public void Choice(UnityAction yesEvent)
	{
		gameObject.SetActive(true);

		yesButton.onClick.RemoveAllListeners();
		yesButton.onClick.AddListener(yesEvent);
		yesButton.onClick.AddListener(ClosePanel);
	}

	void ClosePanel()
	{
		gameObject.SetActive(false);
	}
}