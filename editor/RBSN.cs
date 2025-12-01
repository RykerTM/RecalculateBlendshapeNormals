using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase.Editor.BuildPipeline;
using RykerTM.Tools.RBSN;
using Hai = RykerTM.Tools.RBSN.Hai; // Hai related methods by Hai~ https://github.com/hai-vr/

// Preprocess Hook
public class RBSNHook : IVRCSDKPreprocessAvatarCallback
{
    public bool OnPreprocessAvatar(GameObject avatar)
	{
		try
		{
			Recalculate.Avatar(avatar);
			return true;
		}
		catch (Exception e)
		{
			Debug.LogException(e);
			return false;
		}
	}
	public int callbackOrder => -11000;
}

// Recalculate
public class Recalculate
{
	public static void Avatar(GameObject avatar)
	{
		RBSNComponent[] recalculateComponents = avatar.GetComponentsInChildren<RBSNComponent>(true);
		
		EditorUtility.ClearProgressBar();
		float i = 0;
		foreach (RBSNComponent component in recalculateComponents)
		{
			i++;
			EditorUtility.DisplayProgressBar("RBSN", "Recalculating: " + component.gameObject.name, (i / (float)recalculateComponents.Count()));
			
			SkinnedMeshRenderer smr = component.GetComponent<SkinnedMeshRenderer>();
			
			Mesh mesh = smr.sharedMesh; // Get the shared mesh (important!).
			if (mesh == null) continue;
			
			Selection selected = BuildBSList(component.blendShapes);

			var thatSmrBlendShapes = Enumerable.Range(0, mesh.blendShapeCount)
				.Select(i => mesh.GetBlendShapeName(i))
				.ToList();
			var applicableBlendShapes = selected.blendShapesToRecalculate 
				.Where(blendShape => thatSmrBlendShapes.Contains(blendShape))
				.Distinct()
				.ToList();
			var eraseCustomSplitNormalsBlendShapes = selected.blendShapesToEraseSplitNormals
				.Where(blendShape => thatSmrBlendShapes.Contains(blendShape))
				.Distinct()
				.ToList();

			if (applicableBlendShapes.Count() > 0)
			{
				Hai.NormalCalculator.RecalculateNormalsOf(smr, thatSmrBlendShapes, applicableBlendShapes, eraseCustomSplitNormalsBlendShapes);
			}
		}
		EditorUtility.ClearProgressBar();
	}
	
	public static Selection BuildBSList(IEnumerable blendShapes)
	{
		Selection returnStrings = new Selection();
		
		foreach (RBSNComponent.BlendShape blendShape in blendShapes)
		{
			if (!blendShape.isSelected) continue; // Skip other check if it isn't even marked to recalculate	
			returnStrings.blendShapesToRecalculate.Add(blendShape.name);
			if (blendShape.eraseSplitNormals) returnStrings.blendShapesToEraseSplitNormals.Add(blendShape.name);
		}		
		return returnStrings;
	}
	
	public class Selection
	{
		public List<string> blendShapesToRecalculate = new List<string>();
		public List<string> blendShapesToEraseSplitNormals = new List<string>();
		
		public Selection()
		{
			this.blendShapesToRecalculate = new List<string>();
			this.blendShapesToEraseSplitNormals = new List<string>();
		}
	}
}

// Component GUI

[CustomEditor(typeof(RBSNComponent))]
public class RBSNComponentEditor : Editor
{
	RBSNComponent component;
	private GUIStyle boldLabelStyle;
	
	private void OnEnable()
	{
		component = (RBSNComponent)target;
		component.RefreshSMR();
		
		if (component.smr == null)
		{
			EditorUtility.DisplayDialog("RBSN","This must go on a GameObject with a Skinned Mesh Renderer.", "Okay");
			DestroyImmediate(component);
			return;
		}		
		if (component.smr.sharedMesh == null)
		{
			EditorUtility.DisplayDialog("RBSN","This Skinned Mesh Renderer is empty.", "Okay");
			Debug.LogWarning("Tried to add the RBSN component but the skinned mesh component was empty.");
			DestroyImmediate(component);
			return;
		}
		
		boldLabelStyle = new GUIStyle();
		boldLabelStyle.fontStyle = FontStyle.Bold;
		boldLabelStyle.fontSize = 16;
		boldLabelStyle.normal.textColor = Color.white;
	}
	
	public override void OnInspectorGUI()
	{
		EditorGUILayout.BeginVertical();
		
		GUILayout.Space(5);
		EditorGUILayout.LabelField("RBSN", boldLabelStyle);
		EditorGUILayout.LabelField("Choose which blend shapes to recalculate");
		EditorGUILayout.LabelField("and if their custom split normals should be erased");
		GUILayout.Space(5);
		
		EditorGUI.BeginChangeCheck();
		
		foreach (var blendShape in component.blendShapes)
		{
			EditorGUILayout.BeginHorizontal();
			blendShape.isSelected = EditorGUILayout.Toggle(blendShape.name, blendShape.isSelected);
			
			if (blendShape.isSelected)
			{
				blendShape.eraseSplitNormals = EditorGUILayout.Toggle("Erase Split Normals", blendShape.eraseSplitNormals);
			}
			
			EditorGUILayout.EndHorizontal();
		}
		
		if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(component); 
        }
		
		EditorGUILayout.EndVertical();
	}
}
