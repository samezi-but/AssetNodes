using System;
using System.Collections.Generic;
using UnityEditor;

#nullable enable

namespace MomomaAssets.GraphView
{
    interface IGraphElementEditor : IDisposable
    {
        bool UseDefaultVisualElement { get; }
        void OnGUI();
    }

    sealed class DefaultGraphElementEditor : IGraphElementEditor
    {
        readonly IReadOnlyList<SerializedProperty> _Properties;

        public bool UseDefaultVisualElement => true;

        public DefaultGraphElementEditor(SerializedProperty property)
        {
            var properties = new List<SerializedProperty>();
            using (var endProperty = property.GetEndProperty(false))
            {
                if (property.NextVisible(true))
                {
                    while (true)
                    {
                        if (SerializedProperty.EqualContents(property, endProperty))
                            break;
                        properties.Add(property.Copy());
                        if (!property.NextVisible(false))
                            break;
                    }
                }
            }
            _Properties = properties;
        }

        void IDisposable.Dispose() { }

        public void OnGUI()
        {
            foreach (var property in _Properties)
                EditorGUILayout.PropertyField(property, true);
        }
    }
}
