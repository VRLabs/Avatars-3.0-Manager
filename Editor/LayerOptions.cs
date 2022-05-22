#if VRC_SDK_VRCSDK3
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using static VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;
namespace VRLabs.AV3Manager
{
    public class LayerOptions
    {
        // UI display text
        private static class Content
        {
            public static readonly GUIContent Controller = new GUIContent("Controller", "Controller used for the playable layer.");
            public static readonly GUIContent CustomLayerButton = new GUIContent("Use Custom Animator Layer", "Use your own animator for this layer.");
            public static readonly GUIContent DefaultCustomLayerButton = new GUIContent("Use Default Layer as custom", "Use a copy of the default layer for this layer.");
            public static readonly GUIContent DefaultLayerButton = new GUIContent("Use Default VRC Layer", "Use the default animator set by VRC for this layer.");
            public static readonly GUIContent MergeBoldMini = new GUIContent("Animator to merge");
            
            public static readonly GUIContent AddMerge = new GUIContent("Add animator to merge", "Select animator to merge to the current layer animator.");
            public static readonly GUIContent MergeTitle = new GUIContent("Merge Animators Mode");
            public static readonly GUIContent MergeCurrent = new GUIContent("Merge on current", "Merge animator to the current layer animator.");
            public static readonly GUIContent MergeNew = new GUIContent("Merge as new", "Merge animator to a copy of the current layer animator.");
            
            public static readonly GUIContent Swap = new GUIContent("Swap animations", "Swap animations in the current animator.");
            public static readonly GUIContent SwapTitle = new GUIContent("Swap Animation Mode");
            public static readonly string SwapWarning = "Do NOT modify the animator while you're swapping animations with the manager to avoid issues.";
            public static readonly GUIContent SwapCurrent = new GUIContent("Apply on current", "Apply the new animations to the current layer animator.");
            public static readonly GUIContent SwapNew = new GUIContent("Apply on new", "Apply the new animations to a copy of the current layer animator.");
            
            public static readonly GUIContent Cancel = new GUIContent("Cancel", "Cancel the operation.");
        }
        
        public static Dictionary<AnimLayerType, string> InitializeAnimatorsReferences()
        {
            string assetsPath = "";
            string[] beaconPath = AssetDatabase.FindAssets("ManagerAnimatorBeacon t:AnimationClip", null);
            if (beaconPath.Length > 0)
            {
                string[] pieces = AssetDatabase.GUIDToAssetPath(beaconPath[0]).Split('/');
                ArrayUtility.RemoveAt(ref pieces, pieces.Length - 1);
                assetsPath = string.Join("/", pieces);
            }
            
            return new Dictionary<AnimLayerType, string>
            {
                { AnimLayerType.Base , assetsPath + "/Base.controller"},
                { AnimLayerType.Additive , assetsPath + "/Additive.controller"},
                { AnimLayerType.Gesture , assetsPath + "/Gesture.controller"},
                { AnimLayerType.Action , assetsPath + "/Action.controller"},
                { AnimLayerType.FX , assetsPath + "/FX.controller"},
                { AnimLayerType.Sitting , assetsPath + "/Sitting.controller"},
                { AnimLayerType.TPose , assetsPath + "/TPose.controller"},
                { AnimLayerType.IKPose , assetsPath + "/IKPose.controller"}
            };
        }



        private static Dictionary<AnimLayerType, string> _defaultControllersPath = InitializeAnimatorsReferences();
        public CustomAnimLayer Layer;
        
        public AnimatorController Controller;
        public AnimatorToMerge AdditionalController;
        
        public List<(AnimatorControllerParameter, bool)> Parameters = new List<(AnimatorControllerParameter, bool)>();
        
        private List<AnimationClipSwap> _animationClips;
        
        private readonly int _index;
        private readonly AV3ManagerWindow _window;
        private bool _show;
        private bool _showMerger;
        private bool _showSwap;

