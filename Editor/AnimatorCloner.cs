#if VRC_SDK_VRCSDK3
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace VRLabs.AV3Manager
{
    public static class AnimatorCloner
    {

        public const string STANDARD_NEW_ANIMATOR_FOLDER = "Assets/VRLabs/GeneratedAssets/Animators/";
        public const string STANDARD_NEW_PARAMASSET_FOLDER = "Assets/VRLabs/GeneratedAssets/ExpressionParameters/";
        public const string STANDARD_NEW_MENUASSET_FOLDER = "Assets/VRLabs/GeneratedAssets/ExpressionMenu/";
        private static Dictionary<string, string> _parametersNewName;
        private static string _assetPath;

        public static AnimatorController MergeControllers(AnimatorController mainController, AnimatorController controllerToMerge, Dictionary<string, string> paramNameSwap = null, bool saveToNew = false)
        {
            if (mainController == null)
            {
                return null;
            }

            _parametersNewName = paramNameSwap ?? new Dictionary<string, string>();
            _assetPath = AssetDatabase.GetAssetPath(mainController);

            if (saveToNew)
            {
                Directory.CreateDirectory(STANDARD_NEW_ANIMATOR_FOLDER);
                string uniquePath = AssetDatabase.GenerateUniqueAssetPath(STANDARD_NEW_ANIMATOR_FOLDER + Path.GetFileName(_assetPath));
                AssetDatabase.CopyAsset(_assetPath, uniquePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                _assetPath = uniquePath;
                mainController = AssetDatabase.LoadAssetAtPath<AnimatorController>(_assetPath);
            }

            if (controllerToMerge == null)
            {
                return mainController;
            }

            foreach (var p in controllerToMerge.parameters)
            {
                var newP = new AnimatorControllerParameter
                {
                    name = GetNewParameterNameIfSwapped(p.name),
                    type = p.type,
                    defaultBool = p.defaultBool,
                    defaultFloat = p.defaultFloat,
                    defaultInt = p.defaultInt
                };
                if (mainController.parameters.Count(x => x.name.Equals(newP.name)) == 0)
                {
                    mainController.AddParameter(newP);
                }
            }

            for (int i = 0; i < controllerToMerge.layers.Length; i++)
            {
                AnimatorControllerLayer newL = CloneLayer(controllerToMerge.layers[i], i == 0);
                newL.name = mainController.MakeUniqueLayerName(newL.name);// MakeLayerNameUnique(newL.name, mainController);
                newL.stateMachine.name = newL.name;
                mainController.AddLayer(newL);
            }

            EditorUtility.SetDirty(mainController);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return mainController;
        }

        private static string GetNewParameterNameIfSwapped(string parameterName) => 
            !string.IsNullOrWhiteSpace(parameterName) && _parametersNewName.ContainsKey(parameterName) ? _parametersNewName[parameterName] : parameterName;

        private static string MakeLayerNameUnique(string name, AnimatorController controller)
        {
            string st = "";
            int c = 0;
            bool combinedNameDuplicate = controller.layers.Count(x => x.name.Equals(name)) > 0;
            while (combinedNameDuplicate)
            {
                c++;
                combinedNameDuplicate = controller.layers.Count(x => x.name.Equals(name + st + c)) > 0;
            }
            if (c != 0)
            {
                st += c;
            }

            return name + st;
        }

        private static AnimatorControllerLayer CloneLayer(AnimatorControllerLayer old, bool isFirstLayer = false)
        {
            var n = new AnimatorControllerLayer
            {
                avatarMask = old.avatarMask,
                blendingMode = old.blendingMode,
                defaultWeight = isFirstLayer ? 1f : old.defaultWeight,
                iKPass = old.iKPass,
                name = old.name,
                syncedLayerAffectsTiming = old.syncedLayerAffectsTiming,
                stateMachine = CloneStateMachine(old.stateMachine)
            };
            CloneTransitions(old.stateMachine, n.stateMachine);
            return n;
        }

        private static AnimatorStateMachine CloneStateMachine(AnimatorStateMachine old)
        {
            var n = new AnimatorStateMachine
            {
                anyStatePosition = old.anyStatePosition,
                entryPosition = old.entryPosition,
                exitPosition = old.exitPosition,
                hideFlags = old.hideFlags,
                name = old.name,
                parentStateMachinePosition = old.parentStateMachinePosition,
                stateMachines = old.stateMachines.Select(x => CloneChildStateMachine(x)).ToArray(),
                states = old.states.Select(x => CloneChildAnimatorState(x)).ToArray()
            };
            
            AssetDatabase.AddObjectToAsset(n, _assetPath);
            n.defaultState = FindState(old.defaultState, old, n);

            foreach (var oldb in old.behaviours)
            {
                var behaviour = n.AddStateMachineBehaviour(oldb.GetType());
                CloneBehaviourParameters(oldb, behaviour);
            }
            return n;
        }

        private static ChildAnimatorStateMachine CloneChildStateMachine(ChildAnimatorStateMachine old)
        {
            var n = new ChildAnimatorStateMachine
            {
                position = old.position,
                stateMachine = CloneStateMachine(old.stateMachine)
            };
            return n;
        }

        private static ChildAnimatorState CloneChildAnimatorState(ChildAnimatorState old)
        {
            var n = new ChildAnimatorState
            {
                position = old.position,
                state = CloneAnimatorState(old.state)
            };
            foreach (var oldb in old.state.behaviours)
            {
                var behaviour = n.state.AddStateMachineBehaviour(oldb.GetType());
                CloneBehaviourParameters(oldb, behaviour);
            }
            return n;
        }

        private static AnimatorState CloneAnimatorState(AnimatorState old)
        {
            // Checks if the motion is a blend tree, to avoid accidental blend tree sharing between animator assets
            Motion motion = old.motion;
            if (motion is BlendTree oldTree)
            {
                var tree = CloneBlendTree(null, oldTree);
                motion = tree;
                // need to save the blend tree into the animator
                tree.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(motion, _assetPath);
            }

            var n = new AnimatorState
            {
                cycleOffset = old.cycleOffset,
                cycleOffsetParameter = GetNewParameterNameIfSwapped(old.cycleOffsetParameter),
                cycleOffsetParameterActive = old.cycleOffsetParameterActive,
                hideFlags = old.hideFlags,
                iKOnFeet = old.iKOnFeet,
                mirror = old.mirror,
                mirrorParameter = GetNewParameterNameIfSwapped(old.mirrorParameter),
                mirrorParameterActive = old.mirrorParameterActive,
                motion = motion,
                name = old.name,
                speed = old.speed,
                speedParameter = GetNewParameterNameIfSwapped(old.speedParameter),
                speedParameterActive = old.speedParameterActive,
                tag = old.tag,
                timeParameter = GetNewParameterNameIfSwapped(old.timeParameter),
                timeParameterActive = old.timeParameterActive,
                writeDefaultValues = old.writeDefaultValues
            };
            AssetDatabase.AddObjectToAsset(n, _assetPath);
            return n;
        }

        // Taken from here: https://gist.github.com/phosphoer/93ca8dcbf925fc006e4e9f6b799c13b0
        private static BlendTree CloneBlendTree(BlendTree parentTree, BlendTree oldTree)
        {
            // Create a child tree in the destination parent, this seems to be the only way to correctly 
            // add a child tree as opposed to AddChild(motion)
            BlendTree pastedTree = new BlendTree();
            pastedTree.name = oldTree.name;
            pastedTree.blendType = oldTree.blendType;
            pastedTree.blendParameter = GetNewParameterNameIfSwapped(oldTree.blendParameter);
            pastedTree.blendParameterY = GetNewParameterNameIfSwapped(oldTree.blendParameterY);
            pastedTree.minThreshold = oldTree.minThreshold;
            pastedTree.maxThreshold = oldTree.maxThreshold;
            pastedTree.useAutomaticThresholds = oldTree.useAutomaticThresholds;

            // Recursively duplicate the tree structure
            // Motions can be directly added as references while trees must be recursively to avoid accidental sharing
            foreach (var child in oldTree.children)
            {
                var children = pastedTree.children;

                var childMotion = new ChildMotion
                {
                    timeScale = child.timeScale,
                    position = child.position,
                    cycleOffset = child.cycleOffset,
                    mirror = child.mirror,
                    threshold = child.threshold,
                    directBlendParameter = GetNewParameterNameIfSwapped(child.directBlendParameter)
                };

                if (child.motion is BlendTree tree)
                {
                    var childTree = CloneBlendTree(pastedTree, tree);
                    childMotion.motion = childTree;
                    // need to save the blend tree into the animator
                    childTree.hideFlags = HideFlags.HideInHierarchy;
                    AssetDatabase.AddObjectToAsset(childTree, _assetPath);
                }
                else
                {
                    childMotion.motion = child.motion;
                }
                
                ArrayUtility.Add(ref children, childMotion);
                pastedTree.children = children;
            }

            return pastedTree;
        }

        private static void CloneBehaviourParameters(StateMachineBehaviour old, StateMachineBehaviour n)
        {
            if (old.GetType() != n.GetType())
            {
                throw new ArgumentException("2 state machine behaviours that should be of the same type are not.");
            }
            switch (n)
            {
                case VRCAnimatorLayerControl l:
                    {
                        var o = old as VRCAnimatorLayerControl;
                        l.ApplySettings = o.ApplySettings;
                        l.blendDuration = o.blendDuration;
                        l.debugString = o.debugString;
                        l.goalWeight = o.goalWeight;
                        l.layer = o.layer;
                        l.playable = o.playable;
                        break;
                    }
                case VRCAnimatorLocomotionControl l:
                    {
                        var o = old as VRCAnimatorLocomotionControl;
                        l.ApplySettings = o.ApplySettings;
                        l.debugString = o.debugString;
                        l.disableLocomotion = o.disableLocomotion;
                        break;
                    }
                case VRCAnimatorTemporaryPoseSpace l:
                    {
                        var o = old as VRCAnimatorTemporaryPoseSpace;
                        l.ApplySettings = o.ApplySettings;
                        l.debugString = o.debugString;
                        l.delayTime = o.delayTime;
                        l.enterPoseSpace = o.enterPoseSpace;
                        l.fixedDelay = o.fixedDelay;
                        break;
                    }
                case VRCAnimatorTrackingControl l:
                    {
                        var o = old as VRCAnimatorTrackingControl;
                        l.ApplySettings = o.ApplySettings;
                        l.debugString = o.debugString;
                        l.trackingEyes = o.trackingEyes;
                        l.trackingHead = o.trackingHead;
                        l.trackingHip = o.trackingHip;
                        l.trackingLeftFingers = o.trackingLeftFingers;
                        l.trackingLeftFoot = o.trackingLeftFoot;
                        l.trackingLeftHand = o.trackingLeftHand;
                        l.trackingMouth = o.trackingMouth;
                        l.trackingRightFingers = o.trackingRightFingers;
                        l.trackingRightFoot = o.trackingRightFoot;
                        l.trackingRightHand = o.trackingRightHand;
                        break;
                    }
                case VRCAvatarParameterDriver l:
                    {
                        var d = old as VRCAvatarParameterDriver;
                        l.debugString = d.debugString;
                        l.localOnly = d.localOnly;
                        l.isLocalPlayer = d.isLocalPlayer;
                        l.initialized = d.initialized;
                        l.parameters = d.parameters.ConvertAll(p =>
                        {
                            string name = GetNewParameterNameIfSwapped(p.name);
                            return new VRC_AvatarParameterDriver.Parameter 
                            { 
                                name = name, 
                                value = p.value, 
                                chance = p.chance, 
                                valueMin = p.valueMin, 
                                valueMax = p.valueMax, 
                                type = p.type, 
                                source = GetNewParameterNameIfSwapped(p.source), 
                                convertRange = p.convertRange, 
                                destMax = p.destMax, 
                                destMin = p.destMin, 
                                destParam = p.destParam, 
                                sourceMax = p.sourceMax, 
                                sourceMin = p.sourceMin, 
                                sourceParam = p.sourceParam
                            };
                        });
                        break;
                    }
                case VRCPlayableLayerControl l:
                    {
                        var o = old as VRCPlayableLayerControl;
                        l.ApplySettings = o.ApplySettings;
                        l.blendDuration = o.blendDuration;
                        l.debugString = o.debugString;
                        l.goalWeight = o.goalWeight;
                        l.layer = o.layer;
                        l.outputParamHash = o.outputParamHash;
                        break;
                    }
            }
        }

        private static AnimatorState FindState(AnimatorState original, AnimatorStateMachine old, AnimatorStateMachine n)
        {
            AnimatorState[] oldStates = GetStatesRecursive(old).ToArray();
            AnimatorState[] newStates = GetStatesRecursive(n).ToArray();
            for (int i = 0; i < oldStates.Length; i++)
                if (oldStates[i] == original)
                    return newStates[i];
            
            return null;
        }

        private static AnimatorStateMachine FindStateMachine(AnimatorStateTransition transition, AnimatorStateMachine sm)
        {
            AnimatorStateMachine[] childrenSm = sm.stateMachines.Select(x => x.stateMachine).ToArray();
            var dstSm = Array.Find(childrenSm, x => x.name == transition.destinationStateMachine.name);
            if (dstSm != null)
                return dstSm;

            foreach (var childSm in childrenSm)
            {
                dstSm = FindStateMachine(transition, childSm);
                if (dstSm != null)
                    return dstSm;
            }

            return null;
        }

        private static List<AnimatorState> GetStatesRecursive(AnimatorStateMachine sm)
        {
            List<AnimatorState> childrenStates = sm.states.Select(x => x.state).ToList();
            foreach (var child in sm.stateMachines.Select(x => x.stateMachine))
                childrenStates.AddRange(GetStatesRecursive(child));

            return childrenStates;
        }

        private static List<AnimatorStateMachine> GetStateMachinesRecursive(AnimatorStateMachine sm,
            IDictionary<AnimatorStateMachine, AnimatorStateMachine> newAnimatorsByChildren = null)
        {
            List<AnimatorStateMachine> childrenSm = sm.stateMachines.Select(x => x.stateMachine).ToList();

            List<AnimatorStateMachine> gcsm = new List<AnimatorStateMachine>();
            gcsm.Add(sm);
            foreach (var child in childrenSm)
            {
                newAnimatorsByChildren?.Add(child, sm);
                gcsm.AddRange(GetStateMachinesRecursive(child, newAnimatorsByChildren));
            }
            
            return gcsm;
        }

        private static AnimatorState FindMatchingState(List<AnimatorState> old, List<AnimatorState> n, AnimatorTransitionBase transition)
        {
            for (int i = 0; i < old.Count; i++)
                if (transition.destinationState == old[i])
                    return n[i];

            return null;
        }
        
        private static AnimatorStateMachine FindMatchingStateMachine(List<AnimatorStateMachine> old, List<AnimatorStateMachine> n, AnimatorTransitionBase transition)
        {
            for (int i = 0; i < old.Count; i++)
                if (transition.destinationStateMachine == old[i])
                    return n[i];

            return null;
        }

        private static void CloneTransitions(AnimatorStateMachine old, AnimatorStateMachine n)
        {
            List<AnimatorState> oldStates = GetStatesRecursive(old);
            List<AnimatorState> newStates = GetStatesRecursive(n);
            var newAnimatorsByChildren = new Dictionary<AnimatorStateMachine, AnimatorStateMachine>();
            var oldAnimatorsByChildren = new Dictionary<AnimatorStateMachine, AnimatorStateMachine>();
            List<AnimatorStateMachine> oldStateMachines = GetStateMachinesRecursive(old, oldAnimatorsByChildren);
            List<AnimatorStateMachine> newStateMachines = GetStateMachinesRecursive(n, newAnimatorsByChildren);
            // Generate state transitions
            for (int i = 0; i < oldStates.Count; i++)
            {
                foreach (var transition in oldStates[i].transitions)
                {
                    AnimatorStateTransition newTransition = null;
                    if (transition.isExit && transition.destinationState == null && transition.destinationStateMachine == null)
                    {
                        newTransition = newStates[i].AddExitTransition();
                    }
                    else if (transition.destinationState != null)
                    {
                        var dstState = FindMatchingState(oldStates, newStates, transition);
                        if (dstState != null)
                            newTransition = newStates[i].AddTransition(dstState);
                    }
                    else if (transition.destinationStateMachine != null)
                    {
                        var dstState = FindMatchingStateMachine(oldStateMachines, newStateMachines, transition);
                        if (dstState != null)
                            newTransition = newStates[i].AddTransition(dstState);
                    }

                    if (newTransition != null)
                        ApplyTransitionSettings(transition, newTransition);
                }
            }
            
            for (int i = 0; i < oldStateMachines.Count; i++)
            {
                if(oldAnimatorsByChildren.ContainsKey(oldStateMachines[i]) && newAnimatorsByChildren.ContainsKey(newStateMachines[i]))
                {
                    foreach (var transition in oldAnimatorsByChildren[oldStateMachines[i]].GetStateMachineTransitions(oldStateMachines[i]))
                    {
                        AnimatorTransition newTransition = null;
                        if (transition.isExit && transition.destinationState == null && transition.destinationStateMachine == null)
                        {
                            newTransition = newAnimatorsByChildren[newStateMachines[i]].AddStateMachineExitTransition(newStateMachines[i]);
                        }
                        else if (transition.destinationState != null)
                        {
                            var dstState = FindMatchingState(oldStates, newStates, transition);
                            if (dstState != null)
                                newTransition = newAnimatorsByChildren[newStateMachines[i]].AddStateMachineTransition(newStateMachines[i], dstState);
                        }
                        else if (transition.destinationStateMachine != null)
                        {
                            var dstState = FindMatchingStateMachine(oldStateMachines, newStateMachines, transition);
                            if (dstState != null)
                                newTransition = newAnimatorsByChildren[newStateMachines[i]].AddStateMachineTransition(newStateMachines[i], dstState);
                        }

                        if (newTransition != null)
                            ApplyTransitionSettings(transition, newTransition);
                    }
                }
                // Generate AnyState transitions
                GenerateStateMachineBaseTransitions(oldStateMachines[i], newStateMachines[i], oldStates, newStates, oldStateMachines, newStateMachines);
            }
        }

        private static void GenerateStateMachineBaseTransitions(AnimatorStateMachine old, AnimatorStateMachine n, List<AnimatorState> oldStates,
            List<AnimatorState> newStates, List<AnimatorStateMachine> oldStateMachines, List<AnimatorStateMachine> newStateMachines)
        {
            foreach (var transition in old.anyStateTransitions)
            {
                AnimatorStateTransition newTransition = null;
                if (transition.destinationState != null)
                {
                    var dstState = FindMatchingState(oldStates, newStates, transition);
                    if (dstState != null)
                        newTransition = n.AddAnyStateTransition(dstState);
                }
                else if (transition.destinationStateMachine != null)
                {
                    var dstState = FindMatchingStateMachine(oldStateMachines, newStateMachines, transition);
                    if (dstState != null)
                        newTransition = n.AddAnyStateTransition(dstState);
                }

                if (newTransition != null)
                    ApplyTransitionSettings(transition, newTransition);
            }

            // Generate EntryState transitions
            foreach (var transition in old.entryTransitions)
            {
                AnimatorTransition newTransition = null;
                if (transition.destinationState != null)
                {
                    var dstState = FindMatchingState(oldStates, newStates, transition);
                    if (dstState != null)
                        newTransition = n.AddEntryTransition(dstState);
                }
                else if (transition.destinationStateMachine != null)
                {
                    var dstState = FindMatchingStateMachine(oldStateMachines, newStateMachines, transition);
                    if (dstState != null)
                        newTransition = n.AddEntryTransition(dstState);
                }

                if (newTransition != null)
                    ApplyTransitionSettings(transition, newTransition);
            }
        }

        private static void ApplyTransitionSettings(AnimatorStateTransition transition, AnimatorStateTransition newTransition)
        {
            newTransition.canTransitionToSelf = transition.canTransitionToSelf;
            newTransition.duration = transition.duration;
            newTransition.exitTime = transition.exitTime;
            newTransition.hasExitTime = transition.hasExitTime;
            newTransition.hasFixedDuration = transition.hasFixedDuration;
            newTransition.hideFlags = transition.hideFlags;
            newTransition.isExit = transition.isExit;
            newTransition.mute = transition.mute;
            newTransition.name = transition.name;
            newTransition.offset = transition.offset;
            newTransition.interruptionSource = transition.interruptionSource;
            newTransition.orderedInterruption = transition.orderedInterruption;
            newTransition.solo = transition.solo;
            foreach (var condition in transition.conditions)
                newTransition.AddCondition(condition.mode, condition.threshold, GetNewParameterNameIfSwapped(condition.parameter));
            
        }

        private static void ApplyTransitionSettings(AnimatorTransition transition, AnimatorTransition newTransition)
        {
            newTransition.hideFlags = transition.hideFlags;
            newTransition.isExit = transition.isExit;
            newTransition.mute = transition.mute;
            newTransition.name = transition.name;
            newTransition.solo = transition.solo;
            foreach (var condition in transition.conditions)
                newTransition.AddCondition(condition.mode, condition.threshold, GetNewParameterNameIfSwapped(condition.parameter));
            
        }
    }
}

#endif