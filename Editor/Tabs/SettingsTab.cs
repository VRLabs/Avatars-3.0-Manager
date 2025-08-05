using System;
using DreadScripts.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;
using static VRLabs.AV3Manager.AV3ManagerLocalization.Keys;

namespace VRLabs.AV3Manager
{
	[TabGroup(1)]
	public class SettingsTab : IAV3ManagerTab
	{
		public VisualElement TabContainer { get; set; }
		public LocalizationHandler<AV3ManagerLocalization> LocalizationHandler = AV3Manager.LocalizationHandler;
		public string TabName { get; set; }
		public Texture2D TabIcon { get; set; }

		
		public SettingsTab()
		{
			TabContainer = new VisualElement();
			TabName = LocalizationHandler.Get(Settings_Settings).text;
			TabIcon = EditorGUIUtility.IconContent("d_SettingsIcon@2x").image as Texture2D;

			DrawFieldUIElements(TabContainer);
		}

		public static bool ignoreMaxParameterLimit = false;
		
		public void DrawFieldUIElements(VisualElement parent)
		{
			var dropdown = new DropdownField
			{
				label = LocalizationHandler.GetLanguageWordTranslation(LocalizationHandler.selectedLanguage?.languageName ?? "English"),
				choices = new System.Collections.Generic.List<string>(LocalizationHandler.languageOptionsNames),
				index = LocalizationHandler.selectedLanguageIndex
			};
			dropdown.AddToClassList("top-spaced");
			parent.Add(dropdown);


			var infoLabel =
				new Label(LocalizationHandler.Get(Settings_HelpMessage).text)
					.WithMargin(6, 0, 0, 0)
					.WithFontSize(10); 
			parent.Add(infoLabel);
			
			dropdown.RegisterValueChangedCallback(evt =>
			{
				var newIndex = Array.IndexOf(LocalizationHandler.languageOptionsNames, evt.newValue);
				if (newIndex != -1)
				{
					LocalizationHandler.selectedLanguageIndex = newIndex;
					LocalizationHandler.SetLanguage(LocalizationHandler.languageOptions[LocalizationHandler.selectedLanguageIndex], true);
					var window = EditorWindow.GetWindow<AV3Manager>();
					window.rootVisualElement.Clear();
					window.CreateGUI();
					window.rootVisualElement.MarkDirtyRepaint();
				}
			});
            
			dropdown.RegisterCallback<MouseEnterEvent>(evt =>
			{
				LocalizationHandler.RefreshLanguageOptions();
				dropdown.choices = new System.Collections.Generic.List<string>(LocalizationHandler.languageOptionsNames);
			});
            
			if (LocalizationHandler.selectedLanguage != null)
			{
				dropdown.AddManipulator(new ContextualMenuManipulator(evt =>
				{
					evt.menu.AppendAction(
						$"Set {LocalizationHandler.selectedLanguage.languageName} as globally preferred language",
						action => LocalizationMainHelper.SetGlobalPreferredLanguage(LocalizationHandler.selectedLanguage.languageName)
					);
				}));
			}
			
			
			var ignoreMaxParameterToggle = FluentUIElements.NewToggle(LocalizationHandler.Get(Settings_IgnoreParamLimit).text, ignoreMaxParameterLimit)
				.WithMargin(5, 10, 0, 4)
				.ChildOf(TabContainer);
			ignoreMaxParameterToggle.RegisterValueChangedCallback(evt =>
			{
				ignoreMaxParameterLimit = evt.newValue;
				var window = EditorWindow.GetWindow<AV3Manager>();
				window.rootVisualElement.Clear();
				window.CreateGUI();
			});
		}
		public void UpdateTab(VRCAvatarDescriptor avatar)
		{
		}
	}
}