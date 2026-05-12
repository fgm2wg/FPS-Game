using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Lobby : Control
{
	[Export] private PackedScene _playerEntryScene;
	[Export] private VBoxContainer _teamAContainer;
	[Export] private VBoxContainer _teamBContainer;
	[Export] private Button _readyButton;

	private Dictionary<long, PlayerData> _serverPlayers = new Dictionary<long, PlayerData>();
	private bool _isLocalPlayerReady = false;

	public override void _Ready()
	{
		_readyButton.Pressed += OnReadyButtonPressed;

		if (Multiplayer.IsServer())
		{
			Multiplayer.PeerConnected += OnPeerConnected;
			Multiplayer.PeerDisconnected += OnPeerDisconnected;
			
			AddPlayerToServerList(Multiplayer.GetUniqueId());
		}
	}

	private void OnReadyButtonPressed()
	{
		_isLocalPlayerReady = !_isLocalPlayerReady;
		_readyButton.Text = _isLocalPlayerReady ? "Unready" : "Ready";
		
		RpcId(1, MethodName.ReceiveReadyState, _isLocalPlayerReady);
	}

	private void OnPeerConnected(long id)
	{
		AddPlayerToServerList(id);
	}

	private void OnPeerDisconnected(long id)
	{
		if (_serverPlayers.ContainsKey(id))
		{
			_serverPlayers.Remove(id);
			BroadcastLobbyState();
		}
	}

	private void AddPlayerToServerList(long id)
	{
		int teamACount = _serverPlayers.Values.Count(p => p.Team == 0);
		int teamBCount = _serverPlayers.Values.Count(p => p.Team == 1);

		int assignedTeam = (teamACount <= teamBCount) ? 0 : 1;

		_serverPlayers[id] = new PlayerData
		{
			Id = id,
			Name = $"Player {id}",
			Team = assignedTeam,
			IsReady = false
		};

		BroadcastLobbyState();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void ReceiveReadyState(bool isReady)
	{
		if (!Multiplayer.IsServer()) return;

		long senderId = Multiplayer.GetRemoteSenderId();
		if (_serverPlayers.ContainsKey(senderId))
		{
			_serverPlayers[senderId].IsReady = isReady;
			BroadcastLobbyState();
			
			CheckAllPlayersReady(); 
		}
	}

	private void BroadcastLobbyState()
	{
		var godotArray = new Godot.Collections.Array<Godot.Collections.Dictionary>();

		foreach (var p in _serverPlayers.Values)
		{
			var playerDict = new Godot.Collections.Dictionary
			{
				{ "Id", p.Id }, 
				{ "Name", p.Name }, 
				{ "Team", p.Team }, 
				{ "IsReady", p.IsReady }
			};
			
			godotArray.Add(playerDict);
		}

		string jsonString = Json.Stringify(godotArray);
		Rpc(MethodName.UpdateClientUI, jsonString);
	}

	private void CheckAllPlayersReady()
	{
		if (_serverPlayers.Count > 0 && _serverPlayers.Values.All(p => p.IsReady))
		{
			GD.Print("Everyone is ready! Start 5s countdown...");
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void UpdateClientUI(string jsonState)
	{
		foreach (Node child in _teamAContainer.GetChildren()) child.QueueFree();
		foreach (Node child in _teamBContainer.GetChildren()) child.QueueFree();

		Json json = new Json();
		json.Parse(jsonState);
		var parsedData = json.Data.AsGodotArray<Godot.Collections.Dictionary>();

		int teamAIndex = 0;
		int teamBIndex = 0;

		foreach (var p in parsedData)
		{
			string pName = p["Name"].AsString();
			int pTeam = p["Team"].AsInt32();
			bool pReady = p["IsReady"].AsBool();

			PlayerLobbyEntry entry = _playerEntryScene.Instantiate<PlayerLobbyEntry>();

			if (pTeam == 0)
			{
				bool useAltColor = teamAIndex % 2 != 0;
				entry.UpdateData(pName, pReady, useAltColor);
				_teamAContainer.AddChild(entry);
				teamAIndex++;
			}
			else
			{
				bool useAltColor = teamBIndex % 2 != 0;
				entry.UpdateData(pName, pReady, useAltColor);
				_teamBContainer.AddChild(entry);
				teamBIndex++;
			}
		}
	}
}
