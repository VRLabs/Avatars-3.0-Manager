using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UIElements;

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

        public void UpdateUI()
        {
            bool allowMerge = true;
            var layerParameters = _layer.Parameters.Select(x => x.Parameter).ToArray();
            for (var i = 0; i < _controller.parameters.Length; i++)
            {
                var param = _controller.parameters[i];
                if (layerParameters.Any(x => x.nameHash == param.nameHash && x.type != param.type && _parametersToMerge[i].Suffix == ""))
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
            new Label("Merge Animator Mode")
                .WithClass("header")
                .ChildOf(this);
            
            var controller = FluentUIElements
                .NewObjectField("Animator", typeof(AnimatorController))
                .ChildOf(this);
            
            var paramHeader = new VisualElement()
                .WithFlexDirection(FlexDirection.Row)
                .ChildOf(this);
            var labelHeader = new Label("Parameters").WithClass("header-small").ChildOf(paramHeader);
            var sp = new VisualElement().ChildOf(paramHeader);
            sp.style.flexGrow = 1;
            var checkboxHeader = new Label("Suffix").WithClass("header-small").ChildOf(paramHeader);

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
                    new Label("Cannot merge controller onto itself.")
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
                    
                    var warningLabel = new Label("Target controller contains this parameter with a different type than the base controller. These controllers cannot be merged.")
                        .WithClass("red-text")
                        .WithClass("white-space-normal")
                        .ChildOf(itemContainer);

                    _parameterWarningLabels.Add(warningLabel);
                    
                    if (AV3Manager.VrcParameters.Any(x => x == param.name))
                    {
                        new Label("Parameter is a default one, by default it will be added to the parameters, but not listed in the synced parameters, you should not add any affix unless you know what you're doing")
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
                        new Label("Parameter is already used in another layer, you may want to add a suffix")
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
                    .NewButton("Clear all suffixes", "Remove all suffixes, which will use the same parameters for newly merged parameters",
                        () =>
                        {
                            foreach (var suffixField in suffixFields)
                            {
                                suffixField.value = "";
                            }
                        })
                    .WithClass("grow-control")
                    .ChildOf(parametersListContainer);
                
                warningLabel = new Label("Target controller contains a parameter with a different type than the base controller. These controllers cannot be merged.")
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
                .NewButton("Merge on current", "Merge this animator on the current layer animator")
                .WithClass("grow-control")
                .ChildOf(operationsArea);
            mergeOnNew = FluentUIElements
                .NewButton("Merge on new", "Create a copy of the layer animator and merge this animator on that")
                .WithClass("grow-control")
                .ChildOf(operationsArea);
            var cancelButton = FluentUIElements
                .NewButton("Cancel", "Cancel operation")
                .WithClass("grow-control")
                .ChildOf(operationsArea);

            cancelButton.clicked += () => OnClose?.Invoke();

            mergeOnCurrent.clicked += () =>
            {
                var dictionary = _parametersToMerge.ToDictionary(p => p.Name, p => p.Name + p.Suffix);
                layer.SetController(AnimatorCloner.MergeControllers(layer.Layer.animatorController as AnimatorController, controller.value as AnimatorController, dictionary));
                OnClose?.Invoke();
            };
            
            mergeOnNew.clicked += () =>
            {
                var dictionary = _parametersToMerge.ToDictionary(p => p.Name, p => p.Name + p.Suffix);
                layer.SetController(AnimatorCloner.MergeControllers(layer.Layer.animatorController as AnimatorController, controller.value as AnimatorController, dictionary, true));
                OnClose?.Invoke();
            };

        }
    }
}