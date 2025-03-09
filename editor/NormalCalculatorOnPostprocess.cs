using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;
using RykerTM.Tools.Hai; // Hai related methods by Hai~ https://github.com/hai-vr/

namespace RykerTM.Tools.OnPostprocess
{
	[InitializeOnLoad][DefaultExecutionOrder(200)]
	public class Window : EditorWindow
	{
		[InitializeOnLoadMethod]
        private static void OnInitialized()
        {
			if (IsEnabled)
			{
				AssetDatabase.Refresh();
				EditorApplication.delayCall += RecalculateAllDelayed_Init;
			}
        }
		// CLASSES		
		public class Blendshape
		{
			public bool isSelected = false;
			public string name;			
			
			public Blendshape(bool isSelected, string name)
			{
				this.isSelected = isSelected;
				this.name = name;
			}
		}
		[System.Serializable]
		public class Selection
		{
			public List<string> fbxPaths; 
			public List<string> fbxNames;
			public List<string> selectedBlendshapes;
			
			public Selection(List<string> fbxPaths, List<string> fbxNames, List<string> selectedBlendshapes)
			{
				this.fbxPaths = fbxPaths;
				this.fbxNames = fbxNames;
				this.selectedBlendshapes = selectedBlendshapes;
			}
		}
		[System.Serializable] // For writing with JsonUtility
		public class SelectionWrapper
		{
			public Selection wrappedSelection;
		}
		
		// JSON LOAD
		public static string jsonString;		
		public static List<Selection> LoadedSelections = new List<Selection>();
		
		private static void LoadAllProjectConfigs()
		{
			// Clear the shits
			LoadedSelections.Clear();
			
			string[] configPaths = AssetDatabase.FindAssets("_ryk_config t:TextAsset", new string[]{"Assets/"});
			if (configPaths == null)
			{
				Debug.LogWarning("[Blendshape Recalculator] Could not find any FBX configs.");
				return;
			}
			for (int i = 0; i < configPaths.Count(); i++)
			{
				configPaths[i] = AssetDatabase.GUIDToAssetPath(configPaths[i]);
			}
			
			foreach (string configPath in configPaths)
			{
				if (!File.Exists(configPath))
				{
					Debug.LogWarning($"[Blendshape Recalculator] Could not find {configPath}");
					continue;
				}
				// Read file
				string jsonString = File.ReadAllText(configPath);
				if (string.IsNullOrEmpty(jsonString))
				{
					Debug.Log($"Could not load this config file.[{configPath}]");
					continue;
				}
				// Convert from JSON
				SelectionWrapper wrapper = JsonUtility.FromJson<SelectionWrapper>(jsonString);
				if (!LoadedSelections.Contains(wrapper.wrappedSelection)) LoadedSelections.Add(wrapper.wrappedSelection);
				Debug.Log($"[Blendshape Recalculator] Loaded config {configPath}");
			}
			if (LoadedSelections.Count() > 0) BuildNameList(); // Build list of all FBX names for postprocess call
		}
		// LOAD		
		public static string prefKey;
		public static bool IsEnabled // Recalculate on Import/Init
		{
			get { return EditorPrefs.GetBool(prefKey, true); }
			set { EditorPrefs.SetBool(prefKey, value); }
		}
		public static List<string> allFbxNames = new List<string>(); // Easily readible list for model import
		
		private static void BuildNameList()
		{
			foreach (Selection selection in LoadedSelections)
			{
				if (selection.fbxNames.Count() == 0) continue;
				foreach (string fbxName in selection.fbxNames)
				{
					if (string.IsNullOrEmpty(fbxName)) continue;
					if (!allFbxNames.Contains(fbxName)) allFbxNames.Add(fbxName); // No rundant names
				}
			}
		}		
		// TOOLBAR
		[MenuItem("Tools/RykerTM/Normal Calculator/Configure Import Settings")]
        static void Open()
        {
			GetWindow<Window>("Normal Calculator (Import)", true);
		}		
		[MenuItem("Tools/RykerTM/Normal Calculator/Recalculate On Import")]
		private static void ToggleAction()
		{
			IsEnabled = !IsEnabled;
		}	 
		[MenuItem("Tools/RykerTM/Normal Calculator/Recalculate On Import", true)]
		private static bool ToggleActionValidate()
		{
			Menu.SetChecked("Tools/RykerTM/Normal Calculator/Recalculate On Import", IsEnabled);
			return true;
		}
		
