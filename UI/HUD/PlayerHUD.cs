using Godot;

public partial class PlayerHUD : CanvasLayer
{
	[Export] private ColorRect _crosshair;
	[Export] private Label _ammoLabel;
	[Export] private Label _fireModeLabel;
	[Export] private VBoxContainer _killfeedContainer;
	[Export] private PackedScene _killfeedEntryScene;
	[Export] private ProgressBar _healthBar;
	[Export] private Label _healthBarLabel;

	public override void _Ready()
	{
		if (_crosshair != null)
		{
			_crosshair.PivotOffset = _crosshair.Size / 2.0f;
		}

		EventBus.OnAmmoChanged += UpdateAmmoDisplay;
		EventBus.OnFireModeChanged += UpdateFireModeDisplay;
		EventBus.OnPlayerKilled += HandlePlayerKilled;
		EventBus.OnLocalPlayerHealthChanged += UpdateHealthBar;
	}

	public override void _ExitTree()
	{
		EventBus.OnAmmoChanged -= UpdateAmmoDisplay;
		EventBus.OnFireModeChanged -= UpdateFireModeDisplay;
		EventBus.OnPlayerKilled -= HandlePlayerKilled;
		EventBus.OnLocalPlayerHealthChanged -= UpdateHealthBar;
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
	
	private void UpdateHealthBar(float currentHealth, float maxHealth)
	{
		if (_healthBar != null)
		{
			_healthBar.MaxValue = maxHealth;
			_healthBar.Value = currentHealth;
			
			if (_healthBar.GetThemeStylebox("fill") is StyleBoxFlat fillStyle)
			{
				if (currentHealth <= 35)
				{
					fillStyle.BgColor = Color.FromHtml("#a82020");
				}
				else if (currentHealth <= 65)
				{
					fillStyle.BgColor = Color.FromHtml("#cca300");
				}
				else
				{
					fillStyle.BgColor = Color.FromHtml("#1a851a");
				}
			}
		}
		
		if (_healthBarLabel != null)
		{
			_healthBarLabel.Text = $"{currentHealth:F0} / {maxHealth:F0}";
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
