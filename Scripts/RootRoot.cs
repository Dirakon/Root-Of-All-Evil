using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class RootRoot : Node2D
{
    public static List<RootRoot> AllRoots;
    public static RootController Controller;

    private readonly List<Enemy> affectedEnemies = new();
    private Hero? affectedHero;

    private List<Vector2> circles = new();

    [Export] public float GrowthLimit,
        WidenessFactor,
        LongnessFactor,
        GrowthModifier,
        TimerRandomnessModifier,
        EgoismFactor,
        CreationEgoismFactor;

    [Export] public int MaxOffspring;
    public CollisionPolygon2D Polygon2D;
    public RootLine RootLine;

    [Export] public Color StartColor, EndColor;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Polygon2D = new CollisionPolygon2D();
        GetNode("RootArea").AddChild(Polygon2D);
        AllRoots ??= new List<RootRoot>();
        Controller ??= new RootController();
        AllRoots.Add(this);
        RootLine = new RootLine(
            Vector2.Zero, 
            Vector2.FromAngle(Random.Shared.NextSingle() * (float) Math.PI * 2), GrowthLimit, WidenessFactor,
            LongnessFactor, EgoismFactor, CreationEgoismFactor, MaxOffspring, StartColor, EndColor);
        RegularOffspringCreator();
    }

    public void GetCutByLine((Vector2, Vector2 )line)
    {
        var (startLocal, endLocal) = line;
        startLocal = ToLocal(startLocal);
        endLocal = ToLocal(endLocal);


        if (RootLine.GetCutByLine(startLocal, endLocal))
        {
            Visible = false;
            //AllRoots.Remove(this);
                //QueueFree();    
        }
        else
        {
            //Polygon2D.Polygon = Geometry2D.ConvexHull(RootLine.GetEndPoints().Append(RootLine.Start).ToArray());
            //QueueRedraw();
        }
        
        
    }

    
    private async void RegularOffspringCreator()
    {
        // while (true)
        // {
        // 	await ToSignal(GetTree().CreateTimer(TimerRandomnessModifier * Random.Shared.NextDouble()), "timeout");
        // 	circles.Add(RootLine.GetRandomOffspringPoint());
        // //	RootLine.CreateOffspring();
        // }
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
        
        AffectEnemies(delta);
        if (affectedHero != null)
            AffectHero(delta);
    }

    public void TrueProcess(double delta)
    {
        RootLine.GrowBy((float) (GrowthModifier * delta));
        Polygon2D.Polygon = Geometry2D.ConvexHull(RootLine.GetEndPoints().Append(RootLine.Start).ToArray());
        QueueRedraw();
    }

    public void AffectHero(double delta)
    {
    }

    public void AffectEnemies(double delta)
    {
        affectedEnemies.RemoveAll(enemy => enemy == null);
        affectedEnemies.ForEach(enemy => { }
        );
    }

    public void _on_root_area_body_entered(Node2D node)
    {
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
            affectedEnemies.Add(enemy);
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
        else
        {
            affectedEnemies.Remove(enemy);
        }
    }

    public void _on_area_entered(Area2D area)
    {
    }
}

public class RootController
{
    private double delta;
    private int RootPtr;
    private readonly int RootsPerProcess = 2;

    public void Process(double delta)
    {
        this.delta = delta;
        for (var _ = 0; _ < RootsPerProcess; ++_)
        {
            RootPtr = (RootPtr + 1) % RootRoot.AllRoots.Count;
            var chosenRoot = RootRoot.AllRoots[RootPtr];
            if (!chosenRoot.Visible)
                continue;
            RootRoot.AllRoots[RootPtr].TrueProcess(delta * RootRoot.AllRoots.Count / RootsPerProcess);
        }
    }
}

public class RootLine
{
    private readonly Color color;
    public Vector2 Direction;
    public Vector2 End;
    public float Growth, GrowthLimit, WidenessFactor, LongnessFactor, EgoismFactor, CreationEgoismFactor;
    private bool LimitReached;
    public int MaxOffsprings;
    public List<RootLine> Offsprings = new();
    private readonly Vector2[] polygon;
    public Vector2 Start;
    public Color StartColor, EndColor;
    private bool IsDead = false;
    private RootLine parent;
    public RootLine(Vector2 start, Vector2 direction, float growthLimit, float widenessFactor, float longnessFactor,
        float egoismFactor, float creationEgoismFactor, int maxOffsprings, Color startColor, Color endColor, RootLine parent = null)
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
            LongnessFactor, EgoismFactor, CreationEgoismFactor, MaxOffsprings - 1, StartColor, EndColor,this));
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

    public bool GetCutByLine(Vector2 startLocal, Vector2 endLocal)
    {
        var ans = Geometry2D.SegmentIntersectsSegment(startLocal, endLocal, Start, End).As<Vector2>();
        if (ans.IsEqualApprox(Vector2.Zero))
        {
            foreach (var rootLine in Offsprings)
            {
                rootLine.GetCutByLine(startLocal,endLocal);
            }

            Offsprings.RemoveAll(offspring => offspring.IsDead);
        }
        else
        {
            if (parent == null)
            {
                return true;
            }
            else
            {
                IsDead = true;
            }
        }

        return false;
    }
}