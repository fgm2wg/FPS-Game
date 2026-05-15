using Godot;

public partial class Corpse : RigidBody3D
{
	private Camera3D _cam;
	private Vector3 _camOffset;

	public override void _Ready()
	{
		_cam = GetNodeOrNull<Camera3D>("Camera3D");
		if (_cam != null)
		{
			_camOffset = _cam.GlobalPosition - GlobalPosition;
			_cam.TopLevel = true; 
		}

		GetTree().CreateTimer(3.0f).Timeout += () => QueueFree();
	}

	public override void _Process(double delta)
	{
		if (_cam != null)
		{
			_cam.GlobalPosition = GlobalPosition + _camOffset;
		}
	}

	public void ActivateCamera()
	{
		if (_cam != null) _cam.Current = true;
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
