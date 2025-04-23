using System.Collections;
using System.Collections.Generic;
using Godot;

namespace Core.Components;

public static class NodeUtils
{
	public static void RemoveChildren(this Node node)
	{
		foreach (var xNode in node.GetChildren())
		{
			node.RemoveChild(xNode);
		}
	}

	public static void FreeChildren(this Node node)
	{
		foreach (var xNode in node.GetChildren())
		{
			xNode.Free();
		}
	}

	public static void QueueFreeChildren(this Node node)
	{
		foreach (var xNode in node.GetChildren())
		{
			xNode.QueueFree();
		}
	}

	public static void AddChildren(this Node node, params Node[] children)
	{
		foreach (var child in children)
		{
			node.AddChild(child);
		}
	}

	public static void AddChildren(this Node node, IEnumerable<Node> children, bool forceReadableName = false, Node.InternalMode @internal = Node.InternalMode.Disabled)
	{
		foreach (var child in children)
		{
			node.AddChild(child, forceReadableName, @internal);
		}
	}

	public static void AddChildrenDeferred(this Node node, params Node[] children)
	{
		foreach (var child in children)
		{
		  node.CallDeferred(Node.MethodName.AddChild, child);
		}
	}

	public static void AddChildrenDeferred(this Node node, IEnumerable<Node> children, bool forceReadableName = false, Node.InternalMode @internal = Node.InternalMode.Disabled)
	{
		foreach (var child in children)
		{
			node.CallDeferred(Node.MethodName.AddChild, child, forceReadableName, (long)@internal);
		}
	}
}