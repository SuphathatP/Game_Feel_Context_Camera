using Godot;
using System;

public partial class Enemy : CharacterBody3D
{
	private AnimationPlayer anim;

	[ExportGroup("Enemy Movement")]
	[Export] private float MinEnemySpeed = 2.0f;
	[Export] private float MaxEnemySpeed = 5.0f;

	bool IsMove = false;

    public override void _Ready() 
    { 
        anim = GetNode<AnimationPlayer>("AnimationPlayer"); 
    }

    public override void _PhysicsProcess(double delta)
    {
		MoveAndSlide();
        anim.Play("enemy_idle_anim");
		//anim.Play("enemy_walk_anim");
    }

	public void Initialize(Vector3 startPosition, Vector3 playerPosition)
	{
		LookAtFromPosition(startPosition, playerPosition, Vector3.Up);
		RotateY((float)GD.RandRange(-Mathf.Pi / 4.0, Mathf.Pi / 4.0));

		float randomSpeed = (float)GD.RandRange(MinEnemySpeed, MaxEnemySpeed);

		Velocity = Vector3.Forward * randomSpeed;
		Velocity = Velocity.Rotated(Vector3.Up, Rotation.Y);
	}

	private void OnVisibilityNotifierScreenExited()
	{
		QueueFree();
	}
}
