using System.Collections.Generic;
using Godot;

public partial class Hero : CharacterBody2D
{
    private static Hero instance;

    public int SwingCount = 0; 
    private AnimationPlayer AnimationPlayer;
    private Vector2 AppliedKnockback = Vector2.Zero;

    private readonly List<Enemy> damagedEnemiesThisSwing = new();
    [Export] public float Health, MaxHealth;
    [Export] private float knockBackToDampFactor;
    private bool OnCooldown;
    private Vector2 OriginalSwordReach;
    [Export] private Vector2 SelfScale;
    [Export] private float SpeedDebuffFromRoot, DamageFromRoots;
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

    private RootRoot rootToCut;
    public void _on_area_entered(Area2D area)
    {
        if (area.Name.ToString().StartsWith("Root"))
        {
            rootToCut = area.GetParent() as RootRoot;
        }
        else
        {
            // Cutting projectiles
            area.QueueFree();
            
        }
    }

    public void _on_area_exited(Area2D area)
    {
        var thisAsRoot = area.GetParent() as RootRoot;
        if (thisAsRoot == rootToCut)
            rootToCut = null;
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
        if (rootToCut != null)
        {
            if (swordCollision.Disabled)
            {
                rootToCut = null;
            }
            else if (!rootToCut.Visible)
            {
                rootToCut = null;
            }
            else
            {
                
                rootToCut.GetCutByLine(GetSwordAsLine(), Damage);
            }
        }
        // GD.Print(GetGlobalMousePosition());
        // GD.Print(GetViewport().GetFinalTransform().BasisXformInv(GetGlobalMousePosition()));
        if (!IsAttacking)
            LookAt(Main3D.SupposedMousePosition);
        Velocity = new Vector2(Input.GetAxis("left", "right"), -Input.GetAxis("down", "forward")).Normalized() * Speed +
                   AppliedKnockback;
        if (RootRoot.HeroTouchedThisProcess)
        {
            RootRoot.HeroTouchedThisProcess = false;
            Velocity *= SpeedDebuffFromRoot;
            TakeDamage(delta*DamageFromRoots,Vector2.Zero);
        }
        MoveAndSlide();

        var knockbackDampingFactor = knockBackToDampFactor * delta;
        if (knockBackToDampFactor >= 1) knockbackDampingFactor = 1;

        AppliedKnockback *= 1 - (float) knockbackDampingFactor;


        if (!OnCooldown && Input.IsMouseButtonPressed(MouseButton.Left) && !IsAttacking) {
            AnimationPlayer.Play("Attack");
            SwingCount++;
        }
    }

    public bool IntersectsWithPolygon(Vector2[] polygon)
    {
        return false;
    }

    public void _on_animation_player_animation_finished(string name)
    {
        if (name == "Attack")
        {
            OnCooldown = true;
            DoCooldown();
        }
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