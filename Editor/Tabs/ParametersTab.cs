using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using ValueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType;
using Parameter = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.Parameter;
using static VRLabs.AV3Manager.AV3ManagerLocalization.Keys;
using DreadScripts.Localization;

namespace VRLabs.AV3Manager
{
	// ReSharper disable once UnusedType.Global
	[TabOrder(0)]
	public class ParametersTab : IAV3ManagerTab
	{
		public VisualElement TabContainer { get; set; }
		public LocalizationHandler<AV3ManagerLocalization> LocalizationHandler = AV3Manager.LocalizationHandler;
		public string TabName { get; set; }
		public Texture2D TabIcon { get; set; }

		private Label _additionalCostLabel;

		private VisualElement _paramsListContainer;
		private ObjectField _expressionParametersField;

		private VRCAvatarDescriptor _avatar;
		private VRCExpressionParameters _paramsToCopyAsset;

		// Keeps memory of the open parameter foldouts
		private readonly HashSet<string> _expandedParameters = new HashSet<string>();

		public ParametersTab()
		{
			TabContainer = new VisualElement();
			TabName = LocalizationHandler.Get(Params_Params).text;

			TabIcon = Resources.Load<Texture2D>("AV3M/ParametersTabIcon" +
			                                    (EditorGUIUtility.isProSkin ? "Dark" : "Light"));

			_expressionParametersField = FluentUIElements
				.NewObjectField(LocalizationHandler.Get(Params_ExprParams).text, typeof(VRCExpressionParameters))
				.WithClass("top-spaced")
				.ChildOf(TabContainer);

			FluentUIElements.NewButton(LocalizationHandler.Get(Params_OpenInInspector).text,
					LocalizationHandler.Get(Params_OpenInInspector).tooltip,
					() =>
					{
						if (_avatar == null || _avatar.expressionParameters == null) return;
						Selection.SetActiveObjectWithContext(_avatar.expressionParameters,
							_avatar.expressionParameters);
					})
				.ChildOf(TabContainer);

			var paramHeader = new VisualElement()
				.WithMargin(5, 0)
				.WithPadding(2, 0)
				.WithFlexDirection(FlexDirection.Row)
				.ChildOf(TabContainer);

			new Label(LocalizationHandler.Get(Params_Name).text)
				.WithClass("header-small")
				.WithFlex(2.5f, 0, 1)
				.ChildOf(paramHeader);
			new Label(LocalizationHandler.Get(Params_Type).text)
				.WithClass("header-small")
				.WithFlex(1, 0, 1)
				.ChildOf(paramHeader);
			new Label(LocalizationHandler.Get(Params_Default).text)
				.WithClass("header-small")
				.WithUnityTextAlign(TextAnchor.UpperCenter)
				.WithFlex(1, 0, 1)
				.ChildOf(paramHeader);
			new Label(LocalizationHandler.Get(Params_Saved).text)
				.WithClass("header-small")
				.WithFlex(1, 0, 1)
				.WithUnityTextAlign(TextAnchor.UpperCenter)
				.ChildOf(paramHeader);
			new Label(LocalizationHandler.Get(Params_Synced).text)
				.WithClass("header-small")
				.WithFlex(1.5f, 0, 1)
				.WithUnityTextAlign(TextAnchor.UpperCenter)
				.ChildOf(paramHeader);
			new VisualElement().WithWidth(26).ChildOf(paramHeader);

			_paramsListContainer = new VisualElement().ChildOf(TabContainer);

			new Label(LocalizationHandler.Get(Params_CopyParams).text).WithClass("header-small").WithMargin(5, 20, 5, 0)
				.ChildOf(TabContainer);
			var paramsToCopy = FluentUIElements
				.NewObjectField(LocalizationHandler.Get(Params_ParamsToCopy).text, typeof(VRCExpressionParameters))
				.ChildOf(TabContainer);

			_additionalCostLabel = new Label()
				.WithClass("bordered-container", "margin-normal", "hidden")
				.WithFontSize(10)
				.ChildOf(TabContainer);

			var copyParametersButton = FluentUIElements.NewButton(LocalizationHandler.Get(Params_CopyParamsButton).text,
					LocalizationHandler.Get(Params_CopyParamsButton).tooltip,
					() =>
					{
						if (_avatar == null || _avatar.expressionParameters == null) return;
						if (!(paramsToCopy.value is VRCExpressionParameters paramsToCopyAsset)) return;
						Undo.RecordObject(_avatar.expressionParameters, "Copy Expression Parameters");
						foreach (var parameter in paramsToCopyAsset.parameters)
						{
							Parameter p = _avatar.expressionParameters.FindParameter(parameter.name);
							if (p == null)
							{
								int count = _avatar.expressionParameters.parameters.Length;
								Parameter[] parameterArray = new Parameter[count + 1];
								for (int i = 0; i < count; i++)
								{
									parameterArray[i] = _avatar.expressionParameters.GetParameter(i);
								}

								parameterArray[count] = parameter.GetCopy();
								_avatar.expressionParameters.parameters = parameterArray;
							}
							else
							{
								p.defaultValue = parameter.defaultValue;
								p.saved = parameter.saved;
								p.networkSynced = parameter.networkSynced;
								p.CopyAdvancedSettingsFrom(parameter);
							}
						}

						EditorUtility.SetDirty(_avatar.expressionParameters);
						UpdateParameters();
					})
				.ChildOf(TabContainer);

			paramsToCopy.RegisterValueChangedCallback(evt =>
			{
				if (!(paramsToCopy.value is VRCExpressionParameters paramsToCopyAsset))
				{
					copyParametersButton.SetEnabled(false);
					_additionalCostLabel.AddToClassList("hidden");
					return;
				}

				_paramsToCopyAsset = paramsToCopyAsset;
				int paramsCost = 0;
				int totalCost = 0;
				if (_avatar.expressionParameters != null)
				{
					paramsCost = paramsToCopyAsset.parameters.GetCost(_avatar.expressionParameters.parameters);
					totalCost = paramsCost + _avatar.expressionParameters.CalcTotalCost();
				}
				else
				{
					paramsCost = paramsToCopyAsset.CalcTotalCost();
					totalCost = paramsCost;
				}

				copyParametersButton.SetEnabled(SettingsTab.ignoreMaxParameterLimit || totalCost <= VRCExpressionParameters.MAX_PARAMETER_COST);
				UpdateAdditionalCostLabel(paramsToCopyAsset);
			});

			_expressionParametersField.RegisterValueChangedCallback(evt =>
			{
				_avatar.expressionParameters = evt.newValue as VRCExpressionParameters;
				UpdateParameters();
			});

			Undo.undoRedoPerformed += () =>
			{
				// Skip instances whose window has been closed.
				if (TabContainer.panel == null) return;
				if (_avatar == null || _avatar.expressionParameters == null) return;
				UpdateParameters();
			};
		}

