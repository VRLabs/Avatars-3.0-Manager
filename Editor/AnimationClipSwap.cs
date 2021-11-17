using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace VRLabs.AV3Manager
{
    public class AnimationClipSwap
    {
        public string Layer { get; set; }
        public AnimatorState State { get; set; }
        public Motion Motion { get; set; }

        private bool _foldout;
        
        public AnimationClipSwap[] TreeMotions { get; set; }
        public AnimationClipSwap(string layer, AnimatorState state, Motion motion)
        {
            Layer = layer;
            State = state;
            Motion = motion;
            if (!(Motion is BlendTree tree)) return;
            
            TreeMotions = new AnimationClipSwap[tree.children.Length];
            for (int i = 0; i < TreeMotions.Length; i++)
            {
                string content = tree.blendType == BlendTreeType.Direct || tree.blendType == BlendTreeType.Simple1D
                    ? $"threshold: {tree.children[i].threshold}"
                    : $"X: {tree.children[i].position.x},  Y: {tree.children[i].position.y}";
                TreeMotions[i] = new AnimationClipSwap(content, null, tree.children[i].motion);
            }
        }

        public void DrawField()
        {
            if (Motion is BlendTree)
            {
                _foldout = EditorGUILayout.Foldout(_foldout, Motion.name + " (blendTree)");
                if (!_foldout) return;
                
                EditorGUI.indentLevel++;
                foreach (AnimationClipSwap treeMotion in TreeMotions)
                    treeMotion.DrawField();
                
                EditorGUI.indentLevel--;
                return;
            }
            Motion = (Motion) EditorGUILayout.ObjectField(State != null ? State.name : Layer, Motion, typeof(AnimationClip), false);
        }
    }

    public class StateMissMatchException : Exception
    {
        public StateMissMatchException(string message) : base(message){}
    }

    public static class AnimationClipSwapExtensions
    {
        public static AnimatorController SaveClipChanges(this List<AnimationClipSwap> swaps, AnimatorController controller, bool saveToNew = false)
        {
            if (controller == null) return null;
            
            if (saveToNew)
            {
                var assetPath = AssetDatabase.GetAssetPath(controller);
                
                Directory.CreateDirectory("Assets/VRLabs/GeneratedAssets");
                string uniquePath = AssetDatabase.GenerateUniqueAssetPath(AnimatorCloner.STANDARD_NEW_ANIMATOR_FOLDER + Path.GetFileName(assetPath));
                AssetDatabase.CopyAsset(assetPath, uniquePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                assetPath = uniquePath;
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
            }
            
            foreach (var animationLayer in controller.layers)
                ApplyLayerAnimationChanges(controller, saveToNew, animationLayer.stateMachine, swaps.Where(x => x.Layer.Equals(animationLayer.name)).ToArray());

            return controller;

        }
        
        private static int ApplyLayerAnimationChanges(AnimatorController controller, bool saveToNew, AnimatorStateMachine stateMachine, AnimationClipSwap[] swaps, int index = 0)
        {
            foreach (var state in stateMachine.states.Select(t => t.state))
            {
                if (state.name.Equals(swaps[index].State.name))
                {
                    if (state.motion is BlendTree tree)
                    {
                        string assetPath = AssetDatabase.GetAssetPath(tree);
                        if (saveToNew && !assetPath.Equals(AssetDatabase.GetAssetPath(controller)))
                        {
                            Directory.CreateDirectory("Assets/VRLabs/GeneratedAssets");
                            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(AnimatorCloner.STANDARD_NEW_ANIMATOR_FOLDER + Path.GetFileName(assetPath));
                            AssetDatabase.CopyAsset(assetPath, uniquePath);
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                            state.motion = tree = AssetDatabase.LoadAssetAtPath<BlendTree>(uniquePath);
                        }
                        ApplyBlendTreeChanges(tree, swaps[index].TreeMotions);
                    }
                    else
                        state.motion = swaps[index].Motion;

                    index++;
                }
                else
                {
                    throw new StateMissMatchException("There is a missmatch between the manager states and the animator states, this could be due to modifications done to the animator while the manager was in swap mode for that animator");
                }
            }
            foreach (ChildAnimatorStateMachine t in stateMachine.stateMachines)
               index = ApplyLayerAnimationChanges(controller, saveToNew, t.stateMachine, swaps, index);

            return index;
        }
        
        private static void ApplyBlendTreeChanges(BlendTree tree, AnimationClipSwap[] treeMotions)
        {
            var newChildren = new ChildMotion[tree.children.Length];
            for (int i = 0; i < tree.children.Length; i++)
            {
                ChildMotion child = tree.children[i];
                if (child.motion is BlendTree childTree)
                    ApplyBlendTreeChanges(childTree, treeMotions[i].TreeMotions);
                else
                    child.motion = treeMotions[i].Motion;

                newChildren[i] = child;
            }
            tree.children = newChildren;
        }
    }
}