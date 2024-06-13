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

        public AnimatorMergerElement(VrcAnimationLayer layer)
        {
            _parametersToMerge = new List<ParameterToMerge>();
            
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

            Button mergeOnCurrent = null;
            Button mergeOnNew = null;
            
            controller.RegisterValueChangedCallback(evt =>
            {
                var newController = evt.newValue as AnimatorController;
                bool allowMerge = true;
                
                parametersListContainer.Clear();
                _parametersToMerge.Clear();


                if (newController == layer.Controller)
                {
                    new Label("Cannot merge controller onto itself.")
                        .WithClass("red-text")
                        .ChildOf(parametersListContainer);
                    mergeOnCurrent.SetEnabled(false);
                    mergeOnNew.SetEnabled(false);
                    return;
                }
                
                if (newController == null) return;

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
                    
                    if (allParameters.Any(x => x.nameHash == param.nameHash && x.type != param.type))
                    {
                        allowMerge = false;
                        new Label("Target controller contains this parameter with a different type than the base controller. These controllers cannot be merged.")
                            .WithClass("red-text")
                            .ChildOf(itemContainer);
                    } 
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
                    });

                    _parametersToMerge.Add(p);
                }

                if (!allowMerge)
                {
                    new Label("Target controller contains a parameter with a different type than the base controller. These controllers cannot be merged.")
                        .WithClass("red-text")
                        .ChildOf(parametersListContainer);
                }
                
                mergeOnCurrent.SetEnabled(allowMerge);
                mergeOnNew.SetEnabled(allowMerge);
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