		public void UpdateTab(VRCAvatarDescriptor avatar)
		{
			_avatar = avatar;
			_paramsListContainer.Clear();
			_expressionParametersField.SetValueWithoutNotify(null);
			_expressionParametersField.SetEnabled(false);

			if (_avatar == null) return;

			_expressionParametersField.SetValueWithoutNotify(_avatar.expressionParameters);
			_expressionParametersField.SetEnabled(true);

			if (_avatar.expressionParameters == null) return;

			UpdateParameters();
		}

		/// <summary>
		/// Records an undo step, applies the edit to the avatar's expression parameters and marks the asset dirty.
		/// </summary>
		private void EditParameters(string undoName, Action edit)
		{
			var asset = _avatar.expressionParameters;
			Undo.RecordObject(asset, undoName);
			edit();
			EditorUtility.SetDirty(asset);
		}

		private void UpdateParameters()
		{
			_paramsListContainer.Clear();
			var usedParameters = new List<string>();
			HashSet<string> set = new HashSet<string>();
			foreach (VRCAvatarDescriptor.CustomAnimLayer x in _avatar.baseAnimationLayers.Concat(
				         _avatar.specialAnimationLayers))
			{
				var controller = x.animatorController as AnimatorController;
				if (controller == null) continue;
				foreach (AnimatorControllerParameter parameter in controller.parameters)
				{
					string s = parameter.name;
					if (set.Add(s)) usedParameters.Add(s);
				}
			}

			foreach (var parameter in _avatar.expressionParameters.parameters)
			{
				var paramElement = new VisualElement()
					.WithClass("margin-normal")
					.WithClass("bordered-container")
					.ChildOf(_paramsListContainer);

				var row = new VisualElement()
					.WithFlexDirection(FlexDirection.Row)
					.ChildOf(paramElement);

				new Label(parameter.name).WithAlignSelf(Align.Center).WithFlex(2.5f, 0, 1).ChildOf(row);
				new EnumField(parameter.valueType).WithFlex(1, 0, 1).WithEnabledState(false).ChildOf(row);

				// Re-clamps the default value to the parameter's current range and shows it, used after a range change.
				Action refreshDefault = null;
				switch (parameter.valueType)
				{
					case ValueType.Int:
						var intValue = FluentUIElements.NewIntField((int)parameter.defaultValue)
							.WithFlex(1, 0, 1).ChildOf(row);
						intValue.RegisterValueChangedCallback(evt =>
						{
							var (min, max) = GetIntRange(parameter);
							int clamped = Mathf.Clamp(evt.newValue, min, max);
							EditParameters("Edit Parameter Default", () => parameter.defaultValue = clamped);
							if (clamped != evt.newValue) intValue.SetValueWithoutNotify(clamped);
						});
						refreshDefault = () =>
						{
							var (min, max) = GetIntRange(parameter);
							parameter.defaultValue = Mathf.Clamp((int)parameter.defaultValue, min, max);
							intValue.SetValueWithoutNotify((int)parameter.defaultValue);
						};
						break;
					case ValueType.Float:
						var floatValue = FluentUIElements.NewFloatField(parameter.defaultValue)
							.WithFlex(1, 0, 1).ChildOf(row);
						floatValue.RegisterValueChangedCallback(evt =>
						{
							var (min, max) = GetFloatRange(parameter);
							float clamped = Mathf.Clamp(evt.newValue, min, max);
							EditParameters("Edit Parameter Default", () => parameter.defaultValue = clamped);
							if (!Mathf.Approximately(clamped, evt.newValue)) floatValue.SetValueWithoutNotify(clamped);
						});
						refreshDefault = () =>
						{
							var (min, max) = GetFloatRange(parameter);
							parameter.defaultValue = Mathf.Clamp(parameter.defaultValue, min, max);
							floatValue.SetValueWithoutNotify(parameter.defaultValue);
						};
						break;
					case ValueType.Bool:
						var boolValue = FluentUIElements.NewToggle(parameter.defaultValue != 0)
							.WithClass("centered-toggle").WithFlex(1, 0, 1).ChildOf(row);
						boolValue.RegisterValueChangedCallback(evt =>
							EditParameters("Edit Parameter Default", () => parameter.defaultValue = evt.newValue ? 1f : 0f));
						break;
				}

				var saved = FluentUIElements.NewToggle(parameter.saved).WithClass("centered-toggle")
					.WithFlex(1, 0, 1).ChildOf(row);
				saved.RegisterValueChangedCallback(evt =>
					EditParameters("Edit Parameter Saved", () => parameter.saved = evt.newValue));

				var networkSynced = FluentUIElements.NewToggle(parameter.networkSynced).WithClass("centered-toggle")
					.WithFlex(1.5f, 0, 1).ChildOf(row);
				networkSynced.RegisterValueChangedCallback(evt =>
					EditParameters("Edit Parameter Synced", () => parameter.networkSynced = evt.newValue));

				FluentUIElements.NewButton(() =>
					{
						EditParameters("Delete Expression Parameter", () =>
							_avatar.expressionParameters.parameters = _avatar.expressionParameters.parameters
								.Where(x => x != parameter).ToArray());
						UpdateParameters();
					})
					.WithClass("delete-button")
					.WithFlex(1, 0, 1).ChildOf(row);

				// Bools are always 1 bit, unsigned and never lerp, so they have nothing to configure.
				if (parameter.valueType != ValueType.Bool)
					CreateAdvancedSettings(parameter, refreshDefault).ChildOf(paramElement);

				if (!usedParameters.Contains(parameter.name))
					new Label(LocalizationHandler.Get(Params_UnusedExprParamWarning).text).WithClass("warning-label")
						.ChildOf(paramElement);

				if (AV3Manager.VrcParameters.Contains(parameter.name))
					new Label(LocalizationHandler.Get(Params_BuiltinInExprParamWarning).text).WithClass("warning-label")
						.ChildOf(paramElement);
			}

			UpdateAdditionalCostLabel(_paramsToCopyAsset);
		}

