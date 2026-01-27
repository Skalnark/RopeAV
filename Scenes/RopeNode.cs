using System;
using Godot;
using System.Collections.Generic;
using RopeAV.Lib.DataStructures;

namespace RopeAV;

public partial class RopeNode : Node2D
{
	private readonly Dictionary<Rope, Vector2> _positions = new();
	private readonly Dictionary<Rope, float> _widths = new();
	private readonly Dictionary<Rope, int> _leafStartIndex = new();
	private readonly Dictionary<Rope, Node2D> _nodeInstances = new();
	private Rope? _rope;
	private float _currentX;
	private int _runningIndex;
	private bool _layoutDirty = true;
	private PackedScene? _leafCharScene;
	private readonly List<Node2D> _leafInstances = new();
	private Node2D? _treeContainer;
	private Node2D? _rootInstance;

	private const float VerticalSpacing = 110f;
	private const float NodeWidth = 130f;
	private const float NodeHeight = 64f;
	private readonly Vector2 _origin = new(120f, 180f);
	private const float NodeGap = 40f;
	private const float CharSlotWidth = 26f;
	private const float LeafPadding = 24f;

	public override void _Ready()
	{
		_leafCharScene = ResourceLoader.Load<PackedScene>("res://Scenes/LeafChar.tscn");
		_treeContainer = new Node2D { Name = "TreeGraph" };
		AddChild(_treeContainer);
	}

	public void SetRope(Rope? rope)
	{
		_rope = rope;
		_layoutDirty = true;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_rope is null)
		{
			return;
		}

