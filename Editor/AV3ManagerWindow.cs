#if VRC_SDK_VRCSDK3
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using ValueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType;

namespace VRLabs.AV3Manager
{
    public class AV3ManagerWindow : EditorWindow
    {
        private enum WindowSection
        {
            Layers,
            Parameters
        }
        
        // Menu item
        [MenuItem("VRLabs/Avatars 3.0 Manager")]
        public static void Init()
        {
            var window = EditorWindow.GetWindow<AV3ManagerWindow>();
            window.titleContent = new GUIContent("AV3 Manager");
            window.minSize = new Vector2(400, 442);
        }

        // Default parameters
        public static readonly string[] VrcParameters =
        {
            "IsLocal",
            "Viseme",
            "GestureLeft",
            "GestureRight",
            "GestureLeftWeight",
            "GestureRightWeight",
            "AngularY",
            "VelocityX",
            "VelocityY",
            "VelocityZ",
            "Upright",
            "Grounded",
            "Seated",
            "AFK",
            "TrackingType",
            "VRMode",
            "MuteSelf",
            "InStation",
            "Supine",
            "GroundProximity"
        };

        public int UsedParameterMemory { get; set; }
        // UI text
        private static class Content
        {
            public static readonly GUIContent PlaymodeError = new GUIContent("Please exit Play Mode to use this script.");
            public static readonly GUIContent Avatar = new GUIContent("Avatar", "Your avatar.");
            public static readonly GUIContent WdOff = new GUIContent("Set WD off", "Set Write defaults off for all animators in this avatar descriptor.");
            public static readonly GUIContent WdOn = new GUIContent("Set WD on", "Set Write defaults on for all animators in this avatar descriptor.");
            public static readonly GUIContent RefreshParameterList = new GUIContent("Refresh parameters list", "Refreshes the list of the parameters in the asset based on your currently applied animators.");
            public static readonly GUIContent CopyParameterValues = new GUIContent("Copy parameter values", "Copy the default value and saved bool for each parameter found in the given asset.");
            public static readonly GUIContent ToggleDefaultParameters = new GUIContent("Use default parameters", "If toggled, the manager will check that the default vrc parameters are inside the list regardless of them being used by the animators or not.");
            public static readonly GUIContent ExpressionParameters = new GUIContent("Expression parameters", "Expression parameters asset used by the manager");
        }

        private Vector2 _mainScrollingPosition;

        private VRCExpressionParameters _paramsToCopy;
        
        private VRCAvatarDescriptor _avatar;
        private LayerOptions[] _layers;
        private WindowSection _section;
        private bool _showContent;
        private bool _useDefaultParameters;
        private bool _isMixedWriteDefaults;

        // Rebuild layer objects
        private void RebuildLayers()
        {
            _showContent = _avatar != null;
            if (_avatar != null)
            {
                _layers = new LayerOptions[_avatar.baseAnimationLayers.Length + _avatar.specialAnimationLayers.Length];
                for (int i = 0; i < _avatar.baseAnimationLayers.Length; i++)
                    _layers[i] = new LayerOptions(this, _avatar.baseAnimationLayers[i], i);
                for (int i = _avatar.baseAnimationLayers.Length; i < _layers.Length; i++)
                    _layers[i] = new LayerOptions(this, _avatar.specialAnimationLayers[i - _avatar.baseAnimationLayers.Length], i);
            }
        }
        void OnFocus()
        {
            if (_layers == null || _avatar == null) return;
            foreach (LayerOptions layer in _layers)
            {
                layer.UpdateParameterList();
            }

            CleanupParametersList();
            RefreshWDState();
        }
        

        //Draw GUI
        public void OnGUI()
        {
            // Show message if in play mode
            if (EditorApplication.isPlaying)
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField(Content.PlaymodeError);
                return;
            }
            
            GUILayout.Space(10);
            EditorGUI.BeginChangeCheck();
            _avatar = (VRCAvatarDescriptor)EditorGUILayout.ObjectField(Content.Avatar, _avatar, typeof(VRCAvatarDescriptor), true);

