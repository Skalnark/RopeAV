using System;
using System.Text;

namespace RopeAV.Lib.DataStructures;

public sealed class Rope : SplayTree<Rope>
{
	private string _text;
	private int _weight;

	public bool IsLeaf => Left is null && Right is null;
	public int Length => _weight + _text.Length + (Right?.Length ?? 0);

	public Rope(string text)
	{
		_text = text ?? string.Empty;
		_weight = 0;
	}

	private Rope(Rope left, Rope right)
	{
		_text = string.Empty;
		Left = left;
		Right = right;
		left.Parent = this;
		right.Parent = this;
		UpdateWeight();
	}

	public static Rope FromString(string text) => new(text);

	public char this[int index]
	{
		get
		{
			if (index < 0 || index >= Length)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			(Rope leaf, int offset) = FindLeaf(index);
			return leaf._text[offset];
		}
	}

	public static Rope Concat(Rope? left, Rope? right)
	{
		if (left is null) return right ?? new Rope(string.Empty);
		if (right is null) return left;

		left.Parent = null;
		right.Parent = null;
		return new Rope(left, right);
	}

	public Rope Concat(Rope right) => Concat(this, right);

	public (Rope? Left, Rope? Right) Split(int index)
	{
		if (index < 0 || index > Length)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}

		if (index == Length)
		{
			return (this, null);
		}

		(Rope leaf, int offset) = FindLeaf(index);
		Rope? root = leaf.Splay() ?? throw new InvalidOperationException("Rope structure is corrupt.");

		Rope? leftTree = root.Left;
		Rope? rightTree = root.Right;
		leftTree?.Parent = null;
		rightTree?.Parent = null;

		string leftText = root._text[..offset];
		string rightText = root._text[offset..];

		leftTree = leftText.Length == 0
			? leftTree
			: Join(leftTree, new Rope(leftText));

		rightTree = rightText.Length == 0
			? rightTree
			: Join(new Rope(rightText), rightTree);

		return (leftTree, rightTree);
	}

	public Rope Insert(int index, string text)
	{
		(Rope? left, Rope? right) = Split(index);
		return Concat(Concat(left, new Rope(text)), right);
	}

	public Rope Delete(int index, int count)
	{
		if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

		(Rope? left, Rope? rest) = Split(index);
		Rope? right;
		try
		{
			(Rope? _, right) = rest is null ? (null, null) : rest.Split(count);
		}
		catch (ArgumentOutOfRangeException)
		{
			right = null;
		}

		return Concat(left, right);
	}

	public Rope Substring(int index, int length)
	{
		if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
		Rope? middle;
		try
		{
			(Rope? _, Rope? rest) = Split(index);
			(middle, Rope? _) = rest is null ? (null, null) : rest.Split(length);
		}
		catch (ArgumentOutOfRangeException)
		{
			middle = null;
		}
		return middle ?? new Rope(string.Empty);
	}

	public override string ToString()
	{
		var builder = new StringBuilder(Length);
		Traverse(this, builder);
		return builder.ToString();
	}

	private static void Traverse(Rope? node, StringBuilder builder)
	{
		if (node is null) return;
		Traverse(node.Left, builder);
		builder.Append(node._text);
		Traverse(node.Right, builder);
	}

	private (Rope node, int offset) FindLeaf(int index)
	{
		if (index < 0 || index >= Length)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}

		Rope node = this;
		int currentIndex = index;

		while (true)
		{
			int leftLength = node._weight;
			if (currentIndex < leftLength)
			{
				node = node.Left ?? throw new InvalidOperationException("Rope structure is corrupt.");
				continue;
			}

			currentIndex -= leftLength;

			int textLength = node._text.Length;
			if (currentIndex < textLength)
			{
				return (node, currentIndex);
			}

			currentIndex -= textLength;
			node = node.Right ?? throw new InvalidOperationException("Rope structure is corrupt.");
		}
	}

	private void UpdateWeight()
	{
		_weight = Left?.Length ?? 0;
	}

	protected override void UpdateAugmented() => UpdateWeight();

	private static Rope? Join(Rope? left, Rope? right)
	{
		if (left is null) return right;
		if (right is null) return left;

		Rope? max = left;
		while (max.Right is not null)
		{
			max = max.Right;
		}

		max.Splay();
		max.Right = right;
		right.Parent = max;
		max.UpdateWeight();
		return max;
	}
}
