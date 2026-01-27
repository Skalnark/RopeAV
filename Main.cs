using Godot;
using System;
using RopeAV.Lib.DataStructures;

namespace RopeAV;

public partial class Main : Node2D
{
	private int _maxLeafLength = 4;
	private Rope _rope = Rope.FromString("Hello, world!", 4);
	private RopeNode? _visualizer;
	private PanelContainer? _panel;

	private bool _uiFaded = false;

	private LineEdit? _inputText;
	private LineEdit? _insertText;
	private SpinBox? _indexBox;
	private SpinBox? _lengthBox;
	private SpinBox? _leafSizeBox;
	private Label? _statusLabel;
	private Label? _ropeValueLabel;

	public override void _Ready()
	{
		_visualizer = GetNodeOrNull<RopeNode>("Rope");
		BuildUi();
		RefreshUi("Loaded sample rope");
	}

	private void BuildUi()
	{
		var layer = new CanvasLayer();
		layer.Name = "UiLayer";
		AddChild(layer);

		_panel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(440, 0)
		};
		_panel.Name = "UiPanel";

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 12);
		margin.AddThemeConstantOverride("margin_right", 12);
		margin.AddThemeConstantOverride("margin_top", 12);
		margin.AddThemeConstantOverride("margin_bottom", 12);

		var root = new VBoxContainer();
		root.AddThemeConstantOverride("separation", 6);

		_panel.AddChild(margin);
		margin.AddChild(root);
		layer.AddChild(_panel);

		_ropeValueLabel = new Label
		{
			Text = string.Empty,
			AutowrapMode = TextServer.AutowrapMode.Word
		};
		root.AddChild(_ropeValueLabel);

		root.AddChild(new Label { Text = "Max leaf length" });
		_leafSizeBox = new SpinBox { MinValue = 1, MaxValue = 1024, Step = 1, Value = _maxLeafLength };
		root.AddChild(_leafSizeBox);

		root.AddChild(new Label { Text = "Source text" });
		_inputText = new LineEdit { Text = _rope.ToString() };
		root.AddChild(_inputText);

		root.AddChild(new Label { Text = "Insert text" });
		_insertText = new LineEdit { Text = " + " };
		root.AddChild(_insertText);

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 8);
		row.AddChild(new Label { Text = "Index" });
		_indexBox = new SpinBox { MinValue = 0, Step = 1, Value = 0 };
		row.AddChild(_indexBox);
		row.AddChild(new Label { Text = "Length" });
		_lengthBox = new SpinBox { MinValue = 0, Step = 1, Value = 1 };
		row.AddChild(_lengthBox);
		root.AddChild(row);

		var buttonRow = new HBoxContainer();
		buttonRow.AddThemeConstantOverride("separation", 8);
		var buildButton = new Button { Text = "Build" };
		buildButton.Pressed += () => RunSafe(BuildRopeFromInput, "Built rope from input");
		buttonRow.AddChild(buildButton);

		var insertButton = new Button { Text = "Insert" };
		insertButton.Pressed += () => RunSafe(InsertText, "Inserted text");
		buttonRow.AddChild(insertButton);

		var deleteButton = new Button { Text = "Delete" };
		deleteButton.Pressed += () => RunSafe(DeleteText, "Deleted text");
		buttonRow.AddChild(deleteButton);

		var resetButton = new Button { Text = "Reset" };
		resetButton.Pressed += () => RunSafe(ResetRope, "Reset to empty");
		buttonRow.AddChild(resetButton);

		root.AddChild(buttonRow);

		_statusLabel = new Label
		{
			Text = string.Empty,
			AutowrapMode = TextServer.AutowrapMode.Word
		};
		root.AddChild(_statusLabel);

		// Keep the panel tucked to the top-left.
		_panel.Position = new Vector2(12, 12);
	}

	public void SetUiFaded(bool faded)
	{
		if (_panel is null) return;
		if (faded == _uiFaded) return;
		_uiFaded = faded;
		float alpha = faded ? 0.45f : 1f;
		_panel.Modulate = new Color(1f, 1f, 1f, alpha);
	}

	public override void _Process(double delta)
	{
		Control? focus = GetViewport()?.GuiGetFocusOwner();
		bool isUiFocused = focus is not null && focus.IsVisibleInTree();
		SetUiFaded(!isUiFocused);
	}

	private void RunSafe(Action action, string successMessage)
	{
		try
		{
			action();
			RefreshUi(successMessage);
		}
		catch (Exception ex)
		{
			_statusLabel!.Text = ex.Message;
		}
	}

	private void BuildRopeFromInput()
	{
		UpdateLeafLimit();
		string text = _inputText?.Text ?? string.Empty;
		_rope = Rope.FromString(text, _maxLeafLength);
	}

	private void InsertText()
	{
		UpdateLeafLimit();
		int index = (int)(_indexBox?.Value ?? 0);
		string text = _insertText?.Text ?? string.Empty;
		_rope = _rope.Insert(index, text);
	}

	private void DeleteText()
	{
		UpdateLeafLimit();
		int index = (int)(_indexBox?.Value ?? 0);
		int length = (int)(_lengthBox?.Value ?? 0);
		_rope = _rope.Delete(index, length);
	}

	private void ResetRope()
	{
		UpdateLeafLimit();
		_rope = Rope.FromString(string.Empty, _maxLeafLength);
		_inputText!.Text = string.Empty;
		_insertText!.Text = string.Empty;
	}

	private void RefreshUi(string status)
	{
		_indexBox!.MaxValue = Math.Max(_rope.Length, 0);
		_lengthBox!.MaxValue = Math.Max(_rope.Length, 0);
		_indexBox.Value = Math.Min(_indexBox.Value, _rope.Length);
		_lengthBox.Value = Math.Min(_lengthBox.Value, _rope.Length);

		_ropeValueLabel!.Text = $"Current rope: \"{_rope}\" (len: {_rope.Length})";
		_statusLabel!.Text = status;

		_visualizer?.SetRope(_rope);
		_visualizer?.QueueRedraw();
	}

	private void UpdateLeafLimit()
	{
		_maxLeafLength = (int)(_leafSizeBox?.Value ?? _maxLeafLength);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
		{
			Control? focus = GetViewport()?.GuiGetFocusOwner();
			if (focus is not null && _panel is not null)
			{
				Rect2 rect = _panel.GetGlobalRect();
				Vector2 mouse = GetViewport()?.GetMousePosition() ?? Vector2.Zero;
				if (!rect.HasPoint(mouse))
				{
					focus.ReleaseFocus();
				}
			}
		}
	}
}
