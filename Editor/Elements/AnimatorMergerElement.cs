using System;
using System.Collections.Generic;
using System.Linq;
using DreadScripts.Localization;
using UnityEditor.Animations;
using UnityEngine.UIElements;
using static VRLabs.AV3Manager.AV3ManagerLocalization.Keys;

namespace VRLabs.AV3Manager
{
    public class AnimatorMergerElement : VisualElement
    {
        private class ParameterToMerge
        {
            public string Name { get; set; }
            public string Suffix { get; set; }

            public ParameterToMerge(string name = "", string suffix = "")
            {
                Name = name;
                Suffix = suffix;
            }
        }

        public Action OnClose { get; set; }

        private List<ParameterToMerge> _parametersToMerge;
        private Button suffixClearButton;
        private Label warningLabel;
        private List<Label> _parameterWarningLabels;
        private VrcAnimationLayer _layer;
        private AnimatorController _controller;
        private Button mergeOnCurrent;
        private Button mergeOnNew;

        private readonly LocalizationHandler<AV3ManagerLocalization> LocalizationHandler = AV3Manager.LocalizationHandler;

        public void UpdateUI()
        {
            bool allowMerge = true;
            var layerParameters = _layer.Parameters.Select(x => x.Parameter).ToArray();
            for (var i = 0; i < _controller.parameters.Length; i++)
            {
                var param = _controller.parameters[i];
                if (layerParameters.Any(x =>
                        x.nameHash == param.nameHash && x.type != param.type && _parametersToMerge[i].Suffix == ""))
                {
                    _parameterWarningLabels[i].RemoveFromClassList("hidden");
                    allowMerge = false;
                }
                else
                {
                    _parameterWarningLabels[i].AddToClassList("hidden");
                }
            }

            if (allowMerge)
            {
                warningLabel.AddToClassList("hidden");
            }
            else
            {
                warningLabel.RemoveFromClassList("hidden");
            }

            mergeOnCurrent.SetEnabled(allowMerge);
            mergeOnNew.SetEnabled(allowMerge);

            if (_parametersToMerge.Any(x => x.Suffix != ""))
            {
                suffixClearButton.RemoveFromClassList("hidden");
            }
            else
            {
                suffixClearButton.AddToClassList("hidden");
            }
        }

