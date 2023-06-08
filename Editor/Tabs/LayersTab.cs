using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using static VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;

namespace VRLabs.AV3Manager
{
    public class LayersTab : IAV3ManagerTab
    {
        public VisualElement TabContainer { get; set; }
        public string TabName { get; set; }
        public Texture2D TabIcon { get; set; }

        private Label _label;
        
        public LayersTab()
        {
            TabContainer = new VisualElement();
            TabName = "Layers";

            TabIcon = EditorGUIUtility.IconContent("AnimatorController Icon").image as Texture2D;
        }
        public void UpdateTab(VRCAvatarDescriptor avatar)
        {
            TabContainer = null;
            if (avatar == null) return;
            
            TabContainer = new VisualElement();
            
            _label = new Label()
                .WithClass("bordered-container", "margin-normal")
                .WithFontSize(10)
                .ChildOf(TabContainer);

            for (int i = 0; i < avatar.baseAnimationLayers.Length; i++)
                TabContainer.Add(new AnimatorLayerTabElement(new VrcAnimationLayer(avatar, i), this));
                
            for (int i = 0; i < avatar.specialAnimationLayers.Length; i++)
                TabContainer.Add(new AnimatorLayerTabElement(new VrcAnimationLayer(avatar, i, true), this));
            
            UpdateLabel(avatar.expressionParameters.CalcTotalCost());
        }
        
        public void UpdateLabel(int currentCount)
        {
            if (_label == null) return;
            _label.text = $"Parameters memory used: {currentCount}/{VRCExpressionParameters.MAX_PARAMETER_COST}";
        }
    }
    

    public class AnimatorLayerTabElement : VisualElement
    {
        private Action _onAvatarAnimatorChange;
        
