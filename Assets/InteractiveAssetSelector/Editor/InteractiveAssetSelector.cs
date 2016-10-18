﻿using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class InteractiveAssetSelector : EditorWindow {

	class SelectionAsset : IComparer<SelectionAsset> {
		public Object asset;
		public string path;
		public bool selected;

		public SelectionAsset(Object asset, bool selected = false) {
			this.asset = asset;
			this.path = AssetDatabase.GetAssetPath(asset);
			this.selected = selected;
		}
		//TODO how did I implement the comparer for list sort?

		#region IComparer implementation

		public int Compare (SelectionAsset x, SelectionAsset y)
		{
			return EditorUtility.NaturalCompare(x.path, y.path);
		}

		#endregion
	}

	private List<SelectionAsset> selection = new List<SelectionAsset>();
	private List<string> visiblePaths = new List<string>();

	[MenuItem("Assets/Interactive Asset Selector")]
	public static void InitSelector() {
		InitSelector(Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets));
	}

	public static void InitSelector(Object[] selection) {
		InteractiveAssetSelector ias = GetWindow<InteractiveAssetSelector>("Select Assets");

		ias.selection.Clear();
		ias.selection.Capacity = selection.Length;
		foreach(Object obj in selection) {
			//TODO sorted insert
			string path = AssetDatabase.GetAssetPath(obj);
			Debug.Log(path+" "+AssetDatabase.AssetPathToGUID(path));
			if (path.Equals("") || Directory.Exists(path)) {//Not an asset file
				continue;
			}
			ias.SortedInsert(obj);
		}
//		ias.selection.Sort((x,y) => EditorUtility.NaturalCompare(x.path, y.path));

		ias.Show();
	}

	public void SortedInsert(Object obj) {
		SortedInsert(new SelectionAsset(obj));
	}

	void SortedInsert(SelectionAsset asset) {
		int index = selection.BinarySearch(asset, asset);//TODO there must be another way
		if (index < 0) {
			selection.Insert(~index, asset);
		}
	}

	void OnGUI() {
		string currentPath = "";
		List<string> currentSplittedPath = new List<string>();

		int visiblePathIndex = 0;


		foreach (SelectionAsset asset in selection) {
			GUILayout.Label(asset.asset.name+" "+asset.asset.GetType()+" "+asset.path);
		}

		EditorGUILayout.Space();

		foreach (SelectionAsset asset in selection) {
			string path = asset.path;

			//TODO use split and depth
			string[] splittedPath = path.Split('/');

			int matchDepth = 0;
			while (matchDepth < currentSplittedPath.Count && matchDepth < splittedPath.Length) {
				if (!currentSplittedPath[matchDepth].Equals(splittedPath[matchDepth])) {
					break;
				}
				matchDepth++;
			}
			EditorGUI.indentLevel = matchDepth;
			currentSplittedPath.RemoveRange(matchDepth, currentSplittedPath.Count - matchDepth);
			while (matchDepth < splittedPath.Length - 1) {
				string folder = splittedPath[matchDepth++];
				currentSplittedPath.Add(folder);
				EditorGUILayout.Foldout(true, folder);
				EditorGUI.indentLevel++;
			}
			EditorGUI.indentLevel++;//TODO icon content
			EditorGUILayout.LabelField(splittedPath[matchDepth]);
			//
//			while (!path.StartsWith(currentPath)){
//				int parentPathLength = currentPath.LastIndexOf('/');
//				if (parentPathLength < 0) parentPathLength = 0;
//				currentPath = currentPath.Substring(0, parentPathLength);
//			}
//			while (path.StartsWith(currentPath)) {
//				int nextSlash = path.IndexOf('/', currentPath.Length);
//				if (nextSlash < 0) {
//					string file = path.Substring(currentPath.Length);
//					EditorGUILayout.LabelField(file);
//					break;
//				}
//				else {
//					int parentPathLength = currentPath.Length;
//					currentPath = path.Substring(0, nextSlash);
//					EditorGUILayout.Foldout(true, currentPath.Substring(parentPathLength));
//				}
//			}

//			string visiblePath;// = visiblePaths[visiblePathIndex];

//			while (path.StartsWith(visiblePath = visiblePaths[visiblePathIndex++])) {
//				bool show = EditorGUILayout.Foldout(true, Path.GetDirectoryName(visiblePath));
//				if (!show) {
//					//TODO remove on delaycall
//				}
//				else {
//					currentPath = visiblePath;
//				}
//			}
//			while (assetIndex < selection.Count) {
//
//				assetIndex++;
//			}
//			if (EditorUtility.NaturalCompare(

		}

//		foreach(SelectionAsset asset in selection) {
//			string path = asset.path;
//			string parentPath = Directory.GetParent(path).ToString();
//
//			if (parentPath.StartsWith(currentPath)) {
//				string
//			}
//		}
	}
}
