using UnityEngine;

public class GemBrush: MonoBehaviour 
{
	public Vector2 brushSize = Vector2.zero;
	public int positionID = 0;
	public int gemID = 0;
	public SpriteRenderer renderer2D;

	void OnDrawGizmosSelected()
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
