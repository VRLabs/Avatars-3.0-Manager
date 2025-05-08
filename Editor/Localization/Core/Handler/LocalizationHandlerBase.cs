using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DreadScripts.Localization
{
	public class LocalizationHandlerBase
	{
		private static readonly Lazy<GUIContent> lazyGlobeIcon = new Lazy<GUIContent>(() =>
			new GUIContent(EditorGUIUtility.IconContent("BuildSettings.Web.Small")) { tooltip = "Language" });

		internal static readonly HashSet<Type> loadedLocalizationTypes = new HashSet<Type>();
		public static GUIContent globeIcon => lazyGlobeIcon.Value;
	}
}