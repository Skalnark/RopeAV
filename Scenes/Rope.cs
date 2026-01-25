using System;
using Godot;
using System.Collections.Generic;
using RopeAV.Lib.DataStructures;

namespace RopeAV;

public partial class RopeVisualizer : Node2D
{
	private readonly Dictionary<Rope, Vector2> _positions = new();
	private Rope? _rope;
	private float _currentX;

	private const float HorizontalSpacing = 160f;
	private const float VerticalSpacing = 100f;
	private const float NodeWidth = 130f;
	private const float NodeHeight = 48f;
	private readonly Vector2 _origin = new(120f, 180f);

	public void SetRope(Rope? rope)
	{
		_rope = rope;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_rope is null)
		{
			return;
		}

		_positions.Clear();
		_currentX = 0f;
		ComputeLayout(_rope, 0);
		DrawEdges(_rope);
		DrawNodes();
	}

	private const float NodeGap = 40f;

	private float ComputeLayout(Rope node, int depth)
	{
		float centerX;
		if (node.IsLeaf)
		{
			float width = NodeWidth + NodeGap;
			centerX = _currentX + width * 0.5f;
			_positions[node] = new Vector2(centerX, depth * VerticalSpacing);
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
		return widthCombined;
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

			Rect2 rect = new(pos - new Vector2(NodeWidth * 0.5f, NodeHeight * 0.5f), new Vector2(NodeWidth, NodeHeight));
			Color fill = node.IsLeaf ? new Color(0.27f, 0.45f, 0.68f) : new Color(0.20f, 0.23f, 0.29f);
			DrawRect(rect, fill);
			DrawRect(rect, Colors.LightGray, false, 2f);

			string lengthText = $"len={node.Length}";
			Vector2 lengthPos = new(rect.Position.X + 8f, rect.Position.Y + NodeHeight * 0.35f);
			DrawString(font, lengthPos, lengthText, alignment: HorizontalAlignment.Left, width: NodeWidth - 16f, fontSize: fontSize - 2, modulate: new Color(0.85f, 0.9f, 0.95f));

			string label = node.IsLeaf ? $"\"{node.TextSegment}\"" : $"w={node.Weight}";
			Vector2 textPos = new(rect.Position.X + 8f, rect.Position.Y + NodeHeight * 0.68f);
			DrawString(font, textPos, label, alignment: HorizontalAlignment.Left, width: NodeWidth - 16f, fontSize: fontSize, modulate: Colors.White);
		}
	}

	public Rect2 GetBounds()
	{
		if (_rope is null)
		{
			return new Rect2(_origin, Vector2.Zero);
		}

		_positions.Clear();
		_currentX = 0f;
		ComputeLayout(_rope, 0);

		float minX = float.PositiveInfinity;
		float minY = float.PositiveInfinity;
		float maxX = float.NegativeInfinity;
		float maxY = float.NegativeInfinity;

		foreach (Vector2 pos in _positions.Values)
		{
			Vector2 world = WithOffset(pos);
			minX = MathF.Min(minX, world.X - NodeWidth * 0.5f);
			minY = MathF.Min(minY, world.Y - NodeHeight * 0.5f);
			maxX = MathF.Max(maxX, world.X + NodeWidth * 0.5f);
			maxY = MathF.Max(maxY, world.Y + NodeHeight * 0.5f);
		}

		if (minX == float.PositiveInfinity)
		{
			return new Rect2(_origin, Vector2.Zero);
		}

		Vector2 min = new Vector2(minX, minY);
		Vector2 size = new Vector2(maxX - minX, maxY - minY);
		return new Rect2(min, size);
	}

	private Vector2 WithOffset(Vector2 point) => point + _origin;
}
