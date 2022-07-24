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

namespace VRLabs.AV3Manager
{
    // ReSharper disable once UnusedType.Global
    public class ParametersTab : IAV3ManagerTab
    {
        public VisualElement TabContainer { get; set; }
        public string TabName { get; set; }
        public Texture2D TabIcon { get; set; }

        private Label _label;
        private Label _additionalCostLabel;

        private VisualElement _paramsListContainer;
        private ObjectField _expressionParametersField;

        private VRCAvatarDescriptor _avatar;

        public ParametersTab()
        {
            TabContainer = new VisualElement();
            TabName = "Parameters";

            TabIcon = Resources.Load<Texture2D>("AV3M/ParametersTabIcon" +(EditorGUIUtility.isProSkin ? "Dark" : "Light"));
            
            _expressionParametersField = FluentUIElements
                .NewObjectField("Expression Parameters", typeof(VRCExpressionParameters))
                .WithClass("top-spaced")
                .ChildOf(TabContainer);

            FluentUIElements.NewButton("Open asset in inspector", "Selects the asset and shows it in the inspector.",
                    () =>
                    {
                        if (_avatar == null || _avatar.expressionParameters == null) return;
                        Selection.SetActiveObjectWithContext(_avatar.expressionParameters, _avatar.expressionParameters);
                    })
                .ChildOf(TabContainer);

            _label = new Label()
                .WithClass("bordered-container", "margin-normal", "hidden")
                .WithFontSize(10)
                .ChildOf(TabContainer);

            var paramHeader = new VisualElement()
                .WithMargin(5, 0)
                .WithPadding(2, 0)
                .WithFlexDirection(FlexDirection.Row)
                .ChildOf(TabContainer);

            new Label("Name")
                .WithClass("header-small")
                .WithFlex(5, 0, 1)
                .ChildOf(paramHeader);
            new Label("Type")
                .WithClass("header-small")
                .WithFlex(2, 0, 1)
                .ChildOf(paramHeader);
            new Label("Default")
                .WithClass("header-small")
                .WithUnityTextAlign(TextAnchor.UpperCenter)
                .WithFlex(1, 0, 1)
                .ChildOf(paramHeader);
            new Label("Saved")
                .WithClass("header-small")
                .WithFlex(1, 0, 1)
                .WithUnityTextAlign(TextAnchor.UpperCenter)
                .ChildOf(paramHeader);
            new VisualElement().WithWidth(26).ChildOf(paramHeader);

            _paramsListContainer = new VisualElement().ChildOf(TabContainer);
            
            new Label("Copy parameters").WithClass("header-small").WithMargin(5,20,5,0).ChildOf(TabContainer);
            var paramsToCopy = FluentUIElements
                .NewObjectField("Parameters to copy", typeof(VRCExpressionParameters))
                .ChildOf(TabContainer);
            
            _additionalCostLabel = new Label()
                .WithClass("bordered-container", "margin-normal", "hidden")
                .WithFontSize(10)
                .ChildOf(TabContainer);

            var copyParametersButton = FluentUIElements.NewButton("Copy parameters", "Copies parameters into the current asset. The settings of it will be copied over if a parameter is already listed.",
                    () =>
                    {
                        if (_avatar == null || _avatar.expressionParameters == null) return;
                        if (!(paramsToCopy.value is VRCExpressionParameters paramsToCopyAsset)) return;
                        foreach (var parameter in paramsToCopyAsset.parameters)
                        {
                            VRCExpressionParameters.Parameter p = _avatar.expressionParameters.FindParameter(parameter.name);
                            if (p == null)
                            {
                                var newParam = parameter.GetCopy();
                                int count =  _avatar.expressionParameters.parameters.Length;
                                VRCExpressionParameters.Parameter[] parameterArray = new VRCExpressionParameters.Parameter[count + 1];
                                for (int i = 0; i < count; i++)
                                {
                                    parameterArray[i] =  _avatar.expressionParameters.GetParameter(i);
                                }
                                parameterArray[count] = new VRCExpressionParameters.Parameter
                                {
                                    name = parameter.name,
                                    valueType = parameter.valueType,
                                    defaultValue = parameter.defaultValue,
                                    saved = parameter.saved
                                };
                                _avatar.expressionParameters.parameters = parameterArray;
                            }
                            else
                            {
                                p.defaultValue = parameter.defaultValue;
                                p.saved = parameter.saved;   
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

                copyParametersButton.SetEnabled(totalCost <= VRCExpressionParameters.MAX_PARAMETER_COST);
                UpdateAdditionalCostLabel(paramsCost, totalCost);
            });

            _expressionParametersField.RegisterValueChangedCallback(evt =>
            {
                _avatar.expressionParameters = evt.newValue as VRCExpressionParameters;
                UpdateParameters();
            });
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

        private void UpdateParameters()
        {
            _paramsListContainer.Clear();
            var usedParameters = new List<string>();
            HashSet<string> set = new HashSet<string>();
            foreach (VRCAvatarDescriptor.CustomAnimLayer x in _avatar.baseAnimationLayers.Concat(_avatar.specialAnimationLayers))
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

                new Label(parameter.name).WithAlignSelf(Align.Center).WithFlex(5, 0, 1).ChildOf(row);
                new EnumField(parameter.valueType).WithFlex(2, 0, 1).WithEnabledState(false).ChildOf(row);
                switch (parameter.valueType)
                {
                    case ValueType.Int:
                        var intValue = FluentUIElements.NewIntField((int)parameter.defaultValue)
                            .WithFlex(1, 0, 1).ChildOf(row);
                        intValue.RegisterValueChangedCallback(evt => parameter.defaultValue = Mathf.Clamp(evt.newValue, 0, 255));
                        break;
                    case ValueType.Float:
                        var floatValue = FluentUIElements.NewFloatField(parameter.defaultValue)
                            .WithFlex(1, 0, 1).ChildOf(row);
                        floatValue.RegisterValueChangedCallback(evt => parameter.defaultValue = Mathf.Clamp(evt.newValue, -1f, 1f));
                        break;
                    case ValueType.Bool:
                        var boolValue = FluentUIElements.NewToggle(parameter.defaultValue != 0)
                            .WithClass("centered-toggle").WithFlex(1, 0, 1).ChildOf(row);
                        boolValue.RegisterValueChangedCallback(evt => parameter.defaultValue = evt.newValue ? 1f : 0f);
                        break;
                }

                var saved = FluentUIElements.NewToggle(parameter.saved).WithClass("centered-toggle")
                    .WithFlex(1, 0, 1).ChildOf(row);
                saved.RegisterValueChangedCallback(evt => parameter.saved = evt.newValue);
                
                FluentUIElements.NewButton( () =>
                    {
                        List<VRCExpressionParameters.Parameter> list = new List<VRCExpressionParameters.Parameter>();
                        foreach (VRCExpressionParameters.Parameter x in _avatar.expressionParameters.parameters)
                            if (x != parameter) list.Add(x);

                        _avatar.expressionParameters.parameters = list.ToArray();
                        
                        UpdateParameters();
                    })
                    .WithClass("delete-button")
                    .WithFlex(1, 0, 1).ChildOf(row);
                

                if (!usedParameters.Contains(parameter.name))
                    new Label("This parameter is not used in any animator, remove it to save some parameter space.").WithClass("warning-label").ChildOf(paramElement);

                if (AV3Manager.VrcParameters.Contains(parameter.name))
                    new Label("This parameter doesn't need to be in the parameters asset, remove it to save some parameter space.").WithClass("warning-label").ChildOf(paramElement);
            }

            UpdateLabel(_avatar.expressionParameters.CalcTotalCost());
        }

        public void UpdateLabel(int currentCount)
        {
            if (_label == null) return;
            _label.text = $"Parameters memory used: {currentCount}/{VRCExpressionParameters.MAX_PARAMETER_COST}";
            _label.RemoveFromClassList("hidden");
        }
        
        public void UpdateAdditionalCostLabel(int additionalCost, int totalCost)
        {
            if (_additionalCostLabel == null) return;
            _additionalCostLabel.text = $"Additional cost: {additionalCost}";
            _additionalCostLabel.text += $"\nTotal cost after update: {totalCost}/{VRCExpressionParameters.MAX_PARAMETER_COST}";
            if(totalCost > VRCExpressionParameters.MAX_PARAMETER_COST)
                _additionalCostLabel.text += "\nCan't merge, the cost surpasses the maximum.";
            _additionalCostLabel.RemoveFromClassList("hidden");
        }
    }
}