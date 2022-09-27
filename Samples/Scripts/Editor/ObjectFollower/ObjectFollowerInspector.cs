using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Sxm.SpatialPartitionStructures.Sample
{
    [CustomEditor(typeof(ObjectFollower))]
    public class ObjectFollowerInspector : Editor
    {
        private ObjectFollower _self;
        private Button _followToButton, _stopFollowingButton;
        private VisualElement _followingStateIcon;
        
        private void OnEnable()
        {
            _self = target as ObjectFollower;
        }

        public override VisualElement CreateInspectorGUI()
        {
            VisualTreeAsset uiAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Sxm.SpatialPartitionStructures/Example/Scripts/Editor/ObjectFollower/ObjectFollowerInspector.uxml");
            VisualElement ui = uiAsset.CloneTree();

            VisualElement script = ui.Query<PropertyField>("script").First();
            script.SetEnabled(false);

            _followingStateIcon = ui.Query("following-state-icon").First();
            _followToButton = ui.Query<Button>("follow-to-button").First();
            _stopFollowingButton = ui.Query<Button>("stop-following-button").First();

            _followToButton.clicked += () =>
            {
                _self.StartFollowing();
                UpdateFollowingState();
            };
            _stopFollowingButton.clicked += () =>
            {
                _self.StopFollowing();
                UpdateFollowingState();
            };

            UpdateFollowingState();
            ui.Query<PropertyField>("selected-object").First()
                .RegisterValueChangeCallback((evt) => UpdateFollowingState());

            return ui;
        }

        private void UpdateFollowingState()
        {
            bool isObjectSet = serializedObject.FindProperty("selectedObject").objectReferenceValue != null;
            
            _followToButton.SetEnabled(isObjectSet && _self.IsFollowing == false);
            _stopFollowingButton.SetEnabled(isObjectSet && _self.IsFollowing);

            const string followingEnabledClass = "following-enabled";
            const string followingDisabledClass = "following-disabled";
            
            if (isObjectSet && _self.IsFollowing)
            {
                AddClassInsteadOf(followingEnabledClass, followingDisabledClass);
            }
            else
            {
                AddClassInsteadOf(followingDisabledClass, followingEnabledClass);
            }
        }

        private void AddClassInsteadOf(string addedClass, string insteadOfClass)
        {
            if (_followingStateIcon.ClassListContains(addedClass))
            {
                return;
            }

            _followingStateIcon.RemoveFromClassList(insteadOfClass);
            _followingStateIcon.AddToClassList(addedClass);
        }
    }    
}