            if (EditorGUI.EndChangeCheck())
            {
                _showContent = _avatar != null;
                if (_avatar != null)
                {
                    _avatar.customExpressions = true;
                    if (_avatar.expressionParameters == null)
                        GenerateNewExpressionParametersAsset();
                    
                    if (_avatar.expressionsMenu == null)
                        GenerateNewExpressionMenuAsset();

                    UsedParameterMemory = _avatar.expressionParameters.CalcTotalCost();
                    _useDefaultParameters = EditorPrefs.GetBool("AV3ManagerDefaultParams");
                    RefreshWDState();
                    RebuildLayers();
                }
            }

            if (!_showContent) return;
            
            if (_layers == null)
                RebuildLayers();
            
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox($"Parameters Memory used: {UsedParameterMemory}/{MAX_PARAMETER_COST}.", MessageType.None);
            if (UsedParameterMemory > MAX_PARAMETER_COST)
                EditorGUILayout.HelpBox("You have too many parameters synced, untick the \"sync\" box of some parameters.", MessageType.Error);
            
            if (_isMixedWriteDefaults)
                EditorGUILayout.HelpBox("You have mixed Write Defaults in your layers, you may experience weird interactions ingame.", MessageType.Warning);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(Content.WdOff))
            {
                foreach (var layer in _layers.Where(x => !x.Layer.isDefault))
                    AV3ManagerFunctions.SetWriteDefaults(layer.Controller, false);
                RefreshWDState();
            }

            if (GUILayout.Button(Content.WdOn))
            {
                foreach (var layer in _layers.Where(x => !x.Layer.isDefault))
                    AV3ManagerFunctions.SetWriteDefaults(layer.Controller, true);
                RefreshWDState();
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            _section = (WindowSection)GUILayout.Toolbar((int)_section, Enum.GetNames(typeof(WindowSection)), EditorStyles.toolbarButton, GUI.ToolbarButtonSize.Fixed);
                
            _mainScrollingPosition = EditorGUILayout.BeginScrollView(_mainScrollingPosition);
                
            switch (_section)
            {
                case WindowSection.Layers:
                {
                    foreach (var l in _layers)
                    {
                        GUILayout.Space(l.Index == _avatar.baseAnimationLayers.Length ? 50 : 10);
                        l.DrawLayerOptions();
                    }

                    break;
                }
                case WindowSection.Parameters:
                {
                    DrawParametersTab();
                    break;
                }
            }
            EditorGUILayout.EndScrollView();
        }

        public void RefreshWDState()
        {
            _isMixedWriteDefaults = _avatar.HasMixedWriteDefaults();
        }

