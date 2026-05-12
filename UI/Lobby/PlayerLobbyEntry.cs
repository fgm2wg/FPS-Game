using Godot;
using System;

public partial class PlayerLobbyEntry : PanelContainer
{
	[Export] private Label _nameLabel;
	[Export] private Label _statusLabel;

	public void UpdateData(string playerName, bool isReady, bool useAlternateColor)
	{
		_nameLabel.Text = playerName;
		
		if (isReady)
		{
			_statusLabel.Text = "Ready";
			_statusLabel.AddThemeColorOverride("font_color", new Color("00ff00"));
		}
		else
		{
			_statusLabel.Text = "Not Ready";
			_statusLabel.AddThemeColorOverride("font_color", new Color("ffffff"));
		}

		StyleBoxFlat styleBox = new StyleBoxFlat();
		styleBox.BgColor = useAlternateColor ? new Color("2a2a2a99") : new Color("1a1a1a99");
		AddThemeStyleboxOverride("panel", styleBox);
	}
}
