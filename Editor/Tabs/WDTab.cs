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
using static VRLabs.AV3Manager.AV3ManagerLocalization.Keys;
using DreadScripts.Localization;

namespace VRLabs.AV3Manager
{
	// ReSharper disable once InconsistentNaming
	// ReSharper disable once UnusedType.Global
	[TabOrder(0)]
	public class WDTab : IAV3ManagerTab
	{
		public VisualElement TabContainer { get; set; }
		public LocalizationHandler<AV3ManagerLocalization> LocalizationHandler = AV3Manager.LocalizationHandler;
		public string TabName { get; set; }
		public Texture2D TabIcon { get; set; }

		private VRCAvatarDescriptor _avatar;
		private readonly VisualElement _statesListContainer;
		private readonly Label LocalizationHandlerixedWdLabel;
		private readonly Button _wdOnButton;
		private readonly Button _wdOffButton;
		private readonly Label _emptyMotions;
		private readonly Label _dbtWarningLabel;
		private readonly Toggle _dbtToggle;
		private bool hasWDDefaultOn;
		private bool hasWDDefaultOff;


		public WDTab()
		{
			TabContainer = new VisualElement();
			TabName = LocalizationHandler.Get(WD_WD).text;
			TabIcon = Resources.Load<Texture2D>("AV3M/WdTabIcon" + (EditorGUIUtility.isProSkin ? "Dark" : "Light"));
			bool forceWd = EditorPrefs.GetBool("AV3MForceWD", false);
			bool ignoreDbts = EditorPrefs.GetBool("AV3MIgnoreDBTs", true);


			var forceToggle = FluentUIElements.NewToggle(LocalizationHandler.Get(WD_ForceAllWD).text, forceWd)
				.WithMargin(5, 10, 0, 4)
				.ChildOf(TabContainer);

			Label wdWarningLabel = new Label(LocalizationHandler.Get(WD_ForceAllWDWarning).text)
				.WithClass("warning-label", "bordered-container")
				.WithMargin(5, 0)
				.ChildOf(TabContainer);

			if (!forceWd) wdWarningLabel.AddToClassList("hidden");

			forceToggle.RegisterValueChangedCallback(evt =>
			{
				forceWd = evt.newValue;
				EditorPrefs.SetBool("AV3MForceWD", forceWd);

				if (forceWd)
					wdWarningLabel.RemoveFromClassList("hidden");
				else
					wdWarningLabel.AddToClassList("hidden");
			});

			_dbtWarningLabel = new Label(LocalizationHandler.Get(WD_DBTWarning).text)
				.WithClass("warning-label", "bordered-container")
				.WithMargin(5, 0)
				.ChildOf(TabContainer);

			_dbtToggle = FluentUIElements.NewToggle(LocalizationHandler.Get(WD_IgnoreDBT).text, ignoreDbts)
				.WithMargin(5, 10, 0, 4)
				.ChildOf(TabContainer);

			_dbtToggle.RegisterValueChangedCallback(evt =>
			{
				ignoreDbts = evt.newValue;
				EditorPrefs.SetBool("AV3MIgnoreDBTs", ignoreDbts);
			});


			var buttonsContainer = new VisualElement()
				.WithFlexDirection(FlexDirection.Row)
				.ChildOf(TabContainer);

			_wdOnButton = FluentUIElements.NewButton(LocalizationHandler.Get(WD_SetWDOff).text,
					LocalizationHandler.Get(WD_SetWDOff).tooltip,
					() =>
					{
						if (_avatar == null) return;
						if (forceWd && hasWDDefaultOn)
						{
							bool cancel = !EditorUtility.DisplayDialog(LocalizationHandler.Get(WD_ForceWD).text,
								LocalizationHandler.Get(WD_ForceWDOffMessage).text,
								LocalizationHandler.Get(WD_Proceed).text, LocalizationHandler.Get(WD_Cancel).text);
							if (cancel) return;
						}

						AV3ManagerFunctions.SetWriteDefaults(_avatar, false, forceWd, ignoreDbts);
						UpdateWDList();
					})
				.WithClass("grow-control")
				.ChildOf(buttonsContainer);
			_wdOffButton = FluentUIElements.NewButton(LocalizationHandler.Get(WD_SetWDOn).text,
					LocalizationHandler.Get(WD_SetWDOn).tooltip,
					() =>
					{
						if (_avatar == null) return;
						if (forceWd && hasWDDefaultOff)
						{
							bool cancel = !EditorUtility.DisplayDialog(LocalizationHandler.Get(WD_ForceWD).text,
								LocalizationHandler.Get(WD_ForceWDOnMessage).text,
								LocalizationHandler.Get(WD_Proceed).text, LocalizationHandler.Get(WD_Cancel).text);
							if (cancel) return;
						}

						AV3ManagerFunctions.SetWriteDefaults(_avatar, true, forceWd, ignoreDbts);
						UpdateWDList();
					})
				.WithClass("grow-control")
				.ChildOf(buttonsContainer);

			LocalizationHandlerixedWdLabel = new Label(LocalizationHandler.Get(WD_MixedWDWarning).text)
				.WithClass("warning-label", "bordered-container", "hidden")
				.WithMargin(5, 0)
				.ChildOf(TabContainer);

			_emptyMotions = new Label(LocalizationHandler.Get(WD_EmptyStatesWarning).text)
				.WithClass("warning-label", "bordered-container", "hidden")
				.WithMargin(5, 0)
				.ChildOf(TabContainer);

			new Label(LocalizationHandler.Get(WD_List).text)
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
			hasWDDefaultOff = states.HaveWDDefaults(false);
			hasWDDefaultOn = states.HaveWDDefaults(true);
			bool isMixed = states.HaveMixedWriteDefaults();
			if (isMixed)
				LocalizationHandlerixedWdLabel.RemoveFromClassList("hidden");
			else
				LocalizationHandlerixedWdLabel.AddToClassList("hidden");

			bool hasEmptyAnimations = states.HaveEmpyMotionsInStates();
			if (hasEmptyAnimations)
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

					new Label(LocalizationHandler.Get(WD_State).text).WithClass("header-small").WithFlex(2, 0, 1)
						.ChildOf(headerRow);
					new Label(LocalizationHandler.Get(WD_Motion).text).WithClass("header-small")
						.WithUnityTextAlign(TextAnchor.UpperCenter).WithFlex(1, 0, 1).ChildOf(headerRow);
					new Label(LocalizationHandler.Get(WD_WDOn).text).WithClass("header-small")
						.WithUnityTextAlign(TextAnchor.UpperCenter)
						.WithFlex(1, 0, 1).ChildOf(headerRow);
					var tooltipLabel = new Label(LocalizationHandler.Get(WD_Default).text).WithClass("header-small")
						.WithFlex(1, 0, 1)
						.ChildOf(headerRow);
					tooltipLabel.tooltip = LocalizationHandler.Get(WD_Default).tooltip;
					new Label(LocalizationHandler.Get(WD_ViewState).text).WithClass("header-small")
						.WithUnityTextAlign(TextAnchor.UpperCenter).WithFlex(1, 0, 1).ChildOf(headerRow);
				}

