#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseBrush: MonoBehaviour 
{
	public Vector2 brushSize = Vector2.zero;
	public int positionID = 0;
	public int gemID = 0;
	public SpriteRenderer renderer2D;

	protected void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(transform.position, brushSize);
	}
	
	public void UpdateBrush(SpriteReference spriteReference) 
	{
		gemID = spriteReference.id;
		renderer2D.sprite = spriteReference.sprite;
	}	
}
#endif