using Godot;
using Spellbound;

public partial class MainScene : Control
{
	GlyphText GlyphText => GetNode<GlyphText>("GlyphText");

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GlyphText.MousePressed += (glyphIndex) =>
		{
			GD.Print($"Glyph {glyphIndex} pressed");
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
