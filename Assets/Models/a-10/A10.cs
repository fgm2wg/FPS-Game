using Godot;
using System;

public partial class A10 : Node3D
{
	[ExportCategory("Flight Settings")]
	[Export] public float Speed = 250f;
	[Export] public float DiveHeight = 1000f;
	[Export] public float TurnSpeed = 0.9f;
	
	[ExportCategory("Weapon Settings")]
	[Export] public PackedScene TracerScene;
	[Export] public Marker3D Muzzle;
	[Export] public AudioStreamPlayer3D GunAudio;
	[Export] public float FireRateRPM = 3900f;
	[Export] public float BulletSpeed = 800f;
	
	[ExportCategory("Cinematics")]
	[Export] public Camera3D StrikeCamera;
	
	private Node3D _targetPlayerNode;

	private Vector3 _startPos;
	private Vector3 _targetPos;
	private Vector3 _exitPos;
	private Vector3 _currentTargetPoint;
	private Vector3 _currentVelocity;

	private enum FlightPhase { Approach, DiveAndShoot, PullUp, Egress }
	private FlightPhase _currentPhase = FlightPhase.Approach;

	private double _timeSinceLastShot = 0;
	private long _callerId;
	private float _currentBankAngle = 0f;
	private bool _isLead = false;

	public void Setup(Vector3 start, Vector3 target, long callerId, long victimId, bool isLead)
	{
		_callerId = callerId;
		_isLead = isLead;
		
		if (Multiplayer.GetUniqueId() == _callerId && StrikeCamera != null && !_isLead)
		{
			StrikeCamera.Current = true;
		}

		_startPos = start;
		_targetPos = target;

		if (victimId != -1)
		{
			var players = GetTree().GetNodesInGroup("Player");
			foreach (Node3D p in players)
			{
				if (p.Name == victimId.ToString())
				{
					_targetPlayerNode = p;
					break;
				}
			}
		}

		Vector3 flyDirection = (target - start).Normalized();
		flyDirection.Y = 0; 
		_exitPos = target + (flyDirection * 2500f) + new Vector3(0, 1000f, 0);

		GlobalPosition = _startPos;
		_currentTargetPoint = _targetPos + new Vector3(0, DiveHeight, 0);
		
		_currentVelocity = (_currentTargetPoint - GlobalPosition).Normalized() * Speed;
		Vector3 safeUp = Mathf.Abs(_currentVelocity.Normalized().Y) > 0.99f ? Vector3.Forward : Vector3.Up;
		LookAt(GlobalPosition + _currentVelocity, safeUp);
	}

	private void Shoot(double delta)
	{
		if (TracerScene == null || Muzzle == null) return;

		_timeSinceLastShot += delta;
		float secondsPerShot = 60f / FireRateRPM;

		while (_timeSinceLastShot >= secondsPerShot)
		{
			_timeSinceLastShot -= secondsPerShot;
			
			Vector3 spread = new Vector3(
				(float)GD.RandRange(-15, 15),
				0,
				(float)GD.RandRange(-15, 15)
			);
			
			Vector3 endPos = _targetPos + spread;

			BulletTracer tracer = TracerScene.Instantiate<BulletTracer>();
			GetTree().CurrentScene.AddChild(tracer);
			
			tracer.Scale = new Vector3(10f, 10f, 10f);
			tracer.Setup(Muzzle.GlobalPosition, endPos, BulletSpeed);
			
			if (Multiplayer.IsServer())
			{
				var spaceState = GetWorld3D().DirectSpaceState;
				
				Vector3 rayDirection = (endPos - Muzzle.GlobalPosition).Normalized();
				Vector3 rayEnd = Muzzle.GlobalPosition + (rayDirection * 1000f);

				var query = PhysicsRayQueryParameters3D.Create(Muzzle.GlobalPosition, rayEnd);
				var result = spaceState.IntersectRay(query);
				
				if (result.Count > 0)
				{
					Vector3 impactPoint = (Vector3)result["position"];

					float distanceToGround = Muzzle.GlobalPosition.DistanceTo(impactPoint);
					double travelTime = distanceToGround / BulletSpeed;

					GetTree().CreateTimer(travelTime).Timeout += () =>
					{
						var players = GetTree().GetNodesInGroup("Player");
						foreach (Node3D player in players)
						{
							if (IsInstanceValid(player) && player.GlobalPosition.DistanceTo(impactPoint) <= 8.0f) 
							{
								HealthComponent health = player.GetNodeOrNull<HealthComponent>("HealthComponent");
								if (health != null)
								{
									health.RequestTakeDamage(40f, _callerId, "a10_icon", impactPoint);
								}
							}
						}
					};
				}
			}
		}
	}
	
	private void RestorePlayerCamera()
	{
		var players = GetTree().GetNodesInGroup("Player");
		foreach (Node3D player in players)
		{
			if (player.GetMultiplayerAuthority() == _callerId)
			{
				Camera3D playerCam = player.GetNodeOrNull<Camera3D>("CameraPivot/Camera3D");
				
				if (playerCam != null && player.Visible)
				{
					playerCam.Current = true;
				}
				return;
			}
		}
	}
	
