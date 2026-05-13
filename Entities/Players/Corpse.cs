using Godot;

public partial class Corpse : RigidBody3D
{
	public override void _Ready()
	{
		GetTree().CreateTimer(3.0f).Timeout += () => QueueFree();
	}

	public void ActivateCamera()
	{
		Camera3D cam = GetNodeOrNull<Camera3D>("Camera3D");
		if (cam != null) cam.Current = true;
	}
}
