using System.Collections.Generic;
using Godot;

public partial class Hero : CharacterBody2D
{
    private static Hero instance;

    private AnimationPlayer AnimationPlayer;
    private Vector2 AppliedKnockback = Vector2.Zero;

    private readonly List<Enemy> damagedEnemiesThisSwing = new();
    [Export] public float Health, MaxHealth;
    [Export] private float knockBackToDampFactor;
    private bool OnCooldown;
    private Vector2 OriginalSwordReach;
    [Export] private Vector2 SelfScale;
    [Export] public float Speed, Cooldown, Damage, KnockbackStrength, SpeedUpgradeAmount, KnockbackUpgradeAmount;
    private Node2D sword;
    private CollisionShape2D swordCollision;

    public Hero()
    {
        instance = this;
    }

    public void OnLevelReset()
    {
        Health = MaxHealth;
    }

    public static Hero Instance()
    {
        return instance;
    }

    public void IncreaseAttackSpeedBy(float factor)
    {
        AnimationPlayer.SpeedScale += factor;
    }

    public void MultiplyCooldownBy(float factor)
    {
        Cooldown *= factor;
    }

    public (Vector2, Vector2) GetSwordAsLine()
    {
        return (
            sword.GlobalPosition,
            swordCollision.GlobalTransform.X + sword.GlobalPosition
            );
    }

    public void _on_area_entered(Area2D area)
    {
        GD.Print("Here");
        var RootToCut = area.GetParent() as RootRoot;
        RootToCut.GetCutByLine(GetSwordAsLine());
    }

    public void _on_area_exited(Area2D area)
    {
        var RootToCut = area.GetParent() as RootRoot;
    }

    public override void _Ready()
    {
        AnimationPlayer = GetNode("Attack").GetNode("AnimationPlayer") as AnimationPlayer;
        sword = GetNode("Attack").GetNode("Sword") as Node2D;
        OriginalSwordReach = sword.Scale;
        Scale = SelfScale;
        swordCollision = sword.GetNode("CollisionShape2D") as CollisionShape2D;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta)
    {
        QueueRedraw();
        var IsAttacking = AnimationPlayer.IsPlaying();
        // GD.Print(GetGlobalMousePosition());
        // GD.Print(GetViewport().GetFinalTransform().BasisXformInv(GetGlobalMousePosition()));
        if (!IsAttacking)
            LookAt(Main3D.SupposedMousePosition);
        Velocity = new Vector2(Input.GetAxis("left", "right"), -Input.GetAxis("down", "forward")).Normalized() * Speed +
                   AppliedKnockback;
        MoveAndSlide();

        var knockbackDampingFactor = knockBackToDampFactor * delta;
        if (knockBackToDampFactor >= 1) knockbackDampingFactor = 1;

        AppliedKnockback *= 1 - (float) knockbackDampingFactor;


        if (!OnCooldown && Input.IsMouseButtonPressed(MouseButton.Left) && !IsAttacking) AnimationPlayer.Play("Attack");
    }

    public void _on_animation_player_animation_finished(string name)
    {
        if (name == "Attack")
        {
            OnCooldown = true;
            DoCooldown();
        }
    }

    public override void _Draw()
    {
        base._Draw();
        var (from, to) = GetSwordAsLine();
        DrawLine(from, to, Colors.Red, 10f);
    }

    public async void DoCooldown()
    {
        await ToSignal(GetTree().CreateTimer(Cooldown), "timeout");
        OnCooldown = false;
        damagedEnemiesThisSwing.Clear();
    }

    public void _on_sword_body_entered(Node2D node)
    {
        var enemy = node as Enemy;
        if (enemy == null)
            return;
        if (damagedEnemiesThisSwing.Contains(enemy))
            return;
        damagedEnemiesThisSwing.Add(enemy);
        enemy.TakeDamage(Damage, GlobalPosition.DirectionTo(enemy.GlobalPosition) * KnockbackStrength);
    }

    public void TakeDamage(double damage, Vector2 knockback)
    {
        Health -= (float) damage;
        if (Health < 0)
            QueueFree();
        else
            AppliedKnockback += knockback;
    }

    public void IncreaseReachByFactor(float factor)
    {
        var increase = OriginalSwordReach * factor;
        sword.Scale += increase;
    }
}