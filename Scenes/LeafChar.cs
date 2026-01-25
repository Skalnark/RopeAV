using Godot;

namespace RopeAV;

public partial class LeafChar : Node2D
{
	[Export] public string CharacterText { get; set; } = string.Empty;
	[Export] public int Index { get; set; }
	[Export] public float CharSize { get; set; } = 18f;
	[Export] public float IndexSize { get; set; } = 14f;

	public void Configure(string ch, int index)
	{
		CharacterText = ch;
		Index = index;
		QueueRedraw();
	}

	public override void _Draw()
	{
		Font font = ThemeDB.FallbackFont;
		int charSize = (int)CharSize;
		int idxSize = (int)IndexSize;

		Vector2 charPos = new(0f, -6f);
		Vector2 idxPos = new(0f, 12f);

		DrawString(font, charPos, string.IsNullOrEmpty(CharacterText) ? "" : CharacterText, alignment: HorizontalAlignment.Center, width: 48f, fontSize: charSize, modulate: Colors.White);
		DrawString(font, idxPos, Index.ToString(), alignment: HorizontalAlignment.Center, width: 48f, fontSize: idxSize, modulate: new Color(0.82f, 0.86f, 0.9f));
	}
}
