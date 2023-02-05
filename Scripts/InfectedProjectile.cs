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

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        var difference = target - GlobalPosition;
        var distanceLeft = difference.Length();
        var distanceToMove = delta * speed;
        if (distanceToMove >= distanceLeft)
        {
            CreateRoot();
            QueueFree();
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
    }

    public void _on_body_entered(Node2D body)
    {
        CreateRoot();
        switch (body)
        {
            case Hero hero:
                hero.TakeDamage(damage, Vector2.Zero);
                QueueFree();
                break;
            default:
                QueueFree();
                break;
        }
    }
}