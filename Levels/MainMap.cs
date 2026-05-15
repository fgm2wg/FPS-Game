using Godot;
using System;

public partial class MainMap : Node3D
{
	[Export] private Node3D _playersContainer;
	[Export] private PackedScene _playerScene;
	[Export] private PackedScene _a10Scene;

	public override void _Ready()
	{
		if (!Multiplayer.IsServer()) return;

		SpawnPlayer(Multiplayer.GetUniqueId());

		foreach (long peerId in Multiplayer.GetPeers())
		{
			SpawnPlayer(peerId);
		}

		Multiplayer.PeerDisconnected += RemovePlayer;
	}

	private void SpawnPlayer(long id)
	{
		Node3D player = _playerScene.Instantiate<Node3D>();
		player.Name = id.ToString();
		player.SetMultiplayerAuthority((int)id);
		_playersContainer.AddChild(player, true); 
	}

	private void RemovePlayer(long id)
	{
		Node playerNode = _playersContainer.GetNodeOrNull(id.ToString());
		playerNode?.QueueFree();
	}
	
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void RequestA10Strike()
	{
		if (!Multiplayer.IsServer()) return;

		long callerId = Multiplayer.GetRemoteSenderId(); 
		Vector3 targetPos = Vector3.Zero;
		long victimId = -1;

		var players = GetTree().GetNodesInGroup("Player");
		var validVictims = new System.Collections.Generic.List<Node3D>();

		if (GameManager.Players.ContainsKey(callerId))
		{
			int myTeam = GameManager.Players[callerId].Team;
			foreach (Node3D p in players)
			{
				long pid = long.Parse(p.Name);
				if (pid != callerId && GameManager.Players.ContainsKey(pid) && GameManager.Players[pid].Team != myTeam)
				{
					HealthComponent health = p.GetNodeOrNull<HealthComponent>("HealthComponent");
					if (health != null && health.CurrentHealth > 0)
					{
						validVictims.Add(p);
					}
				}
			}
		}

		if (validVictims.Count > 0)
		{
			int randomIndex = GD.RandRange(0, validVictims.Count - 1);
			targetPos = validVictims[randomIndex].GlobalPosition;
			victimId = long.Parse(validVictims[randomIndex].Name);
			GD.Print($"A-10 Auto-Target locked onto Player {victimId}!");
		}
		else
		{
			targetPos = new Vector3(0, 0, 0); 
		}

		float randomAngle = (float)GD.RandRange(0, Mathf.Tau); 
		Vector2 randomDir = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
		Vector3 leadStart = targetPos + new Vector3(randomDir.X * 2500f, 1000f, randomDir.Y * 2500f);
		Vector3 leadTarget = targetPos;
		Vector3 flightDirection = (leadTarget - leadStart).Normalized();
		flightDirection.Y = 0;
		Vector3 rightDirection = Vector3.Up.Cross(flightDirection).Normalized();
		Vector3 wingmanStart = leadStart + (rightDirection * 60f) - (flightDirection * 200f);
		Vector3 wingmanTarget = leadTarget + (rightDirection * 30f);

		Rpc(MethodName.SpawnA10Rpc, leadStart, leadTarget, callerId, victimId, true);
		Rpc(MethodName.SpawnA10Rpc, wingmanStart, wingmanTarget, callerId, victimId, false);
	}
	
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void SpawnA10Rpc(Vector3 start, Vector3 target, long callerId, long victimId, bool isLead)
	{
		if (_a10Scene != null)
		{
			A10 a10 = _a10Scene.Instantiate<A10>();
			AddChild(a10);
			a10.Setup(start, target, callerId, victimId, isLead);
		}
	}
}
