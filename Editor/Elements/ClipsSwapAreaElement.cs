using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace VRLabs.AV3Manager
{
    public class ClipsSwapAreaElement : VisualElement
    {
        public Action OnClose { get; set; }

        private List<ClipSwapItem> _animationsToSwap;


        public ClipsSwapAreaElement(VrcAnimationLayer layer)
        {
            new Label("Animations Swap Mode")
                .WithClass("header")
                .ChildOf(this);
            
            new Label("Do NOT modify the animator while you're swapping animations with the manager to avoid issues.")
                .WithClass("warning-label", "bordered-container")
                .WithFontSize(10)
                .ChildOf(this);

            _animationsToSwap = new List<ClipSwapItem>();
            
            if (layer.Controller != null)
            {
                foreach (var animatorLayer in layer.Controller.layers)
                {
                    var clips = animatorLayer.GetClipsToSwap().ToList();
                    
                    _animationsToSwap.AddRange(clips);

                    var container = new VisualElement()
                        .WithClass("bordered-container") 
                        .ChildOf(this);

                    new Label(animatorLayer.name)
                        .WithClass("header-small")
                        .ChildOf(container);
                    
                    var clipsContainer = new VisualElement()
                        .WithClass("clips-container")
                        .ChildOf(container);
                    foreach (ClipSwapItem clip in clips)
                    {
                        if (clip.IsTree)
                        {
                            void MakeTree(ClipSwapItem clipSwapItem, VisualElement parentObject)
                            {
                                var foldout = new Foldout().ChildOf(parentObject);
                                foldout.text = clipSwapItem.Name;
                                
                                foreach (var child in clipSwapItem.TreeChildren)
                                {
                                    if (child.IsTree)
                                    {
                                        MakeTree(child, foldout);
                                    }
                                    else
                                    {
                                        ObjectField item = FluentUIElements.NewObjectField(child.Name, typeof(AnimationClip), child.Clip)
                                            .ChildOf(foldout);
                                
                                        item.RegisterValueChangedCallback(x =>
                                        {
                                            child.Clip = x.newValue as AnimationClip;
                                        });
                                    }
                                }
                            }
                            MakeTree(clip, clipsContainer);
                        }
                        else
                        {
                            ObjectField item = FluentUIElements.NewObjectField(clip.Name, typeof(AnimationClip), clip.Clip)
                                .ChildOf(clipsContainer);
                                
                            item.RegisterValueChangedCallback(x =>
                            {
                                clip.Clip = x.newValue as AnimationClip;
                            });
                        }
                    }
                }
            }

            var operationsArea = new VisualElement()
                .WithClass("top-spaced")
                .WithFlexDirection(FlexDirection.Row)
                .ChildOf(this);
            var mergeOnCurrent = FluentUIElements
                .NewButton("Apply on current", "Apply the changes on this controller")
                .WithClass("grow-control")
                .ChildOf(operationsArea);
            var mergeOnNew = FluentUIElements
                .NewButton("Apply on new", "Apply the changes on a new copy of the controller and applies it to the avatar")
                .WithClass("grow-control")
                .ChildOf(operationsArea);
            var cancelButton = FluentUIElements
                .NewButton("Cancel", "Cancel operation")
                .WithClass("grow-control")
                .ChildOf(operationsArea);

            cancelButton.clicked += () => OnClose?.Invoke();

            mergeOnCurrent.clicked += () =>
            {
                layer.SetController(layer.Controller.SwapAnimations(_animationsToSwap));
                OnClose?.Invoke();
            };
            
            mergeOnNew.clicked += () =>
            {
                layer.SetController(layer.Controller.SwapAnimations(_animationsToSwap, true));
                OnClose?.Invoke();
            };

        }
    }
}