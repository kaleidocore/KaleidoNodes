using Godot;

namespace KaleidoNodes;

public static class Extensions
{
	public static Vector2 GlobalCenter(this Control control)
		=> control.GlobalPosition + (control.Size * 0.5f);
}
