#if VRC_SDK_VRCSDK3
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace VRLabs.AV3Manager
{
    public enum ParameterState
    {
        Unique,
        Default,
        Duplicate
    }

    public class AnimatorToMerge
    {
        // Controller to merge
        public AnimatorController Controller;
        //List of parameters of the animator to merge
        public List<(AnimatorControllerParameter, string, ParameterState)> Parameters = new List<(AnimatorControllerParameter, string, ParameterState)>();

        private readonly AV3ManagerWindow _window;

        // UI text
        private static class Content
        {
            public static GUIContent Controller = new GUIContent("Controller", "Controller to merge.");
            public static GUIContent ParameterLabel = new GUIContent("Parameter                           Suffix");
            public static GUIContent InstanceParameter = new GUIContent("Instance");
            public static string DefaultWarning = "Default value contains custom suffix, if you don't have a specific reason to do it you should avoid it.";
            public static string DuplicateWarning = "The name + suffix are already an used parameter in at least 1 layer, when merging this parameter will be merged with the one already available";
        }

        // Constructor
        public AnimatorToMerge(AnimatorController controller, AV3ManagerWindow window)
        {
            Controller = controller;
            _window = window;
            UpdateParameterList();
        }

        //Draw UI
        public void DrawMergingAnimator()
        {
            EditorGUI.BeginChangeCheck();
            Controller = (AnimatorController)EditorGUILayout.ObjectField(Content.Controller, Controller, typeof(AnimatorController), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (Controller == null)
                {
                    Parameters.Clear();
                }
                {
                    UpdateParameterList();
                }
            }
            if (Controller != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Content.ParameterLabel, EditorStyles.miniBoldLabel);
                //EditorGUILayout.LabelField(Content.InstanceParameter, EditorStyles.miniBoldLabel, GUILayout.Width(62));
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2);
                for (int i = 0; i < Parameters.Count; i++)
                {
                    (AnimatorControllerParameter p, string s, ParameterState b) = Parameters[i];
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    s = EditorGUILayout.TextField(p.name, s, GUILayout.MinWidth(10));
                    //b = EditorGUILayout.Toggle(b, GUILayout.Width(36));
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (b != ParameterState.Default)
                        {
                            b = _window.IsParameterDuplicate(p.name + s) ? ParameterState.Duplicate : ParameterState.Unique;
                        }
                        Parameters[i] = (p, s, b);
                    }
                    EditorGUILayout.EndHorizontal();

                    switch (b)
                    {
                        case ParameterState.Default:
                            if (!string.IsNullOrEmpty(s))
                            {
                                EditorGUILayout.HelpBox(Content.DefaultWarning, MessageType.Warning);
                            }
                            break;
                        case ParameterState.Duplicate:
                            EditorGUILayout.HelpBox(Content.DuplicateWarning, MessageType.Warning);
                            break;
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        // Update list of parameters
        public void UpdateParameterList()
        {
            Parameters = new List<(AnimatorControllerParameter, string, ParameterState)>();
            if (Controller != null)
            {
                foreach (var p in Controller.parameters)
                {
                    ParameterState s = ParameterState.Unique;
                    string st = "";
                    if (AV3ManagerWindow.VrcParameters.Count(x => x.Equals(p.name)) > 0)
                    {
                        s = ParameterState.Default;
                    }
                    else if (_window.IsParameterDuplicate(p.name))
                    {
                        s = ParameterState.Duplicate;
                        int c = 0;
                        bool combinedNameDuplicate = true;
                        while (combinedNameDuplicate)
                        {
                            c++;
                            combinedNameDuplicate = _window.IsParameterDuplicate(p.name + st + c);
                        }
                        s = ParameterState.Unique;
                        if (c != 0)
                        {
                            st += c;
                        }
                    }
                    Parameters.Add((p, st, s));
                }
            }
        }

        // Gets a Dictionary of the parameters with the original parameter name as key and new parameter name as value
        public Dictionary<string, string> GetParameterMergingDictionary()
        {
            Dictionary<string, string> para = new Dictionary<string, string>();
            foreach ((AnimatorControllerParameter p, string s, ParameterState _) in Parameters)
            {
                para.Add(p.name, p.name + s);
            }
            return para;
        }

        // Clear data of the animator to merge
        public void Clear()
        {
            Controller = null;
            Parameters.Clear();
        }
    }
}

#endif