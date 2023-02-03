using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	[Export()] float Speed = 300.0f;
	[Export()] float Health = 10.0f;
	[Export()] float Damage = 10.0f;
	
	private Vector2 AppliedKnockback = Vector2.Zero;
	[Export] private float knockBackToDampFactor;

	public override void _PhysicsProcess(double delta)
	{
		LookAt(Hero.Instance().GlobalPosition);
		Velocity = GlobalTransform.X.Normalized()*(float)(delta*Speed);
		Velocity += AppliedKnockback * (float)delta;
		

		var knockbackDampingFactor = knockBackToDampFactor * delta;
		if (knockBackToDampFactor >= 1) knockbackDampingFactor = 1;

		AppliedKnockback *=1 - (float)knockbackDampingFactor;

		var collision = MoveAndCollide(Velocity);
		if (collision?.GetCollider() == null)
			return;
		var hero = collision.GetCollider() as Hero;
		hero.TakeDamage(delta * Damage,Vector2.Zero);
	}

	public void TakeDamage(float damage,Vector2 knockback)
	{
		Health -= damage;
		if (Health < 0)
			QueueFree();
		else
		{
			AppliedKnockback += knockback;
		}
	}
}
