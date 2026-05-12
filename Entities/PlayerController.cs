using Godot;

public partial class PlayerController : CharacterBody3D
{
	[Export] private Camera3D _camera;
	[Export] private Node _movementComponent;

	public override void _EnterTree()
	{
		SetMultiplayerAuthority(int.Parse(Name));
	}

	public override void _Ready()
	{
		if (!IsMultiplayerAuthority())
		{
			_camera.Current = false;
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
		else
		{
			_camera.Current = true;
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
	}
}
