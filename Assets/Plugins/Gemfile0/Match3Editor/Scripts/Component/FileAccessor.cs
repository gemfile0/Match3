#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class FileAccessor
{
	public static void WriteData(string data, string title, string defaultName, string extension) 
	{
		var path = EditorUtility.SaveFilePanel(title, "/Assets", defaultName, extension);
		using (FileStream fs = new FileStream(path, FileMode.Create)) 
		{
			using (StreamWriter writer = new StreamWriter(fs)) {
				writer.Write(data);
			}
		}
		AssetDatabase.Refresh();
	}

	public static string ReadTextFromFile(string title, string extension) 
	{
		var path = EditorUtility.OpenFilePanel(title, "/Assets", extension);
		var reader = new WWW("file:///" + path);
		while(!reader.isDone) {
		}

		return reader.text;
	}

}
#endif