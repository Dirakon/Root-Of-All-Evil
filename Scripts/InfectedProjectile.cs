using System.Collections.Generic;
using Godot;

public partial class InfectedProjectile : Area2D
{
    [Export] private float speed, damage;
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

    private bool IsDead = false;

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (IsDead)
            return;
        var difference = target - GlobalPosition;
        var distanceLeft = difference.Length();
        var distanceToMove = delta * speed;
        if (distanceToMove >= distanceLeft)
        {
            IsDead = true;
            CallDeferred(MethodName.CreateRoot);
        }
        else
        {
            GlobalPosition += difference.Normalized() * (float) distanceToMove;
        }
    }

    public void CreateRoot()
    {
        RootRoot.AllRoots ??= new List<RootRoot>();
        RootRoot.AllRoots.RemoveAll(root => !IsInstanceValid(root));
        RootRoot.Controller.CreateNewRoot(GlobalPosition);
        SoundManager.Play("projectile_lands");
        QueueFree();
    }

    public void _on_body_entered(Node2D body)
    {
        if (IsDead)
            return;
        IsDead = true;
        CallDeferred(MethodName.CreateRoot);
        switch (body)
        {
            case Hero hero:
                hero.TakeDamage(damage, Vector2.Zero);
                break;
            default:
                break;
        }
    }
}