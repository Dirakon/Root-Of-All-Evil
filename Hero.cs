using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class Hero : CharacterBody2D
{
	[Export] private float Health;
	private Vector2 AppliedKnockback = Vector2.Zero;
	[Export] private float knockBackToDampFactor;
	public Hero() : base()
	{
		instance = this;
	}
	public static Hero Instance()
	{
		return instance;
	}

	private static Hero instance;
	[Export] public float Speed, Cooldown, Damage,KnockbackStrength;

	private AnimationPlayer AnimationPlayer;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		AnimationPlayer = GetNode("Attack").GetNode("AnimationPlayer") as AnimationPlayer;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		bool IsAttacking = AnimationPlayer.IsPlaying();
		if (!IsAttacking)
			LookAt(GetGlobalMousePosition());
		Velocity = new Vector2(Input.GetAxis("left","right"),-Input.GetAxis("down","forward")).Normalized()*Speed + AppliedKnockback;
		MoveAndSlide();

		var knockbackDampingFactor = knockBackToDampFactor * delta;
		if (knockBackToDampFactor >= 1) knockbackDampingFactor = 1;

		AppliedKnockback *=1 - (float)knockbackDampingFactor;


		if (!OnCooldown && Input.IsMouseButtonPressed(MouseButton.Left) && !IsAttacking)
		{
			AnimationPlayer.Play("Attack");
		}

	}

	private List<Enemy> damagedEnemiesThisSwing = new List<Enemy>();
	private bool OnCooldown = false;
	public void _on_animation_player_animation_finished(String name)
	{
		if (name == "Attack")
		{
			OnCooldown = true;
			DoCooldown();
		}
	}

	public async void DoCooldown()
	{
		await ToSignal(GetTree().CreateTimer( Cooldown), "timeout");
		OnCooldown = false;
		damagedEnemiesThisSwing.Clear();
	}

	public void _on_sword_body_entered(Node2D node)
	{
		
		Enemy enemy = node as  Enemy;
		if (enemy == null)
			return;
		if (damagedEnemiesThisSwing.Contains(enemy))
			return;
		damagedEnemiesThisSwing.Add(enemy);
		enemy.TakeDamage(Damage, GlobalPosition.DirectionTo(enemy.GlobalPosition)*KnockbackStrength);
	}

	public void TakeDamage(double damage, Vector2 knockback)
	{
		Health -=(float) damage;
		if (Health < 0)
			QueueFree();
		else
		{
			AppliedKnockback += knockback;
		}
	}
}
