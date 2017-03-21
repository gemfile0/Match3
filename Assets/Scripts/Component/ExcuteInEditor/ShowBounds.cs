using UnityEngine;

[ExecuteInEditMode]
public class ShowBounds: MonoBehaviour {
	public Vector3[] edges;
	public Bounds latestBounds;

	void Start() {
		MakeEdges();
	}

	void MakeEdges() {
		var bounds = gameObject.GetBounds();
		if(latestBounds.min != bounds.min && latestBounds.max != bounds.max) {
			edges = new Vector3[5] {
				new Vector3(bounds.min.x, bounds.min.y),
				new Vector3(bounds.max.x, bounds.min.y),
				new Vector3(bounds.max.x, bounds.max.y),
				new Vector3(bounds.min.x, bounds.max.y),
				new Vector3(bounds.min.x, bounds.min.y)
			};
		}
	}

	void Update() {
		MakeEdges();
	}
}