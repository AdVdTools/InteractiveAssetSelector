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
			this.parent = parent;
			this.selected = selected;
		}

		#region IComparable implementation

		public int CompareTo (string other)
		{
			return EditorUtility.NaturalCompare(this.name, other);
		}

		#endregion
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
				if (any && !all) {
					break;
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

	public string description;
	public delegate void OptionsGUIDelegate(InteractiveAssetSelector assetSelector);
	public OptionsGUIDelegate OnOptionsGUI;

	public static InteractiveAssetSelector InitSelector(Object[] selection) {
		InteractiveAssetSelector ias = GetWindow<InteractiveAssetSelector>(true, "Select Assets", true);

		foreach(Object obj in selection) {
			ias.SortedInsert(obj);
		}
		ias.ValidateSelection();

		return ias;
	}

//	[MenuItem("Assets/Interactive Asset Selector")]
	public static InteractiveAssetSelector InitSelector() {
		return InitSelector(Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets));
	}

	[MenuItem("Assets/Custom Exporter")]
	public static InteractiveAssetSelector ExportSelector() {
		InteractiveAssetSelector ias = InitSelector();
		ias.description = "Assets to Export";// Assets to export Assets to export Assets to exportAssets to exportAssets to exportAssets to export";
		ias.OnOptionsGUI = ExportOptionsGUI;
		return ias;
	}

	void OnEnable() {
		if (selectionRoot == null) {
			selectionRoot = new FolderItem(null);
		}
		this.autoRepaintOnSceneChange = true;
//		this.Focus();
	}

	void OnProjectChange() {
		ValidateSelection();
	}

	public void SortedInsert(IEnumerable<Object> objs) {
		foreach (Object obj in objs) {
			SortedInsert(obj);
		}
	}

	public void SortedInsert(IEnumerable<string> paths, bool recursive) {
		foreach (string path in paths) {
			SortedInsert(path, recursive);
		}
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

	AssetItem SortedInsert(Object obj, string path) {
		if (AssetDatabase.IsSubAsset(obj)) {
			return null;
		}

		FolderItem parent;
		// Check parent folders first
		int parentPathLength = path.LastIndexOf('/');
		if (parentPathLength >= 0) {
			parent = SortedInsert(path.Substring(0, parentPathLength), false) as FolderItem;//Try to add parent
		}
		else {
			parent = selectionRoot;
		}

		int index = parent.BinarySearch(path.Substring(parentPathLength+1));
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
		GUIContent content = new GUIContent(EditorGUIUtility.ObjectContent(item.asset, item.asset.GetType()));
		content.text = item.name;

		if (item is FolderItem) {
			FolderItem folder = (FolderItem)item;

			Rect fRect = EditorGUILayout.BeginHorizontal();
			int childrenSelected = folder.ChildrenSelected();
			EditorGUI.showMixedValue = childrenSelected < 0;//Some but not all
			folder.foldout = ContentlessFoldout.DoFoldout(folder.foldout);

			GUI.changed = false;
			bool selectFolder = EditorGUILayout.ToggleLeft(content, folder.selected);
			if (GUI.changed) {
				if (EditorGUI.showMixedValue) {//selectFolder == true if toggle was clicked
					if (selectFolder) {
						folder.selected = false;
						folder.DeselectChildren();
					}
				}
				else {
					if (folder.selected = selectFolder) {
						folder.SelectChildren();
					}
					else {
						folder.DeselectChildren();
					}
				}
			}
			folder.selected = childrenSelected != 0;

			EditorGUILayout.EndHorizontal();
			if (Event.current.type == EventType.ContextClick && fRect.Contains(Event.current.mousePosition)) {
				FolderContextMenu(folder);
			}

			if (folder.foldout) {
				EditorGUI.indentLevel++;
				foreach(AssetItem i in folder.children) {
					DoItemGUI(i);
				}
				EditorGUI.indentLevel--;
			}
		}
		else {
			Rect iRect = EditorGUILayout.BeginHorizontal();
			EditorGUI.showMixedValue = false;
			GUILayout.Space(20f);
			item.selected = EditorGUILayout.ToggleLeft(content, item.selected);
			EditorGUILayout.EndHorizontal();
			if (Event.current.type == EventType.ContextClick && iRect.Contains(Event.current.mousePosition)) {
				AssetContextMenu(item);
			}
		}
	}

	void FolderContextMenu(FolderItem folder) {
		GenericMenu gm = new GenericMenu();
		gm.AddItem(new GUIContent("Include folder recursively"), false, () => {
			SortedInsert(folder.path, true);
			ValidateSelection();
		});
		AddCommonContextOptions(gm, folder);
		gm.ShowAsContext();
	}

	void AssetContextMenu(AssetItem asset) {
		GenericMenu gm = new GenericMenu();
		gm.AddItem(new GUIContent("Include dependencies"), false, () => {
			SortedInsert(AssetDatabase.GetDependencies(asset.path, true), false);
		});
		AddCommonContextOptions(gm, asset);
		gm.ShowAsContext();
	}

	void AddCommonContextOptions(GenericMenu gm, AssetItem asset) {
		gm.AddItem(new GUIContent("Remove from selector"), false, () => {
			asset.parent.children.Remove(asset);
			ValidateSelection();
		});
	}

	static Color light = new Color(0.8f, 0.8f, 0.8f), lighter = new Color(0.9f, 0.9f, 0.9f);
	Vector2 scrollPosition = Vector2.zero;
	void OnGUI() {
		// Description
		Rect descRect = EditorGUILayout.BeginVertical();
		EditorGUI.DrawRect(descRect, light);
		if (!string.IsNullOrEmpty(description)) {
			GUIStyle multiLineBold = new GUIStyle();
			multiLineBold.font = EditorStyles.boldFont;
			multiLineBold.wordWrap = true;
			multiLineBold.padding = new RectOffset(8, 8, 16, 16);
			EditorGUILayout.LabelField(description, multiLineBold);
		}
		EditorGUI.DrawRect(new Rect(descRect.x, descRect.yMax, descRect.width, 1f), Color.grey);
		EditorGUILayout.EndVertical();

		// Selection
		if (selectionRoot.children.Count == 0) {
			EditorGUILayout.HelpBox("Nothing selected", MessageType.Info);
			GUILayout.FlexibleSpace();
		}
		else {
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
			foreach (AssetItem asset in selectionRoot.children) {
				DoItemGUI(asset);
			}
			EditorGUILayout.EndScrollView();
		}

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
			ValidateSelection();
		}

		// Options
		Rect optionsRect = EditorGUILayout.BeginVertical();
		EditorGUI.DrawRect(optionsRect, light);
		EditorGUI.DrawRect(new Rect(optionsRect.x, optionsRect.yMin, optionsRect.width, 1f), lighter);
		EditorGUILayout.Space();
		if (OnOptionsGUI != null) {
			OnOptionsGUI(this);
		}
		else {
			GUILayout.Label("Please, reopen this window to reload the options", EditorStyles.centeredGreyMiniLabel);
		}
		EditorGUILayout.Space();
		EditorGUILayout.EndVertical();
	}

	void ValidateSelection() {
		ValidateAssetPaths(selectionRoot);
//		selectionRoot.RemoveEmptySubFolders();
	}


	void ValidateAssetPaths(FolderItem folder) {
		folder.children.RemoveAll((asset) => {
			if (asset is FolderItem) {
				FolderItem f = (FolderItem)asset;
				ValidateAssetPaths(f);
				if (f.children.Count == 0) {
					return true;
				}
			}
			string guid = AssetDatabase.AssetPathToGUID(asset.path);
			if (asset.asset == null || string.IsNullOrEmpty(guid)) {
				if (asset.asset != null) {
					string newPath = SortedInsert(asset.asset).path;
					Debug.LogWarning(asset.path+" moved to "+newPath);
				}
				return true;
			}
			return false;
		});
	}




	public static void ExportOptionsGUI(InteractiveAssetSelector selector) {
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Include All Dependencies")) {
			selector.SortedInsert(AssetDatabase.GetDependencies(selector.GetSelectedPaths()), false);
		}
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Export...")){
			selector.ExportSelection();
		}
		EditorGUILayout.EndHorizontal();
	}

	public void ExportSelection(string directory = "", string name = "") {
		string path = EditorUtility.SaveFilePanel("Export package", directory, name, "unitypackage");
		AssetDatabase.ExportPackage(GetSelectedPaths(), path, ExportPackageOptions.Interactive);
	}

	public string[] GetSelectedPaths() {
		List<string> paths = new List<string>();
		AddSelectedPaths(selectionRoot, paths);
		return paths.ToArray();
	}

	void AddSelectedPaths(FolderItem folder, List<string> pathsList) {
		foreach(AssetItem item in folder.children) {
			if (item is FolderItem) {
				AddSelectedPaths((FolderItem)item, pathsList);
			}
			else if (item.selected) {
				pathsList.Add(item.path);
			}
		}
	}

	public Object[] GetSelectedAssets() {
		List<Object> objs = new List<Object>();
		AddSelectedAssets(selectionRoot, objs);
		return objs.ToArray();
	}

	void AddSelectedAssets(FolderItem folder, List<Object> objsList) {
		foreach(AssetItem item in folder.children) {
			if (item is FolderItem) {
				AddSelectedAssets((FolderItem)item, objsList);
			}
			else if (item.selected) {
				objsList.Add(item.asset);
			}
		}
	}

	public string[] GetAllPaths() {
		List<string> paths = new List<string>();
		AddChildrenPaths(selectionRoot, paths);
		return paths.ToArray();
	}

	void AddChildrenPaths(FolderItem folder, List<string> pathsList) {
		foreach(AssetItem item in folder.children) {
			pathsList.Add(item.path);
			if (item is FolderItem) {
				AddChildrenPaths((FolderItem)item, pathsList);
			}
		}
	}

	//TODO textures not found as dependencies!! update before checking?

	//TODO save selections + load dropdown
}
