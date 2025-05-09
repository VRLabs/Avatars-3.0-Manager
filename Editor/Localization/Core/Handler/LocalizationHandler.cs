using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using static DreadScripts.Localization.MouseEvents;
using static DreadScripts.Localization.LocalizationMainHelper;
namespace DreadScripts.Localization
{

    public class LocalizationHandler<T> : LocalizationHandlerBase where T : LocalizationScriptableBase
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly Dictionary<T, Dictionary<string, int>> mapToLocalizationCache = new Dictionary<T, Dictionary<string, int>>();
        
        public Action onLanguageChanged;
        public T[] builtinLanguages;
        public T[] languageOptions;
        public string[] languageOptionsNames;
        public T selectedLanguage;
        public int selectedLanguageIndex;

        public bool hasAnyPreferredLanguage {get; internal set;}
        public bool hasTypePreferredLanguage {get; internal set;}
        public bool hasGloballyPreferredLanguage {get; internal set;}
        
        private bool shouldRefresh;
        private bool loadFromAssets;
        private Vector2 scroll;

        public string typePreferredLanguagePrefKey => $"{LocalizationConstants.LANGUAGE_KEY_PREFIX}{typeof(T).Name}";
  
        #region Instancing
        /// <param name="loadFromAssets">Whether to load localization files of this handler's type from the assets.</param>
        /// <param name="builtinLanguages">Additional localization scriptables to load.</param>
        public LocalizationHandler(bool loadFromAssets = true, params T[] builtinLanguages) : this(loadFromAssets, "English", builtinLanguages){}

        /// <param name="loadFromAssets">Whether to load localization files of this handler's type from the assets.</param>
        /// <param name="defaultLanguageName">The default language to load if no preferred language can be found.</param>
        /// <param name="builtinLanguages">Additional localization scriptables to load.</param>
        public LocalizationHandler(bool loadFromAssets, string defaultLanguageName, params T[] builtinLanguages)
        {
            this.loadFromAssets = loadFromAssets;
            this.builtinLanguages = builtinLanguages ?? Array.Empty<T>();
            var type = typeof(T);

            T[] allLanguages = null;
            if (this.loadFromAssets)
            {
                if (loadedLocalizationTypes.Add(type))
                {
                    try
                    {
                        var tempInstance = ScriptableObject.CreateInstance<T>();
                        var ms = MonoScript.FromScriptableObject(tempInstance);
                        Object.DestroyImmediate(tempInstance);
                        var msPath = AssetDatabase.GetAssetPath(ms);
                        var packagePath = msPath.Substring(0, msPath.IndexOf('/', msPath.IndexOf('/') + 1));
                        var guids = AssetDatabase.FindAssets($"t:{type.Name}", new[] {packagePath});
                        if (guids.Length > 0) allLanguages = guids.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<T>).Where(so => so != null).ToArray();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to force load localization files of type '{type.Name}':\n{e}");
                    }
                }

                var resourceLanguages = Resources.FindObjectsOfTypeAll<T>();
                if (resourceLanguages != null) allLanguages = allLanguages != null ? 
                    allLanguages.Concat(resourceLanguages).Distinct().ToArray() : resourceLanguages;
            }

            if (builtinLanguages != null)
                allLanguages = allLanguages == null ? builtinLanguages : allLanguages.Concat(builtinLanguages).Distinct().ToArray();
            
            if (allLanguages == null || allLanguages.Length == 0)
            {
                Debug.LogError($"No localization languages of type {type.Name} found");
                SetLanguage(null, false);
                return;
            }
            
            //Tries to load product specific lannguage first, then preferred language, then base language
            
            T map = null;
            var prefKey = typePreferredLanguagePrefKey;
            if (EditorPrefs.HasKey(prefKey))
            {
                map = allLanguages.FirstOrDefault(m => m.languageName == EditorPrefs.GetString(prefKey));
                if (map != null)
                {
                    hasAnyPreferredLanguage = true;
                    hasTypePreferredLanguage = true;
                }
            }

            if (map == null && EditorPrefs.HasKey(LocalizationConstants.PREFERRED_LANGUAGE_KEY))
            {
                map = allLanguages.FirstOrDefault(m => m.languageName == EditorPrefs.GetString(LocalizationConstants.PREFERRED_LANGUAGE_KEY));
                if (map != null)
                {
                    hasAnyPreferredLanguage = true;
                    hasGloballyPreferredLanguage = true;
                }
            }
            
            if (map == null) 
                map = allLanguages.FirstOrDefault(m => m.languageName == defaultLanguageName);
            
