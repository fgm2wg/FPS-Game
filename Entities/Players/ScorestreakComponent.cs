using Godot;
using System;

public partial class ScorestreakComponent : Node
{
	[Export] private Camera3D _camera;

	private int _currentScore = 0;
	private bool _tier1Unlocked = false;
	private bool _tier1Used = false;
	private bool _tier2Unlocked = false;
	private bool _tier2Used = false;
	private bool _tier3Unlocked = false;
	private bool _tier3Used = false;

	public override void _Ready()
	{
		if (GetMultiplayerAuthority() == Multiplayer.GetUniqueId())
		{
			EventBus.OnPlayerKilled += HandlePlayerKilled;
			EventBus.OnLocalPlayerHealthChanged += HandleHealthChanged;
			EventBus.OnScorestreakUpdated?.Invoke(0, false, false, false);
		}
	}

	public override void _ExitTree()
	{
		if (GetMultiplayerAuthority() == Multiplayer.GetUniqueId())
		{
			EventBus.OnPlayerKilled -= HandlePlayerKilled;
			EventBus.OnLocalPlayerHealthChanged -= HandleHealthChanged;
		}
	}

	private void HandlePlayerKilled(long killerId, string killerName, long victimId, string victimName, string weaponName)
	{
		if (killerId == Multiplayer.GetUniqueId() && victimId != killerId)
		{
			_currentScore += 100;

			if (_currentScore >= 200) _tier1Unlocked = true;
			if (_currentScore >= 400) _tier2Unlocked = true;
			if (_currentScore >= 600) _tier3Unlocked = true;

			EventBus.OnScorestreakUpdated?.Invoke(_currentScore, _tier1Unlocked && !_tier1Used, _tier2Unlocked && !_tier2Used, _tier3Unlocked && !_tier3Used);
		}
	}

	private void HandleHealthChanged(float currentHealth, float maxHealth)
	{
		if (currentHealth <= 0)
		{
			_currentScore = 0;
			_tier1Unlocked = false;
			_tier1Used = false;
			_tier2Unlocked = false;
			_tier2Used = false;
			_tier3Unlocked = false;
			_tier3Used = false;
			EventBus.OnScorestreakUpdated?.Invoke(_currentScore, false, false, false);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (GetMultiplayerAuthority() != Multiplayer.GetUniqueId()) return;

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.Keycode == Key.Key1 && _tier1Unlocked && !_tier1Used)
			{
				CallInA10(1);
			}
			else if (keyEvent.Keycode == Key.Key2 && _tier2Unlocked && !_tier2Used)
			{
				CallInA10(2);
			}
			else if (keyEvent.Keycode == Key.Key3 && _tier3Unlocked && !_tier3Used)
			{
				CallInA10(3);
			}
		}
	}

	private void CallInA10(int tier)
	{
		if (tier == 1) _tier1Used = true;
		else if (tier == 2) _tier2Used = true;
		else if (tier == 3) _tier3Used = true;
		
		EventBus.OnScorestreakUpdated?.Invoke(
			_currentScore, 
			_tier1Unlocked && !_tier1Used, 
			_tier2Unlocked && !_tier2Used, 
			_tier3Unlocked && !_tier3Used
		);

		GetNode("/root/MainMap").RpcId(1, "RequestA10Strike");
	}
}
