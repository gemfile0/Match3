using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PostProcessing;

public class MainCamera : MonoBehaviour 
{
	PostProcessingBehaviour postProcessingBehaviour; 
	
	void Start()
	{
		postProcessingBehaviour = GetComponent<PostProcessingBehaviour>();
		postProcessingBehaviour.enabled = false;

		ModalPanel.Instance.OnVisbileChanged.AddListener(OnModalVisibleChanged);
	}

	void OnModalVisibleChanged(bool isVisible)
	{
		postProcessingBehaviour.enabled = isVisible;
	}
}
