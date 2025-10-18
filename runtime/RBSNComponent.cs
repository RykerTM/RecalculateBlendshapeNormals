using UnityEngine;
using System.Linq;
using VRC.SDKBase;
using System.Collections.Generic;

namespace RykerTM.Tools.RBSN
{
	[AddComponentMenu("RykerTM/Recalculate Blend Shape Normals"), DisallowMultipleComponent]
	public class RBSNComponent : MonoBehaviour, VRC.SDKBase.IEditorOnly
	{
		public SkinnedMeshRenderer smr;
		public List<BlendShape> blendShapes;
		
		[System.Serializable]
		public class BlendShape
		{
			public string name;	
			public bool isSelected = false;
			public bool eraseSplitNormals = false;
			
			public BlendShape(string name)
			{
				this.name = name;
				this.isSelected = false;
				this.eraseSplitNormals = false;
			}
		}
		
		// Called when instantiated or by the GUI
		public void RefreshSMR()
		{
			if (blendShapes == null || blendShapes.Count == 0) blendShapes = new List<BlendShape>();
			
			smr = GetComponent<SkinnedMeshRenderer>();
			if (smr == null) return;
			
			Mesh mesh = smr.sharedMesh;			
			if (mesh == null) return;
			
			int blendShapeCount = mesh.blendShapeCount;
			for (int i = 0; i < blendShapeCount; i++)
			{
				string blendShapeName = mesh.GetBlendShapeName(i);
				
				if (BlendShapeListed(blendShapeName)) continue;
				
				blendShapes.Add(new BlendShape(blendShapeName));
			}
			
			// Clear out blend shapes no longer on the smr
			var thatSmrBlendShapes = Enumerable.Range(0, mesh.blendShapeCount)
				.Select(i => mesh.GetBlendShapeName(i))
				.ToList();
			var applicableBlendShapes = blendShapes 
				.Where(blendShape => thatSmrBlendShapes.Contains(blendShape.name))
				.Distinct()
				.ToList();
				
			blendShapes = applicableBlendShapes;
		}
		
		private bool BlendShapeListed(string name)
		{
			foreach (BlendShape blendShape in blendShapes)
			{
				if (blendShape == null) continue;
				
				if (blendShape.name == name) return true;
			}
			return false;
		}		
	}
}