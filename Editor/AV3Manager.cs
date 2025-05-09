using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DreadScripts.Localization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;

namespace VRLabs.AV3Manager
{
	// ReSharper disable once InconsistentNaming
	public interface IAV3ManagerTab
	{
		VisualElement TabContainer { get; set; }
		string TabName { get; set; }
		Texture2D TabIcon { get; set; }
		void UpdateTab(VRCAvatarDescriptor avatar);
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class TabOrderAttribute : Attribute
	{
		public int Order { get; }

		public TabOrderAttribute(int order)
		{
			Order = order;
		}
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class TabGroupAttribute : Attribute
	{
		public int GroupNumber { get; }

		public TabGroupAttribute(int groupNumber)
		{
			GroupNumber = groupNumber;
		}
	}

	// ReSharper disable once InconsistentNaming
	public class AV3Manager : EditorWindow
	{
		public static readonly string[] VrcParameters =
		{
			//VRC Defaults
			"IsLocal",
			"Viseme",
			"Voice",
			"GestureLeft",
			"GestureRight",
			"GestureLeftWeight",
			"GestureRightWeight",
			"AngularY",
			"VelocityX",
			"VelocityY",
			"VelocityZ",
			"VelocityMagnitude",
			"Upright",
			"Grounded",
			"Seated",
			"AFK",
			"TrackingType",
			"VRMode",
			"MuteSelf",
			"InStation",
			"Earmuffs",
			"Supine",
			"GroundProximity",
			"ScaleModified",
			"ScaleFactor",
			"ScaleFactorInverse",
			"EyeHeightAsMeters",
			"EyeHeightAsPercent",
			"IsOnFriendsList",
			"AvatarVersion",

			//VRLabs Defaults
			//IsMirror is legacy, MirrorDetection/IsMirror is current
			"MirrorDetection/IsMirror",
			"IsMirror"
		};

		private List<List<IAV3ManagerTab>> _tabs;
		private IAV3ManagerTab _selectedTab;
		private ScrollView _selectedTabArea;
		private int _selectedTabGroupIndex = 0;
		private int _selectedTabIndex = 0;
		private List<VisualElement> _tabsContainers = new List<VisualElement>();

		private static LocalizationHandler<AV3ManagerLocalization> _localizationHandler;

		public static LocalizationHandler<AV3ManagerLocalization> LocalizationHandler
		{
			get
			{
				if (_localizationHandler == null)
					_localizationHandler = new LocalizationHandler<AV3ManagerLocalization>();
				return _localizationHandler;
			}
		}

		private VRCAvatarDescriptor _avatar;

		[MenuItem("VRLabs/Avatars 3.0 Manager")]
		internal static void ShowWindow()
		{
			var window = GetWindow<AV3Manager>();
			window.titleContent = new GUIContent("AV3 Manager");
			window.titleContent.image = Resources.Load<Texture>("AV3M/logo");
			window.minSize = new Vector2(400, 20);
			window.Show();
		}

		internal void CreateGUI()
		{
			try
			{
				VisualElement root = rootVisualElement;
				var styleSheet = Resources.Load<StyleSheet>("AV3M/AV3ManagerStyle");
				root.styleSheets.Add(styleSheet);
				styleSheet =
					Resources.Load<StyleSheet>("AV3M/AV3ManagerStyle" +
					                           (EditorGUIUtility.isProSkin ? "Dark" : "Light"));
				root.styleSheets.Add(styleSheet);

				VisualElement topArea = new VisualElement().WithClass("top-area").ChildOf(root);
				VisualElement mainBody = new VisualElement()
					.WithFlexDirection(FlexDirection.RowReverse)
					.WithFlexGrow(1)
					.ChildOf(root);

				_selectedTabArea = new ScrollView().WithClass("selected-tab").ChildOf(mainBody);

				VisualElement tabsContainer = new VisualElement().ChildOf(mainBody);

				ScrollView tabsArea = new ScrollView()
					.WithClass("tabs-area")
#if UNITY_2022_1_OR_NEWER
					.WithScrollerVisibility(ScrollerVisibility.Hidden, ScrollerVisibility.Hidden)
#else
                    .WithScrollbarVisibility(false, false)
#endif
					.ChildOf(tabsContainer);

				new VisualElement().WithClass("tabs-empty-area").ChildOf(tabsContainer);
				var tabsBottomArea = new VisualElement().WithClass("tabs-bottom-area").ChildOf(tabsContainer);


				LoadTopArea(topArea);
				LoadTabs(new List<VisualElement> { tabsArea, tabsBottomArea });

				UpdateTabs();
			}
			catch (Exception e)
			{
				new Label(e.ToString()).WithWhiteSpace(WhiteSpace.Normal).ChildOf(rootVisualElement);
				Debug.LogException(e);
			}
		}

		private void UpdateTabs()
		{
			if (_avatar != null && _avatar.expressionParameters == null)
			{
				GenerateNewExpressionParametersAsset();
			}

			foreach (var tab in _tabs.SelectMany(x => x))
			{
				tab.UpdateTab(_avatar);
			}

			_selectedTabArea.Clear();
			_selectedTabArea.Add(_selectedTab?.TabContainer);
		}

		private void LoadTopArea(VisualElement topArea)
		{
			ObjectField avatar = FluentUIElements.NewObjectField("Avatar", typeof(VRCAvatarDescriptor), _avatar)
				.WithClass("avatar-field")
				.ChildOf(topArea);

			var refreshButton = FluentUIElements.NewButton(UpdateTabs)
				.WithClass("refresh-button")
				.ChildOf(topArea);

			avatar.RegisterValueChangedCallback(e =>
			{
				_avatar = (VRCAvatarDescriptor)e.newValue;

				UpdateTabs();
			});
		}

		private void LoadTabs(List<VisualElement> tabsAreas)
		{
			_tabs = new List<List<IAV3ManagerTab>>();
			var tabGroups = AppDomain.CurrentDomain
				.GetAssemblies()
				.SelectMany(x => x.GetTypes())
				.Where(x => x.GetInterface(typeof(IAV3ManagerTab).FullName) != null)
				.GroupBy(x => x.GetCustomAttribute<TabGroupAttribute>()?.GroupNumber ?? 0);

			int index = 0;

			foreach (var group in tabGroups)
			{
				_tabs.Add(new List<IAV3ManagerTab>());

				var tabTypes = group
					.OrderByDescending(x => x.GetCustomAttribute<TabOrderAttribute>()?.Order ?? 0)
					.ThenBy(x => x.Name)
					.ToList();

				foreach (var type in tabTypes)
				{
					var tab = Activator.CreateInstance(type) as IAV3ManagerTab;

					var tabButton = new Button();
					tabButton.tooltip = tab?.TabName;
					var iconElement = new VisualElement();
					iconElement.style.backgroundImage = new StyleBackground(tab?.TabIcon);
					iconElement.style.flexGrow = 1;
#if UNITY_2022_1_OR_NEWER
					iconElement.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
					iconElement.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
					iconElement.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
					iconElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
#else
                    iconElement.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
#endif
					tabButton.Add(iconElement);
					tabButton.AddToClassList("tab-button");

					var tabIndex = _tabs[index].Count;
					var tabGroupIndex = index;
					tabButton.clicked += () =>
					{
						foreach (var button in tabsAreas.SelectMany(e => e.Children()))
							if (button.ClassListContains("tab-button-selected"))
								button.RemoveFromClassList("tab-button-selected");

						tabButton.AddToClassList("tab-button-selected");

						_selectedTabArea.Clear();
						_selectedTabArea.Add(tab?.TabContainer);
						_selectedTab = tab;
						_selectedTabIndex = tabIndex;
						_selectedTabGroupIndex = tabGroupIndex;

						tab.UpdateTab(_avatar);
					};

					tabsAreas[index].Add(tabButton);
					_tabs[index].Add(tab);
				}

				index++;
			}

			if (tabsAreas.Count > 0 && tabsAreas[0].childCount > 0 && _tabs.Count > 0 && _tabs[0].Count > 0)
			{

				_selectedTab = _tabs[_selectedTabGroupIndex][_selectedTabIndex];
				_selectedTabArea.Add(_tabs[_selectedTabGroupIndex][_selectedTabIndex].TabContainer);
				tabsAreas[_selectedTabGroupIndex][_selectedTabIndex].WithClass("tab-button-selected");
				
				tabsAreas[0][0].WithClass("tab-button-top");
				var lastTab = tabsAreas.Last();
				lastTab[lastTab.childCount - 1].WithClass("tab-button-bottom");
			}
		}

		private void GenerateNewExpressionParametersAsset()
		{
			Directory.CreateDirectory(AnimatorCloner.STANDARD_NEW_PARAMASSET_FOLDER);
			string uniquePath =
				AssetDatabase.GenerateUniqueAssetPath(
					AnimatorCloner.STANDARD_NEW_PARAMASSET_FOLDER + "Parameters.asset");
			_avatar.expressionParameters = CreateInstance<VRCExpressionParameters>();
			// Initialize vrc parameters array
			_avatar.expressionParameters.parameters = new Parameter[3];

			// Add default parameters
			_avatar.expressionParameters.parameters[0] = new Parameter
			{
				name = "VRCEmote",
				valueType = VRCExpressionParameters.ValueType.Int,
				defaultValue = 0,
				saved = false
			};
			_avatar.expressionParameters.parameters[1] = new Parameter
			{
				name = "VRCFaceBlendH",
				valueType = VRCExpressionParameters.ValueType.Float,
				defaultValue = 0,
				saved = false
			};
			_avatar.expressionParameters.parameters[2] = new Parameter
			{
				name = "VRCFaceBlendV",
				valueType = VRCExpressionParameters.ValueType.Float,
				defaultValue = 0,
				saved = false
			};

			AssetDatabase.CreateAsset(_avatar.expressionParameters, uniquePath);
			EditorUtility.SetDirty(_avatar.expressionParameters);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
	}
}