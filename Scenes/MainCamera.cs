using Godot;
namespace RopeAV;

public partial class MainCamera : Camera2D
{
	[Export] public float PanSpeed = 520f;
	[Export] public float ZoomStep = 1.15f;
	[Export] public float MinZoom = 0.35f;
	[Export] public float MaxZoom = 2.5f;
	[Export] public float FollowLerp = 6f;

	private RopeNode? _visualizer;
	private Vector2 _targetPos;
	private Vector2 _targetZoom;
	private bool _dragging = false;
	private Vector2 _lastDrag;

	public override void _Ready()
	{
		_visualizer = GetNodeOrNull<RopeNode>("../Rope");
		_targetPos = Position;
		_targetZoom = Zoom;
		MakeCurrent();

		if (_visualizer is not null)
		{
			Position = GetTreeCenter();
			_targetPos = Position;
		}
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

		if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Space)
		{
			Recentralize();
		}

		if (@event is InputEventScreenTouch touch && touch.Pressed && touch.DoubleTap)
		{
			Recentralize();
		}

		if (@event is InputEventMouseMotion && _dragging)
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
		HandlePan(delta);
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

		if (Input.IsActionJustPressed("recentralize"))
		{
			Recentralize();
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

	private void Recentralize()
	{
		if (IsUiFocused()) return;

		_targetPos = GetTreeCenter();
	}

	public void OnRecentralizeButtonPressed()
	{
		Recentralize();
	}

	private void SmoothFollow(double delta)
	{
		float t = 1f - Mathf.Exp(-FollowLerp * (float)delta);
		Position = Position.Lerp(_targetPos, t);
		Zoom = Zoom.Lerp(_targetZoom, t);
	}

	private bool IsUiFocused()
	{
		Control? focus = GetViewport()?.GuiGetFocusOwner();
		return focus is not null && focus.IsVisibleInTree();
	}

	private Vector2 GetTreeCenter()
	{
		if (_visualizer is null)
		{
			return Vector2.Zero;
		}

		return _visualizer.GetRootScenePosition();
	}
}
