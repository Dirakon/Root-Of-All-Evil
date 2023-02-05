using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class MainArcade : Node2D
{
    private static MainArcade instance;
    [Export] private float cummulativeArchersPerWave, cummulativeMeleesPerWave, maxSpawnDelay, minDistanceFromHero;
    private double currentRootPlantTime;

    private double currentSpawnDelay;

    public List<Enemy> EnemiesThisWave;
    [Export] private PackedScene heroPrefab;
    [Export] public double InfectionValue;
    [Export] private PackedScene meleeEnemy, archerEnemy;

    public int meleesToSpawn, archersToSpawn;
    private Vector2 minPosition, maxPosition;
    [Export] public double RootPlantTimeInfectionFactor, MinRootPlantTime;
    [Export] private Vector2 StartHeroPosition;
    [Export] public double StartRootPlantTime;
    private ArcadeState state;
    [Export] public PackedScene UninfectedProjectilePrefab, InfectedProjectilePrefab, RootPrefab;

    [Export] private PackedScene upgradeStationPrefab;
    [Export] public int WaveCount;

    public MainArcade()
    {
        instance = this;
        currentRootPlantTime = StartRootPlantTime;
    }

    public static MainArcade Instance()
    {
        return instance;
    }

    public void UpgradeSelected(Upgrade selectedUpgrade)
    {
        var currentState = state as ArcadeChoosingUpgrades;
        selectedUpgrade.Activate(Hero.Instance());
        if (currentState == null)
        {
            GD.PrintErr("Trying to choose upgrade during incorrect game state");
            return;
        }
        SoundManager.Play("upgrade");


        currentState.UpgradesStations.ForEach(station => station.QueueFree());
        WaveCount++;
        StartNextWave();
    }

    public void StartNextWave()
    {
        state = new ArcadeActiveWaves();
        EnemiesThisWave = new List<Enemy>();
        var maxMelees = (int) (cummulativeMeleesPerWave * WaveCount);
        var maxArchers = (int) (cummulativeArchersPerWave * WaveCount);
        meleesToSpawn = Random.Shared.Next(maxMelees);
        archersToSpawn = Random.Shared.Next(maxArchers);
        if (meleesToSpawn + archersToSpawn == 0)
            meleesToSpawn = 1;
        currentSpawnDelay = 0;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Init();
    }

    private void Init()
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
        
        StartNextWave();
    }
    
    public Vector2 ChooseSpawnPosition(List<Vector2> positionsToAvoid = null)
    {
        positionsToAvoid ??= new List<Vector2>();
        positionsToAvoid.Add(Hero.Instance().GlobalPosition);
        var PositionToSpawnIn = Vector2.Zero;
        do
        {
            for (var index = 0; index < 2; ++index)
            {
                var distance = maxPosition[index] - minPosition[index];
                PositionToSpawnIn[index] = Random.Shared.NextSingle() * distance + minPosition[index];
            }
        } while (positionsToAvoid.Any(position => PositionToSpawnIn.DistanceTo(position) < minDistanceFromHero));

        return PositionToSpawnIn;
    }

    public void SpawnEnemy(PackedScene enemyPrefab)
    {
        var enemy = enemyPrefab.Instantiate() as Enemy;
        enemy.Init();
        enemy.GlobalPosition = ChooseSpawnPosition();
        AddChild(enemy);
        EnemiesThisWave.Add(enemy);
    }


    private void CreateRoot()
    {
        RootRoot.AllRoots ??= new List<RootRoot>();
        RootRoot.AllRoots.RemoveAll(root => !IsInstanceValid(root));
        RootRoot.Controller.CreateNewRoot(ChooseSpawnPosition());
    }
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        InfectionValue += delta;
        if (InfectionValue > 0)
        {
            currentRootPlantTime -= delta;
            if (currentRootPlantTime < 0)
            {
                currentRootPlantTime = Math.Max(MinRootPlantTime,
                    StartRootPlantTime - InfectionValue * RootPlantTimeInfectionFactor);
                CallDeferred(MethodName.CreateRoot);
            }
        }

        if (meleesToSpawn + archersToSpawn > 0)
        {
            currentSpawnDelay -= delta;
            if (currentSpawnDelay <= 0)
            {
                currentSpawnDelay = Random.Shared.NextDouble() * maxSpawnDelay;
                List<(PackedScene, Action)> spawnOptions = new();
                if (meleesToSpawn > 0)
                    spawnOptions.Add((meleeEnemy, () => meleesToSpawn--));
                if (archersToSpawn > 0)
                    spawnOptions.Add((archerEnemy, () => archersToSpawn--));
                var (packedScene, action) = spawnOptions.Random();
                action.Invoke();
                SpawnEnemy(packedScene);
            }
        }
        else if (EnemiesThisWave == null)
        {
        }
        else if (EnemiesThisWave.IsEmpty())
        {
            EnemiesThisWave = null;
            Hero.Instance().OnLevelReset();
            var currentUpgrades = Upgrade.GetAllViableUpgrades().Shuffle().Take(3).ToList();
            List<Vector2> upgradePositions = new();
            for (var i = 0; i < 3; ++i) upgradePositions.Add(ChooseSpawnPosition(new List<Vector2>(upgradePositions)));

            SoundManager.Play("level_cleared");
            
            state = new ArcadeChoosingUpgrades(
                upgradePositions
                    .Zip(currentUpgrades)
                    .Select(tuple =>
                    {
                        var (position, upgrade) = tuple;
                        var station = upgradeStationPrefab.Instantiate() as UpgradeStation;
                        station.Init(upgrade);
                        AddChild(station);
                        station.GlobalPosition = position;
                        return station;
                    })
                    .ToList()
            );
        }
    }

    public void ResetGame()
    {
        InfectionValue = -10;
        WaveCount = 4;
        EnemiesThisWave = null;
        state = null;
        foreach (var child in GetChildren())
        {
            var lowerName = child.Name.ToString().ToLower();
            if (lowerName.StartsWith("wall") || lowerName.StartsWith("ui") || lowerName.StartsWith("back"))
                continue;
            child.QueueFree();
            
        }

        CallDeferred(MethodName.Init);
    }
}

public abstract record ArcadeState;

public record ArcadeActiveWaves : ArcadeState;

public record ArcadeChoosingUpgrades(List<UpgradeStation> UpgradesStations) : ArcadeState;

public record ArcadeSafeGameOver : ArcadeState;