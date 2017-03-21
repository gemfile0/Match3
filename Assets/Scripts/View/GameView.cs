using UnityEngine;

public class GameView: BaseView<GameModel, GameController<GameModel>> {
	Bounds sampleBounds;
	void Start() {
		ResourceCache.LoadAll("");
		
		Controller.Init();
		MakeField();
		AlignField();
	}

	void MakeField()
	{
		var sampleGem = ResourceCache.Instantiate(Model.gemModels[0].name);
		sampleBounds = sampleGem.GetBounds();
		Destroy(sampleGem);

		var gapX = sampleBounds.size.x;
		var gapY = sampleBounds.size.y;

		Model.gemModels.ForEach( gemModel => {
			var name = gemModel.Value.name;
			var position = gemModel.Value.position;

			var newGem = ResourceCache.Instantiate(name, transform);
			newGem.transform.localPosition = new Vector2(position.col * gapX, position.row * gapY);
		});
	}

	void AlignField()
	{
		var sizeOfField = gameObject.GetBounds();
		transform.localPosition = new Vector2(
			sampleBounds.extents.x-sizeOfField.extents.x, 
			sampleBounds.extents.y-sizeOfField.extents.y
		);
	}
}