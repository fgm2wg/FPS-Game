using Godot;

public partial class KillfeedEntry : HBoxContainer
{
	public void Setup(long killerId, string killerName, long victimId, string victimName, string weaponName)
	{
		Label killerLabel = GetNode<Label>("KillerLabel");
		Label victimLabel = GetNode<Label>("VictimLabel");
		TextureRect weaponIcon = GetNode<TextureRect>("WeaponIcon");

		killerLabel.Text = killerName;
		victimLabel.Text = victimName;
		
		string iconPath = $"res://Assets/UI/Weapons/BaseWeapon/{weaponName}.png";
		if (ResourceLoader.Exists(iconPath))
		{
			weaponIcon.Texture = GD.Load<Texture2D>(iconPath);
		}

		long myId = Multiplayer.GetUniqueId();
		int myTeam = GameManager.Players.ContainsKey(myId) ? GameManager.Players[myId].Team : -1;

		killerLabel.Modulate = GetPlayerColor(killerId, myId, myTeam);
		victimLabel.Modulate = GetPlayerColor(victimId, myId, myTeam);

		Tween tween = CreateTween();
		tween.TweenInterval(3.0f);
		tween.TweenProperty(this, "modulate:a", 0.0f, 1.0f);
		tween.TweenCallback(Callable.From(QueueFree));
	}

	private Color GetPlayerColor(long targetId, long myId, int myTeam)
	{
		if (targetId == myId) 
		{
			return Colors.Yellow;
		}
		
		if (GameManager.Players.ContainsKey(targetId) && GameManager.Players[targetId].Team == myTeam)
		{
			return Colors.DeepSkyBlue;
		}
		
		return Colors.Crimson;
	}
}
