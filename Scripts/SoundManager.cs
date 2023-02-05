using Godot;
using System;
using Godot.Collections;

public partial class SoundManager : Node3D
{
	public static void Play(string soundName)
	{
		if (instance == null || !IsInstanceValid(instance))
			return;
		instance.TryPlay(soundName);
	}

	public void TryPlay(string soundName)
	{
		if (!loadedSounds.ContainsKey(soundName))
		{
			var newStreamPlayer = new AudioStreamPlayer();
			var newSound = ResourceLoader.Load<AudioStreamWav>($"res://Sounds/{soundName}.wav");
			newStreamPlayer.Stream = newSound;
			AddChild(newStreamPlayer);
			loadedSounds[soundName] = newStreamPlayer;
		}

		var player = loadedSounds[soundName];
		player.Play();
	}

	private Dictionary<String, AudioStreamPlayer> loadedSounds = new();

	private static SoundManager instance;

	public SoundManager()
	{
		instance = this;
	}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
