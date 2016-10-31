using UnityEngine;
using UnityEditor;
using System.Collections;

public static class ContentlessFoldout {

	static int clickedFoldoutControl;
	public static float foldoutWidth = 12f;
	public static bool DoFoldout(bool foldout) {
		Rect r = GUILayoutUtility.GetRect(foldoutWidth, EditorGUIUtility.singleLineHeight, EditorStyles.foldout, GUILayout.MaxWidth(foldoutWidth));
		r = EditorGUI.IndentedRect(r); r.width = foldoutWidth;
		return DoFoldout(r, foldout);
	}
	public static bool DoFoldout(Rect rect, bool foldout) {
		Event e = Event.current;
		int controlId = EditorGUIUtility.GetControlID(FocusType.Keyboard);
		bool contains =  rect.Contains(e.mousePosition);
		bool thisControlClicked = clickedFoldoutControl == controlId;
		if (e.type == EventType.MouseDown) {
			if (contains) {
				clickedFoldoutControl = controlId;
				EditorGUIUtility.keyboardControl = controlId;
				e.Use();
			}
		}
		else if (e.type == EventType.MouseUp && thisControlClicked) {
			if (contains) {
				foldout = !foldout;
			}
			clickedFoldoutControl = -1;
			e.Use();
		}
		else if (EditorGUIUtility.keyboardControl == controlId && e.type == EventType.KeyDown) {
			if (e.keyCode == KeyCode.LeftArrow) {
				foldout = false;
				e.Use();
			}
			else if (e.keyCode == KeyCode.RightArrow) {
				foldout = true;
				e.Use();
			}
			else if (e.keyCode == KeyCode.UpArrow) {
				EditorGUIUtility.keyboardControl--;
				e.Use();
			}
			else if (e.keyCode == KeyCode.DownArrow) {
				EditorGUIUtility.keyboardControl++;
				e.Use();
			}
		}

		if (e.type == EventType.Repaint) {
			EditorStyles.foldout.Draw(rect, contains, thisControlClicked, foldout, EditorGUIUtility.keyboardControl == controlId);
		}
		return foldout;
	}

}