        private void DrawParametersTab()
        {
            GUILayout.Space(10);
            _avatar.expressionParameters = (VRCExpressionParameters)EditorGUILayout.ObjectField(Content.ExpressionParameters, _avatar.expressionParameters, typeof(VRCExpressionParameters), false);
            
            if (GUILayout.Button(Content.RefreshParameterList))
                CleanupParametersList();
            EditorGUI.BeginChangeCheck();
            _useDefaultParameters = EditorGUILayout.Toggle(Content.ToggleDefaultParameters, _useDefaultParameters);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool("AV3ManagerDefaultParams", _useDefaultParameters);

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("Type", EditorStyles.miniBoldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField("Default", EditorStyles.miniBoldLabel, GUILayout.Width(64));
            EditorGUILayout.LabelField("Saved", EditorStyles.miniBoldLabel, GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();

            foreach (var parameter in _avatar.expressionParameters.parameters)
            {
                EditorGUILayout.BeginHorizontal();
                //parameter.name = EditorGUILayout.TextField(parameter.name);
                EditorGUILayout.LabelField(parameter.name);
                EditorGUI.BeginDisabledGroup(true);
                parameter.valueType = (ValueType)EditorGUILayout.EnumPopup(parameter.valueType, GUILayout.Width(100));
                EditorGUI.EndDisabledGroup();

                switch (parameter.valueType)
                {
                    case ValueType.Int:
                        parameter.defaultValue = Mathf.Clamp(EditorGUILayout.IntField((int)parameter.defaultValue, GUILayout.Width(64)), 0, 255);
                        break;
                    case ValueType.Float:
                        parameter.defaultValue = Mathf.Clamp(EditorGUILayout.FloatField(parameter.defaultValue, GUILayout.Width(64)), -1f, 1f);
                        break;
                    case ValueType.Bool:
                        GUILayout.Space(24);
                        parameter.defaultValue = EditorGUILayout.Toggle(parameter.defaultValue != 0, GUILayout.Width(40)) ? 1f : 0f;
                        break;
                }

                GUILayout.Space(10);
                parameter.saved = EditorGUILayout.Toggle(parameter.saved, GUILayout.Width(30));

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5);
            }
            
            GUILayout.Space(20);
            _paramsToCopy = (VRCExpressionParameters)EditorGUILayout.ObjectField("Parameters to copy", _paramsToCopy, typeof(VRCExpressionParameters), false);
            if (GUILayout.Button(Content.CopyParameterValues) && _paramsToCopy != null)
            {
                foreach (var parameter in _paramsToCopy.parameters)
                {
                    Parameter p = _avatar.expressionParameters.FindParameter(parameter.name);
                    if (p == null) continue;
                    p.defaultValue = parameter.defaultValue;
                    p.saved = parameter.saved;
                }
            }


        }

        // Generates new Expression parameters Asset
        private void GenerateNewExpressionParametersAsset()
        {
            if (!AssetDatabase.IsValidFolder(AnimatorCloner.STANDARD_NEW_ANIMATOR_FOLDER.Substring(0, AnimatorCloner.STANDARD_NEW_ANIMATOR_FOLDER.Length - 1)))
                AssetDatabase.CreateFolder("Assets/VRLabs", "GeneratedAssets");
            
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(AnimatorCloner.STANDARD_NEW_ANIMATOR_FOLDER + "Parameters.asset");
            _avatar.expressionParameters = ScriptableObject.CreateInstance<VRCExpressionParameters>();
            // Initialize vrc parameters array
            _avatar.expressionParameters.parameters = new Parameter[3];

            // Add default parameters
            _avatar.expressionParameters.parameters[0] = new Parameter
            {
                name = "VRCEmote",
                valueType = ValueType.Int,
                defaultValue = 0,
                saved = false
            };
            _avatar.expressionParameters.parameters[1] = new Parameter
            {
                name = "VRCFaceBlendH",
                valueType = ValueType.Float,
                defaultValue = 0,
                saved = false
            };
            _avatar.expressionParameters.parameters[2] = new Parameter
            {
                name = "VRCFaceBlendV",
                valueType = ValueType.Float,
                defaultValue = 0,
                saved = false
            };

            AssetDatabase.CreateAsset(_avatar.expressionParameters, uniquePath);
            EditorUtility.SetDirty(_avatar.expressionParameters);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // Generates new expression menu asset
        private void GenerateNewExpressionMenuAsset()
        {
            if (!AssetDatabase.IsValidFolder(AnimatorCloner.STANDARD_NEW_ANIMATOR_FOLDER.Substring(0, AnimatorCloner.STANDARD_NEW_ANIMATOR_FOLDER.Length - 1)))
                AssetDatabase.CreateFolder("Assets/VRLabs", "GeneratedAssets");
            
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(AnimatorCloner.STANDARD_NEW_ANIMATOR_FOLDER + "Menu.asset");
            _avatar.expressionsMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            AssetDatabase.CreateAsset(_avatar.expressionsMenu, uniquePath);
            EditorUtility.SetDirty(_avatar.expressionsMenu);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // Updates a layer value, need to do this cause the CustomAnimLayer is a struct and not a class.
        public void UpdateLayer(int index, VRCAvatarDescriptor.CustomAnimLayer layer)
        {
            if(index >= _avatar.baseAnimationLayers.Length)
                _avatar.specialAnimationLayers[index - _avatar.baseAnimationLayers.Length] = layer;
            else
                _avatar.baseAnimationLayers[index] = layer;
        }

        // Check if the provided parameter is in the list.
        public bool IsParameterInList(AnimatorControllerParameter parameter)
        {
            Parameter o = _avatar.expressionParameters.FindParameter(parameter.name);
            return o != null;
        }

        public void CleanupParametersList()
        {
            var syncedParameters = _layers?.SelectMany(x => x.Parameters)
                .Where(x => x.Item2)
                .Select(x => x.Item1)
                .ToArray();

            if (_useDefaultParameters)
            {
                syncedParameters = (syncedParameters ?? Array.Empty<AnimatorControllerParameter>()).Append(new AnimatorControllerParameter { name = "VRCEmote", type = AnimatorControllerParameterType.Int, defaultInt = 0 })
                    .Append(new AnimatorControllerParameter { name = "VRCFaceBlendH", type = AnimatorControllerParameterType.Float, defaultFloat = 0 })
                    .Append(new AnimatorControllerParameter { name = "VRCFaceBlendV", type = AnimatorControllerParameterType.Float, defaultFloat = 0 })
                    .ToArray();
            }

            var param = new List<Parameter>(_avatar.expressionParameters.parameters);
            var remaining = new List<Parameter>(param);

            bool somethingModified = false;
            foreach (var parameter in param)
            {
                bool toDelete = syncedParameters?.All(syncedParameter => !parameter.name.Equals(syncedParameter.name)) ?? false;

                if (!toDelete) continue;
                remaining.Remove(parameter);
                somethingModified = true;
            }

            foreach (var syncedParameter in syncedParameters)
            {
                bool toAdd = param.All(parameter => !parameter.name.Equals(syncedParameter.name));
                if (!toAdd) continue;
                remaining.Add(new Parameter
                {
                    name = syncedParameter.name,
                    valueType = AV3ManagerFunctions.GetValueTypeFromAnimatorParameterType(syncedParameter.type),
                    defaultValue = 0,
                    saved = false
                });
                somethingModified = true;
            }

            if (!somethingModified) return;
            
            _avatar.expressionParameters.parameters = remaining.ToArray();
            EditorUtility.SetDirty(_avatar.expressionParameters);
            UsedParameterMemory = _avatar.expressionParameters.CalcTotalCost();
        }
        
        // Adds or removes a parameter based on the enabled boolean.
        public void UpdateParameter(AnimatorControllerParameter parameter, bool enabled)
        {
            if (parameter.type == AnimatorControllerParameterType.Int || parameter.type == AnimatorControllerParameterType.Float || parameter.type == AnimatorControllerParameterType.Bool)
            {
                List<Parameter> param = new List<Parameter>(_avatar.expressionParameters.parameters);
                bool somethingModified = false;
                if (enabled)
                {
                    param.Add(new Parameter
                    {
                        name = parameter.name,
                        valueType = AV3ManagerFunctions.GetValueTypeFromAnimatorParameterType(parameter.type),
                        defaultValue = 0,
                        saved = false
                    });
                    somethingModified = true;
                }
                else
                {
                    for (int i = 0; i < param.Count; i++)
                    {
                        if (!param[i].name.Equals(parameter.name)) continue;
                        param.RemoveAt(i);
                        somethingModified = true;
                    }
                }

                if (somethingModified)
                {
                    _avatar.expressionParameters.parameters = param.ToArray();
                    foreach (var layer in _layers)
                    {
                        layer.UpdateParameterList();
                    }
                }
                EditorUtility.SetDirty(_avatar.expressionParameters);
            }
            UsedParameterMemory = _avatar.expressionParameters.CalcTotalCost();
        }

        // Check if a specific parameter is a duplicate
        public bool IsParameterDuplicate(string parameterName)
        {
            return _layers.Any(layer => layer.Controller != null && layer.Controller.parameters.Count(x => x.name.Equals(parameterName)) > 0);
        }
    }
}

#endif