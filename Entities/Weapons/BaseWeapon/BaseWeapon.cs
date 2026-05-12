using Godot;

public partial class BaseWeapon : Node3D
{
	[ExportCategory("Weapon Stats")]
	[Export] public int MaxAmmo = 30;
	[Export] public float FireRateRPM = 800f;
	[Export] public float Damage = 25f;
	public enum FireModeType { Auto, Semi }
	[Export] public FireModeType CurrentFireMode = FireModeType.Auto;

	[ExportCategory("Node References")]
	[Export] private RayCast3D _aimRaycast;
	[Export] private AnimationPlayer _animPlayer;
	
	[ExportCategory("Sound Effects")]
	[Export] private AudioStreamPlayer3D _shootAudio;
	[Export] private AudioStreamPlayer3D _reloadAudio;
	
	[ExportCategory("Bullet Settings")]
	[Export] private PackedScene _tracerScene;
	[Export] private Marker3D _muzzlePoint;
	[Export] private GpuParticles3D _muzzleFlash;

	private int _currentAmmo;
	private double _timeSinceLastShot = 0;
	private bool _isReloading = false;

	public override void _Ready()
	{
		_currentAmmo = MaxAmmo;
		EventBus.OnAmmoChanged?.Invoke(_currentAmmo, MaxAmmo);
		EventBus.OnFireModeChanged?.Invoke(CurrentFireMode.ToString().ToUpper());
	}

	public override void _Process(double delta)
	{
		_timeSinceLastShot += delta;
	}
	
	public void ToggleFireMode()
	{
		if (CurrentFireMode == FireModeType.Auto)
			CurrentFireMode = FireModeType.Semi;
		else
			CurrentFireMode = FireModeType.Auto;

		EventBus.OnFireModeChanged?.Invoke(CurrentFireMode.ToString().ToUpper());
	}

	public void AttemptShoot()
	{
		float secondsPerShot = 60f / FireRateRPM;

		if (_isReloading || _timeSinceLastShot < secondsPerShot || _currentAmmo <= 0) 
			return;

		_currentAmmo--;
		_timeSinceLastShot = 0;
		EventBus.OnAmmoChanged?.Invoke(_currentAmmo, MaxAmmo);
		if (_shootAudio != null) _shootAudio.Play();
		
		if (_muzzleFlash != null) _muzzleFlash.Restart();

		// if (_animPlayer != null) _animPlayer.Play("shoot_recoil");
		
		Vector3 targetHitPosition;
		_aimRaycast.ForceRaycastUpdate();

		if (_aimRaycast.IsColliding())
		{
			targetHitPosition = _aimRaycast.GetCollisionPoint();

			Node hitObject = (Node)_aimRaycast.GetCollider();
			HealthComponent health = hitObject.GetNodeOrNull<HealthComponent>("HealthComponent");

			if (health != null)
			{
				long myId = Multiplayer.GetUniqueId();
				health.RpcId(1, HealthComponent.MethodName.RequestTakeDamage, Damage, myId);
			}
		}
		else 
		{
			targetHitPosition = _aimRaycast.ToGlobal(_aimRaycast.TargetPosition);
		}
		
		if (_tracerScene != null && _muzzlePoint != null)
		{
			BulletTracer tracer = _tracerScene.Instantiate<BulletTracer>();
			GetTree().CurrentScene.AddChild(tracer);
			tracer.Setup(_muzzlePoint.GlobalPosition, targetHitPosition);
		}
		
		Rpc(MethodName.BroadcastTracer, _muzzlePoint.GlobalPosition, targetHitPosition);

		if (_currentAmmo == 0)
			AttemptReload();
	}

	public void AttemptReload()
	{
		if (_isReloading || _currentAmmo == MaxAmmo) return;

		_isReloading = true;
		GD.Print("Reloading...");
		
		if (_reloadAudio != null)
		{
			_reloadAudio.Play();
		}

		// if (_animPlayer != null) _animPlayer.Play("reload");

		GetTree().CreateTimer(2.6f).Timeout += () => 
		{
			_currentAmmo = MaxAmmo;
			_isReloading = false;
			EventBus.OnAmmoChanged?.Invoke(_currentAmmo, MaxAmmo);
			GD.Print("Reload Complete!");
		};
	}

	public void ToggleAim(bool isAiming)
	{
		if (_isReloading) return;

		if (isAiming)
			GD.Print("Aiming down sights...");
		else
			GD.Print("Hip firing...");
	}
	
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void BroadcastTracer(Vector3 start, Vector3 end)
	{
		if (_tracerScene != null)
		{
			BulletTracer tracer = _tracerScene.Instantiate<BulletTracer>();
			GetTree().CurrentScene.AddChild(tracer);
			tracer.Setup(start, end);
		}
	}
}
