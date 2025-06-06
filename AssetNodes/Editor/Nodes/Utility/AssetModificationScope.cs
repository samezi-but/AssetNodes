using System;
using UnityEditor;

#nullable enable

namespace MomomaAssets.GraphView.AssetNodes
{
    class AssetModificationScope : IDisposable
    {
        public AssetModificationScope()
        {
            AssetDatabase.StartAssetEditing();
        }

        void IDisposable.Dispose()
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }
    }
}
