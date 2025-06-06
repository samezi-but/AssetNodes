using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

#nullable enable

namespace MomomaAssets.GraphView
{
    sealed class DefaultGraphView : UnityEditor.Experimental.GraphView.GraphView
    {
        readonly ISelection m_Selection;
        readonly IGraphViewCallbackReceiver m_GraphViewCallbackReceiver;

        public DefaultGraphView(ISelection selection, IGraphViewCallbackReceiver graphViewCallbackReceiver)
        {
            m_Selection = selection;
            m_GraphViewCallbackReceiver = graphViewCallbackReceiver;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            if (evt.target is Node node)
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Group selection", GroupSelection, GetStatusAddToGroup);
                evt.menu.AppendAction("Remove from group", RemoveFromGroup, GetStatusRemoveFromGroup);
            }
            else
            {
                evt.menu.AppendAction("Add Stack Node", AddStackNode);
            }
        }

        void GroupSelection(DropdownMenuAction action)
        {
            var guids = new List<string>(selection.Count);
            foreach (var i in selection)
                if (i is Node node)
                    guids.Add(node.viewDataKey);
            var data = new GroupData(guids);
            m_GraphViewCallbackReceiver.AddElement(data, action.eventInfo.mousePosition);
        }

        void RemoveFromGroup(DropdownMenuAction action)
        {
            foreach (var i in selection)
            {
                if (i is Node node)
                    node.GetContainingScope()?.RemoveElement(node);
            }
        }

        void AddStackNode(DropdownMenuAction action)
        {
            m_GraphViewCallbackReceiver.AddElement(new StackNodeData(), GUIUtility.GUIToScreenPoint(action.eventInfo.mousePosition));
        }

        DropdownMenuAction.Status GetStatusAddToGroup(DropdownMenuAction action)
        {
            foreach (var i in selection)
            {
                if (i is Node node && node.GetContainingScope() != null)
                {
                    return DropdownMenuAction.Status.Disabled;
                }
            }
            return DropdownMenuAction.Status.Normal;
        }

        DropdownMenuAction.Status GetStatusRemoveFromGroup(DropdownMenuAction action)
        {
            var canRemove = false;
            foreach (var i in selection)
            {
                if (i is Node node)
                    canRemove |= node.GetContainingScope() != null;
            }
            return canRemove ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Hidden;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var linkedPorts = new HashSet<Port>();
            CollectLinkedPorts(startPort, linkedPorts);
            return ports.ToList().FindAll(p => p.direction != startPort.direction && !linkedPorts.Contains(p) && CanConnectPortType(startPort, p));
        }

        public bool CanConnectPortType(Port src, Port dst)
        {
            return (src.direction == Direction.Input ? src.portType.IsAssignableFrom(dst.portType) : dst.portType.IsAssignableFrom(src.portType));
        }

        static void CollectLinkedPorts(Port startPort, HashSet<Port> linkedPorts)
        {
            var ports = startPort.direction == Direction.Input ? startPort.node.outputContainer.Query<Port>().ToList() : startPort.node.inputContainer.Query<Port>().ToList();
            linkedPorts.UnionWith(ports);
            foreach (var port in ports)
            {
                foreach (var e in port.connections)
                {
                    var pair = startPort.direction == Direction.Input ? e.input : e.output;
                    CollectLinkedPorts(pair, linkedPorts);
                }
            }
        }

        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);
            m_Selection.AddToSelection(selectable);
        }

        public override void ClearSelection()
        {
            base.ClearSelection();
            m_Selection.ClearSelection();
        }

        public override void RemoveFromSelection(ISelectable selectable)
        {
            base.RemoveFromSelection(selectable);
            m_Selection.RemoveFromSelection(selectable);
        }
    }
}
