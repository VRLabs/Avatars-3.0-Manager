#if VRC_SDK_VRCSDK3

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using static VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;
using ValueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType;

namespace VRLabs.AV3Manager
{
    /***
       * The first 3 functions won't overwrite original assets. Copies are created first in your provided directory.
       * Only in that directory can a function overwrite the avatar descriptor's assets.
       * This is so you can call the functions repeatedly without creating multiple unnecessary assets.

              // will create 1 new parameter asset, not 3
              AddParameter(myDescriptor, parameterA, uniqueDirectory);
              AddParameter(myDescriptor, parameterB, uniqueDirectory);
              AddParameter(myDescriptor, parameterC, uniqueDirectory);

       * Consequently, always specify a unique directory per script-run. 
       * A good method is to seed a folder name with the date and time, at the beginning of your script.
       
              string uniqueDirectory = "Assets/MyCoolScript/Generated/" + DateTime.Now.ToString("MM.dd HH.mm.ss") + "/";
              Directory.CreateDirectory(uniqueDirectory);

       * In an emergency case, set the "overwrite" parameter to false in your first call to the function.
              
              AddParameter(myDescriptor, parameterA, someNotUniqueDirectory, false);
              AddParameter(myDescriptor, parameterB, someNotUniqueDirectory);
              AddParameter(myDescriptor, parameterC, someNotUniqueDirectory);
       ***/

    /// <summary>
    /// Helpful functions for script writers using the VRChat Avatars 3.0 SDK and VRLabs 3.0 manager.
    /// Merge controllers, add parameters, and add submenus to an avatar.
    /// </summary>
    public static class AV3ManagerFunctions
    {
        public static bool InitializeAnimatorsReferences()
        {
            string assetsPath = "";
            string[] beaconPath = AssetDatabase.FindAssets("ManagerAnimatorBeacon t:AnimationClip", null);
            if (beaconPath.Length > 0)
            {
                string[] pieces = AssetDatabase.GUIDToAssetPath(beaconPath[0]).Split('/');
                ArrayUtility.RemoveAt(ref pieces, pieces.Length - 1);
                assetsPath = string.Join("/", pieces);
            }
            
            DefaultControllersPath = new Dictionary<AnimLayerType, string>
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

            EmptyClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetsPath + "/EmptyClip.anim");

            return true;
        }

        public static Dictionary<AnimLayerType, string> DefaultControllersPath { get; set; }
        public static AnimationClip EmptyClip { get; set; }
        
        private static bool _isStaticStuffLoaded = InitializeAnimatorsReferences();

        private static string[] _clipPathSeparators = { "##" };
        private const string DEFAULT_DIRECTORY = "Assets/VRLabs/GeneratedAssets/";
        private static readonly string[] _defaultLayerPath = 
        {
            "Assets/VRCSDK/Examples3/Animation/Controllers/vrc_AvatarV3LocomotionLayer.controller",
            "Assets/VRCSDK/Examples3/Animation/Controllers/vrc_AvatarV3IdleLayer.controller",
            "Assets/VRCSDK/Examples3/Animation/Controllers/vrc_AvatarV3HandsLayer.controller",
            "Assets/VRCSDK/Examples3/Animation/Controllers/vrc_AvatarV3ActionLayer.controller"
        };
        public enum PlayableLayer // for function MergeToLayer
        {
            Base = 0,
            Additive = 1,
            Gesture = 2,
            Action = 3,
            FX = 4
        }

