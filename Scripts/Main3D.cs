using Godot;

public partial class Main3D : Node3D
{
    private const float RayLength = 100000.0f;

    public static Vector2 SupposedMousePosition = Vector2.Zero;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        InitArcade();
    }

    public async void InitArcade()
    {
        var viewport = GetNode("SubViewport") as SubViewport;
        //ArcadeViewport = viewport;
        viewport.RenderTargetClearMode = SubViewport.ClearMode.Always;

        await ToSignal(GetTree(), "process_frame");
        await ToSignal(GetTree(), "process_frame");

        ((GetNode("ViewportQuad") as MeshInstance3D).MaterialOverride as StandardMaterial3D).AlbedoTexture =
            viewport.GetTexture();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion eventMouseButton)
        {
            var camera3D = GetNode<Camera3D>("Camera3D");
            var from = camera3D.ProjectRayOrigin(eventMouseButton.Position);
            var to = from + camera3D.ProjectRayNormal(eventMouseButton.Position) * RayLength;

            var spaceState = GetWorld3D().DirectSpaceState;
            // use global coordinates, not local to node
            var query = PhysicsRayQueryParameters3D.Create(from, to);
            var result = spaceState.IntersectRay(query);
            var position = result["position"].As<Vector3>();

            // Approximate mouse position transformations (works because subviewport's camera doesn't move) figured out with trial and error.
            SupposedMousePosition.X = position.X * 51.5f;
            SupposedMousePosition.Y = -position.Y * 51.5f;
        }
    }
}