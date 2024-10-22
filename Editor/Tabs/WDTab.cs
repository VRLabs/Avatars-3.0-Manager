using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

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
        private readonly Label _dbtWarningLabel;
        private readonly Toggle _dbtToggle;

        public WDTab()
        {
            TabContainer = new VisualElement();
            TabName = "Write Defaults";
            TabIcon = Resources.Load<Texture2D>("AV3M/WdTabIcon" +(EditorGUIUtility.isProSkin ? "Dark" : "Light"));
            bool forceWd = EditorPrefs.GetBool("AV3MForceWD", false);
            bool ignoreDbts = EditorPrefs.GetBool("AV3MIgnoreDBTs", true);


            var forceToggle = FluentUIElements.NewToggle("Force all WD", forceWd)
                .WithMargin(5, 10, 0, 4)
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
            
            _dbtWarningLabel = new Label("Direct Blend Tree without explicit WD setting found. These should probably be kept in the same state as they are now.")
                .WithClass("warning-label", "bordered-container")
                .WithMargin(5, 0)
                .ChildOf(TabContainer);
            
            _dbtToggle = FluentUIElements.NewToggle("Ignore Direct Blend Trees", ignoreDbts)
                .WithMargin(5, 10, 0, 4)
                .ChildOf(TabContainer);

            _dbtToggle.RegisterValueChangedCallback(evt =>
            {
                ignoreDbts = evt.newValue;
                EditorPrefs.SetBool("AV3MForceDBT", ignoreDbts);
            });

            
            var buttonsContainer = new VisualElement()
                .WithFlexDirection(FlexDirection.Row)
                .ChildOf(TabContainer);
            
            _wdOnButton = FluentUIElements.NewButton("Set WD Off", "Set Write Defaults to off.",
                    () =>
                    {
                        if (_avatar == null) return;
                        AV3ManagerFunctions.SetWriteDefaults(_avatar, false, forceWd, ignoreDbts);
                        UpdateWDList();
                    })
                .WithClass("grow-control")
                .ChildOf(buttonsContainer);
            _wdOffButton = FluentUIElements.NewButton("Set WD On", "Set Write Defaults to on.",
                    () =>
                    {
                        if (_avatar == null) return;
                        AV3ManagerFunctions.SetWriteDefaults(_avatar,  true, forceWd, ignoreDbts);
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
                .WithMargin(5, 0)
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
            
            bool hasUnspecifiedDirectBlendTrees = states.HaveUnspecifiedDirectBlendTrees();
            if (hasUnspecifiedDirectBlendTrees)
            {
                _dbtWarningLabel.RemoveFromClassList("hidden");
                _dbtToggle.RemoveFromClassList("hidden");
            }
            else
            {
                _dbtWarningLabel.AddToClassList("hidden");
                _dbtToggle.AddToClassList("hidden");
            }
            
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

                    new Label("State").WithClass("header-small").WithFlex(6, 0, 1).ChildOf(headerRow);
                    new Label("Motion").WithClass("header-small").WithUnityTextAlign(TextAnchor.UpperCenter).WithFlex(1, 0, 1).ChildOf(headerRow);
                    new Label("WD On").WithClass("header-small").WithUnityTextAlign(TextAnchor.UpperCenter).WithFlex(1, 0, 1).ChildOf(headerRow);
                    new Label("Default").WithClass("header-small").WithFlex(1, 0, 1).ChildOf(headerRow);
                    new Label("View State").WithClass("header-small").WithUnityTextAlign(TextAnchor.UpperCenter).WithFlex(1, 0, 1).ChildOf(headerRow);
                }

                var row = new VisualElement()
                    .WithFlexDirection(FlexDirection.Row)
                    .ChildOf(group);

                new Label(state.StateName)
                    .WithAlignSelf(Align.Center)
                    .WithFlex(6, 0, 1)
                    .ChildOf(row);

                new Label(state.State.motion == null ? "None" : state.State.motion.name)
                    .WithClass($"{(state.State.motion == null  ? "yellow" : "white")}-text")
                    .WithAlignSelf(Align.Center)
                    .WithUnityTextAlign(TextAnchor.UpperCenter)
                    .WithFlex(1, 0, 1)
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

                VisualElement buttonContainer = new VisualElement()
                    .WithAlignSelf(Align.Center)
                    .WithFlex(1, 0, 1)
                    .ChildOf(row);
                Button button = new Button(() => ViewState(state)).ChildOf(buttonContainer);  
                Label label = new Label("View").ChildOf(button);
            }
        }

        private void ViewState(WDState state)
        {
            List<Object> FindStateBreadcrumbs(List<Object> currentPath, AnimatorStateMachine stateMachine, AnimatorState target) {
                foreach (var state in stateMachine.states) {
                    if (state.state == target)
                    {
                        return currentPath;
                    }
                }
                foreach (var child in stateMachine.stateMachines) {
                    if (child.stateMachine == null) {
                        continue;
                    }
                    currentPath.Add(child.stateMachine);
                    List<Object> found = FindStateBreadcrumbs(currentPath, child.stateMachine, target);
                    if(found != null)
                    {
                        return found;
                    }
                    currentPath.RemoveAt(currentPath.Count - 1);
                }

                return null;
            }

            List<Object> stateBreadCrumbs = FindStateBreadcrumbs(new List<Object>{state.Layer.stateMachine}, state.Layer.stateMachine, state.State);
            
            if ((bool)typeof(EditorWindow).GetMethod("HasOpenInstances")
                    .MakeGenericMethod(
                        typeof(Node).Assembly.GetType("UnityEditor.Graphs.AnimatorControllerTool"))
                    .Invoke(null, null))
            {
                BindingFlags BF_ALL = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance |
                                      BindingFlags.Static;
                // Get Editor Window
                var act = EditorWindow.GetWindow( typeof(Node).Assembly.GetType("UnityEditor.Graphs.AnimatorControllerTool"), false, "Animator", false);
                
                // Set Controller as current viewed controller
                act.GetType().GetProperty("animatorController", BF_ALL).SetValue(act, state.Controller);

                
                GraphGUI gui = act.GetType().GetProperty("activeGraphGUI", BF_ALL).GetValue(act) as GraphGUI;
                
                // Set active breadcrumbs and Repaint
                var crumbs = act.GetType().GetField("m_BreadCrumbs", BF_ALL).GetValue(act);
                crumbs.GetType().GetMethod("Clear", BF_ALL).Invoke(crumbs, null);
                var add_breadcrumb = act.GetType().GetMethod("AddBreadCrumb");
                for (var i = 0; i < stateBreadCrumbs.Count - 1; ++i)
                    add_breadcrumb.Invoke(act, new object[] { stateBreadCrumbs[i], false });
                add_breadcrumb.Invoke(act, new object[] { stateBreadCrumbs.Last(), true });
                act.GetType().GetMethod("Repaint").Invoke(act, null);
                
                // Set the required state node as selected
                var state_node_lookup = gui.graph.GetType().GetField("m_StateNodeLookup", BF_ALL).GetValue(gui.graph);
                var state_node = state_node_lookup.GetType().GetMethod("get_Item", BF_ALL).Invoke(state_node_lookup, new object[] { state.State });
                gui.selection = new List<Node> { state_node as Node };
                gui.GetType().GetMethod("UpdateUnitySelection", BF_ALL).Invoke(gui, Array.Empty<object>());
            }
            
        }
    }
}