        /// <summary>
        /// Creates a copy of the avatar descriptor's parameter asset or creates one if it doesn't exist, adds a provided parameter,
        /// assigns the new parameter asset, and stores it in the specified directory.
        /// </summary>
        /// <param name="descriptor">The avatar descriptor to add the parameter to.</param>
        /// <param name="parameter">The parameter to add.</param>
        /// <param name="directory">The unique directory to store the new parameter asset, ex. "Assets/MyCoolScript/GeneratedAssets/725638/".</param>
        /// <param name="overwrite">Optionally, choose to not overwrite an asset of the same name in directory. See class for more info.</param>
        public static void AddParameter(VRCAvatarDescriptor descriptor, VRCExpressionParameters.Parameter parameter, string directory, bool overwrite = true)
        {
            if (descriptor == null)
            {
                Debug.LogError("Couldn't add the parameter, the avatar descriptor is null!");
                return;
            } 
            if ((parameter == null) || (parameter.name == null) )
            {
                Debug.LogError("Couldn't add the parameter, it or its name is null!");
                return;
            }
            if ((directory == null) || (directory == ""))
            {
                Debug.Log("Directory was not specified, storing new parameters asset in " + DEFAULT_DIRECTORY);
                directory = DEFAULT_DIRECTORY;
            }

            descriptor.customExpressions = true;
            VRCExpressionParameters parameters = ScriptableObject.CreateInstance<VRCExpressionParameters>();
            parameters.parameters = new VRCExpressionParameters.Parameter[0];

            if (descriptor.expressionParameters == null)
            {
                AssetDatabase.CreateAsset(parameters, directory + "Parameters.asset");
            }
            else
            {
                if ((descriptor.expressionParameters.CalcTotalCost() + VRCExpressionParameters.TypeCost(parameter.valueType)) > VRCExpressionParameters.MAX_PARAMETER_COST)
                {
                    Debug.LogError("Couldn't add parameter '" + parameter.name + "', not enough memory free in the avatar's parameter asset!");
                    return;
                }

                string path = (directory + descriptor.expressionParameters.name + ".asset");
                path = (overwrite) ? path : AssetDatabase.GenerateUniqueAssetPath(path);
                if ( AssetDatabase.GetAssetPath(descriptor.expressionParameters) != path) // if we have not made a copy yet
                { // CopyAsset with two identical strings yields exception
                    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(descriptor.expressionParameters), path);
                }
                parameters = AssetDatabase.LoadAssetAtPath(path, typeof(VRCExpressionParameters)) as VRCExpressionParameters;
            }

            if (parameters.FindParameter(parameter.name) == null)
            {
                int count = parameters.parameters.Length;
                VRCExpressionParameters.Parameter[] parameterArray = new VRCExpressionParameters.Parameter[count + 1];
                for (int i = 0; i < count; i++)
                {
                    parameterArray[i] = parameters.GetParameter(i);
                }
                parameterArray[count] = parameter.GetCopy();
                parameters.parameters = parameterArray;
            }

