using Godot;

namespace KaleidoNodes;

public partial class GameCursor : Node2D
{
	bool _active;
	double _mouseFilter;

	public static bool IsMouse { get; private set; }
	public bool IsActive => _active && Enabled;

	bool _enabled = true;
	[Export]
	public bool Enabled
	{
		get => _enabled;
		set
		{
			if (_enabled == value)
				return;

			_enabled = value;

			if (_active)
				HandleActiveChanged();
		}
	}

	[Export]
	public bool HideOSCursor { get; set; } = false;

	[Export]
	public bool Confined { get; set; } = false;

	[Export]
	public double TouchTimeout { get; set; } = 0.5;

	[Signal]
	public delegate void ActiveChangedEventHandler(bool active);

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
			return;

		ZIndex = 999;

		UpdateMouseMode();
		OnActiveChange();
	}

	public override void _Input(InputEvent e)
	{
		if (Engine.IsEditorHint())
			return;

		if (e is InputEventMouseButton mb)
		{
			if (_mouseFilter > 0)
				return;

			IsMouse = true;

			if (_active != mb.Pressed)
			{
				_active = mb.Pressed;

				if (_enabled)
					HandleActiveChanged();
			}

			//GD.Print("Button");
		}
		else if (e is InputEventMouseMotion)
		{
			if (_mouseFilter > 0)
				return;

			IsMouse = true;

			if (_enabled)
				OnMove();
		}
		else if (e is InputEventScreenTouch st)
		{
			_mouseFilter = TouchTimeout;
			IsMouse = false;

			if (_active != st.Pressed)
			{
				_active = st.Pressed;
				if (_enabled)
					HandleActiveChanged();
			}
			//GD.Print("Touch");
		}
		else if (e is InputEventScreenDrag)
		{
			_mouseFilter = TouchTimeout;
			IsMouse = false;

			if (_enabled)
				OnMove();
		}
	}

	void HandleActiveChanged()
	{
		if (Engine.IsEditorHint())
			return;

		OnActiveChange();
		EmitSignal(SignalName.ActiveChanged, IsActive);
	}

	protected virtual void OnActiveChange()
	{
		// Mouse or touch is active
	}

	protected virtual void OnMove()
	{
		// Mouse or touch is moved
	}

	void UpdateMouseMode()
	{
		if (HideOSCursor)
			Input.MouseMode = Confined ? Input.MouseModeEnum.ConfinedHidden : Input.MouseModeEnum.Hidden;
		else
			Input.MouseMode = Confined ? Input.MouseModeEnum.Confined : Input.MouseModeEnum.Visible;
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint())
			return;

		if (_mouseFilter > 0)
			_mouseFilter -= delta;

		GlobalPosition = GetGlobalMousePosition();
	}
}