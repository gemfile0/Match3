using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Moves: MonoBehaviour 
{
	public RectTransform Icon;
	public Text label;

	public void SetMoves(int moves)
	{
		label.text = moves.ToString();
	}
}
