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

	StringName _sendBus = "Master";
	public StringName SendBus
	{
		get => _sendBus;
		set
		{
			_sendBus = value;

			if (!_busName.IsEmpty)
				PanBusPool.SetSend(_busName, value);
		}
	}

	public override void _ValidateProperty(Godot.Collections.Dictionary property)
	{
		if (property["name"].AsStringName() == PropertyName.Bus)
			property["usage"] = (int)PropertyUsageFlags.NoEditor;
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

	[Export(hint: PropertyHint.Range, hintString: "-1,1,0.01")]
	public float Pan { get; set; } = 0f;

	[Export(hint: PropertyHint.Range, hintString: "0,1,0.01")]
	public float PanningScale { get; set; } = .5f;

	[Export]
	public float VolumeScale { get; set; } = 1f;

	[Export]
	public PanningRotation PanningRotation { get; set; } = PanningRotation.None;

	public override void _Ready()
	{
		_busName = PanBusPool.Acquire();
		Bus = _busName;
	}

	private float GetPan()
	{
		var listener = GetViewport().GetAudioListener2D();
		var origin = listener?.GlobalPosition ?? Vector2.Zero;
		var globalPos = GetParentOrNull<Node2D>()?.GlobalPosition ?? origin;

		var spatialPan = PanningRotation switch
		{
			PanningRotation.CW90 => Mathf.Clamp((globalPos.Y - origin.Y) / (GetViewport().GetVisibleRect().Size.Y / 2f), -1f, 1f),
			PanningRotation.CCW90 => Mathf.Clamp((origin.Y - globalPos.Y) / (GetViewport().GetVisibleRect().Size.Y / 2f), -1f, 1f),
			_ => Mathf.Clamp((globalPos.X - origin.X) / (GetViewport().GetVisibleRect().Size.X / 2f), -1f, 1f),
		};

		return Mathf.Clamp(Pan + (spatialPan * PanningScale), -1f, 1f);
	}

	public override void _Process(double delta)
	{
		PanBusPool.SetPan(_busName, GetPan());
		PanBusPool.SetVolume(_busName, VolumeScale);
	}

	public override void _ExitTree()
	{
		PanBusPool.Release(_busName);
	}
}
