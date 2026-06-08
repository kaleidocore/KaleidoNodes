using System.Collections.Generic;

using Godot;

namespace KaleidoNodes;

[Tool]
public partial class PanBusPool : Node
{
	const int PoolSize = 16;
	const string BusPrefix = "_PanBus_";
	static readonly Queue<StringName> _pool = [];
	static int _busCounter = 0;

	public static StringName Master => AudioServer.GetBusName(0);

	public override void _EnterTree()
	{
		base._EnterTree();

		// Nuke any stale pan buses that got baked into the .tres
		Cleanup();

		if (Engine.IsEditorHint())
			return;

		Allocate();
	}

	public override void _ExitTree()
	{
		Cleanup();
		base._ExitTree();
	}

	static void Allocate()
	{
		while (_pool.Count < PoolSize)
		{
			var bus = AddBus();
			_pool.Enqueue(bus);
		}

		GD.Print($"PanBusPool allocated {_pool.Count} buses.");
	}

	static void Cleanup()
	{
		var idx = 0;
		while (idx < AudioServer.BusCount)
		{
			string name = AudioServer.GetBusName(idx);

			if (IsPanBus(name))
				AudioServer.RemoveBus(idx);
			else
				idx++;
		}

		_pool.Clear();
		_busCounter = 0;
	}

	static StringName MakeName() => $"{BusPrefix}{_busCounter++}";

	static StringName AddBus()
	{
		int idx = AudioServer.BusCount;
		AudioServer.AddBus(idx);

		StringName name = MakeName();
		AudioServer.SetBusName(idx, name);
		AudioServer.SetBusSend(idx, "Master");
		AudioServer.AddBusEffect(idx, new AudioEffectPanner());

		if (AudioServer.GetBusIndex(name) != idx)
			throw new System.Exception("Bus registration failed FUCKING PIECE OF SHIT GODOT GARBAGE");

		if (AudioServer.GetBusName(idx) != name)
			throw new System.Exception("Bus name mismatch FUCKING PIECE OF SHIT GODOT GARBAGE");

		if (Engine.IsEditorHint())
			GD.Print($"Added pan bus: {name} with index {idx}");

		return name;
	}

	static void RemoveBus(string name)
	{
		int idx = AudioServer.GetBusIndex(name);

		if (idx >= 0)
			AudioServer.RemoveBus(idx);
	}

	public static StringName Acquire()
	{
		if (_pool.Count > 0)
			return _pool.Dequeue();

		var bus = AddBus();
		return bus;
	}

	public static void Release(StringName busName)
	{
		if (!IsPanBus(busName))
			return;

		if (Engine.IsEditorHint())
			RemoveBus(busName);
		else
			_pool.Enqueue(busName);
	}

	public static bool IsPanBus(StringName busName)
		=> busName.ToString().StartsWith(BusPrefix);

	public static void SetPan(StringName busName, float pan)
	{
		int idx = AudioServer.GetBusIndex(busName);

		if (idx < 0)
			return;

		var panner = (AudioEffectPanner)(AudioServer.GetBusEffect(idx, 0));
		panner.Pan = pan;
	}

	public static void SetSend(StringName busName, StringName sendName)
	{
		int idx = AudioServer.GetBusIndex(busName);

		if (idx < 0)
			return;

		if (sendName.IsEmpty)
			sendName = Master;

		AudioServer.SetBusSend(idx, sendName);
	}

	public static StringName GetSend(StringName busName)
	{
		int idx = AudioServer.GetBusIndex(busName);

		if (idx < 0)
			return Master;

		return AudioServer.GetBusSend(idx);
	}

	public static float GetVolume(StringName busName)
	{
		int idx = AudioServer.GetBusIndex(busName);

		if (idx < 0)
			return 1f;

		return AudioServer.GetBusVolumeLinear(idx);
	}

	public static void SetVolume(StringName busName, float volume)
	{
		int idx = AudioServer.GetBusIndex(busName);

		if (idx < 0)
			return;

		AudioServer.SetBusVolumeLinear(idx, volume);
	}
}