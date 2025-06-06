using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

#nullable enable

namespace MomomaAssets.GraphView.AssetNodes
{
    [Serializable]
    [CreateElement(typeof(AssetNodesGUI), "Clean up/Prefab")]
    sealed class CleanUpPrefabNode : INodeProcessor
    {
        CleanUpPrefabNode() { }

        public Color HeaderColor => ColorDefinition.CleanupNode;

        public void Initialize(IPortDataContainer portDataContainer)
        {
            portDataContainer.AddInputPort(AssetGroupPortDefinition.Default);
        }

        public void Process(IProcessingDataContainer container)
        {
            var assetGroup = container.GetInput(0, AssetGroupPortDefinition.Default);
            if (assetGroup.Count == 0)
                return;
            using (new AssetModificationScope())
            {
                foreach (var asset in assetGroup)
                {
                    var prefabHash = new HashSet<GameObject>();
                    foreach (var go in asset.GetAssetsFromType<GameObject>())
                    {
                        var root = PrefabUtility.GetNearestPrefabInstanceRoot(go);
                        if (prefabHash.Add(root))
                        {
                            var oldModifications = PrefabUtility.GetPropertyModifications(root);
                            if (oldModifications != null)
                            {
                                var modifications = new List<PropertyModification>(oldModifications);
                                for (var i = modifications.Count - 1; i >= 0; --i)
                                {
                                    if (modifications[i].target == null)
                                    {
                                        modifications.RemoveAt(i);
                                    }
                                }
                                if (oldModifications.Length != modifications.Count)
                                    PrefabUtility.SetPropertyModifications(root, modifications.ToArray());
                            }
                        }
                    }
                    foreach (var director in asset.GetAssetsFromType<PlayableDirector>())
                    {
                        if (director != null)
                        {
                            using (var so = new SerializedObject(director))
                            using (var m_SceneBindings = so.FindProperty("m_SceneBindings"))
                            {
                                var bindings = new List<SerializedProperty>();
                                if (director.playableAsset == null)
                                    break;
                                for (var i = 0; i < m_SceneBindings.arraySize; ++i)
                                    bindings.Add(m_SceneBindings.GetArrayElementAtIndex(i));
                                var notToDeleteProps = new HashSet<SerializedProperty>();
                                foreach (var binding in director.playableAsset.outputs)
                                {
                                    if (binding.sourceObject == null)
                                        continue;
                                    foreach (var prop in bindings)
                                    {
                                        if (prop.FindPropertyRelative("key").objectReferenceValue == binding.sourceObject)
                                        {
                                            notToDeleteProps.Add(prop);
                                            break;
                                        }
                                    }
                                }
                                for (var i = bindings.Count - 1; i >= 0; --i)
                                {
                                    if (!notToDeleteProps.Contains(bindings[i]))
                                        bindings[i].DeleteCommand();
                                }
                                so.ApplyModifiedPropertiesWithoutUndo();
                            }
                        }
                    }
                }
            }
        }

        public T DoFunction<T>(IFunctionContainer<INodeProcessor, T> function)
        {
            return function.DoFunction(this);
        }
    }
}
