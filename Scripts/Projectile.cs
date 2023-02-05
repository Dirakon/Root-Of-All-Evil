using Godot;

public partial class Projectile : Area2D
{
    private Vector2 dir;
    [Export] private float speed, damage;

    public void Init(Vector2 dir, Vector2 target)
    {
        this.dir = dir;
        LookAt(target);
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        GlobalPosition += (float) (delta * speed) * dir;
    }

    public void _on_body_entered(Node2D body)
    {
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