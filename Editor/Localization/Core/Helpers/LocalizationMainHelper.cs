using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DreadScripts.Localization
{
	public static class LocalizationMainHelper
	{
		internal static GUIContent TextToContent(string text) => text == null ? null : new GUIContent(text);

		public static readonly GUIContent fallbackMissingContent =
			new GUIContent("[Missing Content]", "This content is missing from the language file");

		private static readonly GUIContent _tempContent = new GUIContent();

		public static GUIContent TempContent(string text, string tooltip = "", Texture2D icon = null)
		{
			_tempContent.text = text;
			_tempContent.tooltip = tooltip;
			_tempContent.image = icon;
			return _tempContent;
		}

		public static GUIContent ToGUIContent(this MiniContent mc, GUIContent fallback = null, Texture2D icon = null)
		{
			if (mc == null) return fallback ?? fallbackMissingContent;

			GUIContent content = mc;
			if (!ReferenceEquals(icon, null)) content.image = icon;
			return content;
		}

		///<summary>Gets the native word of 'Language' in the given language name. If it doesn't exists, returns false and outs 'Language'.</summary>
		public static bool TryGetLanguageWordTranslation(string languageName, out string translatedWord)
		{
			bool found =
				LocalizationConstants.LanguageWordTranslationDictionary.TryGetValue(languageName, out translatedWord);
			if (!found) translatedWord = "Language";
			return found;
		}

		public static void SetGlobalPreferredLanguage(LocalizationScriptableBase languageMap)
		{
			if (languageMap != null)
				SetGlobalPreferredLanguage(languageMap.languageName);
		}

		public static void SetGlobalPreferredLanguage(string languageName)
		{
			EditorPrefs.SetString(LocalizationConstants.PREFERRED_LANGUAGE_KEY, languageName);
			Debug.Log(
				$"[Localization] Preferred language set to {languageName}. This will try to be the default language if no specific language was set.");
		}
	}
}