        private bool _isTabOpen;
        public AnimatorLayerTabElement(VrcAnimationLayer layer, LayersTab tab)
        {
            var titleTab = new Button().WithClass("layer-tab-header");
            titleTab.style.flexDirection = FlexDirection.Row;
            
            var arrow = new VisualElement().WithClass("layer-tab-arrow");
            titleTab.Add(arrow);
            var title = new Label(layer.Layer.type.ToString()).WithClass("layer-tab-title");
            titleTab.Add(title);
            Add(titleTab);
            var content = new VisualElement().WithClass("layer-tab-content")
                .WithDisplay(_isTabOpen ? DisplayStyle.Flex : DisplayStyle.None);
            Add(content);

            var defaultLayerArea = new VisualElement()
                .WithFlexDirection(FlexDirection.Row);
            var newLayerButton = FluentUIElements
                .NewButton("Use Custom Animator Layer", "Use your own animator for this layer.")
                .WithClass("grow-control");
            defaultLayerArea.Add(newLayerButton);
            var copyFromDefaultLayer = FluentUIElements
                .NewButton("Use Default Layer as custom", "Use a copy of the default layer for this layer.")
                .WithClass("grow-control");
            defaultLayerArea.Add(copyFromDefaultLayer);

            var animatorArea = new VisualElement();
            var useDefaultLayer = FluentUIElements
                .NewButton("Use Default VRC Layer", "DefaultLayerButton");
            var layerAnimator = FluentUIElements
                .NewObjectField("Controller", typeof(AnimatorController), layer.Layer.animatorController)
                .WithClass("top-spaced");

            var paramHeader = new VisualElement()
                .WithFlexDirection(FlexDirection.Row);
            var labelHeader = new Label("Parameters").WithClass("header-small").WithFlex(2, 0, 2);
            var expressionCheckboxHeader = new Label("Expression Parameter").WithClass("header-small").WithFlex(2, 0, 2).WithUnityTextAlign(TextAnchor.MiddleCenter);
            var syncedCheckboxHeader = new Label("Synced").WithClass("header-small").WithFlex(1, 0, 1).WithUnityTextAlign(TextAnchor.MiddleCenter);;
            paramHeader.Add(labelHeader);
            paramHeader.Add(expressionCheckboxHeader);
            paramHeader.Add(syncedCheckboxHeader);
            
            var parametersArea = new VisualElement();

            void UpdateParams()
            {
                parametersArea.Clear();
                foreach (var parameter in layer.Parameters)
                {
                    var param = new VisualElement().ChildOf(parametersArea).WithFlexDirection(FlexDirection.Row);
                    var label = new Label(parameter.Parameter.name).ChildOf(param).WithFlex(2, 0, 2);
                    label.style.overflow = Overflow.Hidden;
                    var expressionCheckboxDiv = new VisualElement().ChildOf(param).WithFlex(2, 0, 2).WithAlignItems(Align.Center);
                    var expressionCheckbox = FluentUIElements.NewToggle(parameter.IsExpression).ChildOf(expressionCheckboxDiv);
                    var syncedCheckboxDiv = new VisualElement().ChildOf(param).WithFlex(1, 0, 1).WithAlignItems(Align.Center);
                    var syncedCheckbox = FluentUIElements.NewToggle(parameter.IsSynced).ChildOf(syncedCheckboxDiv);
                    int totalCostIfExpressionAndSynced = layer.ExpressionParametersCost + VRCExpressionParameters.TypeCost(AV3ManagerFunctions.GetValueTypeFromAnimatorParameterType(parameter.Parameter.type));
                    
                    bool canExpressionAndSync = totalCostIfExpressionAndSynced <= VRCExpressionParameters.MAX_PARAMETER_COST;
                    expressionCheckbox.RegisterValueChangedCallback(x =>
                    {
                        layer.ToggleParameterSync(parameter, x.newValue, parameter.IsSynced);
                        UpdateParams();
                        tab.UpdateLabel(layer.ExpressionParametersCost);
                    });

                    syncedCheckbox.SetEnabled(parameter.IsExpression && (canExpressionAndSync || parameter.IsSynced));
                    syncedCheckbox.RegisterValueChangedCallback(x =>
                    {
                        layer.ToggleParameterSync(parameter, parameter.IsExpression, x.newValue);
                        UpdateParams();
                        tab.UpdateLabel(layer.ExpressionParametersCost);
                    });

                }
            }

            UpdateParams();

            var endArea = new VisualElement();

            var operationsArea = new VisualElement()
                .WithFlexDirection(FlexDirection.Row);
            var animatorToMergeButton = FluentUIElements
                .NewButton("Add animator to merge", "Select animator to merge to the current layer animator.")
                .WithClass("grow-control")
                .ChildOf(operationsArea);
            var swapAnimationsButton = FluentUIElements
                .NewButton("Swap Animations", "Swap animations in the current animator.")
                .WithClass("grow-control")
                .ChildOf(operationsArea);

            animatorArea.Add(useDefaultLayer);
            animatorArea.Add(layerAnimator);
            animatorArea.Add(paramHeader);
            animatorArea.Add(parametersArea);
            animatorArea.Add(endArea);
            endArea.Add(operationsArea);

            content.Add(layer.Layer.isDefault ? defaultLayerArea : animatorArea);
            
            layerAnimator.RegisterValueChangedCallback(x =>
            {
                layer.SetController(x.newValue as AnimatorController);
                UpdateParams();
            });

            _onAvatarAnimatorChange += () => layerAnimator.value = layer.Controller;


            newLayerButton.clicked += () =>
            {
                layer.SetDefault(false);
                content.Clear();
                content.Add(animatorArea);
            };
            
            copyFromDefaultLayer.clicked += () =>
            {
                layer.SetDefault(false);
                content.Clear();
                content.Add(animatorArea);
                Directory.CreateDirectory(AnimatorCloner.STANDARD_NEW_ANIMATOR_FOLDER);
                string uniquePath = AssetDatabase.GenerateUniqueAssetPath(AnimatorCloner.STANDARD_NEW_ANIMATOR_FOLDER + Path.GetFileName(AV3ManagerFunctions.DefaultControllersPath[layer.Layer.type]));
                AssetDatabase.CopyAsset(AV3ManagerFunctions.DefaultControllersPath[layer.Layer.type], uniquePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                layer.SetController(AssetDatabase.LoadAssetAtPath<AnimatorController>(uniquePath));
                _onAvatarAnimatorChange?.Invoke();
            };
            
            useDefaultLayer.clicked += () =>
            {
                layer.SetDefault(true);
                content.Clear();
                content.Add(defaultLayerArea);
            };
            
            titleTab.clicked += () =>
            {
                _isTabOpen = !_isTabOpen;
                if (_isTabOpen)
                {
                    arrow.AddToClassList("layer-tab-arrow-on");
                    content.style.display = DisplayStyle.Flex;
                }
                else
                {
                    arrow.RemoveFromClassList("layer-tab-arrow-on");
                    content.style.display = DisplayStyle.None;
                }
            };

            animatorToMergeButton.clicked += () =>
            {
                endArea.Clear();
                var merger = new AnimatorMergerElement(layer).ChildOf(endArea);

                merger.OnClose += () =>
                {
                    merger.OnClose = null;
                    endArea.Clear();
                    endArea.Add(operationsArea);
                    UpdateParams();
                    _onAvatarAnimatorChange?.Invoke();
                };
            };

            swapAnimationsButton.clicked += () =>
            {
                endArea.Clear();
                var swapper = new ClipsSwapAreaElement(layer).ChildOf(endArea);
                
                swapper.OnClose += () =>
                {
                    swapper.OnClose = null;
                    endArea.Clear();
                    endArea.Add(operationsArea);
                    UpdateParams();
                    _onAvatarAnimatorChange?.Invoke();
                };

            };

        }
    }

    public class VrcAnimationLayer
    {
        private VRCAvatarDescriptor _avatar;
        private CustomAnimLayer[] _layerArray;
        private CustomAnimLayer[] _offLayerArray;
        private int _index;
        private VRCExpressionParameters _expressionParameters;
        public CustomAnimLayer Layer => _layerArray[_index];
        public int ExpressionParametersCost => _expressionParameters.CalcTotalCost();
        
