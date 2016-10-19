using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class InteractiveAssetSelector : EditorWindow {

	class AssetItem : IComparer<AssetItem> {
		public readonly Object asset;
		public readonly string path;
		public readonly string name;
		public AssetItem parent;
		public bool state;//whether the asset is selected, if folder this will work as foldout

		public AssetItem(Object asset, AssetItem parent = null, bool state = true) {
			this.asset = asset;
			this.path = AssetDatabase.GetAssetPath(asset);
			this.name = this.path.Substring(this.path.LastIndexOf('/')+1);
			this.parent = parent;
			this.state = state;
		}
		//TODO how did I implement the comparer for list sort?

		#region IComparer implementation

		public int Compare (AssetItem x, AssetItem y)
		{
			return EditorUtility.NaturalCompare(x.path, y.path);
		}

		#endregion
	}

	class FolderItem : AssetItem {
		public List<AssetItem> children = new List<AssetItem> ();

		public FolderItem(Object asset, AssetItem parent = null, bool state = true) : base (asset, parent, state);
	}


//	private List<AssetItem> selection = new List<AssetItem>();

	private FolderItem selectionRoot = new FolderItem(null);

	public System.Action<InteractiveAssetSelector> OnEndGUI;

	[MenuItem("Assets/Interactive Asset Selector")]
	public static InteractiveAssetSelector InitSelector() {
		return InitSelector(Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets));
	}

	[MenuItem("Assets/Custom Exporter")]
	public static InteractiveAssetSelector Export() {
		InteractiveAssetSelector ias = InitSelector();
		ias.OnEndGUI = (selector) => {
			EditorGUILayout.Space();
			if (GUILayout.Button("Export")){
				Debug.Log("Export");
			}
		};
		return ias;
	}

	public static InteractiveAssetSelector InitSelector(Object[] selection) {
		InteractiveAssetSelector ias = GetWindow<InteractiveAssetSelector>("Select Assets");

//		ias.selection.Clear();
//		ias.selection.Capacity = selection.Length;
		foreach(Object obj in selection) {
			ias.SortedInsert(obj);
		}
////		ias.selection.Sort((x,y) => EditorUtility.NaturalCompare(x.path, y.path));

		ias.Show();
		return ias;
	}

	public void SortedInsert(Object obj) {
		SortedInsert(new AssetItem(obj));
	}

	public void SortedInsert(string path) {
		SortedInsert(new AssetItem(AssetDatabase.LoadAssetAtPath(path, typeof(Object))));
	}

	void SortedInsert(AssetItem asset) {
		string[] splittedPath = asset.path.Split ('/');
		FolderItem currentFolder = selectionRoot;
		for (int depth = 0; depth < splittedPath.Length; depth++) {
			string item = splittedPath [depth];

			//Find -> insert -> continue
		}

		//check parent folders first
//		int parentPathLength = asset.path.LastIndexOf('/');
//		if (parentPathLength >= 0) {
//			SortedInsert(asset.path.Substring(0, parentPathLength));//Try to add parent
//		}
//
//		int index = selection.BinarySearch(asset, asset);//TODO there must be another way
//		if (index < 0) {
//			selection.Insert(~index, asset);
//		}
	}

	Vector2 scrollPosition;
	void OnGUI() {
		string currentPath = "";
		List<string> currentSplittedPath = new List<string>();
		int visiblePathIndex = 0;


//		foreach (SelectionAsset asset in selection) {
//			GUILayout.Label(asset.asset.name+" "+asset.asset.GetType()+" "+asset.path);
//		}
//
//		EditorGUILayout.Space();

		EditorGUILayout.BeginScrollView(scrollPosition);
		foreach (AssetItem asset in selection) {
			string path = asset.path;

			int parentPathLength = path.LastIndexOf('/');
			string parent = parentPathLength < 0 ? "" : path.Substring(0, parentPathLength);
			if (!currentPath.StartsWith(parent)) continue;//Hidden
			currentPath = parent;
			EditorGUI.indentLevel = PathDepth(currentPath)+1;

			string name = path.Substring(parentPathLength+1);//TODO make property?
			if (asset.isFolder) {
				asset.state = EditorGUILayout.Foldout(asset.state, name);
				if (asset.state) {
					currentPath = asset.path;
				}
			}
			else {
				asset.state = EditorGUILayout.ToggleLeft(name, asset.state);//
			}



			//TODO use split and depth
//			string[] splittedPath = path.Split('/');
//
//			int matchDepth = 0;
//			while (matchDepth < currentSplittedPath.Count && matchDepth < splittedPath.Length) {
//				if (!currentSplittedPath[matchDepth].Equals(splittedPath[matchDepth])) {
//					break;
//				}
//				matchDepth++;
//			}
//			EditorGUI.indentLevel = matchDepth;
//			currentSplittedPath.RemoveRange(matchDepth, currentSplittedPath.Count - matchDepth);
//			while (matchDepth < splittedPath.Length - 1) {
//				string folder = splittedPath[matchDepth++];
//				currentSplittedPath.Add(folder);
//				EditorGUILayout.Foldout(true, folder);
//				EditorGUI.indentLevel++;
//			}
//			EditorGUI.indentLevel++;//TODO icon content
//			EditorGUILayout.LabelField(splittedPath[matchDepth]);


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

		EditorGUILayout.EndScrollView();
//		foreach(SelectionAsset asset in selection) {
//			string path = asset.path;
//			string parentPath = Directory.GetParent(path).ToString();
//
//			if (parentPath.StartsWith(currentPath)) {
//				string
//			}
//		}

		//TODO drag and drop

		if (OnEndGUI != null) OnEndGUI(this);
	}

	int PathDepth(string path) {
		if (path.Equals("")) {
			return -1;
		}
		int count = 0, index = 0;
		while((index = path.IndexOf('/', index+1)) != -1) {
			count++;
		}
		return count;
	}
}
