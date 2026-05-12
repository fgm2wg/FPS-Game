using Godot;
using System;

public partial class LookComponent : Node3D
{
	[Export] private float _mouseSensitivity = 0.002f;
	[Export] private float _minPitch = -89f; 
	[Export] private float _maxPitch = 89f;  

	private CharacterBody3D _body;

	public override void _Ready()
	{
		_body = GetParent<CharacterBody3D>();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_body.IsMultiplayerAuthority()) return;

		if (Input.MouseMode != Input.MouseModeEnum.Captured) return;

		if (@event is InputEventMouseMotion mouseMotion)
		{
			_body.RotateY(-mouseMotion.Relative.X * _mouseSensitivity);

			RotateX(-mouseMotion.Relative.Y * _mouseSensitivity);

			Vector3 currentRotation = Rotation;
			currentRotation.X = Mathf.Clamp(currentRotation.X, Mathf.DegToRad(_minPitch), Mathf.DegToRad(_maxPitch));
			Rotation = currentRotation;
		}
	}
}
