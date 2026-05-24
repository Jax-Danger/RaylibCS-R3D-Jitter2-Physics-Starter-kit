using Raylib_cs;
using R3D_cs;
using System.Numerics;
using Jitter2;
using Jitter2.LinearMath;
using Jitter2.Collision;
using Core;

Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint);
Raylib.InitWindow(800, 400, "Exit Strategy");
R3D.Init(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

Raylib.SetTargetFPS(100);
Raylib.DisableCursor();

// R3D Environment
var env = R3D.GetEnvironmentEx();
env.Ambient.Color = Color.Gray;

// Light
Light light = R3D.CreateLight(LightType.Spot);
R3D.LightLookAt(light, new Vector3(0, 10, 5), Vector3.Zero);
R3D.EnableShadow(light);
R3D.SetLightActive(light, true);

World world = new(); // creates a physics world
world.Gravity = new JVector(0, -30f, 0);
world.DynamicTree.Filter = World.DefaultDynamicTreeFilter;
world.NarrowPhaseFilter = new TriangleEdgeCollisionFilter();
// Map loading/creating
Map map = new(world);

Player player = new(world);

var camera = new Camera3D
{
    Position = new Vector3(0, 1, 2),
    // Target = new Vector3(0, 0, 1),
    Up = new Vector3(0, 1, 0),
    FovY = 60
};

while (!Raylib.WindowShouldClose())
{
    float delta = Raylib.GetFrameTime();
    world.Step(delta, true);

    player.Update();

    Raylib.BeginDrawing();
    {
        Raylib.ClearBackground(Color. Black);

        R3D.Begin(player.camera);
        {
            map.Draw();
        }
        R3D.End();
    }
    Raylib.EndDrawing();
}
// Cleanup
R3D.Close();
map.Unload();

Raylib.CloseWindow();
