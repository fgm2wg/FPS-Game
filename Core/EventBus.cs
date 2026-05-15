using Godot;
using System;

public partial class EventBus : Node
{
	public static Action<string, int, int> OnHostRequested;
	public static Action<string, int> OnJoinRequested;

	public static Action<long> OnPlayerConnected;
	public static Action<long> OnPlayerDisconnected;
	public static Action<int> OnGameStartTimerUpdated;
	
	public static Action<bool> OnPauseMenuToggled;
	public static Action<int, int> OnAmmoChanged;
	public static Action<string> OnFireModeChanged;
	public static Action<long, string, long, string, string> OnPlayerKilled;
	public static Action<float, float> OnLocalPlayerHealthChanged;
	public static Action<int, bool, bool, bool> OnScorestreakUpdated;
}
