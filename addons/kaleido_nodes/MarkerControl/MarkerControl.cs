using Godot;

namespace KaleidoNodes;

[Tool]
public partial class MarkerControl : Control
{
	const int GizmoSize = 16;

	public override void _Ready()
	{
		base._Ready();
		SetDeferred(PropertyName.Size, new Vector2(GizmoSize, GizmoSize) * 2);
		MouseFilter = MouseFilterEnum.Ignore;
		CustomMinimumSize = Vector2.Zero;
		PivotOffsetRatio = new Vector2(0.5f, 0.5f);
	}

	public override void _Draw()
	{
		if (!Engine.IsEditorHint())
			return;

		var center = Size * 0.5f;
		var color = new Color(0f, 1f, 0.6f);
		DrawLine(new Vector2(0, center.Y), new Vector2(Size.X, center.Y), color, 2f);
		DrawLine(new Vector2(center.X, 0), new Vector2(center.X, Size.Y), color, 2f);
		DrawCircle(center, 5f, color);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		if (!Engine.IsEditorHint())
			return;

		OffsetLeft = -GizmoSize;
		OffsetRight = GizmoSize;
		OffsetTop = -GizmoSize;
		OffsetBottom = GizmoSize;
	}
}