		/// <summary>
		/// Foldout with the bit width, representation and lerp duration of an int or float parameter
		/// </summary>
		private Foldout CreateAdvancedSettings(Parameter parameter, Action refreshDefault)
		{
			var foldout = new Foldout { value = _expandedParameters.Contains(parameter.name) };
			foldout.RegisterValueChangedCallback(evt =>
			{
				if (evt.target != foldout) return;
				if (evt.newValue) _expandedParameters.Add(parameter.name);
				else _expandedParameters.Remove(parameter.name);
			});

			var rangeLabel = new Label()
				.WithUnityTextAlign(TextAnchor.MiddleRight)
				.WithMargin(3, 1, 3, 1);

			// Small square on the foldout's arrow to show settings different from the default state.
			var changedIndicator = new VisualElement { pickingMode = PickingMode.Ignore }
				.WithClass("changed-indicator")
				.ChildOf(foldout.Q(className: Toggle.checkmarkUssClassName));

			void UpdateSummary()
			{
				changedIndicator.EnableInClassList("hidden", !HasChangedAdvancedSettings(parameter));
				foldout.text = GetAdvancedSettingsSummary(parameter);
				rangeLabel.text = GetRangeText(parameter);
			}

			var bits = new SliderInt(LocalizationHandler.Get(Params_BitWidth).text,
					Parameter.MinBitWidth, Parameter.MaxBitWidth)
				{ showInputField = true, value = parameter.GetBitWidthByType() }
				.ChildOf(foldout);
			bits.tooltip = LocalizationHandler.Get(Params_BitWidth).tooltip;
			bits.RegisterValueChangedCallback(evt =>
			{
				if (evt.target != bits) return;
				int width = Mathf.Clamp(evt.newValue, Parameter.MinBitWidth, Parameter.MaxBitWidth);
				EditParameters("Edit Parameter Bit Width", () =>
				{
					parameter.bitWidth = (byte)width;
					refreshDefault?.Invoke();
				});
				if (width != evt.newValue) bits.SetValueWithoutNotify(width);
				UpdateSummary();
			});

			string unsignedText = LocalizationHandler.Get(Params_Unsigned).text;
			string signedText = LocalizationHandler.Get(Params_Signed).text;
			var representation = new DropdownField(LocalizationHandler.Get(Params_Representation).text,
					new List<string> { unsignedText, signedText },
					IsSigned(parameter) ? 1 : 0)
				.ChildOf(foldout);
			representation.tooltip = LocalizationHandler.Get(Params_Representation).tooltip;
			representation.RegisterValueChangedCallback(evt =>
			{
				if (evt.target != representation) return;
				bool signed = representation.index == 1;
				EditParameters("Edit Parameter Representation", () =>
				{
					// Floats are signed by default and ints unsigned, the flag inverts that default.
					parameter.flippedRepresentation = parameter.valueType == ValueType.Float ? !signed : signed;
					refreshDefault?.Invoke();
				});
				UpdateSummary();
			});

			var lerpRow = new VisualElement().WithFlexDirection(FlexDirection.Row).ChildOf(foldout);
			var lerpType = new DropdownField(LocalizationHandler.Get(Params_LerpDuration).text,
					new List<string>
					{
						LocalizationHandler.Get(Params_LerpDefault).text,
						LocalizationHandler.Get(Params_LerpCustom).text
					},
					parameter.useCustomLerpDuration ? 1 : 0)
				.WithFlexGrow(1)
				.ChildOf(lerpRow);
			lerpType.tooltip = LocalizationHandler.Get(Params_LerpDuration).tooltip;
			var lerpSeconds = FluentUIElements.NewFloatField(parameter.customLerpDuration)
				.WithWidth(60)
				.ChildOf(lerpRow);
			lerpSeconds.tooltip = LocalizationHandler.Get(Params_LerpSeconds).text;
			lerpSeconds.EnableInClassList("hidden", !parameter.useCustomLerpDuration);

			lerpType.RegisterValueChangedCallback(evt =>
			{
				if (evt.target != lerpType) return;
				bool custom = lerpType.index == 1;
				EditParameters("Edit Parameter Lerp Duration", () => parameter.useCustomLerpDuration = custom);
				lerpSeconds.EnableInClassList("hidden", !custom);
				UpdateSummary();
			});
			lerpSeconds.RegisterValueChangedCallback(evt =>
			{
				if (evt.target != lerpSeconds) return;
				float seconds = Mathf.Clamp(evt.newValue, Parameter.MinLerpDuration, Parameter.MaxLerpDuration);
				EditParameters("Edit Parameter Lerp Duration", () => parameter.customLerpDuration = seconds);
				if (!Mathf.Approximately(seconds, evt.newValue)) lerpSeconds.SetValueWithoutNotify(seconds);
				UpdateSummary();
			});

			rangeLabel.ChildOf(foldout);
			UpdateSummary();

			return foldout;
		}

