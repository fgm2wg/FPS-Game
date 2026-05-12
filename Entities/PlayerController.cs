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
		}
		else
		{
			_camera.Current = true;
			Input.MouseMode = Input.MouseModeEnum.Captured;
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
		}

		if (Input.IsActionJustPressed(InputMapKeys.Shoot))
		{
			if (Input.MouseMode == Input.MouseModeEnum.Visible)
			{
				Input.MouseMode = Input.MouseModeEnum.Captured;
				EventBus.OnPauseMenuToggled?.Invoke(false);
			}
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
}
