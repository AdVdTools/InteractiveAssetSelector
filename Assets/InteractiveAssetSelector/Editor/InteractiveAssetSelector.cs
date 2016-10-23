/*
 * Created by Angel David on 18/10/2016.
 */

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class InteractiveAssetSelector : EditorWindow {

	class AssetItem : System.IComparable<string> {
		public readonly Object asset;
		public readonly string path;
		public readonly string name;
		public FolderItem parent;
		public bool selected;//whether the asset is selected

		public AssetItem(Object asset, FolderItem parent = null, bool selected = true) {
			this.asset = asset;
			this.path = AssetDatabase.GetAssetPath(asset);
			this.name = this.path.Substring(this.path.LastIndexOf('/')+1);//extension needed to differenciate files from folders
			Debug.Log(path+"_"+name);
			this.parent = parent;
			this.selected = selected;
		}

		#region IComparable implementation

		public int CompareTo (string other)
		{
			return EditorUtility.NaturalCompare(this.name, other);
		}

		#endregion

		public void DoGUI() {
			GUIContent content = new GUIContent(EditorGUIUtility.ObjectContent(this.asset, this.asset.GetType()));

			this.selected = EditorGUILayout.ToggleLeft(content, this.selected);

//			this.selected = EditorGUILayout.Toggle(this.selected, GUILayout.MaxWidth(12f));
//			Rect r = GUILayoutUtility.GetRect(15f, EditorGUIUtility.singleLineHeight, EditorStyles.label, GUILayout.MaxWidth(15f));
//			r.x += EditorGUI.indentLevel * 16f;
//			GUI.DrawTexture(r, content.image, ScaleMode.ScaleToFit);
//			EditorGUILayout.LabelField(asset.name);
		}
	}

	class FolderItem : AssetItem {
		public List<AssetItem> children = new List<AssetItem> ();
		public bool foldout = true;

		public FolderItem(Object asset, FolderItem parent = null, bool selected = true) : base (asset, parent, selected) {
			
		}

		public int BinarySearch(string childName) {
//			System.Array.BinarySearch(children.ToArray(), childName);

			int min = 0;
			int max = children.Count-1;

			while (min <= max) {
				int mid = (min + max) / 2;
				int comparison = children[mid].CompareTo(childName);
				if (comparison == 0) {
					return mid;
				}
				if (comparison < 0) {
					min = mid+1;
				}
				else {
					max = mid-1;
				}
			}
			return ~min;
		}

		public bool AnyChildrenSelected () {
			foreach (AssetItem asset in children) {
				if (asset is FolderItem) {
					FolderItem folder = (FolderItem)asset;
					if (folder.AnyChildrenSelected()) {
						return true;
					}
				}
				else {
					if (asset.selected) {
						return true;
					}
				}
			}
			return false;
		}

		public int ChildrenSelected () { // -1: Any, 0: None, 1: All
			bool any = false, all = true;
			foreach (AssetItem asset in children) {
				if (asset is FolderItem) {
					FolderItem folder = (FolderItem)asset;
					int cs = folder.ChildrenSelected();

					any |= cs != 0;
					all &= cs == 1;
				}
				else {
					any |= asset.selected;
					all &= asset.selected;
				}
			}
			return any ? (all ? 1 : -1) : 0;
		}

		public void SelectChildren() {
			foreach (AssetItem asset in children) {
				if (asset is FolderItem) {
					FolderItem folder = (FolderItem)asset;
					folder.selected = true;
					folder.SelectChildren();
				}
				else {
					asset.selected = true;
				}
			}
		}
		public void DeselectChildren() {
			foreach (AssetItem asset in children) {
				if (asset is FolderItem) {
					FolderItem folder = (FolderItem)asset;
					folder.selected = false;
					folder.DeselectChildren();
				}
				else {
					asset.selected = false;
				}
			}
		}
	}


//	private List<AssetItem> selection = new List<AssetItem>();

	private FolderItem selectionRoot;

	public bool ignoreEmptyFolders;
	public delegate void EndGUIDelegate(InteractiveAssetSelector assetSelector);
	public EndGUIDelegate OnEndGUI;

	[MenuItem("Assets/Interactive Asset Selector")]
	public static InteractiveAssetSelector InitSelector() {
		return InitSelector(Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets));
	}

	[MenuItem("Assets/Custom Exporter")]
	public static InteractiveAssetSelector Export() {
		InteractiveAssetSelector ias = InitSelector();
		ias.ignoreEmptyFolders = true;
		ias.OnEndGUI = (selector) => {
			EditorGUILayout.Space();
			if (GUILayout.Button("Export")){
				Debug.Log("Export");
			}
		};
		return ias;
	}

	public static InteractiveAssetSelector InitSelector(Object[] selection) {
		//GetWindow<InteractiveAssetSelector>().Close();
		InteractiveAssetSelector ias = GetWindow<InteractiveAssetSelector>(true, "Select Assets", true);

		Debug.Log("InitSelector");

//		ias.selection.Clear();
//		ias.selection.Capacity = selection.Length;
		foreach(Object obj in selection) {
			ias.SortedInsert(obj);
		}
////		ias.selection.Sort((x,y) => EditorUtility.NaturalCompare(x.path, y.path));

//		ias.ShowUtility();
		Debug.Log("ShowSelector");
		return ias;
	}

	void OnEnable() {
		if (selectionRoot == null) {
			Debug.Log("Creating Root");
			selectionRoot = new FolderItem(null);
		}
		Debug.Log("Repaint?");
		Repaint();
	}


	public void SortedInsert(IEnumerable<Object> objs) {
		foreach (Object obj in objs) {
			SortedInsert(obj);
		}
		ValidateSelection();
	}

	public void SortedInsert(IEnumerable<string> paths, bool recursive) {
		foreach (string path in paths) {
			SortedInsert(path, recursive);
		}
		ValidateSelection();
	}

	AssetItem SortedInsert(Object obj) {
		return SortedInsert(obj, AssetDatabase.GetAssetPath(obj));
	}

	AssetItem SortedInsert(string path, bool recursive) {
		if (recursive) {
			SortedInsert(GetAssetsAtPaths(path), false);
		}
		return SortedInsert(AssetDatabase.LoadAssetAtPath(path, typeof(Object)), path);
	}

	AssetItem SortedInsert(Object obj, string path) {//AssetItem asset) {
//		string[] splittedPath = asset.path.Split ('/');
//		FolderItem currentFolder = selectionRoot;
//		for (int depth = 0; depth < splittedPath.Length; depth++) {
//			string item = splittedPath [depth];
//
//			//Find -> insert -> continue
//		}
		if (AssetDatabase.IsSubAsset(obj)) {
			return null;
		}

		FolderItem parent;
		//check parent folders first//TODO change to top-down check/insert?
		int parentPathLength = path.LastIndexOf('/');
		if (parentPathLength >= 0) {
			parent = SortedInsert(path.Substring(0, parentPathLength), false) as FolderItem;//Try to add parent
		}
		else {
			parent = selectionRoot;
		}

		int index = parent.BinarySearch(path.Substring(parentPathLength+1));//TODO there must be another way
		if (index < 0) {
			AssetItem item = AssetDatabase.IsValidFolder(path) ? new FolderItem(obj, parent) : new AssetItem(obj, parent);
			parent.children.Insert(~index, item);
			return item;
		}
		else {
			return parent.children[index];
		}
	}


	/// <summary>
	/// Gets the asset paths at the specified paths.
	/// </summary>
	/// <returns>The assets at paths.</returns>
	/// <param name="paths">Paths.</param>
	string[] GetAssetsAtPaths(params string[] paths) {
		string[] assets = AssetDatabase.FindAssets(string.Empty, paths);
		for(int i = 0; i < assets.Length; i++) {
			assets[i] = AssetDatabase.GUIDToAssetPath(assets[i]);
		}
		return assets;
	}


	void DoItemGUI(AssetItem item) {
		if (item is FolderItem) {
			FolderItem folder = (FolderItem)item;

			Rect fRect = EditorGUILayout.BeginHorizontal();
			int childrenSelected = folder.ChildrenSelected();
			EditorGUI.showMixedValue = childrenSelected < 0;//Some but not all //folder.selected;//
			folder.foldout = ContentlessFoldout.DoFoldout(folder.foldout);
			//TODO get foldout rect -> EditorStyles.foldout.Draw..., on click -> switch (warp in method)

			GUI.changed = false;
//			bool selectFolder = EditorGUILayout.ToggleLeft(content, folder.selected);
			folder.DoGUI();
			if (GUI.changed) {
				if (folder.selected) {
					folder.SelectChildren();
				}
				else {
					folder.DeselectChildren();
				}
			}
			folder.selected = childrenSelected > 0;

			EditorGUILayout.EndHorizontal();
			if (Event.current.type == EventType.ContextClick && fRect.Contains(Event.current.mousePosition)) {
				FolderContextMenu(folder);
			}

			if (folder.foldout) {
				EditorGUI.indentLevel++;
				foreach(AssetItem i in folder.children) {
					DoItemGUI(i);//TODO return childrenselected state to decide this state 
//					folder.selected &= i.selected;
				}
				EditorGUI.indentLevel--;
			}
		}
		else {
			Rect iRect = EditorGUILayout.BeginHorizontal();
			EditorGUI.showMixedValue = false;
//			EditorGUI.indentLevel++;
			GUILayout.Space(20f);
			item.DoGUI();
//			EditorGUI.indentLevel--;
			EditorGUILayout.EndHorizontal();
			if (Event.current.type == EventType.ContextClick && iRect.Contains(Event.current.mousePosition)) {
				AssetContextMenu(item);
			}
		}
	}
	//TODO ignoredFolders = false

	void FolderContextMenu(FolderItem folder) {
		GenericMenu gm = new GenericMenu();
		AddCommonContextOptions(gm, folder);
		gm.AddItem(new GUIContent("Include folder recursively"), false, () => {
			SortedInsert(folder.path, true);
			ValidateSelection();
		});
		gm.AddItem(new GUIContent("Colapse children foldouts"), false, () => {
			foreach(AssetItem item in folder.children) {
				if (item is FolderItem) {
					((FolderItem)item).foldout = false;
				}
			}
		});
		gm.ShowAsContext();
	}

	void AssetContextMenu(AssetItem asset) {
		GenericMenu gm = new GenericMenu();
		AddCommonContextOptions(gm, asset);
		gm.AddItem(new GUIContent("Include dependencies"), false, () => {
			SortedInsert(AssetDatabase.GetDependencies(asset.path, true), false);
		});
		gm.ShowAsContext();
	}

	void AddCommonContextOptions(GenericMenu gm, AssetItem asset) {
		gm.AddItem(new GUIContent("Remove from selector"), false, () => {
			asset.parent.children.Remove(asset);
			ValidateSelection();
		});
	}


	Vector2 scrollPosition = Vector2.zero;
	void OnGUI() {
//		string currentPath = "";
//		List<string> currentSplittedPath = new List<string>();
//		int visiblePathIndex = 0;


//		foreach (SelectionAsset asset in selection) {
//			GUILayout.Label(asset.asset.name+" "+asset.asset.GetType()+" "+asset.path);
//		}
//
//		EditorGUILayout.Space();

		scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
		foreach (AssetItem asset in selectionRoot.children) {
			DoItemGUI(asset);

//			string path = asset.path;
//
//			int parentPathLength = path.LastIndexOf('/');
//			string parent = parentPathLength < 0 ? "" : path.Substring(0, parentPathLength);
//			if (!currentPath.StartsWith(parent)) continue;//Hidden
//			currentPath = parent;
//			EditorGUI.indentLevel = PathDepth(currentPath)+1;
//
//			string name = path.Substring(parentPathLength+1);//TODO make property?
//			if (asset.isFolder) {
//				asset.state = EditorGUILayout.Foldout(asset.state, name);
//				if (asset.state) {
//					currentPath = asset.path;
//				}
//			}
//			else {
//				asset.state = EditorGUILayout.ToggleLeft(name, asset.state);//
//			}



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

		// DragAndDrop operation
		if (Event.current.type == EventType.DragUpdated){
			Object[] objs = DragAndDrop.objectReferences;
			if (ArrayUtility.FindIndex(objs, (obj) => AssetDatabase.IsMainAsset(obj)) >= 0) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Link;
			}
		}
		else if (Event.current.type == EventType.DragPerform){
			DragAndDrop.AcceptDrag();
			Object[] objs = DragAndDrop.objectReferences;
			foreach(Object obj in objs) {
				string path = AssetDatabase.GetAssetPath(obj);
				SortedInsert(path, true);
			}
			//TEST
//			foreach(Object o in objs) {
//				string p = AssetDatabase.GetAssetPath(o);
//				if (!AssetDatabase.IsValidFolder(p)) {
//					Debug.Log("Folder: "+p);
//					foreach(string s in AssetDatabase.FindAssets("", new string[]{p})) {
//						Debug.Log("Asset: "+AssetDatabase.GUIDToAssetPath(s));
//					}
//				}
//			}
			ValidateSelection();
		}

		if (OnEndGUI != null) {
			OnEndGUI(this);//TODO close window if no delegate (have an empty one atleast
		}
		else {
			Close();
		}
	}

	void ValidateSelection() {
		if (ignoreEmptyFolders) {
			RemoveEmptyFolders(selectionRoot);
		}
	}

	void RemoveEmptyFolders(FolderItem folder) {//TODO move to FolderItem?
		folder.children.RemoveAll((asset) => {
			if (asset is FolderItem) {
				FolderItem f = (FolderItem)asset;
				RemoveEmptyFolders(f);
				return f.children.Count == 0;
			}
			return false;
		});
	}

	//TODO save selections + load dropdown
}
