using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Object;

#nullable enable

namespace MomomaAssets.GraphView.AssetNodes
{
    [Serializable]
    [CreateElement(typeof(AssetNodesGUI), "Generate/Prefab Variant")]
    sealed class GeneratePrefabVariantNode : INodeProcessor
    {
        GeneratePrefabVariantNode() { }

        public Color HeaderColor => ColorDefinition.ModifyNode;

        public void Initialize(IPortDataContainer portDataContainer)
        {
            portDataContainer.AddInputPort(AssetGroupPortDefinition.Default);
            portDataContainer.AddInputPort(PathDataPortDefinition.Default, "Directory Path");
            portDataContainer.AddInputPort(PathDataPortDefinition.Default, "File Name");
            portDataContainer.AddOutputPort(AssetGroupPortDefinition.Default, "Original");
            portDataContainer.AddOutputPort(AssetGroupPortDefinition.Default, "Variant");
        }

        public void Process(IProcessingDataContainer container)
        {
            var assetGroup = container.GetInput(0, AssetGroupPortDefinition.Default);
            var directoryPathData = container.GetInput(1, PathDataPortDefinition.Default);
            var filePathData = container.GetInput(2, PathDataPortDefinition.Default);
            var variants = new AssetGroup();
            container.SetOutput(0, assetGroup);
            container.SetOutput(1, variants);
            if (assetGroup.Count == 0)
                return;
            using (new AssetModificationScope())
            {
                foreach (var assets in assetGroup)
                {
                    if (assets.MainAsset is GameObject prefab)
                    {
                        var dstPath = Path.ChangeExtension(Path.Combine(directoryPathData.GetPath(assets), filePathData.GetPath(assets)), ".prefab");
                        var directoryPath = Path.GetDirectoryName(dstPath);
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                            AssetDatabase.ImportAsset(directoryPath);
                        }
                        var currentDstPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(dstPath);
                        if (currentDstPrefab == null || PrefabUtility.GetPrefabAssetType(currentDstPrefab) != PrefabAssetType.Variant || PrefabUtility.GetCorrespondingObjectFromOriginalSource(currentDstPrefab) != prefab)
                        {
                            var instance = PrefabUtility.InstantiatePrefab(prefab);
                            try
                            {
                                PrefabUtility.SaveAsPrefabAsset(instance as GameObject, dstPath);
                            }
                            finally
                            {
                                DestroyImmediate(instance);
                            }
                        }
                        variants.Add(new AssetData(dstPath));
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