            if (map == null) map = allLanguages.First();
            SetLanguage(map, false);
        }
        #endregion

        #region Get with KeyName
        public bool TryGet(string keyName, out GUIContent content, Texture2D icon = null)
        {
            var mc = GetMiniContent(keyName);
            bool success = mc != null;
            content = mc.ToGUIContent(null, icon);
            return success;
        }
        
        public GUIContent Get(string keyName) => StringGet_Internal(keyName, null, null);
        public GUIContent Get(string keyName, string fallBack) => StringGet_Internal(keyName, TextToContent(fallBack), null);
        public GUIContent Get(string keyName, GUIContent fallBack) => StringGet_Internal(keyName, fallBack, null);
        public GUIContent Get(string keyName, Texture2D icon) => StringGet_Internal(keyName, null, icon);
        public GUIContent Get(string keyName, string fallBack, Texture2D icon) => StringGet_Internal(keyName, TextToContent(fallBack), icon);
        public GUIContent Get(string keyName, GUIContent fallBack, Texture2D icon) => StringGet_Internal(keyName, fallBack, icon);
        
        public GUIContent this[string keyName] => Get(keyName);
        
        private GUIContent StringGet_Internal(string keyName, GUIContent fallback, Texture2D icon) => GetMiniContent(keyName).ToGUIContent(fallback, icon);
        #endregion
        
        #region Get with EnumKey

        public bool TryGet(Enum enumKey, out GUIContent content, Texture2D icon = null)
        {
            if (!_globalEnumDictionary.TryGetValue(enumKey, out var key))
            {
                key = enumKey.ToString();
                _globalEnumDictionary.Add(enumKey, key);
            }
            return TryGet(key, out content, icon);
        }

        public GUIContent Get(Enum enumKey) => EnumGet_Internal(enumKey, null, null);
        public GUIContent Get(Enum enumKey, string fallBack) => EnumGet_Internal(enumKey, TextToContent(fallBack), null);
        public GUIContent Get(Enum enumKey, GUIContent fallBack) => EnumGet_Internal(enumKey, fallBack, null);
        public GUIContent Get(Enum enumKey, Texture2D icon) => EnumGet_Internal(enumKey, null, icon);
        public GUIContent Get(Enum enumKey, string fallBack, Texture2D icon) =>  EnumGet_Internal(enumKey, TextToContent(fallBack), icon);
        public GUIContent Get(Enum enumKey, GUIContent fallBack, Texture2D icon) => EnumGet_Internal(enumKey, fallBack, icon);
        public GUIContent this[Enum key] => Get(key);
        
        private static readonly Dictionary<Enum, string> _globalEnumDictionary = new Dictionary<Enum, string>();
        private GUIContent EnumGet_Internal(Enum value, GUIContent fallback, Texture2D icon)
        {
            if (!_globalEnumDictionary.TryGetValue(value, out var key))
            {
                key = value.ToString();
                _globalEnumDictionary.Add(value, key);
            }
            return GetMiniContent(key).ToGUIContent(fallback, icon);
        }
        #endregion
        
        public MiniContent GetMiniContent(string keyName)
        {
            if (selectedLanguage == null) return null;
            if (!mapToLocalizationCache.TryGetValue(selectedLanguage, out var localizationCache))
            {
                localizationCache = new Dictionary<string, int>();
                mapToLocalizationCache.Add(selectedLanguage, localizationCache);
            }
            
            var localizedContent = selectedLanguage.localizedContent;
            if (!(localizationCache.TryGetValue(keyName, out var index)))
            {
                index = Array.FindIndex(localizedContent, c => c.keyName == keyName);
                if (index == -1) return null;
                localizationCache.Add(keyName, index);
            }

            return localizedContent[index].content;
        }

        #region Language
        internal void SetLanguage(object userData)
        {
            T map = userData as T;
            if (map != null) SetLanguage(map, true);
        }
        
        public void SetLanguage(T map, bool setAsTypePreferred)
        {
            bool changed = selectedLanguage != map;
            selectedLanguage = map;
            if (setAsTypePreferred) 
                SetCurrentMapAsTypePrefferedLanguage();
            
            if (!EditorPrefs.HasKey(LocalizationConstants.PREFERRED_LANGUAGE_KEY)) 
                SetCurrentMapAsGlobalPrefferedLanguage();

            RefreshLanguageOptions();
            if (map != null) selectedLanguageIndex = Array.FindIndex(languageOptions, l => l == map);
            if (changed) onLanguageChanged?.Invoke();
        }

        /// <summary>Refreshes the options for the language selection dropdown</summary>
        public void RefreshLanguageOptions()
        {
            languageOptions = loadFromAssets ? Resources.FindObjectsOfTypeAll<T>().Concat(builtinLanguages).OrderBy(sb => sb.languageName).ToArray() : builtinLanguages;
            languageOptionsNames = languageOptions.Select(l => string.IsNullOrWhiteSpace(l.languageName) ? "Unnamed" : l.languageName).ToArray();
            shouldRefresh = false;
        }

        public void ClearCache() => mapToLocalizationCache.Clear();
        
        #endregion

        #region GUI
        
        /// <summary>Draws the language selection field. </summary>
        /// <param name="drawWithIcon">Draw a globe icon next to the text</param>
        public void DrawField(bool drawWithIcon = false)
        {
            string label = GetLanguageWordTranslation(selectedLanguage.languageName ?? "English");
            GUIContent content = new GUIContent(label);
            if (drawWithIcon) content.image = globeIcon.image;
            DrawField(content);
        }
        
        /// <summary>Draws the language selection field.</summary>
        /// <param name="content">The content to use for the label of the dropdown.</param>
        /// <param name="drawWithIcon">Draw a globe icon next to the text</param>
        public void DrawField(GUIContent content, bool drawWithIcon = false)
        {
            if (drawWithIcon) content = new GUIContent(content) {image = globeIcon.image};
            EditorGUI.BeginChangeCheck();
            selectedLanguageIndex = EditorGUILayout.Popup(content, selectedLanguageIndex, languageOptionsNames);
            if (EditorGUI.EndChangeCheck()) SetLanguage(languageOptions[selectedLanguageIndex]);
            

            var dropdownRect = GUILayoutUtility.GetLastRect();
            DoLanguageContextEvent(dropdownRect);
        }

        public void DrawIconOnlyField() => DrawIconOnlyField(EditorGUILayout.GetControlRect(false, 20, GUIStyle.none, GUILayout.Width(20)));
        public void DrawIconOnlyField(Rect rect)
        {
            Color ogColor = GUI.color;
            try
            {
                GUI.color = new Color(0.2f,0.9f,1);
                GUI.Label(rect, globeIcon);
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
                DoLanguageClickEvent(rect);
                DoLanguageContextEvent(rect);
            }
            finally { GUI.color = ogColor; }
        }

        public void DrawLanguageSelectionList()
        {
            var languageButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 24,
                padding = new RectOffset(8, 8, 8, 8)
            };

            void Draw(int i)
            {
                if (GUILayout.Button(languageOptionsNames[i], languageButtonStyle))
                    SetLanguage(languageOptions[i]);
                
                EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            using (new GUILayout.HorizontalScope())
            {
                int l = languageOptions.Length;
                using (new GUILayout.VerticalScope())
                    for (int i = 0; i < l; i+=2) Draw(i);
                
                using (new GUILayout.VerticalScope())
                    for (int i = 1; i < l; i+=2) Draw(i);
            }
            EditorGUILayout.EndScrollView();
        }

        internal void DoLanguageClickEvent(Rect rect)
        {
            if (OnLeftClick(rect)) 
                ShowLanguageOptionsMenu();
            
        }
        internal void DoLanguageContextEvent(Rect rect)
        {
            //Refresh the languages when the dropdown for languages gets hovered over.
            if (OnHoverEnter(rect, ref shouldRefresh))
                RefreshLanguageOptions();
            
            if (selectedLanguage != null && OnContextClick(rect))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent($"Set {selectedLanguage.languageName} as globally preferred language."), false, () =>
                {
                    SetGlobalPreferredLanguage(selectedLanguage.languageName);
                });
                menu.ShowAsContext();
            }
        }
        #endregion
        
        #region Helper
        public string GetLanguageWordTranslation(string languageName)
        {
            if (!TryGet("LanguageWordTranslation", out var content))
            {
                TryGetLanguageWordTranslation(languageName, out string translatedWord);
                content = TempContent(translatedWord);
            }

            return content.text;
        }

        public void ShowLanguageOptionsMenu()
        {
            GenericMenu menu = new GenericMenu();
            for (var i = 0; i < languageOptions.Length; i++)
            {
                var l = languageOptions[i];
                menu.AddItem(new GUIContent(languageOptionsNames[i]), selectedLanguageIndex == i, SetLanguage, l);
            }

            menu.ShowAsContext();
        }

        public void SetCurrentMapAsTypePrefferedLanguage() => SetTypePrefferedLanguage(selectedLanguage);
        public void SetCurrentMapAsGlobalPrefferedLanguage() => SetGlobalPreferredLanguage(selectedLanguage);

        public void SetTypePrefferedLanguage(LocalizationScriptableBase languageMap)
        {
            if (selectedLanguage != null) 
                SetTypePrefferedLanguage(languageMap.languageName);
        }
        public void SetTypePrefferedLanguage(string languageName)
        {
            EditorPrefs.SetString(typePreferredLanguagePrefKey, languageName);
            hasAnyPreferredLanguage = true;
            hasTypePreferredLanguage = true;
        }
        #endregion
    }
}
