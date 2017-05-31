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
		postProcessingBehaviour.profile.depthOfField.enabled = false;

		ModalPanel.Instance.OnVisbileChanged.AddListener(OnModalVisibleChanged);
	}
	
	void OnDestroy()
	{
		ModalPanel.Instance.OnVisbileChanged.RemoveListener(OnModalVisibleChanged);
	}

	void OnModalVisibleChanged(bool isVisible)
	{
		postProcessingBehaviour.profile.depthOfField.enabled = isVisible;
	}
}
