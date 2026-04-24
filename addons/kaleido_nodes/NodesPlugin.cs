#if TOOLS
using Godot;

namespace KaleidoNodes;

[Tool]
public partial class NodesPlugin : EditorPlugin
{
	string PluginDir => GetScript().As<Script>().ResourcePath.GetBaseDir();

	public override void _EnterTree()
	{
		var markerControlScript = GD.Load<Script>(PluginDir.PathJoin("MarkerControl/MarkerControl.cs"));
		var markerControlIcon = GD.Load<Texture2D>(PluginDir.PathJoin("MarkerControl/icon.svg"));
		AddCustomType(nameof(MarkerControl), nameof(Control), markerControlScript, markerControlIcon);

		var glyphTextScript = GD.Load<Script>(PluginDir.PathJoin("GlyphText/GlyphText.cs"));
		var glyphTextIcon = GD.Load<Texture2D>(PluginDir.PathJoin("GlyphText/icon.svg"));
		AddCustomType(nameof(GlyphText), nameof(Node2D), glyphTextScript, glyphTextIcon);

		var gameCursorScript = GD.Load<Script>(PluginDir.PathJoin("GameCursor/GameCursor.cs"));
		var gameCursorIcon = GD.Load<Texture2D>(PluginDir.PathJoin("GameCursor/icon.svg"));
		AddCustomType(nameof(GameCursor), nameof(Node2D), gameCursorScript, gameCursorIcon);
	}

	public override void _ExitTree()
	{
		RemoveCustomType(nameof(MarkerControl));
		RemoveCustomType(nameof(GlyphText));
		RemoveCustomType(nameof(GameCursor));
	}
}
#endif