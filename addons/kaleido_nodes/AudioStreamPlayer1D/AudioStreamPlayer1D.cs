using System.Linq;
using Godot;

namespace KaleidoNodes;

public enum PanRotation
{
	None,
	CW90,
	CCW90,
}

[Tool]
[GlobalClass]
public partial class AudioStreamPlayer1D : Node2D
{
	StringName _busName = string.Empty;
	AudioStreamPlayer _player;

	[Export]
	public AudioStream Stream
	{
		get => _player.Stream;
		set => _player.Stream = value;
	}

	[Export(hint: PropertyHint.Range, hintString: "-80,24,0.1")]
	public float VolumeDb
	{
		get => _player.VolumeDb;
		set => _player.VolumeDb = value;
	}

	//[Export(hint: PropertyHint.Range, hintString: "0,15,0.01")]
	public float VolumeLinear
	{
		get => _player.VolumeLinear;
		set => _player.VolumeLinear = value;
	}

	[Export(hint: PropertyHint.Range, hintString: "0.01,4,0.01")]
	public float PitchScale
	{
		get => _player.PitchScale;
		set => _player.PitchScale = value;
	}

	[Export]
	public bool Playing
	{
		get => _player.Playing;
		set => _player.Playing = value;
	}

	[Export]
	public bool Autoplay
	{
		get => _player.Autoplay;
		set => _player.Autoplay = value;
	}

	[Export]
	public bool StreamPaused
	{
		get => _player.StreamPaused;
		set => _player.StreamPaused = value;
	}

	[Export]
	public int MaxPolyphony
	{
		get => _player.MaxPolyphony;
		set => _player.MaxPolyphony = value;
	}

	public StringName Bus
	{
		get => PanBusPool.GetSend(_busName);
		set => PanBusPool.SetSend(_busName, value);
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
				{ "name", "Bus" },
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
	public float SpatialPanningStrength { get; set; } = .5f;


	[Export]
	public PanRotation PanRotation { get; set; } = PanRotation.None;

	public AudioStreamPlayer1D()
	{
		_player = new AudioStreamPlayer();
	}

	public override void _Ready()
	{
		_busName = PanBusPool.Acquire();
		_player.Bus = _busName;
		AddChild(_player);
	}

	private float GetPan()
	{
		var listener = GetViewport().GetAudioListener2D();
		var origin = listener?.GlobalPosition ?? GetViewport().GetVisibleRect().GetCenter();

		var spatialPan = PanRotation switch
		{
			PanRotation.CW90 => Mathf.Clamp((GlobalPosition.Y - origin.Y) / (GetViewport().GetVisibleRect().Size.Y / 2f), -1f, 1f),
			PanRotation.CCW90 => Mathf.Clamp((origin.Y - GlobalPosition.Y) / (GetViewport().GetVisibleRect().Size.Y / 2f), -1f, 1f),
			_ => Mathf.Clamp((GlobalPosition.X - origin.X) / (GetViewport().GetVisibleRect().Size.X / 2f), -1f, 1f),
		};

		return Mathf.Clamp(Pan + (spatialPan * SpatialPanningStrength), -1f, 1f);
	}

	public override void _Process(double delta)
	{
		PanBusPool.SetPan(_busName, GetPan());
	}

	public override void _ExitTree()
	{
		PanBusPool.Release(_busName);
	}

	public void Play()
		=> _player.Play();

}
