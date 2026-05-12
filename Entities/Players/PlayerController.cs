using Godot;
using System;

public partial class PlayerController : CharacterBody3D
{
	[Export] private Camera3D _camera;
	[Export] private Node _movementComponent;
	[Export] private Label3D _nameBillboard;
	[Export] private Node3D _playerBody;
	[Export] private BaseWeapon _equippedWeapon;
	[Export] private CanvasLayer _playerHUD;

	public override void _EnterTree()
	{
		SetMultiplayerAuthority(int.Parse(Name));
	}

	public override void _Ready()
	{
		long myId = long.Parse(Name);

		if (GameManager.Players.ContainsKey(myId))
		{
			PlayerData data = GameManager.Players[myId];
			_nameBillboard.Text = data.Name;

			if (_playerBody is MeshInstance3D bodyMesh)
			{
				StandardMaterial3D teamMaterial = new StandardMaterial3D();
				teamMaterial.AlbedoColor = data.Team == 0 ? Colors.Blue : Colors.Red;
				bodyMesh.MaterialOverride = teamMaterial;
			}
		}

		if (IsMultiplayerAuthority())
		{
			_camera.Current = true;
			Input.MouseMode = Input.MouseModeEnum.Captured;
			if (_nameBillboard != null) _nameBillboard.Visible = false; 
			if (_playerHUD != null) _playerHUD.Visible = true;
			if (_playerBody != null) SetShadowsOnlyRecursively(_playerBody);

			if (GameManager.Players.ContainsKey(myId))
			{
				int team = GameManager.Players[myId].Team;
				string targetGroup = team == 0 ? "Team1Spawns" : "Team2Spawns";
				var spawnNodes = GetTree().GetNodesInGroup(targetGroup);

				if (spawnNodes.Count > 0)
				{
					Random rng = new Random((int)myId);
					int randomIndex = rng.Next(spawnNodes.Count);

					if (spawnNodes[randomIndex] is Marker3D spawnMarker)
					{
						GlobalPosition = spawnMarker.GlobalPosition;
					}
				}
				else
				{
					GlobalPosition = new Vector3(0, 2.0f, 0);
					GD.PrintErr($"WARNING: No spawn points found in group {targetGroup}!");
				}
			}
		}
		else
		{
			_camera.Current = false;
			if (_playerHUD != null) _playerHUD.Visible = false;
		}
	}
	
	public override void _Process(double delta)
	{		
		if (!IsMultiplayerAuthority()) return;

		if (Input.MouseMode == Input.MouseModeEnum.Captured && _equippedWeapon != null)
		{
			bool wantsToShoot = _equippedWeapon.CurrentFireMode == BaseWeapon.FireModeType.Auto 
				? Input.IsActionPressed(InputMapKeys.Shoot) 
				: Input.IsActionJustPressed(InputMapKeys.Shoot);

			if (wantsToShoot)
			{
				_equippedWeapon.AttemptShoot();
			}
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!IsMultiplayerAuthority()) return;

		if (Input.IsActionJustPressed(InputMapKeys.Pause))
		{
			if (Input.MouseMode == Input.MouseModeEnum.Captured)
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
				EventBus.OnPauseMenuToggled?.Invoke(true); 
			}
			else 
			{
				Input.MouseMode = Input.MouseModeEnum.Captured;
				EventBus.OnPauseMenuToggled?.Invoke(false);
			}
			return;
		}

		if (Input.MouseMode == Input.MouseModeEnum.Captured && _equippedWeapon != null)
		{
			if (Input.IsActionJustPressed(InputMapKeys.ToggleFireMode))
			{
				_equippedWeapon.ToggleFireMode();
			}

			if (Input.IsActionJustPressed(InputMapKeys.Reload))
			{
				_equippedWeapon.AttemptReload();
			}

			if (Input.IsActionJustPressed(InputMapKeys.Aim))
				_equippedWeapon.ToggleAim(true);
			else if (Input.IsActionJustReleased(InputMapKeys.Aim))
				_equippedWeapon.ToggleAim(false);
		}
		else if (Input.IsActionJustPressed(InputMapKeys.Shoot))
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
			EventBus.OnPauseMenuToggled?.Invoke(false);
		}
	}
	
	public override void _Notification(int what)
	{
		if (what == MainLoop.NotificationApplicationFocusOut)
		{
			if (IsMultiplayerAuthority() && Input.MouseMode == Input.MouseModeEnum.Captured)
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
				EventBus.OnPauseMenuToggled?.Invoke(true);
			}
		}
	}
	
	private void SetShadowsOnlyRecursively(Node node)
	{
		if (node is GeometryInstance3D mesh)
		{
			mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly;
		}

		foreach (Node child in node.GetChildren())
		{
			SetShadowsOnlyRecursively(child);
		}
	}
}