	public override void _Process(double delta)
	{
		if ((_currentPhase == FlightPhase.Approach || _currentPhase == FlightPhase.DiveAndShoot) && IsInstanceValid(_targetPlayerNode))
		{
			HealthComponent health = _targetPlayerNode.GetNodeOrNull<HealthComponent>("HealthComponent");
			if (health != null && health.CurrentHealth > 0)
			{
				if (_isLead)
				{
					_targetPos = _targetPlayerNode.GlobalPosition;
				}
				else
				{
					Vector3 forwardDir = (_targetPlayerNode.GlobalPosition - _startPos).Normalized();
					Vector3 rightDir = Vector3.Up.Cross(forwardDir).Normalized();
					_targetPos = _targetPlayerNode.GlobalPosition + (rightDir * 30f);
				}
				
				if (_currentPhase == FlightPhase.Approach)
					_currentTargetPoint = _targetPos + new Vector3(0, DiveHeight, 0);
				else if (_currentPhase == FlightPhase.DiveAndShoot)
					_currentTargetPoint = _targetPos;
					
				Vector3 flyDirection = (_targetPos - _startPos).Normalized();
				flyDirection.Y = 0; 
				_exitPos = _targetPos + (flyDirection * 2500f) + new Vector3(0, 1000f, 0);
			}
		}
		
		Vector3 targetDirection = (_currentTargetPoint - GlobalPosition).Normalized();
		Vector3 currentDirection = _currentVelocity.Normalized();
		Vector3 smoothDirection = currentDirection.Lerp(targetDirection, (float)delta * TurnSpeed).Normalized();
		
		_currentVelocity = smoothDirection * Speed;
		GlobalPosition += _currentVelocity * (float)delta;

		Vector3 safeBaseUp = Mathf.Abs(smoothDirection.Y) > 0.99f ? Vector3.Forward : Vector3.Up;
		Vector3 wingsRight = safeBaseUp.Cross(smoothDirection).Normalized();
		Vector3 turnDifference = targetDirection - currentDirection;
		float bankFactor = wingsRight.Dot(turnDifference); 
		float targetBankAngle = -bankFactor * 2.5f;
		targetBankAngle = Mathf.Clamp(targetBankAngle, -1.0f, 1.0f);
		_currentBankAngle = Mathf.Lerp(_currentBankAngle, targetBankAngle, (float)delta * 2.5f);
		Vector3 bankedUp = safeBaseUp.Rotated(smoothDirection, _currentBankAngle);
		var targetTransform = Transform.LookingAt(GlobalPosition + smoothDirection, bankedUp);
		Transform = Transform.InterpolateWith(targetTransform, (float)delta * 5f);
		float distanceToTargetXY = new Vector2(GlobalPosition.X - _targetPos.X, GlobalPosition.Z - _targetPos.Z).Length();

		switch (_currentPhase)
		{
			case FlightPhase.Approach:
				if (distanceToTargetXY < 1400f)
				{
					_currentPhase = FlightPhase.DiveAndShoot;
					_currentTargetPoint = _targetPos;
				}
				break;
			
			case FlightPhase.DiveAndShoot:
				Vector3 directionToTarget = (_currentTargetPoint - GlobalPosition).Normalized();
				float alignment = _currentVelocity.Normalized().Dot(directionToTarget);

				if (alignment > 0.99f)
				{
					if (GunAudio != null && !GunAudio.Playing) GunAudio.Play();
					Shoot(delta);
				}
				
				if (distanceToTargetXY < 400f || GlobalPosition.Y < _targetPos.Y + 250f) 
				{
					_currentPhase = FlightPhase.PullUp;
					if (GunAudio != null) GunAudio.Stop();

					Vector3 forwardDir = (_targetPos - _startPos).Normalized();
					forwardDir.Y = 0;

					_currentTargetPoint = _targetPos + (forwardDir * 2000f) + new Vector3(0, 800f, 0);
					_exitPos = _currentTargetPoint; 
				}
				break;

			case FlightPhase.PullUp:
				if (GlobalPosition.Y > _targetPos.Y + 450f || distanceToTargetXY > 800f)
				{
					_currentPhase = FlightPhase.Egress;

					Vector3 forwardDir = (_targetPos - _startPos).Normalized();
					forwardDir.Y = 0;

					float turnDirection = GD.Randf() > 0.5f ? 1.0f : -1.0f;
					float egressAngle = (float)GD.RandRange(0.78f, 1.57f) * turnDirection;
					Vector3 egressDir = forwardDir.Rotated(Vector3.Up, egressAngle);

					_currentTargetPoint = GlobalPosition + (egressDir * 3000f) + new Vector3(0, 500f, 0);
					_exitPos = _currentTargetPoint;
				}
				break;

			case FlightPhase.Egress:
				if (GlobalPosition.DistanceTo(_exitPos) < 250f)
				{
					if (Multiplayer.GetUniqueId() == _callerId && !_isLead)
					{
						RestorePlayerCamera();
					}
					
					QueueFree();
				}
				break;
		}
	}
}