            EditorUtility.SetDirty(parameters);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            descriptor.expressionParameters = parameters;
        }
        
        /// <summary>
        /// Creates a copy of the avatar descriptor's parameter asset or creates one if it doesn't exist, adds a provided parameter,
        /// assigns the new parameter asset, and stores it in the specified directory.
        /// </summary>
        /// <param name="descriptor">The avatar descriptor to add the parameters to.</param>
        /// <param name="parameters">The parameters to add.</param>
        /// <param name="directory">The unique directory to store the new parameter asset, ex. "Assets/MyCoolScript/GeneratedAssets/725638/".</param>
        /// <param name="overwrite">Optionally, choose to not overwrite an asset of the same name in directory. See class for more info.</param>
        public static void AddParameters(VRCAvatarDescriptor descriptor, IEnumerable<VRCExpressionParameters.Parameter> parameters, string directory, bool overwrite = true)
        {
            if (descriptor == null)
            {
                Debug.LogError("Couldn't add the parameters, the avatar descriptor is null!");
                return;
            } 
            if (parameters == null)
            {
                Debug.LogError("Couldn't add the parameters, it is null!");
                return;
            }
            if ((directory == null) || (directory == ""))
            {
                Debug.Log("Directory was not specified, storing new parameters asset in " + DEFAULT_DIRECTORY);
                directory = DEFAULT_DIRECTORY;
            }

            descriptor.customExpressions = true;
            VRCExpressionParameters newParameters = ScriptableObject.CreateInstance<VRCExpressionParameters>();
            newParameters.parameters = new VRCExpressionParameters.Parameter[0];

            if (descriptor.expressionParameters == null)
            {
                AssetDatabase.CreateAsset(newParameters, directory + "Parameters.asset");
            }
            else
            {
                if (descriptor.expressionParameters.CalcTotalCost() + parameters.GetCost(descriptor.expressionParameters.parameters) > VRCExpressionParameters.MAX_PARAMETER_COST)
                {
                    Debug.LogError("Couldn't add parameters, not enough memory free in the avatar's parameter asset!");
                    return;
                }

                string path = (directory + descriptor.expressionParameters.name + ".asset");
                path = (overwrite) ? path : AssetDatabase.GenerateUniqueAssetPath(path);
                if ( AssetDatabase.GetAssetPath(descriptor.expressionParameters) != path) // if we have not made a copy yet
                { // CopyAsset with two identical strings yields exception
                    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(descriptor.expressionParameters), path);
                }
                newParameters = AssetDatabase.LoadAssetAtPath(path, typeof(VRCExpressionParameters)) as VRCExpressionParameters;
            }

            var ParametersList = new List<VRCExpressionParameters.Parameter>(newParameters.parameters);

            foreach (var prm in parameters)
            {
                if (newParameters.FindParameter(prm.name) == null)
                {
                    ParametersList.Add(prm.GetCopy());
                }   
            }
            
            newParameters.parameters = ParametersList.ToArray();

            EditorUtility.SetDirty(newParameters);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            descriptor.expressionParameters = newParameters;
        }

        public static VRCExpressionParameters.Parameter GetCopy(this VRCExpressionParameters.Parameter parameter)
        {
            var newParam = new VRCExpressionParameters.Parameter();
            newParam.name = parameter.name;
            newParam.valueType = parameter.valueType;
            newParam.defaultValue = parameter.defaultValue;
            newParam.saved = parameter.saved;

            return newParam;
        }

        public static int GetCost(this IEnumerable<VRCExpressionParameters.Parameter> parameters, IEnumerable<VRCExpressionParameters.Parameter> blackList = null)
        {
            return blackList == null ? parameters.Sum(parameter => VRCExpressionParameters.TypeCost(parameter.valueType)) 
                : parameters.Where(x => !blackList.Any(y => y.name.Equals(x.name))).Sum(parameter => VRCExpressionParameters.TypeCost(parameter.valueType));
        }

        /// <summary>
        /// Creates a copy of the avatar descriptor's topmost menu asset or creates one if it doesn't exist, adds the provided menu as a submenu,
        /// assigns the new topmost menu asset, and stores it in the specified directory.
        /// </summary>
        /// <param name="descriptor">The avatar descriptor to add the submenu to.</param>
        /// <param name="menuToAdd">The menu to add, which will become a submenu of the topmost menu.</param>
        /// <param name="controlName">The name of the submenu control for the menu to add.</param>
        /// <param name="directory">The unique directory to store the new topmost menu asset, ex. "Assets/MyCoolScript/GeneratedAssets/725638/".</param>
        /// <param name="controlParameter">Optionally, the parameter to trigger when the submenu is opened.</param>
        /// <param name="icon"> Optionally, the icon to display on this submenu. </param>
        /// <param name="overwrite">Optionally, choose to not overwrite an asset of the same name in directory. See class for more info.</param>
        public static void AddSubMenu(VRCAvatarDescriptor descriptor, VRCExpressionsMenu menuToAdd, string controlName, string directory, VRCExpressionsMenu.Control.Parameter controlParameter = null, Texture2D icon = null, bool overwrite = true)
        {
            if (descriptor == null)
            {
                Debug.LogError("Couldn't add the menu, the avatar descriptor is null!");
                return;
            }
            else if ((menuToAdd == null) || (controlName == null) || (controlName == ""))
            {
                Debug.LogError("Couldn't add the menu, it or the name of its control is null!");
                return;
            }
            else if ((directory == null) || (directory == ""))
            {
                Debug.Log("Directory was not specified, storing new topmost menu in " + DEFAULT_DIRECTORY);
                directory = DEFAULT_DIRECTORY;
            }

            descriptor.customExpressions = true;
            VRCExpressionsMenu topMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();

            if (descriptor.expressionsMenu == null)
            {
                AssetDatabase.CreateAsset(topMenu, directory + "Menu Topmost.asset");
            }
            else
            {
                if (descriptor.expressionsMenu.controls.Count == 8)
                {
                    Debug.LogWarning("Couldn't add menu. Please have an available slot in your avatar's topmost Expression Menu.");
                    return;
                }

                string path = (directory + descriptor.expressionsMenu.name + ".asset");
                path = (overwrite) ? path : AssetDatabase.GenerateUniqueAssetPath(path);
                if (AssetDatabase.GetAssetPath(descriptor.expressionsMenu) != path) // if we have not made a copy yet
                { // CopyAsset with two identical strings yields exception
                    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(descriptor.expressionsMenu), path);
                }
                topMenu = AssetDatabase.LoadAssetAtPath(path, typeof(VRCExpressionsMenu)) as VRCExpressionsMenu;
            }

            List<VRCExpressionsMenu.Control> controlList = topMenu.controls;

            for (int i = 0; i < controlList.Count; i++)
            {
                if (controlList[i].name.Equals(controlName) && controlList[i].type.Equals(VRCExpressionsMenu.Control.ControlType.SubMenu))
                { // if a control for a submenu exists with the same name, replace the submenu
                    controlList[i].subMenu = menuToAdd;
                    controlList[i].parameter = controlParameter;
                    controlList[i].icon = icon;
                    EditorUtility.SetDirty(topMenu);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    descriptor.expressionsMenu = topMenu;
                    return;
                }
            }

            VRCExpressionsMenu.Control control = new VRCExpressionsMenu.Control
            { name = controlName, type = VRCExpressionsMenu.Control.ControlType.SubMenu, subMenu = menuToAdd, parameter = controlParameter, icon = icon };
            controlList.Add(control);
            topMenu.controls = controlList;
            EditorUtility.SetDirty(topMenu);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            descriptor.expressionsMenu = topMenu;
        }

        /// <summary>
        /// Merges a controller "as new" with the specified playable layer on an avatar's descriptor,
        /// assigns it on the avatar, and stores the new controller at the given path.
        /// </summary>
        /// <param name="descriptor">The avatar descriptor that merging is being done on.</param>
        /// <param name="controllerToAdd">The controller to merge to the playable layer.</param>
        /// <param name="playable">The playable layer to merge to.</param>
        /// <param name="directory">The unique directory to store the new merged controller, ex. "Assets/MyCoolScript/GeneratedAssets/725638/".</param>
        /// <param name="overwrite">Optionally, choose to not overwrite an asset of the same name in directory. See class for more info.</param>
        public static void MergeToLayer(VRCAvatarDescriptor descriptor, AnimatorController controllerToAdd, PlayableLayer playable, string directory, bool overwrite = true)
        {
            int layer = (int)playable;

            if (descriptor == null)
            {
                Debug.LogError("The avatar descriptor is null! Merging was not performed.");
                return;
            }
            else if (controllerToAdd == null)
            {
                Debug.LogError("The controller to add is null! Merging was not performed.");
                return;
            }
            else if ((layer < 0) || (layer > 4))
            {
                Debug.LogError("Layer is out of bounds! Merging was not performed.");
                return;
            }
            else if (layer < 4) // fx layer has no default layer
            {
                if ((AssetDatabase.LoadAssetAtPath(_defaultLayerPath[layer], typeof(AnimatorController)) as AnimatorController) == null)
                {
                    Debug.LogError("Couldn't find VRChat's default animator controller at path '" + _defaultLayerPath[layer] + "'! Merging was not performed.");
                    return;
                }
            }
            else if (string.IsNullOrEmpty(directory))
            {
                Debug.Log("Directory was not specified, storing new controller in " + DEFAULT_DIRECTORY);
                directory = DEFAULT_DIRECTORY;
            }

            if ((descriptor.baseAnimationLayers[layer].isDefault) || descriptor.baseAnimationLayers[layer].animatorController == null)
            {
                descriptor.customizeAnimationLayers = true;
                descriptor.baseAnimationLayers[layer].isDefault = false;

                AnimatorController controllerFromNew = new AnimatorController();
                string pathFromNew = directory + playable.ToString() + ".controller";

                if (layer == 4) // fx layer has no default layer
                {   // you cannot add a layer to a controller without creating its asset first
                    AssetDatabase.CreateAsset(controllerFromNew, pathFromNew);
                    controllerFromNew.AddLayer("Base Layer");
                }
                else
                {
                    AssetDatabase.CopyAsset(_defaultLayerPath[layer], pathFromNew);
                    controllerFromNew = AssetDatabase.LoadAssetAtPath(pathFromNew, typeof(AnimatorController)) as AnimatorController;
                }
                descriptor.baseAnimationLayers[layer].animatorController = controllerFromNew;
            }

            string path = (directory + descriptor.baseAnimationLayers[layer].animatorController.name + ".controller");
            path = (overwrite) ? path : AssetDatabase.GenerateUniqueAssetPath(path);
            if (AssetDatabase.GetAssetPath(descriptor.baseAnimationLayers[layer].animatorController) != path) // if we have not made a copy yet
            { // CopyAsset with two identical strings yields exception
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(descriptor.baseAnimationLayers[layer].animatorController), path);
            }

            AnimatorController controllerOriginal = AssetDatabase.LoadAssetAtPath(path, typeof(AnimatorController)) as AnimatorController;
            AnimatorController mergedController = AnimatorCloner.MergeControllers(controllerOriginal, controllerToAdd, null, false);

            descriptor.baseAnimationLayers[layer].animatorController = mergedController;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        /// <summary>
        /// Checks if the avatar descriptor has mixing "Write defaults" settings across its animators.
        /// </summary>
        /// <param name="descriptor">Avatar descriptor to check.</param>
        /// <returns>True if the avatar animators contain mixed write defaults, false otherwise.</returns>
        public static List<WDState> AnalyzeWDState(this VRCAvatarDescriptor descriptor)
        {
            var states = new List<WDState>();
            foreach (var layer in descriptor.baseAnimationLayers)
            {
                if (!(layer.animatorController is AnimatorController controller) || controller == null) continue;
                foreach (var animationLayer in controller.layers)
                    AnalyzeWdStateMachine(animationLayer.stateMachine, states, layer.type.ToString());
                
            }
            foreach (var layer in descriptor.specialAnimationLayers)
            {
                if (!(layer.animatorController is AnimatorController controller)) continue;
                foreach (var animationLayer in controller.layers)
                    AnalyzeWdStateMachine(animationLayer.stateMachine, states, layer.type.ToString());
            }

            return states;
        }

        /// <summary>
        /// Checks if the avatar descriptor has mixing "Write defaults" settings across its animators.
        /// </summary>
        /// <param name="descriptor">Avatar descriptor to check.</param>
        /// <returns>True if the avatar animators contain mixed write defaults, false otherwise.</returns>
        public static bool HasMixedWriteDefaults(this VRCAvatarDescriptor descriptor)
        {
            bool isOn = false;
            bool checkedFirst = false;
            bool isMixed;
            foreach (var layer in descriptor.baseAnimationLayers)
            {
                if (!(layer.animatorController is AnimatorController controller) || controller == null) continue;
                foreach (var animationLayer in controller.layers)
                {
                    (checkedFirst, isOn, isMixed) = GetWdInStateMachine(animationLayer.stateMachine, checkedFirst, isOn);
                    if(isMixed)
                        return true;
                }
            }
            foreach (var layer in descriptor.specialAnimationLayers)
            {
                if (!(layer.animatorController is AnimatorController controller)) continue;
                foreach (var animationLayer in controller.layers)
                {
                    (checkedFirst, isOn, isMixed) = GetWdInStateMachine(animationLayer.stateMachine, checkedFirst, isOn);
                    if(isMixed)
                        return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// Checks if the avatar descriptor has mixing "Write defaults" settings across its animators.
        /// </summary>
        /// <param name="states">States to check.</param>
        /// <returns>True if the avatar animators contain mixed write defaults, false otherwise.</returns>
        public static bool HaveMixedWriteDefaults(this IEnumerable<WDState> states)
        {
            bool isOn = false;
            bool checkedFirst = false;
            foreach (var state in states)
            {
                if (state.HasDefault)
                {
                    if (state.IsOn ^ state.IsDefaultOn)
                        return true;
                    continue;   
                }

                if (!checkedFirst)
                {
                    checkedFirst = true;
                    isOn = state.IsOn;
                    continue;
                }

                if (state.IsOn != isOn)
                    return true;
            }

            return false;
        }
        
        /// <summary>
        /// Checks if the avatar descriptor has mixing "Write defaults" settings across its animators.
        /// </summary>
        /// <param name="states">States to check.</param>
        /// <returns>True if the avatar animators contain mixed write defaults, false otherwise.</returns>
        public static bool HaveEmpyMotionsInStates(this IEnumerable<WDState> states)
        {
            foreach (var state in states)
                if (!state.HasMotion)
                    return true;

            return false;
        }
        
        /// <summary>
        /// Sets the "Write Defaults" value of all the states in an entire animator controller to true or false.
        /// Will modify the controller directly.
        /// </summary>
        /// <param name="avatar">The avatar to update controllers.</param>
        /// <param name="writeDefaults">The value of "Write Defaults" to set the controller's states to. True if unspecified.</param>
        /// <returns></returns>
        public static void SetWriteDefaults(VRCAvatarDescriptor avatar, bool writeDefaults = true, bool force = false)
        {
            if (avatar == null)
            {
                Debug.LogError("Couldn't set Write Defaults value, the avatar is null!");
                return;
            }
            for (int i = 0; i < avatar.baseAnimationLayers.Length; i++)
            {
                var controller = avatar.baseAnimationLayers[i].animatorController as AnimatorController;
                if(controller != null)
                    SetWriteDefaults(controller, writeDefaults, force);
            }
            for (int i = 0; i < avatar.specialAnimationLayers.Length; i++)
            {
                var controller = avatar.specialAnimationLayers[i].animatorController as AnimatorController;
                if(controller != null)
                    SetWriteDefaults(controller, writeDefaults, force);
            }
        }
        
        
        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Sets the "Write Defaults" value of all the states in an entire animator controller to true or false.
        /// Will modify the controller directly.
        /// </summary>
        /// <param name="controller">The controller to modify.</param>
        /// <param name="writeDefaults">The value of "Write Defaults" to set the controller's states to. True if unspecified.</param>
        /// <returns></returns>
        public static void SetWriteDefaults(AnimatorController controller, bool writeDefaults = true, bool force = false)
        {
            if (controller == null)
            {
                Debug.LogError("Couldn't set Write Defaults value, the controller is null!");
                return;
            }
            for (int i = 0; i < controller.layers.Length; i++)
            {
                SetInStateMachine(controller.layers[i].stateMachine, writeDefaults, force);
            }
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Get all states of an animator controller.
        /// </summary>
        /// <param name="controller">Controller used.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="AnimatorState"/> contained in the given controller.</returns>
        public static IEnumerable<AnimatorState> GetAnimatorStates(this AnimatorController controller)
        {
            var animatorStates = new List<AnimatorState>();
            foreach (var animationLayer in controller.layers)
                animatorStates.AddRange(GetLayerStatesRecursive(animationLayer.stateMachine));

            return animatorStates;
        }
        
        /// <summary>
        /// Get all states of an animator layer
        /// </summary>
        /// <param name="layer">Layer used.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="AnimatorState"/> contained in the given layer.</returns>
        public static IEnumerable<AnimatorState> GetLayerStates(this AnimatorControllerLayer layer)
        {
            return GetLayerStatesRecursive(layer.stateMachine);
        }
        
        /// <summary>
        /// Get animation clip tree from an animator
        /// </summary>
        /// <param name="controller">Animator controller</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of swappable clips</returns>
        public static IEnumerable<ClipSwapItem> GetClipsToSwap(this AnimatorController controller)
        {
            foreach (AnimatorControllerLayer layer in controller.layers)
            foreach (ClipSwapItem item in GetClipsToSwapRecursive(layer.stateMachine, layer.name))
                yield return item;
        }
        
        /// <summary>
        /// Get animation clip tree from an animator layer
        /// </summary>
        /// <param name="layer">Animator layer</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of swappable clips</returns>
        public static IEnumerable<ClipSwapItem> GetClipsToSwap(this AnimatorControllerLayer layer)
        {
            return GetClipsToSwapRecursive(layer.stateMachine, layer.name);
        }

        /// <summary>
        /// Swaps animations in a controller.
        /// </summary>
        /// <param name="controller">Controller to modify.</param>
        /// <param name="items">Clips.</param>
        /// <param name="saveToNew">If to save to a new controller.</param>
        /// <returns>The controller edited (same one if did not save to new, a new one otherwise)</returns>
        public static AnimatorController SwapAnimations(this AnimatorController controller, IEnumerable<ClipSwapItem> items, bool saveToNew = false)
        {
            if (controller == null) return null;

            var assetPath = AssetDatabase.GetAssetPath(controller);

            if (saveToNew)
            {
                Directory.CreateDirectory(AnimatorCloner.STANDARD_NEW_ANIMATOR_FOLDER);
                string uniquePath = AssetDatabase.GenerateUniqueAssetPath(AnimatorCloner.STANDARD_NEW_ANIMATOR_FOLDER + Path.GetFileName(assetPath));
                AssetDatabase.CopyAsset(assetPath, uniquePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                assetPath = uniquePath;
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
            }
            foreach (var clip in items)
                SwapClip(controller, clip);

            return controller;
        }

        /// <summary>
        /// Return the VRC ValueType value based on the type of the animator parameter given.
        /// </summary>
        /// <param name="type">Animator parameter type.</param>
        /// <returns>VRC SDK3 ValueType that corresponds to the given animator ValueType.</returns>
        public static ValueType GetValueTypeFromAnimatorParameterType(AnimatorControllerParameterType type)
        {
            return type == AnimatorControllerParameterType.Int
                ? ValueType.Int
                : (type == AnimatorControllerParameterType.Bool ? ValueType.Bool : ValueType.Float);
        }
        
        private static void AnalyzeWdStateMachine(AnimatorStateMachine stateMachine, List<WDState> states, string layerName)
        {
            foreach (ChildAnimatorState t in stateMachine.states)
            {
                states.Add(new WDState
                {
                    AvatarLayer = layerName,
                    StateName = t.state.name,
                    IsOn = t.state.writeDefaultValues,
                    HasDefault = t.state.name.Contains("(WD On)") || t.state.name.Contains("(WD Off)"),
                    IsDefaultOn = t.state.name.Contains("(WD On)"),
                    HasMotion = t.state.motion != null
                });
            }

            foreach (ChildAnimatorStateMachine t in stateMachine.stateMachines)
                AnalyzeWdStateMachine(t.stateMachine, states, layerName);
        }
        
        private static (bool, bool, bool) GetWdInStateMachine(AnimatorStateMachine stateMachine, bool checkedFirst, bool isOn)
        {
            foreach (ChildAnimatorState t in stateMachine.states)
            {
                if (!checkedFirst)
                {
                    isOn = t.state.writeDefaultValues;
                    checkedFirst = true;
                    continue;
                }

                if (t.state.name.Contains("(WD On)") || t.state.name.Contains("(WD Off)"))
                {
                    if (t.state.writeDefaultValues ^ t.state.name.Contains("(WD On)"))
                        return (true, isOn, true);
                    continue;
                }
                if (isOn != t.state.writeDefaultValues)
                    return (true, isOn, true);
            }

            bool isMixed;
            foreach (ChildAnimatorStateMachine t in stateMachine.stateMachines)
            {
                (checkedFirst, isOn, isMixed) = GetWdInStateMachine(t.stateMachine, checkedFirst, isOn);
                if(isMixed)
                    return (checkedFirst, isOn, true);
            }

            return (checkedFirst, isOn, false);
        }
        
        private static void SetInStateMachine(AnimatorStateMachine stateMachine, bool wd, bool force)
        {
            foreach (ChildAnimatorState t in stateMachine.states) {
                t.state.writeDefaultValues = wd;
                // Force corresponding Write Defaults setting for states with "(WD On)" or "(WD Off)" tags
                if(!force && t.state.name.Contains("(WD On)")) 
                    t.state.writeDefaultValues = true;
                else if(!force && t.state.name.Contains("(WD Off)"))
                    t.state.writeDefaultValues = false;
                else
                    t.state.writeDefaultValues = wd;

                if (t.state.motion == null)
                    t.state.motion = EmptyClip;

            }
            
            foreach (ChildAnimatorStateMachine t in stateMachine.stateMachines)
                SetInStateMachine(t.stateMachine, wd, force);
        }
        
        private static IEnumerable<AnimatorState> GetLayerStatesRecursive(AnimatorStateMachine stateMachine)
        {
            var animatorStates = stateMachine.states
                .Select(t => t.state)
                .ToList();
            foreach (ChildAnimatorStateMachine t in stateMachine.stateMachines)
                animatorStates.AddRange(GetLayerStatesRecursive(t.stateMachine));

            return animatorStates;
        }
        
        private static IEnumerable<ClipSwapItem> GetClipsToSwapRecursive(AnimatorStateMachine stateMachine, string currentPath)
        {
            var items = new List<ClipSwapItem>();
            var animatorStates = stateMachine.states
                .Select(t => t.state)
                .ToList();
            foreach (var state in animatorStates)
            {
                // a switch here would be way more visual bloat ironically
                if (state.motion is AnimationClip clip)
                {
                    var item = new ClipSwapItem
                    {
                        Name = state.name,
                        StatePath = currentPath,
                        Clip = clip
                    };
                    
                    items.Add(item);
                }
                else if (state.motion is BlendTree tree)
                {
                    var item = new ClipSwapItem
                    {
                        IsTree = true,
                        Name = state.name,
                        StatePath = currentPath,
                        Clip = null,
                    };
                    items.Add(item);
                    GetClipsToSwapFromBlendTreeRecursive(tree, currentPath, item);
                }
                else if (state.motion == null)
                {
                    var item = new ClipSwapItem
                    {
                        Name = state.name,
                        StatePath = currentPath,
                        Clip = null
                    };
                    
                    items.Add(item);
                }
            }
            foreach (ChildAnimatorStateMachine t in stateMachine.stateMachines)
                items.AddRange(GetClipsToSwapRecursive(t.stateMachine, $"{currentPath}##{t.stateMachine.name}"));

            return items;
        }

        public static void SwapClip(AnimatorController controller, ClipSwapItem item)
        {
            var pathPieces = item.StatePath.Split(_clipPathSeparators, System.StringSplitOptions.RemoveEmptyEntries);
            if (pathPieces.Length == 0) return;
            var layer = controller.layers.FirstOrDefault(x => x.name.Equals(pathPieces[0]));
            if (layer == null) return;

            AnimatorStateMachine stateMachine = layer.stateMachine;
            for (int i = 1; i < pathPieces.Length; i++)
            {
                stateMachine = stateMachine.stateMachines.Select(t => t.stateMachine).FirstOrDefault(x => x.name.Equals(pathPieces[i]));
                if (stateMachine == null) return;
            }
            
            var state = stateMachine.states.Select(t => t.state).FirstOrDefault(x => x.name.Equals(item.Name));

            if (state == null) return;
            
            if (item.IsTree && state.motion is BlendTree tree)
                SwapTree(tree, item.TreeChildren);
            else if (!item.IsTree)
                state.motion = item.Clip;
        }

        private static void SwapTree(BlendTree tree, List<ClipSwapItem> treeMotions)
        {
            var newChildren = new ChildMotion[tree.children.Length];
            for (int i = 0; i < tree.children.Length; i++)
            {
                ChildMotion child = tree.children[i];
                if (treeMotions[i].IsTree && child.motion is BlendTree childTree)
                    SwapTree(childTree, treeMotions[i].TreeChildren);
                else
                    child.motion = treeMotions[i].Clip;

                newChildren[i] = child;
            }
            tree.children = newChildren;
        }
        
        private static void GetClipsToSwapFromBlendTreeRecursive(BlendTree tree, string statePath, ClipSwapItem parent)
        {
            foreach (var child in tree.children)
            {
                if (child.motion is AnimationClip clip)
                {
                    var item = new ClipSwapItem
                    {
                        Name = tree.blendType == BlendTreeType.Direct || tree.blendType == BlendTreeType.Simple1D
                            ? $"threshold: {child.threshold}"
                            : $"X: {child.position.x}, Y: {child.position.y}",
                        StatePath = statePath,
                        Clip = clip,
                    };
                    
                    parent.TreeChildren.Add(item);
                }
                else if (child.motion is BlendTree childTree)
                {
                    var item = new ClipSwapItem
                    {
                        IsTree = true,
                        Name = childTree.name,
                        StatePath = statePath,
                        Clip = null,
                    };
                    parent.TreeChildren.Add(item);

                    GetClipsToSwapFromBlendTreeRecursive(childTree, statePath, item);
                }
            }
        }
    }
    
    public class ClipSwapItem
    {
        public bool IsTree { get; set; }
        public string StatePath { get; set; }
        public string Name { get; set; }
        public AnimationClip Clip { get; set; }
        
        public List<ClipSwapItem> TreeChildren { get; set; }

        public ClipSwapItem()
        {
            TreeChildren = new List<ClipSwapItem>();
        }
    }
    
    // ReSharper disable once InconsistentNaming
    public struct WDState
    {
        public string AvatarLayer { get; set; }
        public string StateName { get; set; }
        public bool IsOn { get; set; }
        public bool HasDefault { get; set; }
        public bool IsDefaultOn { get; set; }
        public bool HasMotion { get; set; }
    }
}
#endif