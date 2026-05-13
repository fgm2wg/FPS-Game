using Godot;
using System;

public partial class HealthComponent : Node
{
	[Export] public float MaxHealth = 100f;
	[Export] private PackedScene _damageIndicatorScene;

	public float CurrentHealth { get; private set; }
	private CharacterBody3D _parentPlayer;

	public override void _Ready()
	{
		CurrentHealth = MaxHealth;
		_parentPlayer = GetParent<CharacterBody3D>();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void RequestTakeDamage(float amount, long shooterId, string weaponName)
	{
		if (!Multiplayer.IsServer()) return;

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

		Rpc(MethodName.BroadcastDamageVisuals, amount);

		if (CurrentHealth <= 0)
		{
			GD.Print($"Player {myId} was killed by {shooterId} with {weaponName}!");

			CurrentHealth = MaxHealth;

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

			RpcId(myId, MethodName.ClientPerformRespawn, respawnPosition);
			Rpc(MethodName.BroadcastKillfeed, shooterId, myId, weaponName);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void ClientPerformRespawn(Vector3 respawnPos)
	{
		if (Multiplayer.GetRemoteSenderId() != 1 && Multiplayer.GetRemoteSenderId() != 0) return;

		CurrentHealth = MaxHealth;
		_parentPlayer.GlobalPosition = respawnPos;
		_parentPlayer.Velocity = Vector3.Zero;
		
		GD.Print("I have respawned at: " + respawnPos);
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
		string killerName = GameManager.Players.ContainsKey(killerId) ? GameManager.Players[killerId].Name : "Unknown";
		string victimName = GameManager.Players.ContainsKey(victimId) ? GameManager.Players[victimId].Name : "Unknown";
		EventBus.OnPlayerKilled?.Invoke(killerId, killerName, victimId, victimName, weaponName);
	}
}