        public AnimatorControllerParameter[] AvatarParameters => _layerArray.Concat(_offLayerArray)
            .Select(x => x.animatorController as AnimatorController)
            .Where(x => x != null)
            .SelectMany(x => x.parameters).ToArray();
        
        public List<SyncedParameter> Parameters { get; set; }

        public AnimatorController Controller => Layer.animatorController as AnimatorController;

        public VrcAnimationLayer(VRCAvatarDescriptor avatar, int index, bool useSpecialLayers = false)
        {
            _avatar = avatar;
            _layerArray = useSpecialLayers ? _avatar.specialAnimationLayers : _avatar.baseAnimationLayers;
            _offLayerArray = useSpecialLayers ? _avatar.baseAnimationLayers : _avatar.specialAnimationLayers;
            _index = index;
            _expressionParameters = _avatar.expressionParameters;
            
            Parameters = new List<SyncedParameter>();
            UpdateParameters();
        }

        public void SetDefault(bool value)
        {
            _layerArray[_index].isDefault = value;

            if (value)
            {
                _layerArray[_index].animatorController = null;
            }
        }

        [Obsolete("ToggleParameterSync(SyncedParameter, bool) is deprecated and will be removed in the future. Please move to using the ToggleParameterSync(SyncedParameter, bool, bool) method instead.")]
        public void ToggleParameterSync(SyncedParameter parameter, bool toggle)
        {
            ToggleParameterSync(parameter, toggle, toggle);    
        }


        public void ToggleParameterSync(SyncedParameter parameter, bool isExpression, bool synced)
        {
            if (_expressionParameters == null) return;

            var index = Parameters.IndexOf(parameter);

            parameter.IsExpression = isExpression;
            parameter.IsSynced = isExpression ? synced : false;
            Parameters[index] = parameter;
            
            var expParam =_expressionParameters.FindParameter(parameter.Parameter.name);

            var paramType = AV3ManagerFunctions.GetValueTypeFromAnimatorParameterType(parameter.Parameter.type);
            
            if (parameter.IsExpression && 
                _expressionParameters.CalcTotalCost() + VRCExpressionParameters.TypeCost(paramType) <= VRCExpressionParameters.MAX_PARAMETER_COST || !parameter.IsSynced)
            {
                // Add parameter if it doesnt exist.
                if (expParam == null)
                {

                    int count = _expressionParameters.parameters.Length;
                    VRCExpressionParameters.Parameter[] parameterArray =
                        new VRCExpressionParameters.Parameter[count + 1];
                    for (int i = 0; i < count; i++)
                    {
                        parameterArray[i] = _expressionParameters.GetParameter(i);
                    }

                    parameterArray[count] = new VRCExpressionParameters.Parameter
                    {
                        name = parameter.Parameter.name,
                        valueType = paramType,
                        networkSynced = parameter.IsSynced,
                        defaultValue = 0,
                        saved = false
                    };
                    _expressionParameters.parameters = parameterArray;
                }
                else
                {
                    var parameterArray = _expressionParameters.parameters;
                    int ind = Array.IndexOf(parameterArray, expParam);
                    expParam.networkSynced = parameter.IsSynced;
                    parameterArray[ind] = expParam;
                    _expressionParameters.parameters = parameterArray;
                }
            }
            else if (!parameter.IsExpression && expParam != null)
            {
                List<VRCExpressionParameters.Parameter> list = new List<VRCExpressionParameters.Parameter>();
                foreach (VRCExpressionParameters.Parameter x in _expressionParameters.parameters)
                {
                    if (x != expParam) list.Add(x);
                }

                _expressionParameters.parameters = list.ToArray();
            }
            
            EditorUtility.SetDirty(_expressionParameters);
            //AssetDatabase.SaveAssets();
            //AssetDatabase.Refresh();
        }

        public void SetController(AnimatorController controller)
        {
            if (controller == null) return;
            _layerArray[_index].isDefault = false;
            _layerArray[_index].animatorController = controller;
            
            UpdateParameters();
            
            EditorUtility.SetDirty(_avatar);
        }

        private void UpdateParameters()
        {
            Parameters.Clear();

            if (Layer.animatorController is AnimatorController animator)
            {
                foreach (var parameter in animator.parameters.Where(x =>
                             x.type == AnimatorControllerParameterType.Int ||
                             x.type == AnimatorControllerParameterType.Float ||
                             x.type == AnimatorControllerParameterType.Bool))
                {
                    if (AV3Manager.VrcParameters.Count(x => x.Equals(parameter.name)) > 0) continue;
                    bool isExpression = _expressionParameters != null && _expressionParameters.FindParameter(parameter.name) != null;
                    bool isSynced = isExpression
                        ? _expressionParameters.FindParameter(parameter.name).networkSynced
                        : false;
                    Parameters.Add(new SyncedParameter { Parameter = parameter, IsExpression = isExpression, IsSynced = isSynced});
                }
            }
        }
    }

    public struct SyncedParameter
    {
        public AnimatorControllerParameter Parameter;
        public bool IsSynced;
        public bool IsExpression;
    }
    
}