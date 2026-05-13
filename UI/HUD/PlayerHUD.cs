using Godot;

public partial class PlayerHUD : CanvasLayer
{
	[Export] private ColorRect _crosshair;
	[Export] private Label _ammoLabel;
	[Export] private Label _fireModeLabel;
	[Export] private VBoxContainer _killfeedContainer;
	[Export] private PackedScene _killfeedEntryScene;

	public override void _Ready()
	{
		if (_crosshair != null)
		{
			_crosshair.PivotOffset = _crosshair.Size / 2.0f;
		}

		EventBus.OnAmmoChanged += UpdateAmmoDisplay;
		EventBus.OnFireModeChanged += UpdateFireModeDisplay;
		EventBus.OnPlayerKilled += HandlePlayerKilled;
	}

	public override void _ExitTree()
	{
		EventBus.OnAmmoChanged -= UpdateAmmoDisplay;
		EventBus.OnFireModeChanged -= UpdateFireModeDisplay;
		EventBus.OnPlayerKilled -= HandlePlayerKilled;
	}

	private void UpdateAmmoDisplay(int currentAmmo, int maxAmmo)
	{
		if (_ammoLabel != null)
		{
			_ammoLabel.Text = $"{currentAmmo} / {maxAmmo}";
		}
	}
	
	private void UpdateFireModeDisplay(string modeText)
	{
		if (_fireModeLabel != null)
		{
			_fireModeLabel.Text = modeText;
		}
	}
	
	private void HandlePlayerKilled(long killerId, string killerName, long victimId, string victimName, string weaponName)
	{
		if (_killfeedEntryScene != null && _killfeedContainer != null)
		{
			KillfeedEntry entry = _killfeedEntryScene.Instantiate<KillfeedEntry>();
			_killfeedContainer.AddChild(entry);
			entry.Setup(killerId, killerName, victimId, victimName, weaponName);
		}
	}

	public override void _Process(double delta)
	{
		if (_crosshair != null)
		{
			Vector2 currentZoom = GetViewport().GetScreenTransform().Scale;
			_crosshair.Scale = new Vector2(1.0f / currentZoom.X, 1.0f / currentZoom.Y);
		}
	}
}
