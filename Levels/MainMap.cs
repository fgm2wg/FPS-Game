using Godot;
using System;

public partial class MainMap : Node3D
{
	[Export] private Node3D _playersContainer;
	[Export] private PackedScene _playerScene;

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
}
