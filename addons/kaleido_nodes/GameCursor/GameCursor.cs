using Godot;

namespace KaleidoNodes;

public partial class GameCursor : Node2D
{
	bool _active;

	public bool IsTouchMode { get; private set; } = DisplayServer.IsTouchscreenAvailable();
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
			{
				if (_enabled)
					SendActiveChanged(true);
				else
					SendActiveChanged(false);
			}
		}
	}

	[Export]
	public bool HideOSCursor { get; set; } = false;

	[Export]
	public bool Confined { get; set; } = false;

	[Signal] public delegate void ActiveChangedEventHandler(bool active);

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
			return;

		if (IsTouchMode)
			OnHide();
		else
			OnShow();

		UpdateMouseMode();

		ZIndex = 999;
	}

	public override void _Input(InputEvent e)
	{
		if (Engine.IsEditorHint())
			return;

		if (e is InputEventScreenTouch touch)
		{
			if (!IsTouchMode)
			{
				IsTouchMode = true;
				UpdateMouseMode();
			}

			if (_active != touch.Pressed)
			{
				_active = touch.Pressed;

				if (_enabled)
				{
					if (_active)
						OnShow();
					else
						OnHide();

					SendActiveChanged(_active);
				}
			}
		}
		else if (e is InputEventMouseMotion motion)
		{
			if (IsTouchMode)
			{
				IsTouchMode = false;
				UpdateMouseMode();
				OnShow();
			}
		}
		else if (e is InputEventMouseButton mb)
		{
			if (IsTouchMode)
			{
				IsTouchMode = false;
				UpdateMouseMode();
				OnShow();
			}

			if (_active != mb.Pressed)
			{
				_active = mb.Pressed;

				if (_enabled)
					SendActiveChanged(_active);
			}
		}
	}

	void SendActiveChanged(bool active)
	{
		EmitSignal(SignalName.ActiveChanged, active);
	}

	protected virtual void OnShow()
	{
	}

	protected virtual void OnHide()
	{
	}

	void UpdateMouseMode()
	{
		if (HideOSCursor || IsTouchMode)
			Input.MouseMode = Confined ? Input.MouseModeEnum.ConfinedHidden : Input.MouseModeEnum.Hidden;
		else
			Input.MouseMode = Confined ? Input.MouseModeEnum.Confined : Input.MouseModeEnum.Visible;
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint())
			return;

		GlobalPosition = GetGlobalMousePosition();
	}
}