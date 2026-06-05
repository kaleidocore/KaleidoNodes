using System.Linq;
using Godot;

namespace KaleidoNodes;

public enum PanningRotation
{
	None,
	CW90,
	CCW90,
}

[Tool]
[GlobalClass]
public partial class AudioStreamPlayer1D : AudioStreamPlayer
{
	StringName _busName = string.Empty;

	[Export(hint: PropertyHint.Range, hintString: "-1,1,0.01")]
	public float PanningOffset { get; set; } = 0f;

	[Export(hint: PropertyHint.Range, hintString: "0,1,0.01")]
	public float PanningScale { get; set; } = .5f;

	[Export]
	public float VolumeScale { get; set; } = 1f;

	[Export]
	public PanningRotation PanningRotation { get; set; } = PanningRotation.None;

	StringName _sendBus = string.Empty;
	public StringName SendBus
	{
		get => _sendBus.IsEmpty ? PanBusPool.Master : _sendBus;
		set => _sendBus = value;
	}

	[Export]
	public bool Allocated
	{
		get => !_busName.IsEmpty;
		set
		{
			if (value)
				EnsureBus();
			else
				ReleaseBus();
		}
	}

	public override void _ValidateProperty(Godot.Collections.Dictionary property)
	{
		if (property["name"].AsStringName() == PropertyName.Bus)
			property["usage"] = (int)(PropertyUsageFlags.Default | PropertyUsageFlags.ReadOnly);
	}

	public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
	{
		var buses = Enumerable.Range(0, AudioServer.BusCount)
			.Select(i => AudioServer.GetBusName(i))
			.Where(b => !PanBusPool.IsPanBus(b))
			.ToArray();

		var hint = string.Join(",", buses);

		return
		[
			new Godot.Collections.Dictionary
	{
				{ "name", nameof(SendBus) },
				{ "type", (int)Variant.Type.String },
				{ "hint", (int)PropertyHint.Enum },
				{ "hint_string", hint },
				{ "usage", (int)PropertyUsageFlags.Default },
		}
		];
	}


	public override void _Ready()
	{
		if (!Engine.IsEditorHint())
			Allocated = true;
	}

	public override void _ExitTree()
	{
		ReleaseBus();
	}

	void EnsureBus()
	{
		if (_busName.IsEmpty)
		{
			_busName = PanBusPool.Acquire();
			UpdateValues();
		}
	}

	void ReleaseBus()
	{
		if (!_busName.IsEmpty)
		{
			Bus = SendBus;
			PanBusPool.Release(_busName);
			_busName = string.Empty;
		}
	}

	public override void _Process(double delta)
	{
		UpdateValues();
	}

	void UpdateValues()
	{
		if (_busName.IsEmpty)
		{
			Bus = SendBus;
			return;
		}

		if (Bus != _busName)
			Bus = _busName;

		PanBusPool.SetSend(_busName, SendBus);
		PanBusPool.SetPan(_busName, GetPan());
		PanBusPool.SetVolume(_busName, VolumeScale);
	}

	private float GetPan()
	{
		var vp = GetViewport();
		if (vp == null)
			return 0f;

		var camera = vp.GetCamera2D();
		var zoom = camera?.Zoom ?? Vector2.One;

		// Viewport size: use actual game size at runtime, project settings in editor
		var vpSize = Engine.IsEditorHint()
			? new Vector2(
				ProjectSettings.GetSetting("display/window/size/viewport_width").AsInt32(),
				ProjectSettings.GetSetting("display/window/size/viewport_height").AsInt32())
			: vp.GetVisibleRect().Size;

		var worldHalfWidth = (vpSize.X / 2f) / zoom.X;
		var worldHalfHeight = (vpSize.Y / 2f) / zoom.Y;

		// Origin: listener -> camera -> viewport center
		var cameraCenter = camera?.GlobalPosition ?? (vpSize / 2f);
		var listener = vp.GetAudioListener2D();
		var origin = listener?.GlobalPosition ?? cameraCenter;

		// Sound position: parent -> origin
		var globalPos = GetParentOrNull<Node2D>()?.GlobalPosition ?? origin;

		var spatialPan = PanningRotation switch
		{
			PanningRotation.CW90 => Mathf.Clamp((globalPos.Y - origin.Y) / worldHalfHeight, -1f, 1f),
			PanningRotation.CCW90 => Mathf.Clamp((origin.Y - globalPos.Y) / worldHalfHeight, -1f, 1f),
			_ => Mathf.Clamp((globalPos.X - origin.X) / worldHalfWidth, -1f, 1f),
		};

		return Mathf.Clamp(PanningOffset + (spatialPan * PanningScale), -1f, 1f);
	}
}
