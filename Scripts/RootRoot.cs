using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class RootRoot : Node2D
{
    public static List<RootRoot> AllRoots;

    private static RootController _controller;

    public static bool HeroTouchedThisProcess;

    private Hero? affectedHero;


    [Export] public float GrowthLimit,
        WidenessFactor,
        LongnessFactor,
        GrowthModifier,
        TimerRandomnessModifier,
        EgoismFactor,
        CreationEgoismFactor,
        RootHealthStart,
        HealthInfectionFactor;


    [Export] public int MaxOffspring;
    public ConvexPolygonShape2D Polygon2D;
    public RootLine RootLine;
    public CollisionShape2D Shape2D;

    [Export] public Color StartColor, EndColor;

    public static RootController Controller
    {
        get
        {
            _controller ??= new RootController();
            return _controller;
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Shape2D = new CollisionShape2D();
        Polygon2D = new ConvexPolygonShape2D();
        Shape2D.Shape = Polygon2D;
        (GetNode("RootArea") as Area2D).AddChild(Shape2D);
        AllRoots ??= new List<RootRoot>();
        if (AllRoots.Any(root => !IsInstanceValid(root)))
            AllRoots = new List<RootRoot>();
        AllRoots.Add(this);
        RootLine = new RootLine(
            Vector2.Zero,
            Vector2.FromAngle(Random.Shared.NextSingle() * (float) Math.PI * 2), GrowthLimit, WidenessFactor,
            LongnessFactor, EgoismFactor, CreationEgoismFactor, MaxOffspring, StartColor, EndColor,
            RootHealthStart + (float) Math.Max(0, HealthInfectionFactor * MainArcade.Instance().InfectionValue));
    }

    public void GetCutByLine((Vector2, Vector2 ) line, double damage)
    {
        var (startLocal, endLocal) = line;
        startLocal = ToLocal(startLocal);
        endLocal = ToLocal(endLocal);


        if (RootLine.GetCutByLine(startLocal, endLocal, damage))
        {
            Visible = false;
            Shape2D.Disabled = true;
            //AllRoots.Remove(this);
            //QueueFree();    
        }
        //Polygon2D.Polygon = Geometry2D.ConvexHull(RootLine.GetEndPoints().Append(RootLine.Start).ToArray());
        //QueueRedraw();
    }

    public override void _Draw()
    {
        base._Draw();
        RootLine.Draw((polygon, colors) => DrawColoredPolygon(polygon, colors),
            (start, end, color, width) => DrawLine(start, end, color, width));
        // DrawColoredPolygon(Polygon2D.Polygon, new Color(Colors.Red, 0.5f));
        // circles.ForEach(circle=>DrawCircle(circle,2f,Colors.Red));
        // DrawCircle(RootLine.End,2f,Colors.Red);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (this == AllRoots[0])
            Controller.Process(delta);

        if (!Visible)
            return;

        if (affectedHero != null)
            AffectHero(delta);
    }

    public void TrueProcess(double delta)
    {
        RootLine.GrowBy((float) (GrowthModifier * delta));
        (Shape2D.Shape as ConvexPolygonShape2D).SetPointCloud(RootLine.GetEndPoints().Append(RootLine.Start).ToArray());
        //Polygon2D.Disabled = true;
        //CallDeferred(MethodName.EnablePolygon);
        QueueRedraw();
    }

    // public void EnablePolygon()
    // {
    //     Polygon2D.Disabled = false;
    // }
    public void AffectHero(double delta)
    {
        HeroTouchedThisProcess = true;
    }


    public void _on_root_area_body_entered(Node2D node)
    {
        if (!Visible)
            return;
        var enemy = node as Enemy;
        if (enemy == null)
        {
            var hero = node as Hero;
            if (hero == null)
                return;
            affectedHero = hero;
        }
        else
        {
            if (!enemy.IsInfected())
                enemy.GetInfected();
        }
    }

    public void _on_root_area_body_exited(Node2D node)
    {
        var enemy = node as Enemy;
        if (enemy == null)
        {
            var hero = node as Hero;
            if (hero == null)
                return;
            affectedHero = null;
        }
    }

    public void _on_area_entered(Area2D area)
    {
    }

    public void Reset()
    {
        Visible = true;
        Shape2D.Disabled = false;

        affectedHero = null;
        RootLine = new RootLine(
            Vector2.Zero,
            Vector2.FromAngle(Random.Shared.NextSingle() * (float) Math.PI * 2), GrowthLimit, WidenessFactor,
            LongnessFactor, EgoismFactor, CreationEgoismFactor, MaxOffspring, StartColor, EndColor,
            RootHealthStart + (float) Math.Max(0, HealthInfectionFactor * MainArcade.Instance().InfectionValue));
        QueueRedraw();
    }
}

public class RootController
{
    private readonly int RootsPerProcess = 2;
    private double delta;
    private int RootPtr;

    public RootController()
    {
        RootRoot.AllRoots ??= new List<RootRoot>();
    }

    public void Process(double delta)
    {
        this.delta = delta;
        var maxRootsToProcess = RootRoot.AllRoots.Count(root => root.Visible);

        for (var i = 0; i < Math.Min(maxRootsToProcess, RootsPerProcess); ++i)
        {
            RootPtr = (RootPtr + 1) % RootRoot.AllRoots.Count;
            var chosenRoot = RootRoot.AllRoots[RootPtr];
            if (!chosenRoot.Visible)
            {
                --i;
                continue;
            }

            RootRoot.AllRoots[RootPtr].TrueProcess(delta * RootRoot.AllRoots.Count / RootsPerProcess);
        }
    }

    public void CreateNewRoot(Vector2 position)
    {
        RootRoot.AllRoots ??= new List<RootRoot>();

        if (RootRoot.AllRoots.Any(root => root == null)) RootRoot.AllRoots = new List<RootRoot>();
        var root = RootRoot.AllRoots.Find(root => !root.Visible);
        if (root == null)
        {
            root = MainArcade.Instance().RootPrefab.Instantiate() as RootRoot;
            MainArcade.Instance().AddChild(root);
        }
        else
        {
            root.Reset();
        }

        root.GlobalPosition = position;
    }
}

public class RootLine
{
    private readonly Color color;
    private readonly RootLine parent;
    private readonly Vector2[] polygon;
    public Vector2 Direction;
    public Vector2 End;
    public float Growth, GrowthLimit, WidenessFactor, LongnessFactor, EgoismFactor, CreationEgoismFactor;
    private float Health;
    private bool IsDead;
    private int LastSwingWithDamage = -1;
    private bool LimitReached;
    public int MaxOffsprings;
    public List<RootLine> Offsprings = new();
    public Vector2 Start;
    public Color StartColor, EndColor;

    public RootLine(Vector2 start, Vector2 direction, float growthLimit, float widenessFactor, float longnessFactor,
        float egoismFactor, float creationEgoismFactor, int maxOffsprings, Color startColor, Color endColor,
        float rootHealth, RootLine parent = null)
    {
        StartColor = startColor;
        EndColor = endColor;
        GrowthLimit = growthLimit;
        Start = start;
        Direction = direction;
        Growth = 0;
        MaxOffsprings = maxOffsprings;
        End = Start;
        WidenessFactor = widenessFactor;
        LongnessFactor = longnessFactor;
        EgoismFactor = egoismFactor;
        polygon = new Vector2[4];
        color = StartColor;
        CreationEgoismFactor = creationEgoismFactor;
        this.parent = parent;
        Health = rootHealth;
        CalculatePolygon();
    }

    public void Draw(Action<Vector2[], Color> drawingFunction, Action<Vector2, Vector2, Color, float> DrawLine)
    {
        if (Growth < 0.1)
            return;
        drawingFunction(polygon, color);
        Offsprings.ForEach(offspring => offspring.Draw(drawingFunction, DrawLine));
        //
        // DrawLine(Start,End,Color.FromHsv(0.9f,0.2f,0.7f,0.5f),2f);
    }

    // public void CreateOffspring()
    // {
    // 	if (Offsprings.IsEmpty())
    // 	{
    // 		CreateOffspringSelf();
    // 		return;
    // 	}
    // 	GetSemiRandomRoot().CreateOffspring();
    //
    // }
    private double GetRandomNumber(double minimum, double maximum)
    {
        var random = Random.Shared;
        return random.NextDouble() * (maximum - minimum) + minimum;
    }

    private int GetRandomSign()
    {
        return Random.Shared.NextSingle() < 0.5 ? -1 : 1;
    }


    private void CreateOffspringSelf()
    {
        const double maxRotation = Math.PI / 3;
        const double minRotation = Math.PI / 8;
        var rotation = GetRandomNumber(minRotation, maxRotation) * GetRandomSign();
        if (rotation < 0)
            rotation = Math.PI * 2 + rotation;
        Offsprings.Add(new RootLine(GetRandomOffspringPoint(),
            Direction.Rotated((float) rotation).Normalized(), GrowthLimit, WidenessFactor,
            LongnessFactor, EgoismFactor, CreationEgoismFactor, MaxOffsprings - 1, StartColor, EndColor, Health, this));
    }

    public RootLine GetRandomOffspring()
    {
        if (Offsprings.IsEmpty())
            return this;
        return Offsprings.Random();
    }

    public Vector2[] GetAsPolygon()
    {
        return polygon;
    }

    public Vector2 GetRandomOffspringPoint()
    {
        var a = Start + Direction * ((End - Start).Length() * Random.Shared.NextSingle());
        return a;
    }

    public RootLine GetSemiRandomRoot()
    {
        var chanceForThis = CreationEgoismFactor;
        if (Random.Shared.NextSingle() <= chanceForThis)
            return this;
        return GetRandomOffspring();
    }

    private int GetAppropriateOffspringCount()
    {
        if (Growth < 0.001)
            return 0;
        var offspringDivisor = 1f / MaxOffsprings;
        return (int) (Growth / GrowthLimit / offspringDivisor);
    }

    public float GrowBy(float growthAmount)
    {
        if (growthAmount <= 0.00001)
            return 0;

        var personalPart = LimitReached ? 0 : growthAmount * EgoismFactor * Random.Shared.NextSingle();
        var othersPart = growthAmount - personalPart;

        var startingOffspring = (int) Random.Shared.NextInt64(Offsprings.Count);
        for (var i = 0; i < Offsprings.Count; ++i)
        {
            var thisOffspringPart = Random.Shared.NextSingle() * othersPart;
            othersPart -= thisOffspringPart;
            othersPart += Offsprings[(startingOffspring + i) % Offsprings.Count].GrowBy(thisOffspringPart);
        }

        Growth += growthAmount;
        while (GetAppropriateOffspringCount() > Offsprings.Count) CreateOffspringSelf();
        if (Growth >= GrowthLimit)
        {
            Growth = GrowthLimit;
            LimitReached = true;
            return growthAmount;
        }

        End = Start + Direction * Growth * LongnessFactor;
        CalculatePolygon();
        return 0;
    }

    public int CountOffsprings()
    {
        return Offsprings.Select(offspring => offspring.CountOffsprings()).Sum() + Offsprings.Count;
    }


    private void CalculatePolygon()
    {
        var start = Start;
        var end = End;
        var width = Growth * WidenessFactor;
        // color = StartColor.Lerp(EndColor, 1f / MaxOffsprings);

        var angle = Math.Atan2((end - start).X, (end - start).Y) * 180 / Math.PI;

        var offset = LengthDir(new Vector2(0, width / 2), angle);


        polygon[0].X = start.X - offset.X;
        polygon[0].Y = start.Y - offset.Y;
        polygon[1].X = start.X + offset.X;
        polygon[1].Y = start.Y + offset.Y;
        polygon[2].X = end.X + offset.X;
        polygon[2].Y = end.Y + offset.Y;
        polygon[3].X = end.X - offset.X;
        polygon[3].Y = end.Y - offset.Y;
    }

    private Vector2 LengthDir(Vector2 point, double dir)
    {
        return new Vector2((float) (point.X * Math.Cos(dir) - point.Y * Math.Sin(dir)),
            (float) (point.X * Math.Sin(dir) + point.Y * Math.Cos(dir)));
    }

    public IEnumerable<Vector2> GetEndPoints()
    {
        return Offsprings.SelectMany(offspring => offspring.GetEndPoints()).Append(End);
    }

    public bool GetCutByLine(Vector2 startLocal, Vector2 endLocal, double damage)
    {
        var diesThisSwing = false;
        if (Hero.Instance().SwingCount != LastSwingWithDamage)
        {
            var ans = Geometry2D.SegmentIntersectsSegment(startLocal, endLocal, Start, End).As<Vector2>();
            if (!ans.IsEqualApprox(Vector2.Zero))
            {
                LastSwingWithDamage = Hero.Instance().SwingCount;
                Health -= (float) damage;
                if (Health <= 0) diesThisSwing = true;
            }
        }

        if (!diesThisSwing)
        {
            foreach (var rootLine in Offsprings) rootLine.GetCutByLine(startLocal, endLocal, damage);

            Offsprings.RemoveAll(offspring => offspring.IsDead);
        }
        else
        {
            if (parent == null)
                // No parent to delegate death handling to, assuming we are the root of the roots.
                return true;
            IsDead = true;
        }

        return false;
    }
}