		private static bool HasChangedAdvancedSettings(Parameter parameter) =>
			parameter.GetBitWidthByType() != AV3ManagerFunctions.GetDefaultBitWidth(parameter.valueType) ||
			parameter.flippedRepresentation || parameter.useCustomLerpDuration;

		private string GetAdvancedSettingsSummary(Parameter parameter)
		{
			string representation = LocalizationHandler.Get(IsSigned(parameter) ? Params_Signed : Params_Unsigned).text;
			float? lerp = parameter.GetCustomLerpDuration();
			string lerpText = lerp.HasValue ? $"{lerp.Value:0.###}s" : LocalizationHandler.Get(Params_LerpDefault).text;
			return $"{LocalizationHandler.Get(Params_BitWidth).text}: {parameter.GetBitWidthByType()} · " +
			       $"{representation} · {LocalizationHandler.Get(Params_LerpDuration).text}: {lerpText}";
		}

		/// <summary>
		/// Values the parameter can hold with its current settings
		/// </summary>
		private string GetRangeText(Parameter parameter)
		{
			string range = LocalizationHandler.Get(Params_Range).text;
			if (parameter.valueType == ValueType.Int)
			{
				var (min, max) = GetIntRange(parameter);
				return $"{range}: [{min},{max}]";
			}

			var (minFloat, maxFloat) = GetFloatRange(parameter);
			float step = (maxFloat - minFloat) / ((1 << parameter.GetBitWidthByType()) - 2);
			return $"{range}: [{minFloat},{maxFloat}]  {LocalizationHandler.Get(Params_Precision).text}: {step:G4}";
		}

