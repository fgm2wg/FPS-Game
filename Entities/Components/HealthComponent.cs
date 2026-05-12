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
	public void RequestTakeDamage(float amount, long shooterId)
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
			GD.Print($"Player {myId} was killed by {shooterId}!");

			CurrentHealth = MaxHealth;

			Random rng = new Random();
			float randomX = (float)(rng.NextDouble() * 20 - 10);
			float randomZ = (float)(rng.NextDouble() * 20 - 10);
			Vector3 respawnPosition = new Vector3(randomX, 2.0f, randomZ);

			RpcId(myId, MethodName.ClientPerformRespawn, respawnPosition);
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
}
