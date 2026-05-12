using Godot;
using System;

public partial class PauseMenu : CanvasLayer
{
	[Export] private Button _quitButton;

	public override void _Ready()
	{
		Visible = false;
		
		EventBus.OnPauseMenuToggled += ToggleMenu;

		if (_quitButton != null)
		{
			_quitButton.Pressed += OnQuitButtonPressed;
		}
	}

	public override void _ExitTree()
	{
		EventBus.OnPauseMenuToggled -= ToggleMenu;
	}

	private void ToggleMenu(bool isPaused)
	{
		Visible = isPaused;
	}

	private void OnQuitButtonPressed()
	{
		GetTree().Quit();
	}
}
