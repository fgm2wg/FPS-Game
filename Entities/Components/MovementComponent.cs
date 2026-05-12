using Godot;

public partial class MovementComponent : Node
{
	[Export] private float _walkSpeed = 10.0f; 
	[Export] private float _sprintSpeed = 20.0f;
	[Export] private float _jumpVelocity = 10.0f;

	private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	private CharacterBody3D _body;

	public override void _Ready()
	{
		_body = GetParent<CharacterBody3D>();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_body.IsMultiplayerAuthority()) return;

		Vector3 velocity = _body.Velocity;

		if (!_body.IsOnFloor())
			velocity.Y -= _gravity * (float)delta;

		if (Input.IsActionJustPressed(InputMapKeys.Jump) && _body.IsOnFloor())
			velocity.Y = _jumpVelocity;

		float currentSpeed = Input.IsActionPressed(InputMapKeys.Sprint) ? _sprintSpeed : _walkSpeed;

		Vector2 inputDir = Input.GetVector(InputMapKeys.MoveLeft, InputMapKeys.MoveRight, InputMapKeys.MoveForward, InputMapKeys.MoveBackward);
		Vector3 direction = (_body.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * currentSpeed;
			velocity.Z = direction.Z * currentSpeed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(_body.Velocity.X, 0, currentSpeed);
			velocity.Z = Mathf.MoveToward(_body.Velocity.Z, 0, currentSpeed);
		}

		_body.Velocity = velocity;
		_body.MoveAndSlide();
	}
}
