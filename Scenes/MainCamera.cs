using Godot;
using System;

namespace RopeAV;

public partial class MainCamera : Camera2D
{
	[Export] public float PanSpeed = 520f;
	[Export] public float ZoomStep = 1.15f;
	[Export] public float MinZoom = 0.35f;
	[Export] public float MaxZoom = 2.5f;
	[Export] public float FitPadding = 1.2f;
	[Export] public float FollowLerp = 6f;

	private RopeNode? _visualizer;
	private Vector2 _targetPos;
	private Vector2 _targetZoom;
	private Rect2 _lastBounds;
	private bool _boundsDirty = true;
	private bool _dragging = false;
	private Vector2 _lastDrag;

	public override void _Ready()
	{
		_visualizer = GetNodeOrNull<RopeNode>("../Rope");
		_targetPos = Position;
		_targetZoom = Zoom;
		MakeCurrent();
		UpdateBounds(force:true);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (IsUiFocused())
		{
			_dragging = false;
			return;
		}

		if (@event is InputEventMouseButton mb)
		{
			if (mb.ButtonIndex == MouseButton.WheelUp && mb.Pressed)
			{
				ApplyZoom(ZoomStep);
			}
			else if (mb.ButtonIndex == MouseButton.WheelDown && mb.Pressed)
			{
				ApplyZoom(1f / ZoomStep);
			}

			if (mb.ButtonIndex == MouseButton.Middle)
			{
				_dragging = mb.Pressed;
				_lastDrag = GetGlobalMousePosition();
			}
		}

		if (@event is InputEventMouseMotion motion && _dragging)
		{
			Vector2 now = GetGlobalMousePosition();
			Vector2 delta = _lastDrag - now;
			Position += delta;
			_targetPos = Position;
			_lastDrag = now;
		}
	}

	public override void _Process(double delta)
	{
		UpdateBounds();
		HandlePan(delta);
		EnsureTreeVisible();
		SmoothFollow(delta);
	}

	private void HandlePan(double delta)
	{
		if (IsUiFocused())
		{
			return;
		}

		Vector2 move = Vector2.Zero;
		if (Input.IsActionPressed("ui_left") || Input.IsKeyPressed(Key.A)) move.X -= 1;
		if (Input.IsActionPressed("ui_right") || Input.IsKeyPressed(Key.D)) move.X += 1;
		if (Input.IsActionPressed("ui_up") || Input.IsKeyPressed(Key.W)) move.Y -= 1;
		if (Input.IsActionPressed("ui_down") || Input.IsKeyPressed(Key.S)) move.Y += 1;

		if (move != Vector2.Zero)
		{
			Position += move.Normalized() * PanSpeed * (float)delta;
			_targetPos = Position;
		}
	}

	private void ApplyZoom(float factor)
	{
		if (IsUiFocused()) return;

		Vector2 z = Zoom * factor;
		z.X = Mathf.Clamp(z.X, MinZoom, MaxZoom);
		z.Y = Mathf.Clamp(z.Y, MinZoom, MaxZoom);
		Zoom = z;
		_targetZoom = z;
	}

	private void UpdateBounds(bool force = false)
	{
		if (_visualizer is null) return;
		Rect2 bounds = _visualizer.GetBounds();
		if (!force && Approximately(bounds, _lastBounds))
		{
			return;
		}

		_lastBounds = bounds;
		_boundsDirty = true;
	}

	private void SmoothFollow(double delta)
	{
		if (_boundsDirty)
		{
			FitToBounds(_lastBounds);
		}

		float t = 1f - Mathf.Exp(-FollowLerp * (float)delta);
		Position = Position.Lerp(_targetPos, t);
		Zoom = Zoom.Lerp(_targetZoom, t);
	}

	private void EnsureTreeVisible()
	{
		if (_visualizer is null) return;
		if (_lastBounds.Size == Vector2.Zero) return;
		Rect2 view = GetViewRectWorld();
		if (!view.Encloses(_lastBounds))
		{
			FitToBounds(_lastBounds);
		}
	}

	private Rect2 GetViewRectWorld()
	{
		Vector2 viewport = GetViewportRect().Size;
		Vector2 size = viewport * Zoom;
		Vector2 pos = Position - size * 0.5f;
		return new Rect2(pos, size);
	}

	private void FitToBounds(Rect2 bounds)
	{
		if (bounds.Size == Vector2.Zero)
		{
			_targetPos = Position;
			_targetZoom = Zoom;
			_boundsDirty = false;
			return;
		}

		Vector2 viewport = GetViewportRect().Size;
		if (viewport == Vector2.Zero)
		{
			_boundsDirty = false;
			return;
		}

		Vector2 padded = bounds.Size * FitPadding;
		float scaleX = padded.X / viewport.X;
		float scaleY = padded.Y / viewport.Y;
		float target = MathF.Max(scaleX, scaleY);
		if (target < MinZoom) target = MinZoom;
		target = MathF.Min(target, MaxZoom);

		_targetZoom = new Vector2(target, target);
		_targetPos = bounds.Position + bounds.Size * 0.5f;
		_boundsDirty = false;
	}

	private static bool Approximately(Rect2 a, Rect2 b)
	{
		const float eps = 0.5f;
		return MathF.Abs(a.Position.X - b.Position.X) < eps
			&& MathF.Abs(a.Position.Y - b.Position.Y) < eps
			&& MathF.Abs(a.Size.X - b.Size.X) < eps
			&& MathF.Abs(a.Size.Y - b.Size.Y) < eps;
	}

	private bool IsUiFocused()
	{
		Control? focus = GetViewport()?.GuiGetFocusOwner();
		return focus is not null && focus.IsVisibleInTree();
	}
}
