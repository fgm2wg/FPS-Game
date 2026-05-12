using Godot;
using System;

public partial class MultiplayerManager : Node
{
	[Export] public PackedScene LobbyScene { get; set; } 

	private ENetMultiplayerPeer _peer;

	public override void _Ready()
	{
		EventBus.OnHostRequested += HostGame;
		EventBus.OnJoinRequested += JoinGame;

		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;

		Multiplayer.ConnectedToServer += OnConnectedToServer;
		Multiplayer.ConnectionFailed += OnConnectionFailed;
		Multiplayer.ServerDisconnected += OnServerDisconnected;
	}

	public override void _ExitTree()
	{
		EventBus.OnHostRequested -= HostGame;
		EventBus.OnJoinRequested -= JoinGame;
	}

	private void HostGame(string ip, int port, int maxPlayers)
	{
		_peer = new ENetMultiplayerPeer();
		Error error = _peer.CreateServer(port, maxPlayers);
		
		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to host: {error}");
			return;
		}

		Multiplayer.MultiplayerPeer = _peer;
		GD.Print($"Hosting on Port {port} with {maxPlayers} max players.");
		
		LoadLobbyScene();
	}

	private void JoinGame(string ip, int port)
	{
		_peer = new ENetMultiplayerPeer();
		Error error = _peer.CreateClient(ip, port);
		
		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to join: {error}");
			return;
		}

		Multiplayer.MultiplayerPeer = _peer;
		GD.Print($"Attempting to join {ip}:{port}...");
	}

	private void OnConnectedToServer()
	{
		GD.Print("Successfully connected to the server!");
		LoadLobbyScene();
	}

	private void OnConnectionFailed()
	{
		GD.PrintErr("Failed to connect to the server.");
	}

	private void OnServerDisconnected()
	{
		GD.Print("Disconnected from the server.");
		GetTree().ChangeSceneToFile("res://UI/MainMenu/MainMenu.tscn");
	}

	private void OnPeerConnected(long id)
	{
		GD.Print($"Player {id} connected!");
		EventBus.OnPlayerConnected?.Invoke(id);
	}

	private void OnPeerDisconnected(long id)
	{
		GD.Print($"Player {id} disconnected.");
		EventBus.OnPlayerDisconnected?.Invoke(id);
	}

	private void LoadLobbyScene()
	{
		if (LobbyScene == null)
		{
			GD.PrintErr("LobbyScene is not assigned in the MultiplayerManager!");
			return;
		}

		if (GetTree().CurrentScene.SceneFilePath != LobbyScene.ResourcePath)
		{
			GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToPacked, LobbyScene);
		}
	}
}
