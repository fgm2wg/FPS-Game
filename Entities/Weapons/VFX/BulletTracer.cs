using Godot;

public partial class BulletTracer : Node3D
{
	public void Setup(Vector3 startPos, Vector3 endPos, float speed = 250f)
	{
		GlobalPosition = startPos;
		
		LookAt(endPos, Vector3.Up); 

		float distance = startPos.DistanceTo(endPos);
		float travelTime = distance / speed;

		Tween tween = CreateTween();
		tween.TweenProperty(this, "global_position", endPos, travelTime);
		tween.TweenCallback(Callable.From(QueueFree));
	}
}
