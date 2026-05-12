using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Lobby : Control
{
	[Export] private PackedScene _playerEntryScene;
	[Export] private VBoxContainer _teamAContainer;
	[Export] private VBoxContainer _teamBContainer;
	[Export] private Control _teamANoPlayerRow;
	[Export] private Control _teamBNoPlayerRow;
	[Export] private Button _readyButton;
	[Export] private Label _matchStartLabel;

	private bool _isLocalPlayerReady = false;
	private double _countdownTime = 5.0;
	private bool _isCountdownActive = false;

	public override void _Ready()
	{
		_readyButton.Pressed += OnReadyButtonPressed;
		
		if (_matchStartLabel != null)
			_matchStartLabel.Visible = false;

		if (Multiplayer.IsServer())
		{
			Multiplayer.PeerConnected += OnPeerConnected;
			Multiplayer.PeerDisconnected += OnPeerDisconnected;
			AddPlayerToServerList(Multiplayer.GetUniqueId());
		}
		else
		{
			RpcId(1, MethodName.RegisterPlayerName, GameManager.LocalPlayerName);
		}
	}

	public override void _Process(double delta)
	{
		if (!Multiplayer.IsServer() || !_isCountdownActive) return;

		_countdownTime -= delta;
		
		Rpc(MethodName.UpdateTimerUI, _countdownTime);

		if (_countdownTime <= 0)
		{
			_isCountdownActive = false;
			Rpc(MethodName.LoadGameScene);
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
		if (GameManager.Players.ContainsKey(id))
		{
			GameManager.Players.Remove(id);
			BroadcastLobbyState();
			CheckAllPlayersReady(); 
		}
	}

	private void AddPlayerToServerList(long id)
	{
		int teamACount = GameManager.Players.Values.Count(p => p.Team == 0);
		int teamBCount = GameManager.Players.Values.Count(p => p.Team == 1);
		int assignedTeam = (teamACount <= teamBCount) ? 0 : 1;

		string finalName = id == 1 
			? (string.IsNullOrWhiteSpace(GameManager.LocalPlayerName) ? $"Player {id}" : GameManager.LocalPlayerName) 
			: $"Player {id}";

		GameManager.Players[id] = new PlayerData
		{
			Id = id,
			Name = finalName,
			Team = assignedTeam,
			IsReady = false
		};

		BroadcastLobbyState();
		CheckAllPlayersReady();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void ReceiveReadyState(bool isReady)
	{
		if (!Multiplayer.IsServer()) return;

		long senderId = Multiplayer.GetRemoteSenderId();
		if (GameManager.Players.ContainsKey(senderId))
		{
			GameManager.Players[senderId].IsReady = isReady;
			BroadcastLobbyState();
			CheckAllPlayersReady(); 
		}
	}

	private void BroadcastLobbyState()
	{
		var godotArray = new Godot.Collections.Array<Godot.Collections.Dictionary>();

		foreach (var p in GameManager.Players.Values)
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
		bool allReady = GameManager.Players.Count > 0 && GameManager.Players.Values.All(p => p.IsReady);

		if (allReady)
		{
			if (!_isCountdownActive)
			{
				_isCountdownActive = true;
				_countdownTime = 5.0;
				Rpc(MethodName.ToggleTimerVisibility, true);
			}
		}
		else
		{
			if (_isCountdownActive)
			{
				_isCountdownActive = false;
				Rpc(MethodName.ToggleTimerVisibility, false);
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void ToggleTimerVisibility(bool isVisible)
	{
		if (_matchStartLabel != null)
		{
			_matchStartLabel.Visible = isVisible;
			if (isVisible) _matchStartLabel.Text = "Match Starting in 5.0s...";
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void UpdateTimerUI(double timeLeft)
	{
		if (_matchStartLabel != null && _matchStartLabel.Visible)
		{
			_matchStartLabel.Text = $"Match Starting in {timeLeft:0.0}s...";
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void LoadGameScene()
	{
		GetTree().ChangeSceneToFile("res://Levels/MainMap.tscn");
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void UpdateClientUI(string jsonState)
	{
		foreach (Node child in _teamAContainer.GetChildren())
		{
			if (child != _teamANoPlayerRow) child.QueueFree();
		}
		foreach (Node child in _teamBContainer.GetChildren())
		{
			if (child != _teamBNoPlayerRow) child.QueueFree();
		}

		Json json = new Json();
		json.Parse(jsonState);
		var parsedData = json.Data.AsGodotArray<Godot.Collections.Dictionary>();

		int teamAIndex = 0;
		int teamBIndex = 0;

		GameManager.Players.Clear();

		foreach (var p in parsedData)
		{
			long pId = p["Id"].AsInt64();
			string pName = p["Name"].AsString();
			int pTeam = p["Team"].AsInt32();
			bool pReady = p["IsReady"].AsBool();

			GameManager.Players[pId] = new PlayerData { Id = pId, Name = pName, Team = pTeam, IsReady = pReady };

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

		if (_teamANoPlayerRow != null) _teamANoPlayerRow.Visible = (teamAIndex == 0);
		if (_teamBNoPlayerRow != null) _teamBNoPlayerRow.Visible = (teamBIndex == 0);
	}
	
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void RegisterPlayerName(string chosenName)
	{
		if (!Multiplayer.IsServer()) return;

		long senderId = Multiplayer.GetRemoteSenderId();
		if (GameManager.Players.ContainsKey(senderId))
		{
			string finalName = string.IsNullOrWhiteSpace(chosenName) ? $"Player {senderId}" : chosenName;
			
			GameManager.Players[senderId].Name = finalName;
			BroadcastLobbyState();
		}
	}
}
