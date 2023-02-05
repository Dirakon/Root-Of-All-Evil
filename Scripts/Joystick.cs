using Godot;
using System;

public partial class Joystick : Node3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion motionEvent)
		{
			mouseDelta = new Vector2(
				Math.Sign( motionEvent.Relative.X),
				Math.Sign(motionEvent.Relative.Y)
			);
		}
	}

	[Export] private float speed;
	private Vector2 mouseDelta = Vector2.Zero;

	private int recordedXSign = 0,recordedZSign = 0;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

		Vector2 controls;
		if (Name == "Move")
			controls = new Vector2(Input.GetAxis("left", "right"), -Input.GetAxis("down", "forward"));
		else
		{
			controls = mouseDelta;
			mouseDelta = Vector2.Zero;
		}

		// controls[0] = Math.Clamp(-1, controls[0], 1);
		var desiredRotationZ = -controls[0]*(Math.PI* 20/180);
		var desiredRotationX = controls[1] * (Math.PI* 20/180);
		
		var currentRotationZ = Rotation.Z;
		var currentRotationX = Rotation.X;

		var newZSign = Math.Sign(Math.Round(currentRotationZ / (Math.PI * 20 / 180)));
		var newXSign = Math.Sign(Math.Round(currentRotationX / (Math.PI * 20 / 180)));

		if (newZSign != recordedZSign)
		{
			if (recordedZSign == 0)
			{
				SoundManager.Play("joystick_limit");
			}

			recordedZSign = newZSign;
		}
		
		if (newXSign != recordedXSign)
		{
			if (recordedXSign == 0)
			{
				SoundManager.Play("joystick_limit");
			}

			recordedXSign = newXSign;
		}

		var zDir = Math.Sign(desiredRotationZ - currentRotationZ);
		var zDistance = Math.Abs(desiredRotationZ - currentRotationZ);
		var distanceThisTickZ = (float)Math.Min(zDistance, speed * delta);
		RotateZ(zDir*distanceThisTickZ);
		


		var xDir = Math.Sign(desiredRotationX - currentRotationX);
		var xDistance = Math.Abs(desiredRotationX - currentRotationX);
		var distanceThisTickX = (float)Math.Min(xDistance, speed * delta);
		RotateX(xDir*distanceThisTickX);


	}
}
