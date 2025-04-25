using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;

namespace VRLabs.AV3Manager
{
    [TabGroup(1)]
    public class SettingsTab : IAV3ManagerTab
    {
        public VisualElement TabContainer { get; set; }
        public string TabName { get; set; }
        public Texture2D TabIcon { get; set; }

        public SettingsTab()
        {
            TabContainer = new VisualElement();
            TabName = "Settings";
            TabIcon = EditorGUIUtility.IconContent("d_SettingsIcon@2x").image as Texture2D;
        }
        
        public void UpdateTab(VRCAvatarDescriptor avatar)
        {
        }
    }
}