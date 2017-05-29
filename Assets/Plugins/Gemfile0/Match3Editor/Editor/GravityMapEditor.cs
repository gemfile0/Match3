using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GravityMap))]
public class GravityMapEditor: BaseMapEditor 
{
    protected override string ReadPostName() 
	{ 
		return "gravity";
	}
}
