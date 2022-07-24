using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;

namespace VRLabs.AV3Manager
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedType.Global
    public class WDTab : IAV3ManagerTab
    {
        public VisualElement TabContainer { get; set; }
        public string TabName { get; set; }
        public Texture2D TabIcon { get; set; }
        
        private VRCAvatarDescriptor _avatar;
        private readonly VisualElement _statesListContainer;
        private readonly Label _mixedWdLabel;
        private readonly Button _wdOnButton;
        private readonly Button _wdOffButton;
        private readonly Label _emptyMotions;

        public WDTab()
        {
            TabContainer = new VisualElement();
            TabName = "Write Defaults";
            TabIcon = Resources.Load<Texture2D>("AV3M/WdTabIcon" +(EditorGUIUtility.isProSkin ? "Dark" : "Light"));
            bool forceWd = EditorPrefs.GetBool("AV3MForceWD", false);

            var forceToggle = FluentUIElements.NewToggle("Force all WD", forceWd)
                .WithMargin(0, 10, 0, 4)
                .ChildOf(TabContainer);
            
            Label wdWarningLabel = new Label("Forcing the Write Defaults settings on states that explicitly request a specific setting is not advisable.")
                .WithClass("warning-label", "bordered-container")
                .WithMargin(5, 0)
                .ChildOf(TabContainer);
            
            if(!forceWd) wdWarningLabel.AddToClassList("hidden");

            forceToggle.RegisterValueChangedCallback(evt =>
            {
                forceWd = evt.newValue;
                EditorPrefs.SetBool("AV3MForceWD", forceWd);
                
                if(forceWd) 
                    wdWarningLabel.RemoveFromClassList("hidden");
                else
                    wdWarningLabel.AddToClassList("hidden");
            });
            
            var buttonsContainer = new VisualElement()
                .WithFlexDirection(FlexDirection.Row)
                .ChildOf(TabContainer);
            
            _wdOnButton = FluentUIElements.NewButton("Set WD Off", "Set Write Defaults to off.",
                    () =>
                    {
                        if (_avatar == null) return;
                        AV3ManagerFunctions.SetWriteDefaults(_avatar, false, forceWd);
                        UpdateWDList();
                    })
                .WithClass("grow-control")
                .ChildOf(buttonsContainer);
            _wdOffButton = FluentUIElements.NewButton("Set WD On", "Set Write Defaults to on.",
                    () =>
                    {
                        if (_avatar == null) return;
                        AV3ManagerFunctions.SetWriteDefaults(_avatar,  true, forceWd);
                        UpdateWDList();
                    })
                .WithClass("grow-control")
                .ChildOf(buttonsContainer);
            
            _mixedWdLabel = new Label("You have mixed write defaults in your layers, you may experience weird interactions in-game.")
                .WithClass("warning-label", "bordered-container", "hidden")
                .WithMargin(5, 0)
                .ChildOf(TabContainer);
            
            _emptyMotions = new Label("Some states have no motions, this can be an issue when using WD Off. Upon setting a unified WD state, an empty motion clip will fill those states.")
                .WithClass("warning-label", "bordered-container", "hidden")
                .WithMargin(5, 0)
                .ChildOf(TabContainer);

            new Label("WD List")
                .WithClass("header")
                .ChildOf(TabContainer);
            
            _statesListContainer = new VisualElement().WithMargin(5, 0).ChildOf(TabContainer);

        }
        
        public void UpdateTab(VRCAvatarDescriptor avatar)
        {
            _avatar = avatar;
            _statesListContainer.Clear();
            _wdOnButton.SetEnabled(false);
            _wdOffButton.SetEnabled(false);
            
            if (_avatar == null) return;
            
            _wdOnButton.SetEnabled(true);
            _wdOffButton.SetEnabled(true);
            
            UpdateWDList();
        }

        // ReSharper disable once InconsistentNaming
        private void UpdateWDList()
        {
            _statesListContainer.Clear();
            var states = _avatar.AnalyzeWDState();

            bool isMixed = states.HaveMixedWriteDefaults();
            if(isMixed)
                _mixedWdLabel.RemoveFromClassList("hidden");
            else
                _mixedWdLabel.AddToClassList("hidden");
            
            bool hasEmptyAnimations = states.HaveEmpyMotionsInStates();
            if(hasEmptyAnimations)
                _emptyMotions.RemoveFromClassList("hidden");
            else
                _emptyMotions.AddToClassList("hidden");
            
            string oldName = "";
            VisualElement group = null;
            foreach (var state in states)
            {
                if (!state.AvatarLayer.Equals(oldName))
                {
                    oldName = state.AvatarLayer;
                    group = new VisualElement()
                        .WithClass("bordered-container")
                        .ChildOf(_statesListContainer);

                    new Label(state.AvatarLayer)
                        .WithClass("header")
                        .WithMargin(0, 0, 0, 6)
                        .ChildOf(group);

                    var headerRow = new VisualElement()
                        .WithFlexDirection(FlexDirection.Row)
                        .ChildOf(group);

                    new Label("State").WithClass("header-small").WithFlex(7, 0, 1).ChildOf(headerRow);
                    new Label("WD On").WithClass("header-small").WithUnityTextAlign(TextAnchor.UpperCenter).WithFlex(1, 0, 1).ChildOf(headerRow);
                    new Label("Default").WithClass("header-small").WithFlex(1, 0, 1).ChildOf(headerRow);
                }

                var row = new VisualElement()
                    .WithFlexDirection(FlexDirection.Row)
                    .ChildOf(group);

                new Label(state.StateName)
                    .WithAlignSelf(Align.Center)
                    .WithFlex(7, 0, 1)
                    .ChildOf(row);

                FluentUIElements.NewToggle(state.IsOn)
                    .WithEnabledState(false)
                    .WithClass("centered-toggle")
                    .WithAlignSelf(Align.Center)
                    .WithFlex(1, 0, 1)
                    .ChildOf(row);

                new Label(state.HasDefault ? state.IsDefaultOn ? "On" : "Off" : "None")
                    .WithClass($"{(state.HasDefault ? state.IsDefaultOn ? "green" : "red" : "gray")}-text")
                    .WithAlignSelf(Align.Center)
                    .WithFlex(1, 0, 1)
                    .ChildOf(row);
            }
        }
    }
}