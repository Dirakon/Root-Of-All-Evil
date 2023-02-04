using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class MainArcade : Node2D
{
    private static MainArcade instance;
    [Export] private PackedScene heroPrefab;
    private Vector2 minPosition, maxPosition;
    [Export] private Vector2 StartHeroPosition;
    private ArcadeState state;
    [Export] public PackedScene UninfectedProjectile, InfectedProjectile;

    public MainArcade()
    {
        instance = this;
    }

    public static MainArcade Instance()
    {
        return instance;
    }

    public void UpgradeSelected(Upgrade selectedUpgrade)
    {
        var currentState = state as ArcadeChoosingUpgrades;
        selectedUpgrade.Activate(Hero.Instance());
        if (currentState == null) GD.PrintErr("Trying to choose upgrade during incorrect game state");

        currentState.UpgradesStations.ForEach(station => station.QueueFree());
        StartNextWave(currentState.WaveCount + 1);
    }

    public void StartNextWave(int currentWaveCount)
    {
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        var hero = heroPrefab.Instantiate() as Node2D;
        hero.GlobalPosition = StartHeroPosition;
        AddChild(hero);
        var worldBoundaries = Enumerable.Range(1, 4).Select(index => GetNode($"Wall{index}") as Node2D)
            .Select(node => node.GlobalPosition).ToList();

        for (var i = 0; i < 2; ++i)
        {
            minPosition[i] = worldBoundaries.MinBy(position => position[i])[i];
            maxPosition[i] = worldBoundaries.MaxBy(position => position[i])[i];
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}

public abstract record ArcadeState;

public record ArcadeActiveWaves(List<Enemy> Enemies) : ArcadeState;

public record ArcadeChoosingUpgrades(List<UpgradeStation> UpgradesStations, int WaveCount) : ArcadeState;

public record ArcadeSafeGameOver(List<Enemy> Enemies, int WaveCount) : ArcadeState;