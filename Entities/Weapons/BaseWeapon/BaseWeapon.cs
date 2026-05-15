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

	private bool _isAdminModeActive = false;
	private int _originalMaxAmmo;
	private float _originalFireRateRPM;

	public override void _Ready()
	{
		_originalMaxAmmo = MaxAmmo;
		_originalFireRateRPM = FireRateRPM;

		_currentAmmo = MaxAmmo;
		EventBus.OnAmmoChanged?.Invoke(_currentAmmo, MaxAmmo);
		EventBus.OnFireModeChanged?.Invoke(CurrentFireMode.ToString().ToUpper());
	}

	public override void _Process(double delta)
	{
		_timeSinceLastShot += delta;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (GetMultiplayerAuthority() != Multiplayer.GetUniqueId()) return;

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == Key.M)
		{
			ToggleAdminMode();
		}
	}

	private void ToggleAdminMode()
	{
		_isAdminModeActive = !_isAdminModeActive;

		if (_isAdminModeActive)
		{
			FireRateRPM = 2000f;
			MaxAmmo = 9999;
			_currentAmmo = MaxAmmo;
			GD.Print("Admin Mode: ACTIVATED (1200 RPM, 999 Ammo)");
		}
		else
		{
			FireRateRPM = _originalFireRateRPM;
			MaxAmmo = _originalMaxAmmo;
			if (_currentAmmo > MaxAmmo) _currentAmmo = MaxAmmo; 
			GD.Print("Admin Mode: DEACTIVATED");
		}

		EventBus.OnAmmoChanged?.Invoke(_currentAmmo, MaxAmmo);
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
		
		if (_muzzleFlash != null) _muzzleFlash.Restart();

		if (_animPlayer != null && _animPlayer.HasAnimation("shoot"))
		{
			float nativeAnimLength = _animPlayer.GetAnimation("shoot").Length;
			float syncSpeed = nativeAnimLength / secondsPerShot;
			_animPlayer.Stop();
			_animPlayer.Play("shoot", -1, syncSpeed);
		}
		
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
				health.RpcId(1, HealthComponent.MethodName.RequestTakeDamage, Damage, myId, "mp5_icon", GlobalPosition);
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

		if (_animPlayer != null && _animPlayer.HasAnimation("reload"))
		{
			float nativeAnimLength = _animPlayer.GetAnimation("reload").Length;
			float syncSpeed = nativeAnimLength / 2.6f;
			_animPlayer.Play("reload", -1, syncSpeed);
		}

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
		if (_shootAudio != null) 
		{
			_shootAudio.PitchScale = (float)GD.RandRange(0.9f, 1.1f);
			_shootAudio.Play();
		}
	
		if (_tracerScene != null)
		{
			BulletTracer tracer = _tracerScene.Instantiate<BulletTracer>();
			GetTree().CurrentScene.AddChild(tracer);
			tracer.Setup(start, end);
		}
	}
}
