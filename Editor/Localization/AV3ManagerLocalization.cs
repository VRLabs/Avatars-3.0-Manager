using DreadScripts.Localization;

namespace VRLabs.AV3Manager
{
	public class AV3ManagerLocalization : LocalizationScriptableBase
	{
		public override string hostTitle => "Avatar 3.0 Manager Localization";

		public override KeyCollection[] keyCollections =>
			new[] { new KeyCollection("Avatar 3.0 Manager Localization", typeof(Keys)) };

		public enum Keys
		{
			Merger_AnimatorMode,
			Merger_Animator,
			Merger_Parameters,
			Merger_Suffix,
			Merger_SelfMergeWarning,
			Merger_DuplicateParamWarning,
			Merger_BuiltinParamWarning,
			Merger_ParamInDifferentLayerWarning,
			Merger_ClearSuffixes,
			Merger_ParamTypeMismatchWarning,
			Merger_MergeOnCurrent,
			Merger_MergeOnNew,
			Merger_Cancel,
			Clips_SwapMode,
			Clips_DontModifyAnimatorWarning,
			Clips_ApplyOnCurrent,
			Clips_ApplyOnNew,
			Clips_Cancel,
			Layers_Layers,
			Layers_UsedParamMemory,
			Layers_UseCustomLayer,
			Layers_UseDefaultLayer,
			Layers_UseDefaulVRCLayer,
			Layers_Controller,
			Layers_Params,
			Layers_ExprParams,
			Layers_Synced,
			Layers_AddAnimatorToMerge,
			Layers_SwapAnimations,
			Params_Params,
			Params_ExprParams,
			Params_OpenInInspector,
			Params_Name,
			Params_Type,
			Params_Default,
			Params_Saved,
			Params_Synced,
			Params_CopyParams,
			Params_ParamsToCopy,
			Params_CopyParamsButton,
			Params_UnusedExprParamWarning,
			Params_BuiltinInExprParamWarning,
			Params_UsedParamMemory,
			Params_AdditionalCostOfAnimatorToMerge,
			Params_TotalParamMemoryAfterMerge,
			Params_MergeWouldSurpassParamMemoryWarning,
			WD_WD,
			WD_ForceAllWD,
			WD_ForceAllWDWarning,
			WD_DBTWarning,
			WD_IgnoreDBT,
			WD_SetWDOff,
			WD_ForceWD,
			WD_ForceWDOffMessage,
			WD_ForceWDOnMessage,
			WD_Proceed,
			WD_Cancel,
			WD_SetWDOn,
			WD_MixedWDWarning,
			WD_EmptyStatesWarning,
			WD_List,
			WD_State,
			WD_Motion,
			WD_WDOn,
			WD_Default,
			WD_ViewState,
			WD_On,
			WD_Off,
			WD_None,
			WD_View,
			Settings_Settings,
			Settings_HelpMessage,
			Settings_IgnoreParamLimit
		}
	}
}