using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using static UnityEngine.Object;
using UnityObject = UnityEngine.Object;

#nullable enable

namespace MomomaAssets.GraphView.AssetNodes
{
    [Serializable]
    [CreateElement(typeof(AssetNodesGUI), "Modify/Component")]
    sealed class ModifyComponentNode : INodeProcessor, IAdditionalAssetHolder, IPrefabModifier
    {
        sealed class ModifyComponentNodeEditor : INodeProcessorEditor
        {
            [NodeProcessorEditorFactory]
            static void Entry()
            {
                NodeProcessorEditorFactory.EntryEditorFactory<ModifyComponentNode>((data, serializedPropertyList) => new ModifyComponentNodeEditor(serializedPropertyList.GetProcessorProperty()));
            }

            readonly SerializedProperty _IncludeChildrenProperty;
            readonly SerializedProperty _PresetProperty;

            Editor m_CachedEditor;
            string m_OldMenuPath = string.Empty;

            public bool UseDefaultVisualElement => false;

            public ModifyComponentNodeEditor(SerializedProperty processorProperty)
            {
                _IncludeChildrenProperty = processorProperty.FindPropertyRelative(nameof(m_IncludeChildren));
                _PresetProperty = processorProperty.FindPropertyRelative(nameof(m_Preset));
                m_CachedEditor = Editor.CreateEditor(_PresetProperty.objectReferenceValue);
            }

            public void Dispose()
            {
                if (m_CachedEditor != null)
                {
                    DestroyImmediate(m_CachedEditor);
                }
            }

            public void OnGUI()
            {
                EditorGUILayout.PropertyField(_IncludeChildrenProperty);
                string menuPath;
                using (var presetSo = new SerializedObject(_PresetProperty.objectReferenceValue))
                using (var m_NativeTypeIDProperty = presetSo.FindProperty("m_TargetType.m_NativeTypeID"))
                using (var m_ManagedTypePPtrProperty = presetSo.FindProperty("m_TargetType.m_ManagedTypePPtr"))
                    menuPath = (m_ManagedTypePPtrProperty.objectReferenceValue is MonoScript monoScript) ? UnityObjectTypeUtility.GetMenuPath(monoScript) : UnityObjectTypeUtility.GetMenuPath(m_NativeTypeIDProperty.intValue);
                EditorGUI.BeginChangeCheck();
                menuPath = UnityObjectTypeUtility.ComponentTypePopup(menuPath, true);
                if (menuPath != m_OldMenuPath)
                {
                    if (m_CachedEditor != null)
                        DestroyImmediate(m_CachedEditor);
                }
                m_OldMenuPath = menuPath;
                if (EditorGUI.EndChangeCheck())
                {
                    if (UnityObjectTypeUtility.TryGetComponentTypeFromMenuPath(menuPath, out var type))
                    {
                        var go = new GameObject();
                        try
                        {
                            if (!go.TryGetComponent(type, out var comp))
                                comp = go.AddComponent(type);
                            var newPreset = new Preset(comp) { hideFlags = HideFlags.HideInHierarchy };
                            var oldPreset = _PresetProperty.objectReferenceValue;
                            if (AssetDatabase.Contains(oldPreset))
                                AssetDatabase.AddObjectToAsset(newPreset, AssetDatabase.GetAssetPath(oldPreset));
                            Undo.RegisterCreatedObjectUndo(newPreset, nameof(ModifyComponentNode));
                            _PresetProperty.objectReferenceValue = newPreset;
                            _PresetProperty.serializedObject.ApplyModifiedProperties();
                            Undo.DestroyObjectImmediate(oldPreset);
                        }
                        finally
                        {
                            DestroyImmediate(go);
                        }
                    }
                }
                Editor.CreateCachedEditor(_PresetProperty.objectReferenceValue, null, ref m_CachedEditor);
                m_CachedEditor.OnInspectorGUI();
            }
        }

        public bool IncludeChildren => m_IncludeChildren;
        public string RegexPattern => m_RegexPattern;

        ModifyComponentNode() { }

        [SerializeField]
        bool m_IncludeChildren = false;
        [SerializeField]
        string m_RegexPattern = string.Empty;
        [SerializeField]
        Preset? m_Preset = null;

        public Color HeaderColor => ColorDefinition.ModifyNode;

        public IEnumerable<UnityObject> Assets
        {
            get
            {
                if (m_Preset == null)
                {
                    var go = new GameObject();
                    try
                    {
                        m_Preset = new Preset(go.transform);
                        m_Preset.hideFlags = HideFlags.HideInHierarchy;
                    }
                    finally
                    {
                        DestroyImmediate(go);
                    }
                }
                return new UnityObject[] { m_Preset };
            }
        }

        public void OnClone()
        {
            foreach (Preset i in this.CloneAssets())
                m_Preset = i;
        }

        public void Initialize(IPortDataContainer portDataContainer)
        {
            portDataContainer.AddInputPort(AssetGroupPortDefinition.Default);
            portDataContainer.AddOutputPort(AssetGroupPortDefinition.Default);
        }

        public void Process(IProcessingDataContainer container)
        {
            var assetGroup = container.GetInput(0, AssetGroupPortDefinition.Default);
            if (m_Preset != null)
                this.ModifyPrefab(assetGroup);
            container.SetOutput(0, assetGroup);
        }

        public void Modify(GameObject go)
        {
            foreach (var component in go.GetComponents<Component>())
                if (m_Preset!.CanBeAppliedTo(component))
                    m_Preset.ApplyTo(component);
        }

        public T DoFunction<T>(IFunctionContainer<INodeProcessor, T> function)
        {
            return function.DoFunction(this);
        }
    }
}
