using Godot;
using System;

public partial class MainMenu : Control
{
	[Export] private LineEdit _addressInput;
	[Export] private LineEdit _maxPlayersInput;
	[Export] private Button _hostButton;
	[Export] private Button _joinButton;
	[Export] private LineEdit _nameInput;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;

		_addressInput.Text = "127.0.0.1:1234";
		_maxPlayersInput.Text = "12";

		_nameInput.Text = "";
		_nameInput.PlaceholderText = "Enter Name (Optional)";

		_hostButton.Pressed += OnHostPressed;
		_joinButton.Pressed += OnJoinPressed;
	}

	private void OnHostPressed()
	{
		GameManager.LocalPlayerName = _nameInput.Text.Trim();

		ParseAddress(out string ip, out int port);
		int maxPlayers = int.TryParse(_maxPlayersInput.Text, out int parsedPlayers) ? parsedPlayers : 12;
		EventBus.OnHostRequested?.Invoke(ip, port, maxPlayers);
	}

	private void OnJoinPressed()
	{
		GameManager.LocalPlayerName = _nameInput.Text.Trim();

		ParseAddress(out string ip, out int port);
		EventBus.OnJoinRequested?.Invoke(ip, port);
	}

	private void ParseAddress(out string ip, out int port)
	{
		ip = "127.0.0.1";
		port = 1234;
		
		string[] parts = _addressInput.Text.Split(':');
		if (parts.Length == 2)
		{
			ip = parts[0];
			int.TryParse(parts[1], out port);
		}
	}
}
