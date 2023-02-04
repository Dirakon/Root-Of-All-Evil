using System.Collections.Generic;
using Godot;

public partial class HealthLine : HBoxContainer
{
    [Export] private PackedScene HealthPrefab;

    private readonly List<Node> healths = new();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        var realPlayerHp = Mathf.CeilToInt(Hero.Instance().Health);
        while (healths.Count > realPlayerHp)
        {
            healths[0].QueueFree();
            healths.RemoveAt(0);
        }

        while (healths.Count < realPlayerHp)
        {
            var newHealth = HealthPrefab.Instantiate();
            AddChild(newHealth);
            healths.Add(newHealth);
        }
    }
}