		EnsureLayout();
		DrawEdges(_rope);
		DrawNodes();
	}

	private float ComputeLayout(Rope node, int depth)
	{
		float centerX;
		if (node.IsLeaf)
		{
			int len = node.TextSegment.Length;
			float totalWidth = len * CharSlotWidth;
			float contentWidth = Math.Max(NodeWidth, totalWidth + LeafPadding * 2f);
			float width = contentWidth + NodeGap;
			centerX = _currentX + width * 0.5f;
			_positions[node] = new Vector2(centerX, depth * VerticalSpacing);
			_widths[node] = contentWidth;
			_leafStartIndex[node] = _runningIndex;
			_runningIndex += len;
			_currentX += width;
			return width;
		}

		float leftWidth = 0f;
		float rightWidth = 0f;
		Vector2? leftPos = null;
		Vector2? rightPos = null;

		if (node.Left is not null)
		{
			leftWidth = ComputeLayout(node.Left, depth + 1);
			leftPos = _positions[node.Left];
		}

		if (node.Right is not null)
		{
			rightWidth = ComputeLayout(node.Right, depth + 1);
			rightPos = _positions[node.Right];
		}

		float widthCombined = (node.Left is not null && node.Right is not null)
			? leftWidth + rightWidth
			: MathF.Max(NodeWidth + NodeGap, leftWidth + rightWidth);

		if (leftPos is not null && rightPos is not null)
		{
			centerX = (leftPos.Value.X + rightPos.Value.X) * 0.5f;
		}
		else if (leftPos is not null)
		{
			centerX = leftPos.Value.X;
		}
		else if (rightPos is not null)
		{
			centerX = rightPos.Value.X;
		}
		else
		{
			float width = NodeWidth + NodeGap;
			centerX = _currentX + width * 0.5f;
			_currentX += width;
			widthCombined = width;
		}

		_positions[node] = new Vector2(centerX, depth * VerticalSpacing);
		_widths[node] = NodeWidth;
		return widthCombined;
	}

	private void EnsureLayout()
	{
		if (!_layoutDirty || _rope is null) return;
		_positions.Clear();
		_widths.Clear();
		_leafStartIndex.Clear();
		_currentX = 0f;
		_runningIndex = 0;
		ComputeLayout(_rope, 0);
		RebuildNodeInstances();
		RebuildLeafInstances();
		_layoutDirty = false;
	}

	private void ClearLeafInstances()
	{
		foreach (Node2D inst in _leafInstances)
		{
			inst.QueueFree();
		}
		_leafInstances.Clear();
	}

	private void ClearNodeInstances()
	{
		_nodeInstances.Clear();
		_rootInstance = null;
		if (_treeContainer is null) return;
		foreach (Node child in _treeContainer.GetChildren())
		{
			child.QueueFree();
		}
	}

	private void RebuildNodeInstances()
	{
		ClearNodeInstances();
		if (_treeContainer is null) return;

		int idx = 0;
		foreach (KeyValuePair<Rope, Vector2> pair in _positions)
		{
			Rope node = pair.Key;
			Node2D inst = new Node2D();
			inst.Name = node.IsLeaf ? $"LeafNode_{idx}" : $"Node_{idx}";
			inst.Position = WithOffset(pair.Value);
			_treeContainer.AddChild(inst);
			_nodeInstances[node] = inst;
			if (node == _rope)
			{
				_rootInstance = inst;
			}
			idx++;
		}
	}

	private void RebuildLeafInstances()
	{
		ClearLeafInstances();
		if (_leafCharScene is null || _rope is null) return;

		foreach (KeyValuePair<Rope, Vector2> pair in _positions)
		{
			Rope node = pair.Key;
			if (!node.IsLeaf) continue;

			int startIndex = _leafStartIndex.TryGetValue(node, out int s) ? s : 0;
			Node? parent = _nodeInstances.TryGetValue(node, out Node2D instNode) ? instNode : _treeContainer;
			Vector2 center = parent is null ? WithOffset(pair.Value) : Vector2.Zero;
			string text = node.TextSegment;
			int len = text.Length;
			float totalWidth = len * CharSlotWidth;
			float start = -totalWidth * 0.5f + CharSlotWidth * 0.5f;
			for (int i = 0; i < len; i++)
			{
				Node inst = _leafCharScene.Instantiate();
				if (inst is LeafChar lc)
				{
					lc.Configure(text[i].ToString(), startIndex + i);
				}
				if (inst is Node2D n2d)
				{
					n2d.Position = center + new Vector2(start + i * CharSlotWidth, -NodeHeight * 0.05f);
					parent?.AddChild(n2d);
					_leafInstances.Add(n2d);
				}
			}

			if (len == 0)
			{
				Node inst = _leafCharScene.Instantiate();
				if (inst is LeafChar lc)
				{
					lc.Configure("", startIndex);
				}
				if (inst is Node2D n2d)
				{
					n2d.Position = center;
					parent?.AddChild(n2d);
					_leafInstances.Add(n2d);
				}
			}
		}
	}

	private void DrawEdges(Rope node)
	{
		if (node.Left is not null)
		{
			DrawEdge(node, node.Left);
			DrawEdges(node.Left);
		}

		if (node.Right is not null)
		{
			DrawEdge(node, node.Right);
			DrawEdges(node.Right);
		}
	}

	private void DrawEdge(Rope parent, Rope child)
	{
		DrawLine(WithOffset(_positions[parent]), WithOffset(_positions[child]), Colors.SlateGray, 2f);
	}

	private void DrawNodes()
	{
		Font font = ThemeDB.FallbackFont;
		int fontSize = ThemeDB.FallbackFontSize;

		foreach (KeyValuePair<Rope, Vector2> pair in _positions)
		{
			Rope node = pair.Key;
			Vector2 pos = WithOffset(pair.Value);
			float width = _widths.TryGetValue(node, out float w) ? w : NodeWidth;

			Rect2 rect = new(pos - new Vector2(width * 0.5f, NodeHeight * 0.5f), new Vector2(width, NodeHeight));
			Color fill = node.IsLeaf ? new Color(0.27f, 0.45f, 0.68f) : new Color(0.20f, 0.23f, 0.29f);
			DrawRect(rect, fill);
			DrawRect(rect, Colors.LightGray, false, 2f);

			string lengthText = $"len={node.Length}";
			Vector2 lengthPos = new(rect.Position.X + 8f, rect.Position.Y + NodeHeight * 0.35f);
			DrawString(font, lengthPos, lengthText, alignment: HorizontalAlignment.Left, width: width - 16f, fontSize: fontSize - 2, modulate: new Color(0.85f, 0.9f, 0.95f));

			if (node.IsLeaf)
			{
				string label = $"leaf ({node.TextSegment.Length})";
				Vector2 textPos = new(rect.Position.X + 8f, rect.Position.Y + NodeHeight * 0.7f);
				DrawString(font, textPos, label, alignment: HorizontalAlignment.Left, width: width - 16f, fontSize: fontSize - 2, modulate: Colors.White);
			}
			else
			{
				string label = $"w={node.Weight}";
				Vector2 textPos = new(rect.Position.X + 8f, rect.Position.Y + NodeHeight * 0.7f);
				DrawString(font, textPos, label, alignment: HorizontalAlignment.Left, width: width - 16f, fontSize: fontSize, modulate: Colors.White);
			}
		}
	}

	private Vector2 WithOffset(Vector2 point) => point + _origin;

	public Rect2 GetBounds()
	{
		if (_rope is null)
		{
			return new Rect2(_origin, Vector2.Zero);
		}

		EnsureLayout();

		float minX = float.PositiveInfinity;
		float minY = float.PositiveInfinity;
		float maxX = float.NegativeInfinity;
		float maxY = float.NegativeInfinity;

		foreach (KeyValuePair<Rope, Vector2> pair in _positions)
		{
			float width = _widths.TryGetValue(pair.Key, out float w) ? w : NodeWidth;
			Vector2 world = WithOffset(pair.Value);
			minX = MathF.Min(minX, world.X - width * 0.5f);
			minY = MathF.Min(minY, world.Y - NodeHeight * 0.5f);
			maxX = MathF.Max(maxX, world.X + width * 0.5f);
			maxY = MathF.Max(maxY, world.Y + NodeHeight * 0.5f);
		}

		if (float.IsPositiveInfinity(minX))
		{
			return new Rect2(_origin, Vector2.Zero);
		}

		Vector2 min = new Vector2(minX, minY);
		Vector2 size = new Vector2(maxX - minX, maxY - minY);
		return new Rect2(min, size);
	}

	public Vector2 GetLeafSpanCenter()
	{
		if (_rope is null)
		{
			return _origin;
		}

		EnsureLayout();

		float minX = float.PositiveInfinity;
		float maxX = float.NegativeInfinity;
		float minY = float.PositiveInfinity;
		float maxY = float.NegativeInfinity;

		foreach (KeyValuePair<Rope, Vector2> pair in _positions)
		{
			Rope node = pair.Key;
			if (!node.IsLeaf) continue;

			Vector2 world = WithOffset(pair.Value);
			minX = MathF.Min(minX, world.X);
			maxX = MathF.Max(maxX, world.X);
			minY = MathF.Min(minY, world.Y);
			maxY = MathF.Max(maxY, world.Y);
		}

		if (float.IsPositiveInfinity(minX) || float.IsNegativeInfinity(maxX))
		{
			return GetBounds().Position + GetBounds().Size * 0.5f;
		}

		float midX = (minX + maxX) * 0.5f;
		float midY = (minY + maxY) * 0.5f;
		return new Vector2(midX, midY);
	}

	public Vector2 GetRootScenePosition()
	{
		if (_rootInstance is not null)
		{
			return _rootInstance.GlobalPosition;
		}

		if (_treeContainer is not null && _treeContainer.GetChildCount() > 0)
		{
			return _treeContainer.GlobalPosition;
		}

		return GlobalPosition + _origin;
	}
}
