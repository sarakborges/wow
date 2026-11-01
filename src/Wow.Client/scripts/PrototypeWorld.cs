using Godot;

namespace Wow.Client;

/// <summary>
/// Temporary engine smoke-test scene. All geometry is generated at runtime;
/// no World of Warcraft assets or data are loaded by this prototype.
/// </summary>
public partial class PrototypeWorld : Node3D
{
    private const float WalkSpeed = 6.0f;
    private const float Gravity = 24.0f;
    private const float JumpVelocity = 7.0f;

    private CharacterBody3D? _player;
    private Camera3D? _camera;

    public override void _Ready()
    {
        ConfigureInput();
        CreateTestWorld();

        _player = CreatePlayer();
        AddChild(_player);

        _camera = new Camera3D
        {
            Name = "PrototypeCamera",
            Current = true,
            Fov = 65.0f
        };
        AddChild(_camera);

        CreateHud();
        UpdateCamera();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player is null)
        {
            return;
        }

        var input = Input.GetVector(
            "move_left",
            "move_right",
            "move_forward",
            "move_back");

        var direction = new Vector3(input.X, 0.0f, input.Y);
        if (direction.LengthSquared() > 1.0f)
        {
            direction = direction.Normalized();
        }

        var velocity = _player.Velocity;
        velocity.X = direction.X * WalkSpeed;
        velocity.Z = direction.Z * WalkSpeed;

        if (_player.IsOnFloor())
        {
            if (velocity.Y < 0.0f)
            {
                velocity.Y = 0.0f;
            }

            if (Input.IsActionJustPressed("jump"))
            {
                velocity.Y = JumpVelocity;
            }
        }
        else
        {
            velocity.Y -= Gravity * (float)delta;
        }

        _player.Velocity = velocity;
        _player.MoveAndSlide();

        if (direction.LengthSquared() > 0.001f)
        {
            _player.LookAt(_player.GlobalPosition + direction, Vector3.Up);
        }
    }

    public override void _Process(double delta)
    {
        UpdateCamera();
    }

    private static void ConfigureInput()
    {
        BindKey("move_forward", Key.W);
        BindKey("move_back", Key.S);
        BindKey("move_left", Key.A);
        BindKey("move_right", Key.D);
        BindKey("jump", Key.Space);
    }

    private static void BindKey(string action, Key key)
    {
        if (!InputMap.HasAction(action))
        {
            InputMap.AddAction(action);
        }

        var keyEvent = new InputEventKey
        {
            PhysicalKeycode = key
        };

        InputMap.ActionAddEvent(action, keyEvent);
    }

    private void CreateTestWorld()
    {
        var groundSize = new Vector3(160.0f, 1.0f, 160.0f);

        var groundBody = new StaticBody3D
        {
            Name = "GeneratedTestFloor",
            Position = new Vector3(0.0f, -0.5f, 0.0f)
        };

        groundBody.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = groundSize }
        });
        AddChild(groundBody);

        var groundMesh = new MeshInstance3D
        {
            Name = "GeneratedTestFloorVisual",
            Position = groundBody.Position,
            Mesh = new BoxMesh { Size = groundSize },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.20f, 0.29f, 0.19f),
                Roughness = 1.0f
            }
        };
        AddChild(groundMesh);

        var sun = new DirectionalLight3D
        {
            Name = "PrototypeSun",
            RotationDegrees = new Vector3(-52.0f, 28.0f, 0.0f),
            ShadowEnabled = true
        };
        AddChild(sun);
    }

    private static CharacterBody3D CreatePlayer()
    {
        var player = new CharacterBody3D
        {
            Name = "PlaceholderPlayer",
            Position = new Vector3(0.0f, 1.0f, 0.0f),
            FloorSnapLength = 0.2f
        };

        player.AddChild(new CollisionShape3D
        {
            Position = new Vector3(0.0f, 0.9f, 0.0f),
            Shape = new CapsuleShape3D
            {
                Radius = 0.4f,
                Height = 1.8f
            }
        });

        player.AddChild(new MeshInstance3D
        {
            Name = "PlaceholderCharacterVisual",
            Position = new Vector3(0.0f, 0.9f, 0.0f),
            Mesh = new CapsuleMesh
            {
                Radius = 0.4f,
                Height = 1.8f,
                RadialSegments = 20,
                Rings = 8
            },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.35f, 0.56f, 0.82f),
                Roughness = 0.8f
            }
        });

        return player;
    }

    private void CreateHud()
    {
        var layer = new CanvasLayer { Name = "PrototypeHud" };
        var label = new Label
        {
            Position = new Vector2(20.0f, 18.0f),
            Text = "WOW SINGLE-PLAYER RUNTIME  |  ENGINE SMOKE TEST\nWASD: move    SPACE: jump\nGenerated placeholder geometry — no WoW assets loaded"
        };

        label.AddThemeFontSizeOverride("font_size", 16);
        layer.AddChild(label);
        AddChild(layer);
    }

    private void UpdateCamera()
    {
        if (_player is null || _camera is null)
        {
            return;
        }

        _camera.GlobalPosition = _player.GlobalPosition + new Vector3(0.0f, 4.2f, 7.5f);
        _camera.LookAt(_player.GlobalPosition + new Vector3(0.0f, 0.9f, 0.0f), Vector3.Up);
    }
}
