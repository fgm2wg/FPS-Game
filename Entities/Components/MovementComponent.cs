using Godot;

public partial class MovementComponent : Node
{
	[Export] private float _speed = 5.0f;
	[Export] private float _jumpVelocity = 4.5f;

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

		Vector2 inputDir = Input.GetVector(InputMapKeys.MoveLeft, InputMapKeys.MoveRight, InputMapKeys.MoveForward, InputMapKeys.MoveBackward);
		Vector3 direction = (_body.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * _speed;
			velocity.Z = direction.Z * _speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(_body.Velocity.X, 0, _speed);
			velocity.Z = Mathf.MoveToward(_body.Velocity.Z, 0, _speed);
		}

		_body.Velocity = velocity;
		_body.MoveAndSlide();
	}
}
