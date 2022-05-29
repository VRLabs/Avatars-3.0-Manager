using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace VRLabs.AV3Manager
{
    public static class FluentUIElements
    {
        public static Button NewButton(string text, string tooltip, Action action)
        {
            var button = new Button(action);
            button.text = text;
            button.tooltip = tooltip;
            return button;
        }
        
        public static Button NewButton(string text, string tooltip = "") => NewButton(text, tooltip, null);
        public static Button NewButton(Action action) => NewButton(null, null, action);
        public static Button NewButton() => NewButton(null, null, null);

        public static ObjectField NewObjectField(string label, Type type = null, UnityEngine.Object value = null)
        {
            var objectField = new ObjectField(label);
            objectField.objectType = type;
            objectField.value = value;
            return objectField;
        }
        
        public static Toggle NewToggle(string label, string tooltip, bool value)
        {
            var toggle = new Toggle(label);
            toggle.tooltip = tooltip;
            toggle.value = value;
            return toggle;
        }
        
        public static Toggle NewToggle(string text, string tooltip = "") => NewToggle(text, tooltip, false);
        public static Toggle NewToggle(string text, bool value) => NewToggle(text, "", value);
        public static Toggle NewToggle(bool value) => NewToggle("", "", value);

        public static IntegerField NewIntField(string label, string tooltip, int value, int maxLength = -1)
        {
            var field = new IntegerField(label, maxLength);
            field.tooltip = tooltip;
            field.value = value;
            return field;
        }
        
        public static IntegerField NewIntField(int value, int maxLength = -1)
        {
            var field = new IntegerField(maxLength);
            field.value = value;
            return field;
        }

        public static IntegerField NewIntField(string label, int value, int maxLength = -1) => NewIntField(label, "", value, maxLength);
        
        public static FloatField NewFloatField(string label, string tooltip, float value, int maxLength = -1)
        {
            var field = new FloatField(label, maxLength);
            field.tooltip = tooltip;
            field.value = value;
            return field;
        }
        
        public static FloatField NewFloatField(float value, int maxLength = -1)
        {
            var field = new FloatField(maxLength);
            field.value = value;
            return field;
        }

        public static FloatField NewFloatField(string label, float value, int maxLength = -1) => NewFloatField(label, "", value, maxLength);
        
        public static T WithName<T>(this T control, string name) where T : VisualElement
        {
            control.name = name;
            return control;
        }

        public static T WithClass<T>(this T control, string className) where T : VisualElement
        {
            control.AddToClassList(className);
            return control;
        }
        
        public static T WithEnabledState<T>(this T control, bool enabled) where T : VisualElement
        {
            control.SetEnabled(enabled);
            return control;
        }
        
        public static T WithClass<T>(this T control, params string[] classNames) where T : VisualElement
        {
            foreach (var className in classNames)
                control.AddToClassList(className);
            return control;
        }
        
        public static T WithFlexDirection<T>(this T control, StyleEnum<FlexDirection> direction) where T : VisualElement
        {
            control.style.flexDirection = direction;
            return control;
        }
        
        public static T WithFlexGrow<T>(this T control, StyleFloat grow) where T : VisualElement
        {
            control.style.flexGrow = grow;
            return control;
        }
        
        public static T WithFlexShrink<T>(this T control, StyleFloat shrink) where T : VisualElement
        {
            control.style.flexGrow = shrink;
            return control;
        }
        
        public static T WithFlexBasis<T>(this T control, StyleLength basis) where T : VisualElement
        {
            control.style.flexBasis = basis;
            return control;
        }
        
        public static T WithFlex<T>(this T control, StyleFloat grow, StyleFloat shrink, StyleLength basis) where T : VisualElement
        {
            control.style.flexGrow = grow;
            control.style.flexShrink = shrink;
            control.style.flexBasis = basis;
            return control;
        }
        
        public static T WithFontSize<T>(this T control, StyleLength fontSize) where T : VisualElement
        {
            control.style.fontSize = fontSize;
            return control;
        }
        
        public static T WithDisplay<T>(this T control, StyleEnum<DisplayStyle> display) where T : VisualElement
        {
            control.style.display = display;
            return control;
        }
        
        public static T WithWidth<T>(this T control, StyleLength width) where T : VisualElement
        {
            control.style.width = width;
            return control;
        }
        
        public static T WithHeight<T>(this T control, StyleLength height) where T : VisualElement
        {
            control.style.height = height;
            return control;
        }
        
        public static T WithMargin<T>(this T control, StyleLength marginLeft, StyleLength marginTop, StyleLength marginRight, StyleLength marginBottom) where T : VisualElement
        {
            control.style.marginBottom = marginBottom;
            control.style.marginLeft = marginLeft;
            control.style.marginRight = marginRight;
            control.style.marginTop = marginTop;
            return control;
        }
        
        public static T WithMargin<T>(this T control, StyleLength marginLeftRight, StyleLength marginTopBottom) where T : VisualElement
            => control.WithMargin(marginLeftRight, marginTopBottom, marginLeftRight, marginTopBottom);
        
        public static T WithMargin<T>(this T control, StyleLength margin) where T : VisualElement
            => control.WithMargin(margin, margin, margin, margin);

        public static T WithPadding<T>(this T control, StyleLength paddingLeft, StyleLength paddingTop, StyleLength paddingRight, StyleLength paddingBottom) where T : VisualElement
        {
            control.style.paddingBottom = paddingBottom;
            control.style.paddingLeft = paddingLeft;
            control.style.paddingRight = paddingRight;
            control.style.paddingTop = paddingTop;
            return control;
        }
        
        public static T WithPadding<T>(this T control, StyleLength paddingLeftRight, StyleLength paddingTopBottom) where T : VisualElement
            => control.WithPadding(paddingLeftRight, paddingTopBottom, paddingLeftRight, paddingTopBottom);
        
        public static T WithPadding<T>(this T control, StyleLength padding) where T : VisualElement
            => control.WithPadding(padding, padding, padding, padding);

        public static T WithWhiteSpace<T>(this T control, StyleEnum<WhiteSpace> whitespace) where T : VisualElement
        {
            control.style.whiteSpace = whitespace;
            return control;
        }
        
        public static T WithAlignSelf<T>(this T control, StyleEnum<Align> align) where T : VisualElement
        {
            control.style.alignSelf = align;
            return control;
        }
        
        public static T WithAlignContent<T>(this T control, StyleEnum<Align> align) where T : VisualElement
        {
            control.style.alignContent = align;
            return control;
        }
        
        public static T WithAlignItems<T>(this T control, StyleEnum<Align> align) where T : VisualElement
        {
            control.style.alignItems = align;
            return control;
        }
        public static T WithUnityTextAlign<T>(this T control, StyleEnum<TextAnchor> align) where T : VisualElement
        {
            control.style.unityTextAlign = align;
            return control;
        }

        public static T ChildOf<T>(this T control, VisualElement parent) where T : VisualElement
        {
            parent.Add(control);
            return control;
        }
        
    }
}