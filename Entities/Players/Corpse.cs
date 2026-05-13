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
	
	public void SetColor(Color teamColor)
	{
		MeshInstance3D mesh = GetNodeOrNull<MeshInstance3D>("Body");
		if (mesh != null)
		{
			StandardMaterial3D mat = new StandardMaterial3D();
			mat.AlbedoColor = teamColor;
			mesh.MaterialOverride = mat;
		}
	}
}
