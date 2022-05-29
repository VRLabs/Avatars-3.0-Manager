using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    
    // ReSharper disable once InconsistentNaming
    public class AV3Manager : EditorWindow
    {
        public static readonly string[] VrcParameters =
        {
            //VRC Defaults
            "IsLocal",
            "Viseme",
            "GestureLeft",
            "GestureRight",
            "GestureLeftWeight",
            "GestureRightWeight",
            "AngularY",
            "VelocityX",
            "VelocityY",
            "VelocityZ",
            "Upright",
            "Grounded",
            "Seated",
            "AFK",
            "TrackingType",
            "VRMode",
            "MuteSelf",
            "InStation",
            "Supine",
            "GroundProximity",
            
            //VRLabs Defaults
            "IsMirror"
        };
        
        private List<IAV3ManagerTab> _tabs;
        private IAV3ManagerTab _selectedTab;
        private ScrollView _selectedTabArea;

        private VRCAvatarDescriptor _avatar;

        [MenuItem("VRLabs/Avatars 3.0 Manager")]
        private static void ShowWindow()
        {
            var window = GetWindow<AV3Manager>();
            window.titleContent = new GUIContent("AV3 Manager");
            window.titleContent.image = Resources.Load<Texture>("AV3M/logo");
            window.minSize = new Vector2(400, 500);
            window.Show();
            
        }

        private void CreateGUI()
        {
            try
            {
                VisualElement root = rootVisualElement;
                var styleSheet = Resources.Load<StyleSheet>("AV3M/AV3ManagerStyle");
                root.styleSheets.Add(styleSheet);
                styleSheet = Resources.Load<StyleSheet>("AV3M/AV3ManagerStyle" + (EditorGUIUtility.isProSkin ? "Dark" : "Light"));
                root.styleSheets.Add(styleSheet);

                VisualElement topArea = new VisualElement().WithClass("top-area").ChildOf(root);
                VisualElement mainBody = new VisualElement()
                    .WithFlexDirection(FlexDirection.RowReverse)
                    .WithFlexGrow(1)
                    .ChildOf(root);
            
                _selectedTabArea = new ScrollView().WithClass("selected-tab").ChildOf(mainBody);
                VisualElement tabsArea = new VisualElement().WithClass("tabs-area").ChildOf(mainBody);

                LoadTopArea(topArea);
                LoadTabs(tabsArea);

                UpdateTabs();
            }
            catch (Exception e)
            {
                new Label(e.ToString()).WithWhiteSpace(WhiteSpace.Normal).ChildOf(rootVisualElement);
            }
        }

        private void UpdateTabs()
        {
            if (_avatar != null && _avatar.expressionParameters == null)
            {
                GenerateNewExpressionParametersAsset();
            }
            
            foreach (var tab in _tabs)
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

        private void LoadTabs(VisualElement tabsArea)
        {
            _tabs = new List<IAV3ManagerTab>();
            var tabTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => x.GetInterface(typeof(IAV3ManagerTab).FullName) != null)
                .OrderBy(x => x.Name)
                .ToList();
            
            foreach (var type in tabTypes)
            {
                var tab = Activator.CreateInstance(type) as IAV3ManagerTab;
                
                var tabButton = new Button();
                tabButton.tooltip = tab?.TabName;
                var iconElement = new VisualElement();
                iconElement.style.backgroundImage = new StyleBackground(tab?.TabIcon);
                iconElement.style.flexGrow = 1;
                iconElement.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                tabButton.Add(iconElement);
                tabButton.AddToClassList("tab-button");
                
                tabButton.clicked += () =>
                {
                    foreach (var button in tabsArea.Children())
                        if(button.ClassListContains("tab-button-selected"))
                            button.RemoveFromClassList("tab-button-selected");
                    
                    tabButton.AddToClassList("tab-button-selected");
                   
                    _selectedTabArea.Clear();
                    _selectedTabArea.Add(tab?.TabContainer);
                    _selectedTab = tab;
                    
                    tab.UpdateTab(_avatar);
                };
                
                tabsArea.Add(tabButton);
                _tabs.Add(tab);
            }

            if (tabsArea.childCount > 0 && _tabs.Count > 0)
            {
                tabsArea[0].AddToClassList("tab-button-selected");
                _selectedTabArea.Add(_tabs[0].TabContainer);
                _selectedTab = _tabs[0];
            }
        }
        
        private void GenerateNewExpressionParametersAsset()
        {
            Directory.CreateDirectory(AnimatorCloner.STANDARD_NEW_PARAMASSET_FOLDER);
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(AnimatorCloner.STANDARD_NEW_PARAMASSET_FOLDER + "Parameters.asset");
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