        // Constructor
        public LayerOptions(AV3ManagerWindow window, CustomAnimLayer layer, int index)
        {
            _window = window;
            Layer = layer;
            _index = index;
            if (layer.animatorController is AnimatorController controller)
            {
                Controller = controller;
                UpdateParameterList();
            }
            AdditionalController = new AnimatorToMerge(null, _window);
        }

        public int Index => _index;

        // Draws this object
        public void DrawLayerOptions()
        {
            // Header
            EditorGUILayout.BeginVertical("box");
            Rect r = EditorGUILayout.BeginHorizontal();
            _show = EditorGUILayout.Toggle(_show, EditorStyles.foldout, GUILayout.MaxWidth(15.0f));
            EditorGUILayout.LabelField(Layer.type.ToString(), EditorStyles.boldLabel);
            _show = GUI.Toggle(r, _show, GUIContent.none, new GUIStyle());
            EditorGUILayout.EndHorizontal();
            if (_show)
            {
                if (Layer.isDefault)
                {
                    EditorGUILayout.BeginHorizontal();
                    // Custom layer button
                    if (GUILayout.Button(Content.CustomLayerButton))
                    {
                        Layer.isDefault = false;
                        _window.UpdateLayer(_index, Layer);
                    }
                    
                    if (GUILayout.Button(Content.DefaultCustomLayerButton))
                    {
                        Layer.isDefault = false;
                        
                        Directory.CreateDirectory(AnimatorCloner.STANDARD_NEW_ANIMATOR_FOLDER);
                        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(AnimatorCloner.STANDARD_NEW_ANIMATOR_FOLDER + Path.GetFileName(_defaultControllersPath[Layer.type]));
                        AssetDatabase.CopyAsset(_defaultControllersPath[Layer.type], uniquePath);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        
                        Controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(uniquePath);
                        Layer.animatorController = Controller;
                        
                        _window.UpdateLayer(_index, Layer);
                        UpdateParameterList();
                        _window.CleanupParametersList();
                        _window.RefreshWDState();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    // Default layer button
                    if (GUILayout.Button(Content.DefaultLayerButton))
                    {
                        Layer.isDefault = true;
                        Controller = null;
                        Layer.animatorController = null;
                        _window.UpdateLayer(_index, Layer);
                        UpdateParameterList();
                        _window.CleanupParametersList();
                        _window.RefreshWDState();
                    }
                    GUILayout.Space(10);
                    EditorGUI.BeginChangeCheck();
                    // Animator used
                    Controller = (AnimatorController)EditorGUILayout.ObjectField(Content.Controller, Controller, typeof(AnimatorController), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Layer.animatorController = Controller;
                        _window.UpdateLayer(_index, Layer);
                        UpdateParameterList();
                        _window.CleanupParametersList();
                        _window.RefreshWDState();
                    }

                    // Only show the list of parameters if there is a controller
                    if (Layer.animatorController != null)
                        DrawParameterList();

                    // If the merger is shown
                    if (_showMerger)
                    {
                        DrawMerger();
                    }
                    else if (_showSwap)
                    {
                        DrawSwapper();
                    }
                    // Merger show button.
                    else
                    {
                        GUILayout.Space(10);
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button(Content.AddMerge))
                            _showMerger = true;
                        if (GUILayout.Button(Content.Swap))
                        {
                            _animationClips = new List<AnimationClipSwap>();
                            foreach (var layer in Controller.layers)
                            {
                                _animationClips.AddRange(layer.GetLayerStates().Select(x => new AnimationClipSwap(layer.name, x, x.motion)));
                            }
                            _showSwap = true;
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSwapper()
        {
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField(Content.SwapTitle, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(Content.SwapWarning, MessageType.Warning);
            string layerName = "";
            EditorGUI.indentLevel++;
            foreach (AnimationClipSwap t in _animationClips)
            {
                if (!layerName.Equals(t.Layer))
                {
                    EditorGUI.indentLevel--;
                    GUILayout.Space(4);
                    layerName = t.Layer;
                    EditorGUILayout.LabelField(t.Layer, EditorStyles.miniBoldLabel);
                    EditorGUI.indentLevel++;
                }

                t.DrawField();
            }
            EditorGUI.indentLevel--;
            
            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            // Buttons
            if (GUILayout.Button(Content.SwapCurrent))
            {
                try
                {
                    Layer.animatorController = Controller = _animationClips.SaveClipChanges(Controller);
                    _window.UpdateLayer(_index, Layer);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                
                _showSwap = false;
            }

            if (GUILayout.Button(Content.SwapNew))
            {
                try
                {
                    Layer.animatorController = Controller = _animationClips.SaveClipChanges(Controller, true);
                    _window.UpdateLayer(_index, Layer);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                
                _showSwap = false;
            }

            if (GUILayout.Button(Content.Cancel))
            {
                _showSwap = false;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawMerger()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField(Content.MergeTitle, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(Content.MergeBoldMini, EditorStyles.miniBoldLabel);
            // Controller to merge
            GUILayout.Space(6);
            AdditionalController.DrawMergingAnimator();

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            // Buttons
            if (GUILayout.Button(Content.MergeCurrent) && AdditionalController.Controller != null)
            {
                Layer.animatorController = Controller = AnimatorCloner.MergeControllers(Controller, AdditionalController.Controller, AdditionalController.GetParameterMergingDictionary());
                _window.UpdateLayer(_index, Layer);
                UpdateParameterList();
                _showMerger = false;
                AdditionalController.Clear();
                _window.RefreshWDState();
            }

            if (GUILayout.Button(Content.MergeNew) && AdditionalController.Controller != null)
            {
                Layer.animatorController = Controller = AnimatorCloner.MergeControllers(Controller, AdditionalController.Controller, AdditionalController.GetParameterMergingDictionary(), true);
                _window.UpdateLayer(_index, Layer);
                UpdateParameterList();
                _showMerger = false;
                AdditionalController.Clear();
                _window.RefreshWDState();
            }

            if (GUILayout.Button(Content.Cancel))
            {
                _showMerger = false;
                AdditionalController.Clear();
            }

            EditorGUILayout.EndHorizontal();
        }

        // Draws UI of the parameters of the layer animator
        public void DrawParameterList()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Syncable Parameters", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("Synced", EditorStyles.miniBoldLabel, GUILayout.Width(38));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
            for (int i = 0; i < Parameters.Count; i++)
            {
                (AnimatorControllerParameter p, bool b) = Parameters[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(p.name);
                EditorGUI.BeginChangeCheck();
                EditorGUI.BeginDisabledGroup(!((_window.UsedParameterMemory + VRCExpressionParameters.TypeCost(AV3ManagerFunctions.GetValueTypeFromAnimatorParameterType(p.type))) <= VRCExpressionParameters.MAX_PARAMETER_COST) && !b);
                b = EditorGUILayout.Toggle(b, GUILayout.Width(20));
                EditorGUI.EndDisabledGroup();
                if (EditorGUI.EndChangeCheck())
                {
                    Parameters[i] = (p, b);
                    _window.UpdateParameter(p, b);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        // Update list of parameters
        public void UpdateParameterList()
        {
            Parameters = new List<(AnimatorControllerParameter, bool)>();
            if (Controller == null) return;
            foreach (var p in Controller.parameters.Where(x => x.type == AnimatorControllerParameterType.Int || x.type == AnimatorControllerParameterType.Float || x.type == AnimatorControllerParameterType.Bool))
                if (AV3ManagerWindow.VrcParameters.Count(x => x.Equals(p.name)) <= 0)
                    Parameters.Add((p, _window.IsParameterInList(p)));
        }
    }
}

#endif