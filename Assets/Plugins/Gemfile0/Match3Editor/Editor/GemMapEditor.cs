using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GemMap))]
public class GemMapEditor: BaseMapEditor 
{
    protected override string ReadPostName() 
	{ 
		return "gem";
	}
}
