


using System.Linq;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using R3D_cs;
using Raylib_cs;

public class Map
{
    private R3D_cs.Model scene;
    public RigidBody StaticBody;

    private const int COLLISION_MESH = 0;
    private const int VISUAL_MESH = 1;
    private string mapPath = "src/Map_Src/Test_Map.glb";

    public unsafe Map(World world)
    {
        Raylib_cs.Model rlModel = Raylib.LoadModel(mapPath);
        Raylib_cs.Mesh physicsMesh = rlModel.Meshes[COLLISION_MESH];

        float* verts = (float*)physicsMesh.Vertices;
        ushort* indices = (ushort*)physicsMesh.Indices;

        var jVertices = new JVector[physicsMesh.VertexCount];
        for (int i = 0; i < physicsMesh.VertexCount; i++)
        {
            jVertices[i] = new JVector(verts[i * 3 + 0], verts[i * 3 + 1], verts[i * 3 + 2]);
        }

        var jIndices = new ushort[physicsMesh.TriangleCount * 3];
        for (int i = 0; i < physicsMesh.TriangleCount * 3; i++)
        {
            jIndices[i] = indices[i];
        }


        var triMesh = new TriangleMesh(jVertices, jIndices);
        var shapes = TriangleShape.CreateAllShapes(triMesh);

        StaticBody = world.CreateRigidBody();
        StaticBody.AddShapes(shapes.Cast<RigidBodyShape>(), MassInertiaUpdateMode.Preserve);
        StaticBody.Position = JVector.Zero;
        StaticBody.MotionType = MotionType.Static;

        // Free memory off CPU for the model
        Raylib.UnloadModel(rlModel);

        LoadModel();
    }

    private void LoadModel()
    {
        // Load model to gpu via R3D
        scene = R3D.LoadModel(mapPath);
    }

    public void Unload()
    {
        R3D.UnloadModel(scene, true);
    }


    public void Draw()
    {
        for (int i = VISUAL_MESH; i < scene.Meshes.Length; i++)
        {
            var mesh = scene.Meshes[i];
            var material = scene.Materials[scene.MeshMaterials[i]];

            R3D.DrawMesh(mesh, material, JVector.Zero, 1f);
        }
    }
}