using UnityEngine;

[System.Serializable]
public class GemItem
{
	public int id;
	public string name;
	public Texture2D texture2D;
	public string path;

	public override string ToString()
	{
		return string.Format("{0}, {1}, {2}", id, name, path);
	}
}
