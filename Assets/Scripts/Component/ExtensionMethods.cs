using System.Collections.Generic;
using UnityEngine;
using System;

public static class ExtensionMethods {
	public static Bounds GetBounds(this GameObject gameObject) {
		var bounds = new Bounds (gameObject.transform.position, Vector3.zero);
		foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>()) {
			bounds.Encapsulate(renderer.bounds);
		}
		return bounds;
	}

	public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> handler) {
		foreach (var item in enumerable) {
			handler(item);
		}
	}

	public static void ForEachWithIndex<T>(this IEnumerable<T> enumerable, Action<T, int> handler) {
        int idx = 0;
        foreach (T item in enumerable) {
            handler(item, idx++);
		}
    }
}   