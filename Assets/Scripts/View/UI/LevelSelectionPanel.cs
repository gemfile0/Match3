using UnityEngine;
using UnityEngine.UI;

public class LevelSelectionPanel: MonoBehaviour 
{
	public ScrollRectSnap scrollRectSnap;
	public Sprite[] levelTextures;

	void Start() 
	{
		RectTransform[] levelItems = new RectTransform[levelTextures.Length];
		for (var i = 0; i < levelTextures.Length; i++)
		{
			var levelIndex = i + 1;
			var levelItem = ResourceCache.Instantiate("LevelItem", scrollRectSnap.transform).GetComponent<RectTransform>();
			levelItem.name = "LevelItem" + levelIndex;
			levelItem.anchoredPosition = new Vector2(800 * i, levelItem.anchoredPosition.y);
			levelItem.GetComponent<Image>().sprite = levelTextures[i];
			levelItem.GetComponent<LevelItem>().title.text = "LEVEL " + levelIndex;

			levelItems[i] = levelItem;
		}

		int latestLevelIndex = -1;
		if (PlayerPrefs.HasKey("LatestLevel")) {
			latestLevelIndex = PlayerPrefs.GetInt("LatestLevel");
		}
		scrollRectSnap.Setup(levelItems, latestLevelIndex);
	}
	
}