		// GUI-CONFIG BUILDER
		private Vector2 scrollPosition;		
		public static Selection GUISelection = new Selection(new List<string>(), new List<string>(), new List<string>());
		public static List<GameObject> GUIFbxs = new List<GameObject>();
		public static List<Blendshape> GUIBlendshapeList = new List<Blendshape>();
		public static string GUIConfigTextField = ""; // Assets/ [GUIConfigTextField] _ryk_config.json
		private static bool CheckBSName(string name)
		{
			foreach (Blendshape blendshape in GUIBlendshapeList)
			{
				if (blendshape.name == name) return true;
			}
			return false;
		}		
		
		private Object configAsset;
		private void LoadGUIConfig(Object configAsset)
		{
			// Clear GUI
			GUISelection = new Selection(new List<string>(), new List<string>(), new List<string>());
			GUIFbxs.Clear();
			GUIBlendshapeList.Clear();
			
			string configAssetPath = AssetDatabase.GetAssetPath(configAsset);
			// Convert to text field
			GUIConfigTextField = configAssetPath;
			GUIConfigTextField = GUIConfigTextField.Replace("Assets/", "");
			GUIConfigTextField = GUIConfigTextField.Replace("_ryk_config.json", "");
			
			string jsonString = File.ReadAllText(configAssetPath);
			if (string.IsNullOrEmpty(jsonString)) {Debug.LogWarning("Could not read config"); return;} // Stop if JSON is empty
			
			SelectionWrapper wrapper = JsonUtility.FromJson<SelectionWrapper>(jsonString);
			if (wrapper != null && wrapper.wrappedSelection != null)
			{
				GUISelection = wrapper.wrappedSelection;
				Debug.Log($"Loaded config successfully from: {configAssetPath}");
				LoadGUIConfigFBXs();
				RefreshGUI();
			}
			else
			{
				Debug.LogWarning("Config was empty or corrupt, could not load.");
			}
		}
		private void LoadGUIConfigFBXs()
		{
			foreach (string fbxPath in GUISelection.fbxPaths)
			{
				GameObject fbx = AssetDatabase.LoadAssetAtPath(fbxPath, typeof(GameObject)) as GameObject;
				GUIFbxs.Add(fbx);
			}
		}
		
		private void SaveConfig()
		{
			string saveConfigPath = $"Assets/{GUIConfigTextField}_ryk_config.json";
			
			if (GUISelection == null) return;
			
			// Wrap
			SelectionWrapper wrapper = new SelectionWrapper { wrappedSelection = GUISelection};
			// Convert to JSON string
			string jsonString = JsonUtility.ToJson(wrapper);			
			if (string.IsNullOrEmpty(jsonString))
			{
				Debug.LogWarning("Could not save preferences. JSON string is empty.");
				return;
			}
			
			// Write file
			File.WriteAllText(saveConfigPath, jsonString);
			Debug.Log($"Wrote config JSON to: {saveConfigPath}");
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			LoadAllProjectConfigs();
		}
			
