using Godot;
using System;

public partial class InfectedProjectile : Area2D
{
	[Export()] private float speed, damage;
	private Vector2 target;
	public void Init(Vector2 target)
	{
		this.target = target;
		LookAt(target);
	}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var difference = target - GlobalPosition;
		var distanceLeft = difference.Length();
		var distanceToMove = delta * speed;
		if (distanceToMove >= distanceLeft)
		{
			RootRoot.Controller.CreateNewRoot(GlobalPosition);
			QueueFree();
		}
		else
		{
			GlobalPosition += difference.Normalized() * (float)distanceToMove;
		}
	}
	
	public void _on_body_entered(Node2D body)
	{
		RootRoot.Controller.CreateNewRoot(GlobalPosition);
		switch (body)
		{
			case Hero hero:
				hero.TakeDamage(damage,Vector2.Zero);
				QueueFree();
				break;
			default:
				QueueFree();
				break;
		}
	}
}