        public AnimatorMergerElement(VrcAnimationLayer layer)
        {
            _layer = layer;
            _parametersToMerge = new List<ParameterToMerge>();
            _parameterWarningLabels = new List<Label>();
            new Label(LocalizationHandler.Get(Merger_AnimatorMode).text)
                .WithClass("header")
                .ChildOf(this);

            var controller = FluentUIElements
                .NewObjectField(LocalizationHandler.Get(Merger_Animator).text, typeof(AnimatorController))
                .ChildOf(this);

            var paramHeader = new VisualElement()
                .WithFlexDirection(FlexDirection.Row)
                .ChildOf(this);
            var labelHeader = new Label(LocalizationHandler.Get(Merger_Parameters).text).WithClass("header-small").ChildOf(paramHeader);
            var sp = new VisualElement().ChildOf(paramHeader);
            sp.style.flexGrow = 1;
            var checkboxHeader = new Label(LocalizationHandler.Get(Merger_Suffix).text).WithClass("header-small").ChildOf(paramHeader);

            var parametersListContainer = new VisualElement()
                .ChildOf(this);

            var layerParameters = layer.Parameters.Select(x => x.Parameter).ToArray();
            var allParameters = layer.AvatarParameters.Where(x => !layerParameters.Contains(x)).ToArray();

            mergeOnCurrent = null;
            mergeOnNew = null;

            controller.RegisterValueChangedCallback(evt =>
            {
                var newController = evt.newValue as AnimatorController;

                parametersListContainer.Clear();
                _parametersToMerge.Clear();

                if (newController == layer.Controller)
                {
                    new Label(LocalizationHandler.Get(Merger_SelfMergeWarning).text)
                        .WithClass("red-text")
                        .WithClass("white-space-normal")
                        .ChildOf(parametersListContainer);
                    mergeOnCurrent.SetEnabled(false);
                    mergeOnNew.SetEnabled(false);
                    return;
                }

                if (newController == null) return;

                _controller = newController;

                List<TextField> suffixFields = new List<TextField>();
                foreach (var param in newController.parameters)
                {
                    var itemContainer = new VisualElement()
                        .WithClass("bordered-container").ChildOf(parametersListContainer);

                    var p = new ParameterToMerge(param.name);
                    /*var row = new VisualElement()
                        .WithFlexDirection(FlexDirection.Row)
                        .ChildOf(parametersListContainer);*/

                    var suffixField = new TextField(p.Name).ChildOf(itemContainer);
                    suffixField.tooltip = p.Name;

                    var warningLabel = new Label(LocalizationHandler.Get(Merger_DuplicateParamWarning).text)
                        .WithClass("red-text")
                        .WithClass("white-space-normal")
                        .ChildOf(itemContainer);

                    _parameterWarningLabels.Add(warningLabel);

                    if (AV3Manager.VrcParameters.Any(x => x == param.name))
                    {
                        new Label(LocalizationHandler.Get(Merger_BuiltinParamWarning).text)
                            .WithClass("warning-label")
                            .ChildOf(itemContainer);
                    }
                    else if (layerParameters.Any(x => x.nameHash == param.nameHash))
                    {
                        string fixedName = layer.Controller.MakeUniqueParameterName(param.name);

                        List<char> charNumber = new List<char>();
                        for (int i = fixedName.Length - 1; i >= 0; i--)
                            if (char.IsNumber(fixedName[i]))
                                charNumber.Insert(0, fixedName[i]);

                        if (int.TryParse(string.Concat(charNumber), out int number))
                            p.Suffix = " " + (number);
                        else
                            p.Suffix = " 0";
                    }
                    else if (allParameters.Any(x => x.nameHash == param.nameHash))
                    {
                        new Label(LocalizationHandler.Get(Merger_ParamInDifferentLayerWarning).text)
                            .WithClass("warning-label")
                            .ChildOf(itemContainer);
                    }

                    suffixField.value = p.Suffix;

                    suffixField.RegisterValueChangedCallback(e =>
                    {
                        p.Suffix = e.newValue;
                        UpdateUI();
                    });

                    suffixFields.Add(suffixField);

                    _parametersToMerge.Add(p);
                }


                suffixClearButton = FluentUIElements
                    .NewButton(LocalizationHandler.Get(Merger_ClearSuffixes).text, LocalizationHandler.Get(Merger_ClearSuffixes).tooltip,
                        () =>
                        {
                            foreach (var suffixField in suffixFields)
                            {
                                suffixField.value = "";
                            }
                        })
                    .WithClass("grow-control")
                    .ChildOf(parametersListContainer);

                warningLabel = new Label(LocalizationHandler.Get(Merger_ParamTypeMismatchWarning).text)
                    .WithClass("red-text")
                    .WithClass("white-space-normal")
                    .ChildOf(parametersListContainer);
                UpdateUI();
            });

            var operationsArea = new VisualElement()
                .WithClass("top-spaced")
                .WithFlexDirection(FlexDirection.Row)
                .ChildOf(this);
            mergeOnCurrent = FluentUIElements
                .NewButton(LocalizationHandler.Get(Merger_MergeOnCurrent).text, LocalizationHandler.Get(Merger_MergeOnCurrent).tooltip)
                .WithClass("grow-control")
                .ChildOf(operationsArea);
            mergeOnNew = FluentUIElements
                .NewButton(LocalizationHandler.Get(Merger_MergeOnNew).text, LocalizationHandler.Get(Merger_MergeOnNew).tooltip)
                .WithClass("grow-control")
                .ChildOf(operationsArea);
            var cancelButton = FluentUIElements
                .NewButton(LocalizationHandler.Get(Merger_Cancel).text, LocalizationHandler.Get(Merger_Cancel).tooltip)
                .WithClass("grow-control")
                .ChildOf(operationsArea);

            cancelButton.clicked += () => OnClose?.Invoke();

            mergeOnCurrent.clicked += () =>
            {
                var dictionary = _parametersToMerge.ToDictionary(p => p.Name, p => p.Name + p.Suffix);
                layer.SetController(AnimatorCloner.MergeControllers(
                    layer.Layer.animatorController as AnimatorController, controller.value as AnimatorController,
                    dictionary));
                OnClose?.Invoke();
            };

            mergeOnNew.clicked += () =>
            {
                var dictionary = _parametersToMerge.ToDictionary(p => p.Name, p => p.Name + p.Suffix);
                layer.SetController(AnimatorCloner.MergeControllers(
                    layer.Layer.animatorController as AnimatorController, controller.value as AnimatorController,
                    dictionary, true));
                OnClose?.Invoke();
            };
        }
    }
}