		private void OnGUI()
		{	
			GUILayout.Label("Ensure the asset path to your configured FBX(s)\nis final and will not change.");
			if (GUIFbxs.Count() > 0 && GUISelection.selectedBlendshapes.Count() > 0)
			{
				GUILayout.Space(5);
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("Assets/", GUILayout.Width(50));
				GUIConfigTextField = GUILayout.TextField(GUIConfigTextField, 96);
				GUILayout.Label("_ryk_config", GUILayout.Width(80));
				EditorGUILayout.EndHorizontal();
				if (GUILayout.Button("Save")) SaveConfig();
			}
			GUILayout.Space(5);			
			scrollPosition = GUILayout.BeginScrollView(scrollPosition);
			
			// BUILD FBX FIELDS
			int indexToRemove = 0;
			
			for (int i = 0; i < GUIFbxs.Count(); i++)
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.BeginHorizontal();
				
				GUIFbxs[i] = EditorGUILayout.ObjectField($"FBX {i}", GUIFbxs[i], typeof(GameObject), false) as GameObject;
				if (GUIFbxs[i] != null)
				{
					GUISelection.fbxNames[i] = GUIFbxs[i].name;
					GUISelection.fbxPaths[i] = AssetDatabase.GetAssetPath(GUIFbxs[i]);
				}
				
				if(i > 0) 
				{
					if (GUILayout.Button("-", GUILayout.Width(25)))
					{
						indexToRemove = i;
					}
				}
				EditorGUILayout.EndHorizontal();
				
				if (EditorGUI.EndChangeCheck() && GUISelection.fbxPaths.Count() > 0) 
				{
					RefreshGUI();
				}
			}
				
			if (GUILayout.Button("Add FBX"))
			{
				GUIFbxs.Add(null);
				GUISelection.fbxNames.Add(null);
				GUISelection.fbxPaths.Add(null);
			}
			if (indexToRemove > 0)
			{
				GUIFbxs.RemoveAt(indexToRemove);
				GUISelection.fbxPaths.RemoveAt(indexToRemove);
				GUISelection.fbxNames.RemoveAt(indexToRemove);
				indexToRemove = 0;
			}
			GUILayout.Space(5);
			
			// BUILD BLENDSHAPE LIST
			if (GUIBlendshapeList != null)
			{
				foreach (Blendshape blendshape in GUIBlendshapeList)
				{
					blendshape.isSelected = GUILayout.Toggle(blendshape.isSelected, blendshape.name);
					
					// Add or remove from selectedBlendshapes
					if (blendshape.isSelected && !GUISelection.selectedBlendshapes.Contains(blendshape.name))
					{
						GUISelection.selectedBlendshapes.Add(blendshape.name);
						//Debug.Log(blendshape.name + " blendshape will be recalculated");
					}
					if (!blendshape.isSelected && GUISelection.selectedBlendshapes.Contains(blendshape.name))
					{
						GUISelection.selectedBlendshapes.Remove(blendshape.name);
						//Debug.Log(blendshape.name + " blendshape will NOT be recalculated");
					}
				}
			}
			
			GUILayout.EndScrollView();
			// Footer
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
				EditorGUI.BeginChangeCheck();
				configAsset = EditorGUILayout.ObjectField("Load a _ryk_config", configAsset, typeof(TextAsset), false);
				if (EditorGUI.EndChangeCheck() && configAsset != null)
				{
					LoadGUIConfig(configAsset);
				}
			GUILayout.EndHorizontal();
		}
		private static void RefreshGUI()
		{
			GUIBlendshapeList.Clear();
			
			if (GUIFbxs.Count() == 0) return;
			foreach (GameObject fbx in GUIFbxs)
			{
				if (fbx == null) continue;
				SkinnedMeshRenderer[] skinnedMeshRenderers = fbx.GetComponentsInChildren<SkinnedMeshRenderer>();

				if (skinnedMeshRenderers.Length == 0)
				{
					Debug.LogWarning($"No Skinned Mesh Renderers found in the reference model {fbx.name}");
				}
				
				foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
				{
					Mesh mesh = skinnedMeshRenderer.sharedMesh; // Use sharedMesh, not mesh

					if (mesh != null)
					{
						int blendShapeCount = mesh.blendShapeCount;

						for (int i = 0; i < blendShapeCount; i++)
						{
							string blendshapeName = mesh.GetBlendShapeName(i);
							
							if (CheckBSName(blendshapeName)) continue; // Checks if name is redundant
							
							// Keep aleady selected blendshapes
							bool hasBeenSelected = GUISelection.selectedBlendshapes.Contains(blendshapeName);
							GUIBlendshapeList.Add(new Blendshape(hasBeenSelected, blendshapeName));
						}
					}
					else
					{
						Debug.LogWarning("Skinned Mesh Renderer has no mesh assigned!");
					}
				}
			}	
		}
		
