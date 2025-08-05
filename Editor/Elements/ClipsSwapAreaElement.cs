using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static VRLabs.AV3Manager.AV3ManagerLocalization.Keys;
using DreadScripts.Localization;

namespace VRLabs.AV3Manager
{
	public class ClipsSwapAreaElement : VisualElement
	{
		public Action OnClose { get; set; }

		private List<ClipSwapItem> _animationsToSwap;

		private readonly LocalizationHandler<AV3ManagerLocalization> LocalizationHandler = AV3Manager.LocalizationHandler;

		public ClipsSwapAreaElement(VrcAnimationLayer layer)
		{
			new Label(LocalizationHandler.Get(Clips_SwapMode).text)
				.WithClass("header")
				.ChildOf(this);

			new Label(LocalizationHandler.Get(Clips_DontModifyAnimatorWarning).text)
				.WithClass("warning-label", "bordered-container")
				.WithFontSize(10)
				.ChildOf(this);

			_animationsToSwap = new List<ClipSwapItem>();

			if (layer.Controller != null)
			{
				foreach (var animatorLayer in layer.Controller.layers)
				{
					var clips = animatorLayer.GetClipsToSwap().ToList();

					_animationsToSwap.AddRange(clips);

					var container = new VisualElement()
						.WithClass("bordered-container")
						.ChildOf(this);

					new Label(animatorLayer.name)
						.WithClass("header-small")
						.ChildOf(container);

					var clipsContainer = new VisualElement()
						.WithClass("clips-container")
						.ChildOf(container);
					foreach (ClipSwapItem clip in clips)
					{
						if (clip.IsTree)
						{
							void MakeTree(ClipSwapItem clipSwapItem, VisualElement parentObject)
							{
								var foldout = new Foldout().ChildOf(parentObject);
								foldout.text = clipSwapItem.Name;

								foreach (var child in clipSwapItem.TreeChildren)
								{
									if (child.IsTree)
									{
										MakeTree(child, foldout);
									}
									else
									{
										ObjectField item = FluentUIElements
											.NewObjectField(child.Name, typeof(AnimationClip), child.Clip)
											.ChildOf(foldout);

										item.RegisterValueChangedCallback(x =>
										{
											child.Clip = x.newValue as AnimationClip;
										});
									}
								}
							}

							MakeTree(clip, clipsContainer);
						}
						else
						{
							ObjectField item = FluentUIElements
								.NewObjectField(clip.Name, typeof(AnimationClip), clip.Clip)
								.ChildOf(clipsContainer);

							item.RegisterValueChangedCallback(x => { clip.Clip = x.newValue as AnimationClip; });
						}
					}
				}
			}

			var operationsArea = new VisualElement()
				.WithClass("top-spaced")
				.WithFlexDirection(FlexDirection.Row)
				.ChildOf(this);
			var mergeOnCurrent = FluentUIElements
				.NewButton(LocalizationHandler.Get(Clips_ApplyOnCurrent).text, LocalizationHandler.Get(Clips_ApplyOnCurrent).tooltip)
				.WithClass("grow-control")
				.ChildOf(operationsArea);
			var mergeOnNew = FluentUIElements
				.NewButton(LocalizationHandler.Get(Clips_ApplyOnNew).text, LocalizationHandler.Get(Clips_ApplyOnNew).tooltip)
				.WithClass("grow-control")
				.ChildOf(operationsArea);
			var cancelButton = FluentUIElements
				.NewButton(LocalizationHandler.Get(Clips_Cancel).text, LocalizationHandler.Get(Clips_Cancel).tooltip)
				.WithClass("grow-control")
				.ChildOf(operationsArea);

			cancelButton.clicked += () => OnClose?.Invoke();

			mergeOnCurrent.clicked += () =>
			{
				layer.SetController(layer.Controller.SwapAnimations(_animationsToSwap));
				OnClose?.Invoke();
			};

			mergeOnNew.clicked += () =>
			{
				layer.SetController(layer.Controller.SwapAnimations(_animationsToSwap, true));
				OnClose?.Invoke();
			};
		}
	}
}