				var row = new VisualElement()
					.WithFlexDirection(FlexDirection.Row)
					.ChildOf(group);

				new Label(state.StateName)
					.WithAlignSelf(Align.Center)
					.WithFlex(2, 0, 1)
					.ChildOf(row);

				new Label(state.State.motion == null ? "None" : state.State.motion.name)
					.WithClass($"{(state.State.motion == null ? "yellow" : "white")}-text")
					.WithAlignSelf(Align.Center)
					.WithUnityTextAlign(TextAnchor.UpperCenter)
					.WithClass("text-overflow-elipsis")
					.WithFlex(1, 0, 1)
					.ChildOf(row);

				FluentUIElements.NewToggle(state.IsOn)
					.WithEnabledState(false)
					.WithClass("centered-toggle")
					.WithAlignSelf(Align.Center)
					.WithFlex(1, 0, 1)
					.ChildOf(row);

				new Label(state.HasDefault
						? state.IsDefaultOn ? LocalizationHandler.Get(WD_On).text : LocalizationHandler.Get(WD_Off).text
						: LocalizationHandler.Get(WD_None).text)
					.WithClass($"{(state.HasDefault ? state.IsDefaultOn ? "green" : "red" : "gray")}-text")
					.WithAlignSelf(Align.Center)
					.WithFlex(1, 0, 1)
					.ChildOf(row);

				VisualElement buttonContainer = new VisualElement()
					.WithAlignSelf(Align.Center)
					.WithFlex(1, 0, 1)
					.ChildOf(row);
				Button button = new Button(() => ViewState(state)).ChildOf(buttonContainer);
				Label label = new Label(LocalizationHandler.Get(WD_View).text).ChildOf(button);
			}
		}

		private void ViewState(WDState state)
		{
			if (state.Controller == null || state.Layer == null || state.Layer.stateMachine == null ||
			    state.State == null) return;

			List<Object> FindStateBreadcrumbs(List<Object> currentPath, AnimatorStateMachine stateMachine,
				AnimatorState target)
			{
				foreach (var animatorState in stateMachine.states)
				{
					if (animatorState.state == target)
					{
						return currentPath;
					}
				}

				foreach (var child in stateMachine.stateMachines)
				{
					if (child.stateMachine == null)
					{
						continue;
					}

					currentPath.Add(child.stateMachine);
					List<Object> found = FindStateBreadcrumbs(currentPath, child.stateMachine, target);
					if (found != null)
					{
						return found;
					}

					currentPath.RemoveAt(currentPath.Count - 1);
				}

				return null;
			}

			List<Object> stateBreadCrumbs = FindStateBreadcrumbs(new List<Object> { state.Layer.stateMachine },
				state.Layer.stateMachine, state.State);

			if ((bool)typeof(EditorWindow).GetMethod("HasOpenInstances")
				    .MakeGenericMethod(
					    typeof(Node).Assembly.GetType("UnityEditor.Graphs.AnimatorControllerTool"))
				    .Invoke(null, null))
			{
				BindingFlags BF_ALL = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance |
				                      BindingFlags.Static;
				// Get Editor Window
				var act = EditorWindow.GetWindow(
					typeof(Node).Assembly.GetType("UnityEditor.Graphs.AnimatorControllerTool"), false, "Animator",
					false);

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
				var state_node = state_node_lookup.GetType().GetMethod("get_Item", BF_ALL)
					.Invoke(state_node_lookup, new object[] { state.State });
				gui.selection = new List<Node> { state_node as Node };
				gui.GetType().GetMethod("UpdateUnitySelection", BF_ALL).Invoke(gui, Array.Empty<object>());
			}
		}
	}
}