		private static void RecalculateAllDelayed_Init()
		{
			EditorApplication.delayCall -= RecalculateAllDelayed_Init;
			LoadAllProjectConfigs();
			RecalculateAll();
		}
		private static void RecalculateAllDelayed()
		{
			EditorApplication.delayCall -= RecalculateAllDelayed;
			LoadAllProjectConfigs();
			RecalculateAll();
		}
		private static void RecalculateAll()
		{
			ModelImport.recalculationScheduled = false;
			if (!IsEnabled) return;
			// Populate this list so we can check if multiple configs are referencing the same FBX path
			List<string> loadedFBXPaths = new List<string>();
				
			if (LoadedSelections.Count() == 0) return;
			Debug.Log("Begin recalculating blendshapes.");			
			
			EditorUtility.DisplayProgressBar("Please wait", "Recalculating blendshape normals...", 0.5f);
			
			foreach (Selection selection in LoadedSelections)
			{ // Isolate each FBX.
				List<GameObject> recalculateFbxs = new List<GameObject>();
				
				if (selection == null) continue;
				if (selection.selectedBlendshapes.Count() == 0 || selection.fbxPaths.Count() == 0) continue;
				// LOAD FBXs FROM PATHS
				foreach (string fbxPath in selection.fbxPaths)
				{
					if (loadedFBXPaths.Contains(fbxPath))
					{
						Debug.LogWarning($"[Blendshape Recalculator] This FBX ({fbxPath}) has been referenced more than once. Make sure there isn't another _ryk_config referencing this path. Skipping this specific configuration.");
						continue;
					}
					loadedFBXPaths.Add(fbxPath);
					
					GameObject fbx = AssetDatabase.LoadAssetAtPath(fbxPath, typeof(GameObject)) as GameObject;
					if (fbx != null) recalculateFbxs.Add(fbx);
				}
				if (recalculateFbxs.Count() == 0)
				{
					Debug.LogWarning("[Blendshape Recalculator] Could not load any FBXs from a config. No FBXs left to configure after removing redundant FBXs?");
					continue;
				}
				// RECALCULATE
				foreach (GameObject fbx in recalculateFbxs)
				{	
					if (fbx == null) continue; // Check next in list
					SkinnedMeshRenderer[] smrs = fbx.GetComponentsInChildren<SkinnedMeshRenderer>();
					if (smrs.Length == 0)
					{
						Debug.LogWarning("No Skinned Mesh Renderers found in the reference model.");
						continue;
					}

					foreach (SkinnedMeshRenderer smr in smrs)
					{
						Mesh mesh = smr.sharedMesh; // Get the shared mesh (important!).
						if (mesh == null) continue;

						var thatSmrBlendShapes = Enumerable.Range(0, mesh.blendShapeCount)
							.Select(i => mesh.GetBlendShapeName(i))
							.ToList();
						var applicableBlendShapes = selection.selectedBlendshapes // Check both isSelected and presence on the mesh
							.Where(recalculate => smr)
							.SelectMany(normals => selection.selectedBlendshapes)
							.Where(blendShape => thatSmrBlendShapes.Contains(blendShape))
							.Distinct()
							.ToList();
						var eraseCustomSplitNormalsBlendShapes = new List<string>(); // Implement this later. Most cases will be fine without this.

						if (applicableBlendShapes.Count > 0)
						{
							Hai.NormalCalculator.RecalculateNormalsOf(smr, thatSmrBlendShapes, applicableBlendShapes, eraseCustomSplitNormalsBlendShapes);
						}
					}			
					AssetDatabase.SaveAssets();		
					AssetDatabase.Refresh();
				}
			}
			
			EditorUtility.ClearProgressBar();
		}
		
		public class ModelImport : AssetPostprocessor
		{
			public static bool recalculationScheduled = false;
			
			void OnPostprocessModel(GameObject g)
			{
				if (allFbxNames.Contains(g.name) && !recalculationScheduled)
				{
					// Recalculate all configured FBXs - Easier to match g with the right configuration
					recalculationScheduled = true;
					EditorApplication.delayCall += RecalculateAllDelayed;
				}
			}		
		}
	}
}