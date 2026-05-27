using System.Collections.Generic;

using Godot;

namespace KaleidoNodes;

public partial class PanBusPool : Node
{
	const string BusPrefix = "_PanBus_";
	static readonly Queue<string> _available = new();

	static void AddBus()
	{
		string name = $"{BusPrefix}{AudioServer.BusCount}";
		AudioServer.AddBus();
		int idx = AudioServer.BusCount - 1;
		AudioServer.SetBusName(idx, name);
		AudioServer.SetBusSend(idx, "Master");
		AudioServer.AddBusEffect(idx, new AudioEffectPanner());
		_available.Enqueue(name);
	}

	public static StringName Acquire()
	{
		if (_available.Count == 0)
			AddBus();

		return _available.Count > 0 ? _available.Dequeue() : "Master";
	}

	public static void Release(StringName busName)
	{
		if (busName != "Master")
			_available.Enqueue(busName);
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

		AudioServer.SetBusSend(idx, sendName);
	}

	public static StringName GetSend(StringName busName)
	{
		int idx = AudioServer.GetBusIndex(busName);

		if (idx < 0)
			return "Master";

		return AudioServer.GetBusSend(idx);
	}
}