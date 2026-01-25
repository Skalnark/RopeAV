using Godot;
using System.Collections.Generic;
using RopeAV.Lib.DataStructures;

namespace RopeAV;

public partial class RopeNode : Node2D
{
	private readonly Dictionary<Rope, Vector2> _positions = [];
	private Rope? _rope;

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
		int leafIndex = 0;
		ComputeLayout(_rope, 0, ref leafIndex);
		DrawEdges(_rope);
		DrawNodes();
	}

	private float ComputeLayout(Rope node, int depth, ref int leafIndex)
	{
		float centerX;
		if (node.IsLeaf)
		{
			centerX = leafIndex * HorizontalSpacing;
			leafIndex++;
		}
		else
		{
			float leftX = node.Left is not null ? ComputeLayout(node.Left, depth + 1, ref leafIndex) : 0f;
			float rightX = node.Right is not null ? ComputeLayout(node.Right, depth + 1, ref leafIndex) : leftX + HorizontalSpacing;
			centerX = (leftX + rightX) * 0.5f;
		}

		_positions[node] = new Vector2(centerX, depth * VerticalSpacing);
		return centerX;
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

	private Vector2 WithOffset(Vector2 point) => point + _origin;
}