		private static bool IsSigned(Parameter parameter) =>
			parameter.GetRepresentationByType() == Parameter.ValueRepresentation.Signed;

		/// <summary>
		/// Range an int parameter can hold with its bit width and representation.
		/// </summary>
		private static (int min, int max) GetIntRange(Parameter parameter)
		{
			int width = parameter.GetBitWidthByType();
			return IsSigned(parameter)
				? (-(1 << (width - 1)), (1 << (width - 1)) - 1)
				: (0, (1 << width) - 1);
		}

		/// <summary>
		/// Range a float parameter can hold with its representation.
		/// </summary>
		private static (float min, float max) GetFloatRange(Parameter parameter) =>
			IsSigned(parameter) ? (-1f, 1f) : (0f, 1f);

		public void UpdateAdditionalCostLabel(VRCExpressionParameters paramsToCopyAsset)
		{
			if (_additionalCostLabel == null) return;

			int additionalCost = 0;
			int totalCost = 0;
			if (_avatar.expressionParameters != null)
			{
				additionalCost = paramsToCopyAsset == null
					? 0
					: paramsToCopyAsset.parameters.GetCost(_avatar.expressionParameters.parameters);
				totalCost = additionalCost + _avatar.expressionParameters.CalcTotalCost();
			}
			else
			{
				additionalCost = paramsToCopyAsset == null ? 0 : paramsToCopyAsset.CalcTotalCost();
				totalCost = additionalCost;
			}

			_additionalCostLabel.text =
				$"{LocalizationHandler.Get(Params_AdditionalCostOfAnimatorToMerge).text}: {additionalCost}";
			_additionalCostLabel.text +=
				$"\n{LocalizationHandler.Get(Params_TotalParamMemoryAfterMerge).text}: {totalCost}/{VRCExpressionParameters.MAX_PARAMETER_COST}";
			if (totalCost > VRCExpressionParameters.MAX_PARAMETER_COST)
				_additionalCostLabel.text +=
					$"\n{LocalizationHandler.Get(Params_MergeWouldSurpassParamMemoryWarning).text}";
			_additionalCostLabel.RemoveFromClassList("hidden");
		}
	}
}