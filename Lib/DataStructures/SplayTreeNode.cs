using System;

namespace RopeAV.Lib.DataStructures;

public abstract class SplayTree<TNode> where TNode : SplayTree<TNode>
{
	public TNode? Left { get; protected set; }
	public TNode? Right { get; protected set; }
	public TNode? Parent { get; protected set; }

	protected TNode This => (TNode)this;

	public TNode Splay()
	{
		TNode node = This;

		while (node.Parent is not null)
		{
			TNode parent = node.Parent;
			TNode? grand = parent.Parent;

			if (grand is null)
			{
				if (ReferenceEquals(parent.Left, node))
				{
					parent.RotateRight();
				}
				else
				{
					parent.RotateLeft();
				}
			}
			else if (ReferenceEquals(grand.Left, parent) && ReferenceEquals(parent.Left, node))
			{
				grand.RotateRight();
				parent.RotateRight();
			}
			else if (ReferenceEquals(grand.Right, parent) && ReferenceEquals(parent.Right, node))
			{
				grand.RotateLeft();
				parent.RotateLeft();
			}
			else if (ReferenceEquals(grand.Left, parent) && ReferenceEquals(parent.Right, node))
			{
				parent.RotateLeft();
				grand.RotateRight();
			}
			else
			{
				parent.RotateRight();
				grand.RotateLeft();
			}
		}

		return node;
	}

	public void RotateLeft()
	{
		TNode pivot = Right ?? throw new InvalidOperationException("Cannot rotate left without right child.");
		Right = pivot.Left;
		pivot.Left?.Parent = This;

		pivot.Parent = Parent;
		if (Parent is not null)
		{
			if (ReferenceEquals(Parent.Left, This))
			{
				Parent.Left = pivot;
			}
			else
			{
				Parent.Right = pivot;
			}
		}

		pivot.Left = This;
		Parent = pivot;

		UpdateAugmented();
		pivot.UpdateAugmented();
	}

	public void RotateRight()
	{
		TNode pivot = Left ?? throw new InvalidOperationException("Cannot rotate right without left child.");
		Left = pivot.Right;
		pivot.Right?.Parent = This;

		pivot.Parent = Parent;
		if (Parent is not null)
		{
			if (ReferenceEquals(Parent.Left, This))
			{
				Parent.Left = pivot;
			}
			else
			{
				Parent.Right = pivot;
			}
		}

		pivot.Right = This;
		Parent = pivot;

		UpdateAugmented();
		pivot.UpdateAugmented();
	}

	protected abstract void UpdateAugmented();
}
