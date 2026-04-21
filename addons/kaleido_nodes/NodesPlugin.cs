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
		AddCustomType("MarkerControl", "Control", markerControlScript, markerControlIcon);

		var glyphTextScript = GD.Load<Script>(PluginDir.PathJoin("GlyphText/GlyphText.cs"));
		var glyphTextIcon = GD.Load<Texture2D>(PluginDir.PathJoin("GlyphText/icon.svg"));
		AddCustomType("GlyphText", "Node2D", glyphTextScript, glyphTextIcon);
	}

	public override void _ExitTree()
	{
		RemoveCustomType("MarkerControl");
		RemoveCustomType("GlyphText");
	}
}
#endif