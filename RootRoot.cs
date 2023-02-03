using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Range = System.Range;

public partial class RootRoot : Node2D
{
	public static List<RootRoot> AllRoots;
	public static RootController Controller;
	public CollisionPolygon2D Polygon2D;
	public RootLine RootLine;

	[Export] public float GrowthLimit, WidenessFactor, LongnessFactor, GrowthModifier, TimerRandomnessModifier, EgoismFactor,CreationEgoismFactor;
	[Export] public Color StartColor, EndColor;
	[Export] public int MaxOffspring;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Polygon2D = GetNode("RootArea").GetNode("CollisionPolygon2D") as CollisionPolygon2D;
		AllRoots ??= new();
		Controller ??= new();
		AllRoots.Add(this);
		RootLine = new RootLine(
			GlobalPosition, 
			Vector2.FromAngle(Random.Shared.NextSingle() * (float) Math.PI * 2),GrowthLimit,WidenessFactor,LongnessFactor,EgoismFactor,CreationEgoismFactor,MaxOffspring,StartColor,EndColor);
		RegularOffspringCreator();
		
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

	private List<Vector2> circles = new List<Vector2>();

	public override void _Draw()
	{
		base._Draw();
		 RootLine.Draw((polygon,colors)=>DrawColoredPolygon(polygon,colors),(start,end,color,width)=>DrawLine(start,end,color,width));
		 DrawColoredPolygon(Polygon2D.Polygon,new Color(Colors.Red,0.5f));
		 // circles.ForEach(circle=>DrawCircle(circle,2f,Colors.Red));
		// DrawCircle(RootLine.End,2f,Colors.Red);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (this == AllRoots[0])
			Controller.Process(delta);
		
		
		 AffectEnemies(delta);
		 if (affectedHero != null)
			AffectHero(delta);
	}

	public void TrueProcess(double delta)
	{
		RootLine.GrowBy((float) (GrowthModifier*delta));
		Polygon2D.Polygon =  Geometry2D.ConvexHull(RootLine.GetEndPoints().Append(RootLine.Start).ToArray());
		QueueRedraw();
		
	}

	private List<Enemy> affectedEnemies = new();
	private Hero? affectedHero;

	public void AffectHero(double delta)
	{
	}

	public void AffectEnemies(double delta)
	{
		affectedEnemies.RemoveAll(enemy => enemy==null);
		affectedEnemies.ForEach(enemy =>
			{
				
			}
		);
	}
	
	public void _on_root_area_body_entered(Node2D node)
	{		
		
		Enemy enemy = node as  Enemy;
		if (enemy == null)
		{
			Hero hero = node as Hero;
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
		Enemy enemy = node as  Enemy;
		if (enemy == null)
		{
			Hero hero = node as Hero;
			if (hero == null)
				return;
			affectedHero = null;
		}
		else
		{
			affectedEnemies.Remove(enemy);
		}
	}
}

public class RootController
{
	private int RootPtr = 0;
	private double delta;
	private int RootsPerProcess = 2;

	public void Process(double delta)
	{
		this.delta = delta;
		for (int _ = 0; _ < RootsPerProcess; ++_)
		{
			RootPtr = (RootPtr + 1) % RootRoot.AllRoots.Count;
			RootRoot.AllRoots[RootPtr].TrueProcess(delta * RootRoot.AllRoots.Count / RootsPerProcess);
		}
	}

}

public class RootLine
{
	private Vector2[] polygon;
	private List<RootLine> Offsprings = new();
	public Vector2 Start;
	public Vector2 Direction;
	public Vector2 End;
	public Color StartColor, EndColor;
	public float Growth, GrowthLimit, WidenessFactor,LongnessFactor,EgoismFactor,CreationEgoismFactor;
	private bool LimitReached = false;
	public int MaxOffsprings;
		
