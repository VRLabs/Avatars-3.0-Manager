using DreadScripts.Localization;

namespace VRLabs.AV3Manager
{
    public class AV3ManagerLocalization : LocalizationScriptableBase
    {
        public override string hostTitle => "Avatar 3.0 Manager Localization";

        public override KeyCollection[] keyCollections =>
            new[] { new KeyCollection("Avatar 3.0 Manager Localization", typeof(LocalizationKeys)) };

        public enum LocalizationKeys
        {
            TestKey1
        }
    }
}