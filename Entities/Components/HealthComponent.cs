using Godot;
using System;

public partial class HealthComponent : Node
{
	[Export] public float MaxHealth = 100f;
	[Export] private PackedScene _damageIndicatorScene;
	[Export] private PackedScene _corpseScene;
	
	[ExportCategory("Regeneration")]
	[Export] public float RegenDelay = 5.0f;
	[Export] public float RegenRate = 15.0f;

	public float CurrentHealth { get; private set; }
	private CharacterBody3D _parentPlayer;
	private double _timeSinceLastDamage = 0;

	public override void _Ready()
	{
		CurrentHealth = MaxHealth;
		_parentPlayer = GetParent<CharacterBody3D>();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void RequestTakeDamage(float amount, long shooterId, string weaponName, Vector3 shooterPos)
	{
		if (!Multiplayer.IsServer()) return;
		
		if (CurrentHealth <= 0) return;

		long myId = long.Parse(_parentPlayer.Name);

		if (GameManager.Players.ContainsKey(myId) && GameManager.Players.ContainsKey(shooterId))
		{
			if (GameManager.Players[myId].Team == GameManager.Players[shooterId].Team)
			{
				return; 
			}
		}

		CurrentHealth -= amount;
		if (CurrentHealth < 0) CurrentHealth = 0;
		
		_timeSinceLastDamage = 0;

		Rpc(MethodName.BroadcastDamageVisuals, amount);
		RpcId(myId, MethodName.ClientUpdateHealthUI, CurrentHealth, MaxHealth);

		if (CurrentHealth <= 0)
		{
			GD.Print($"Player {myId} was killed by {shooterId} with {weaponName}!");
			
			Vector3 knockbackDir = (_parentPlayer.GlobalPosition - shooterPos).Normalized();
			knockbackDir.Y = 0.25f;
			knockbackDir = knockbackDir.Normalized();

			Rpc(MethodName.BroadcastDeathPhysics, knockbackDir);
			Rpc(MethodName.BroadcastKillfeed, shooterId, long.Parse(_parentPlayer.Name), weaponName);

			int myTeam = GameManager.Players[myId].Team;
			string targetGroup = myTeam == 0 ? "Team1Spawns" : "Team2Spawns";

			var spawnNodes = GetTree().GetNodesInGroup(targetGroup);
			Vector3 respawnPosition = new Vector3(0, 2.0f, 0);

			if (spawnNodes.Count > 0)
			{
				Random rng = new Random();
				int randomIndex = rng.Next(spawnNodes.Count);
				
				if (spawnNodes[randomIndex] is Marker3D spawnMarker)
				{
					respawnPosition = spawnMarker.GlobalPosition;
				}
			}

			GetTree().CreateTimer(3.0f).Timeout += () => 
			{
				Rpc(MethodName.BroadcastRespawn, respawnPosition);
			};
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void BroadcastRespawn(Vector3 respawnPos)
	{
		if (Multiplayer.GetRemoteSenderId() != 1 && Multiplayer.GetRemoteSenderId() != 0) return;

		CurrentHealth = MaxHealth;

		_parentPlayer.Visible = true;
		_parentPlayer.SetCollisionLayerValue(1, true);
		_parentPlayer.SetCollisionMaskValue(1, true);

		if (_parentPlayer.GetMultiplayerAuthority() == Multiplayer.GetUniqueId())
		{
			_parentPlayer.GlobalPosition = respawnPos;
			_parentPlayer.Velocity = Vector3.Zero;

			Camera3D mainCam = _parentPlayer.GetNodeOrNull<Camera3D>("CameraPivot/Camera3D");
			if (mainCam != null) mainCam.Current = true;
			
			EventBus.OnLocalPlayerHealthChanged?.Invoke(CurrentHealth, MaxHealth);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void BroadcastDamageVisuals(float amount)
	{
		if (_damageIndicatorScene != null)
		{
			DamageIndicator indicator = _damageIndicatorScene.Instantiate<DamageIndicator>();
			GetTree().CurrentScene.AddChild(indicator);

			Random rng = new Random();
			float randomX = (float)(rng.NextDouble() * 0.6 - 0.3);
			float randomZ = (float)(rng.NextDouble() * 0.6 - 0.3);

			Vector3 spawnPos = _parentPlayer.GlobalPosition + new Vector3(randomX, 1.5f, randomZ);
			indicator.Setup(spawnPos, amount);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void BroadcastKillfeed(long killerId, long victimId, string weaponName)
	{
		if (Multiplayer.GetRemoteSenderId() != 1 && Multiplayer.GetRemoteSenderId() != 0) return;

		string killerName = GameManager.Players.ContainsKey(killerId) ? GameManager.Players[killerId].Name : "Unknown";
		string victimName = GameManager.Players.ContainsKey(victimId) ? GameManager.Players[victimId].Name : "Unknown";
		EventBus.OnPlayerKilled?.Invoke(killerId, killerName, victimId, victimName, weaponName);
	}
	
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void BroadcastDeathPhysics(Vector3 knockbackDir)
	{
		if (Multiplayer.GetRemoteSenderId() != 1 && Multiplayer.GetRemoteSenderId() != 0) return;

		_parentPlayer.Visible = false;
		_parentPlayer.SetCollisionLayerValue(1, false);
		_parentPlayer.Velocity = Vector3.Zero;

		if (_corpseScene != null)
		{
			Corpse corpse = _corpseScene.Instantiate<Corpse>();
			GetTree().CurrentScene.AddChild(corpse);
			corpse.GlobalPosition = _parentPlayer.GlobalPosition;
			
			long myId = long.Parse(_parentPlayer.Name);
			if (GameManager.Players.ContainsKey(myId))
			{
				int myTeam = GameManager.Players[myId].Team;
				Color teamColor = myTeam == 0 ? Colors.Blue : Colors.Red; 
				corpse.SetColor(teamColor);
			}

			float flingForce = 25.0f;
			Vector3 forceToApply = knockbackDir * flingForce;
			Vector3 hitOffset = new Vector3(0, 0.1f, 0);
			corpse.ApplyImpulse(forceToApply, hitOffset);
			corpse.ApplyTorqueImpulse(new Vector3((float)GD.RandRange(-1, 1), (float)GD.RandRange(-1, 1), (float)GD.RandRange(-1, 1)));

			if (_parentPlayer.GetMultiplayerAuthority() == Multiplayer.GetUniqueId())
			{
				Camera3D mainCam = _parentPlayer.GetNodeOrNull<Camera3D>("CameraPivot/Camera3D");
				if (mainCam != null) mainCam.Current = false;
				corpse.ActivateCamera();
			}
		}
	}
	
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void ClientUpdateHealthUI(float current, float max)
	{
		EventBus.OnLocalPlayerHealthChanged?.Invoke(current, max);
	}
	
	public override void _Process(double delta)
	{
		if (!Multiplayer.IsServer()) return;

		_timeSinceLastDamage += delta;

		if (_timeSinceLastDamage >= RegenDelay && CurrentHealth < MaxHealth)
		{
			CurrentHealth += RegenRate * (float)delta;
			if (CurrentHealth > MaxHealth) CurrentHealth = MaxHealth;

			long myId = long.Parse(_parentPlayer.Name);
			RpcId(myId, MethodName.ClientUpdateHealthUI, CurrentHealth, MaxHealth);
		}
	}
}
