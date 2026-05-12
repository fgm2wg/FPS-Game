using Godot;
using System;

public partial class DamageIndicator : Label3D
{
	public void Setup(Vector3 spawnPosition, float damageAmount)
	{
		GlobalPosition = spawnPosition;
		Text = $"-{damageAmount}";

		Tween tween = CreateTween();

		Random rng = new Random();
		float driftX = (float)(rng.NextDouble() * 1.0 - 0.5); 
		float driftZ = (float)(rng.NextDouble() * 1.0 - 0.5); 

		Vector3 targetPosition = GlobalPosition + new Vector3(driftX, -1.5f, driftZ);

		tween.TweenProperty(this, "global_position", targetPosition, 1.0f);
		tween.Parallel().TweenProperty(this, "modulate:a", 0.0f, 1.0f);
		tween.TweenCallback(Callable.From(QueueFree));
	}
}