	public RootLine(Vector2 start, Vector2 direction, float growthLimit, float widenessFactor, float longnessFactor,float egoismFactor, float creationEgoismFactor, int maxOffsprings, Color startColor, Color endColor)
	{
		this.StartColor = startColor;
		this.EndColor = endColor;
		this.GrowthLimit = growthLimit;
		this.Start = start;
		this.Direction = direction;
		this.Growth = 0;
		this.MaxOffsprings = maxOffsprings;
		this.End = Start;
		this.WidenessFactor = widenessFactor;
		this.LongnessFactor = longnessFactor;
		this.EgoismFactor = egoismFactor;
		this.polygon = new Vector2[4];
		this.color = StartColor;
		this.CreationEgoismFactor = creationEgoismFactor;
		CalculatePolygon();
	}

	public void Draw(Action<Vector2[],Color> drawingFunction, Action<Vector2,Vector2,Color,float> DrawLine)
	{
		if (Growth < 0.1)
			return;
		drawingFunction(polygon,color);
		Offsprings.ForEach(offspring=>offspring.Draw(drawingFunction,DrawLine));
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
		Random random = Random.Shared;
		return random.NextDouble() * (maximum - minimum) + minimum;
	}
	private int GetRandomSign()
	{
		return Random.Shared.NextSingle() < 0.5 ? -1 : 1;
	}


	private void CreateOffspringSelf()
	{
		const double maxRotation = Math.PI/3;
		const double minRotation = Math.PI/8;
		double rotation = GetRandomNumber(minRotation,maxRotation) * GetRandomSign();
		if (rotation < 0)
			rotation = Math.PI * 2 + rotation;
		Offsprings.Add(new RootLine(GetRandomOffspringPoint(),
			Direction.Rotated((float)rotation).Normalized(),GrowthLimit, WidenessFactor,
			LongnessFactor,EgoismFactor,CreationEgoismFactor,MaxOffsprings - 1,StartColor,EndColor));
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
		var a =  Start + Direction * ((End-Start).Length() * Random.Shared.NextSingle() );
		return a;
	}

	public RootLine GetSemiRandomRoot()
	{
		float chanceForThis = CreationEgoismFactor;
		if (Random.Shared.NextSingle() <= chanceForThis)
			return this;
		return GetRandomOffspring();
	
	}

	private Color color;

	private int GetAppropriateOffspringCount()
	{
		if (Growth < 0.001)
			return 0;
		float offspringDivisor = 1f/MaxOffsprings;
		return (int) ((Growth / GrowthLimit) / offspringDivisor);
	}

	public float GrowBy(float growthAmount)
	{
		if (growthAmount <= 0.00001)
			return 0;
		
		float personalPart =LimitReached? 0 : growthAmount * EgoismFactor * Random.Shared.NextSingle();
		float othersPart = growthAmount - personalPart;
		
		int startingOffspring = (int)Random.Shared.NextInt64(Offsprings.Count);
		for (int i = 0; i < Offsprings.Count; ++i)
		{
			float thisOffspringPart = Random.Shared.NextSingle()*othersPart;
			othersPart -= thisOffspringPart;
			othersPart += Offsprings[(startingOffspring + i ) % Offsprings.Count].GrowBy(thisOffspringPart);
		}

		Growth += growthAmount;
		while (GetAppropriateOffspringCount() > Offsprings.Count)
		{
			CreateOffspringSelf();
		}
		if (Growth >= GrowthLimit)
		{
			Growth = GrowthLimit;
			LimitReached = true;
			return growthAmount;
		}
		else
		{
			End = Start + Direction * Growth * LongnessFactor;
			CalculatePolygon();
			return 0;
		}

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

		var offset = LengthDir(new(0, (float) width / 2), angle);

		
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
		return new((float) (point.X * Math.Cos(dir) - point.Y * Math.Sin(dir)),
			(float)(point.X * Math.Sin(dir) + point.Y * Math.Cos(dir)));
	}

	public IEnumerable<Vector2> GetEndPoints()
	{
		return Offsprings.SelectMany(offspring => offspring.GetEndPoints()).Append(End);
	}
}
