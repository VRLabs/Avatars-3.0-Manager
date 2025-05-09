using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DreadScripts.Localization
{
	public static class LocalizationDebugger
	{
		public static bool foldout;
		public static string debugKey = "";

		public static void DrawDebugger<T>(LocalizationHandler<T> handler) where T : LocalizationScriptableBase
		{
			using (new GUILayout.VerticalScope(GUI.skin.box))
			{
				var type = typeof(T);
				foldout = EditorGUILayout.Foldout(foldout, $"{type.Name} Debugger");
				if (foldout)
				{
					EditorGUI.indentLevel++;
					DrawPrefStringField("Global Pref Language", LocalizationConstants.PREFERRED_LANGUAGE_KEY);
					DrawPrefStringField("Type Pref Language",
						$"{LocalizationConstants.LANGUAGE_KEY_PREFIX}{type.Name}");
					if (handler != null)
					{
						EditorGUILayout.Space();
						handler.hasAnyPreferredLanguage =
							EditorGUILayout.Toggle("Has Any Language", handler.hasAnyPreferredLanguage);
						handler.hasTypePreferredLanguage =
							EditorGUILayout.Toggle("Has Type Language", handler.hasTypePreferredLanguage);
						handler.hasGloballyPreferredLanguage = EditorGUILayout.Toggle("Has Global Language",
							handler.hasGloballyPreferredLanguage);
						using (new GUILayout.HorizontalScope())
						{
							handler.DrawField(true);
							handler.DrawIconOnlyField();
						}

						using (new GUILayout.HorizontalScope())
						{
							debugKey = EditorGUILayout.TextField(debugKey);
							if (GUILayout.Button(GUIContent.none, EditorStyles.popup, GUILayout.Width(20)))
							{
								GenericMenu menu = new GenericMenu();
								foreach (var key in handler.selectedLanguage.localizedContent.Select(lc => lc.keyName)
									         .Distinct().OrderBy(s => s))
									menu.AddItem(new GUIContent(key), debugKey == key, SetDebugKey, key);
								menu.ShowAsContext();
							}

							GUILayout.Label(handler[debugKey]);
						}

						using (new EditorGUI.DisabledScope(handler.onLanguageChanged == null))
							if (GUILayout.Button("Invoke On Change"))
								handler.onLanguageChanged?.Invoke();
					}

					EditorGUI.indentLevel--;
				}
			}
		}

		private static void DrawPrefStringField(string label, string prefKey)
		{
			using (new GUILayout.HorizontalScope())
			{
				string dummy = EditorPrefs.GetString(prefKey, string.Empty);
				EditorGUI.BeginChangeCheck();
				dummy = EditorGUILayout.DelayedTextField(label, dummy);
				if (EditorGUI.EndChangeCheck())
				{
					EditorPrefs.SetString(prefKey, dummy);
				}

				if (GUILayout.Button("Delete", GUILayout.ExpandWidth(false)))
					EditorPrefs.DeleteKey(prefKey);
			}
		}

		private static void SetDebugKey(object userData)
		{
			if (!(userData is string key)) return;
			debugKey = key;
		}
	}
}