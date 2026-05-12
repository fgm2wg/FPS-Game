using Godot;
using System;

public partial class FPSOverlay : Label
{
	public override void _Ready()
	{
		if (LabelSettings == null)
			LabelSettings = new LabelSettings();
	}

	public override void _Process(double delta)
	{
		double fps = Engine.GetFramesPerSecond();
		Text = $"{fps} FPS";

		if (fps >= 100) 
			LabelSettings.FontColor = new Color("#00FF00");
		else if (fps >= 60) 
			LabelSettings.FontColor = new Color("#FFFF00");
		else 
			LabelSettings.FontColor = new Color("#FF0000");
	}
}
