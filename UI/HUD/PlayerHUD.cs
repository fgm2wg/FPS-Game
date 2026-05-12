using Godot;

public partial class PlayerHUD : CanvasLayer
{
	[Export] private ColorRect _crosshair;
	[Export] private Label _ammoLabel;
	[Export] private Label _fireModeLabel;

	public override void _Ready()
	{
		if (_crosshair != null)
		{
			_crosshair.PivotOffset = _crosshair.Size / 2.0f;
		}

		EventBus.OnAmmoChanged += UpdateAmmoDisplay;
		EventBus.OnFireModeChanged += UpdateFireModeDisplay;
	}

	public override void _ExitTree()
	{
		EventBus.OnAmmoChanged -= UpdateAmmoDisplay;
		EventBus.OnFireModeChanged -= UpdateFireModeDisplay;
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

	public override void _Process(double delta)
	{
		if (_crosshair != null)
		{
			Vector2 currentZoom = GetViewport().GetScreenTransform().Scale;
			_crosshair.Scale = new Vector2(1.0f / currentZoom.X, 1.0f / currentZoom.Y);
		}
	}
}
