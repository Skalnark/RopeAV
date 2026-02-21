using Godot;
using System;
using RopeAV.Lib.DataStructures;

namespace RopeAV;

public partial class Main : Node2D
{
	private int _maxLeafLength = 4;
	private Rope _rope = Rope.FromString("Hello, world!", 4);
	private RopeNode? _visualizer;
	private Control? _panel;
	private Window? _uiWindow;
	private Button? _toggleUiButton;

	private bool _uiFaded = false;

	private LineEdit? _inputText;
	private LineEdit? _insertText;
	private SpinBox? _indexBox;
	private SpinBox? _lengthBox;
	private SpinBox? _leafSizeBox;
	private TextEdit? _fullString;
	private Label? _statusLabel;
	private Label? _ropeValueLabel;

	public override void _Ready()
	{
		_visualizer = GetNodeOrNull<RopeNode>("Rope");
		CacheUiNodes();
		HookUiEvents();
		InitializeUiValues();
		RefreshUi("Loaded sample rope");
	}

	private void CacheUiNodes()
	{
		_uiWindow = GetNodeOrNull<Window>("UiLayer/UiWindow");
		_toggleUiButton = GetNodeOrNull<Button>("UiLayer/ToggleUiButton");
		_panel = GetNodeOrNull<Control>("UiLayer/UiWindow/Margin/Root");
		_ropeValueLabel = GetNodeOrNull<Label>("UiLayer/UiWindow/Margin/Root/RopeValueLabel");
		_fullString = GetNodeOrNull<TextEdit>("UiLayer/UiWindow/Margin/Root/FullString");
		_leafSizeBox = GetNodeOrNull<SpinBox>("UiLayer/UiWindow/Margin/Root/LeafSizeBox");
		_inputText = GetNodeOrNull<LineEdit>("UiLayer/UiWindow/Margin/Root/InputText");
		_insertText = GetNodeOrNull<LineEdit>("UiLayer/UiWindow/Margin/Root/InsertText");
		_indexBox = GetNodeOrNull<SpinBox>("UiLayer/UiWindow/Margin/Root/Row/IndexBox");
		_lengthBox = GetNodeOrNull<SpinBox>("UiLayer/UiWindow/Margin/Root/Row/LengthBox");
		_statusLabel = GetNodeOrNull<Label>("UiLayer/UiWindow/Margin/Root/StatusLabel");
	}

	private void HookUiEvents()
	{
		Button? buildButton = GetNodeOrNull<Button>("UiLayer/UiWindow/Margin/Root/ButtonRow/BuildButton");
		if (buildButton is not null)
		{
			buildButton.Pressed += () => RunSafe(BuildRopeFromInput, "Built rope from input");
		}

		Button? insertButton = GetNodeOrNull<Button>("UiLayer/UiWindow/Margin/Root/ButtonRow/InsertButton");
		if (insertButton is not null)
		{
			insertButton.Pressed += () => RunSafe(InsertText, "Inserted text");
		}

		Button? deleteButton = GetNodeOrNull<Button>("UiLayer/UiWindow/Margin/Root/ButtonRow/DeleteButton");
		if (deleteButton is not null)
		{
			deleteButton.Pressed += () => RunSafe(DeleteText, "Deleted text");
		}

		Button? resetButton = GetNodeOrNull<Button>("UiLayer/UiWindow/Margin/Root/ButtonRow/ResetButton");
		if (resetButton is not null)
		{
			resetButton.Pressed += () => RunSafe(ResetRope, "Reset to empty");
		}

		if (_toggleUiButton is not null && _uiWindow is not null)
		{
			_toggleUiButton.Pressed += () => _uiWindow.Visible = !_uiWindow.Visible;
		}

		if (_uiWindow is not null)
		{
			_uiWindow.CloseRequested += () => _uiWindow.Hide();
		}
	}

	private void InitializeUiValues()
	{
		if (_uiWindow is not null)
		{
			_uiWindow.Visible = true;
		}

		if (_leafSizeBox is not null)
		{
			_leafSizeBox.Value = _maxLeafLength;
		}

		if (_inputText is not null)
		{
			_inputText.Text = _rope.ToString();
		}

		if (_insertText is not null)
		{
			_insertText.Text = string.Empty;
		}

		if (_fullString is not null)
		{
			_fullString.Text = _rope.ToString();
		}

		if (_indexBox is not null)
		{
			_indexBox.Value = 0;
		}

		if (_lengthBox is not null)
		{
			_lengthBox.Value = 1;
		}
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
			if (_statusLabel is not null)
			{
				_statusLabel.Text = ex.Message;
			}
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
		if (_inputText is not null)
		{
			_inputText.Text = string.Empty;
		}
		if (_insertText is not null)
		{
			_insertText.Text = string.Empty;
		}
	}

	private void RefreshUi(string status)
	{
		if (_indexBox is not null)
		{
			_indexBox.MaxValue = Math.Max(_rope.Length, 0);
			_indexBox.Value = Math.Min(_indexBox.Value, _rope.Length);
		}

		if (_lengthBox is not null)
		{
			_lengthBox.MaxValue = Math.Max(_rope.Length, 0);
			_lengthBox.Value = Math.Min(_lengthBox.Value, _rope.Length);
		}

		if (_ropeValueLabel is not null)
		{
			_ropeValueLabel.Text = $"Current rope: \"{_rope}\" (len: {_rope.Length})";
		}

		if (_fullString is not null)
		{
			_fullString.Text = _rope.ToString();
		}

		if (_statusLabel is not null)
		{
			_statusLabel.Text = status;
		}

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
