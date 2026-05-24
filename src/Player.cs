using System.Numerics;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Raylib_cs;
using Jitter2.Collision;
using R3D_cs;


namespace Core;



public class Player
{
    private const float GRAVITY = 30f;
    private const float MAX_SPEED = 8f;
    private const float CROUCH_SPEED = 4f;
    private const float JUMP_FORCE = 15f;
    private const float STAND_HEIGHT = 4f;
    private const float CROUCH_HEIGHT = 0.8f;

    // These are the shape of the player's collision.
    private const float CAPSULE_RADIUS = 0.4f;
    private const float CAPSULE_HEIGHT = 1.0f;


    private Vector2 sensitivity = new(0.001f, 0.001f);

    // Head bobbing vars
    private float headTimer = 0f;
    private float walkLerp = 0f;
    private float headLerp = STAND_HEIGHT;



    private Vector2 lookRotation = new();
    private Vector2 lean = new(0, 0);
    private bool isGrounded = false;

    // public references for the player and camera.
    // body makes the player a rigidbody for physics.
    public RigidBody body;
    public Camera3D camera;
    private readonly World world;



    public Player(World world)
    {
        this.world = world; // this is the physics world passed through from main.cs

        body = world.CreateRigidBody();

        // gives the capsule shape to the player for collisions
        body.AddShape(new CapsuleShape(CAPSULE_RADIUS, CAPSULE_HEIGHT));

        // JVector is similar to Vector3
        // sets position of the player.
        body.Position = new JVector(0, 1, 0);

        body.AffectedByGravity = false; // allows me to control the gravity myself.
        body.Damping = (0, 0); // either slows in air, or determines how much the player slides around


        // Camera SETUP
        // These are default values for the camera.
        camera.Position = Vector3.Zero;
        camera.Target = Vector3.Zero;
        camera.Up = Vector3.UnitY;
        camera.FovY = 75f;
        camera.Projection = CameraProjection.Perspective;
    }


    public void Update()
    {
        Vector2 mouseDelta = Raylib.GetMouseDelta();
        // lookRotation.X = lookRotation.X - (mouseDelta.X * sensitivity.X);
        lookRotation.X -= mouseDelta.X * sensitivity.X;
        lookRotation.Y += mouseDelta.Y * sensitivity.Y;

        // no idea what CBool is
        // this gets keys for movement
        CBool sideway = Raylib.IsKeyDown(KeyboardKey.D) - Raylib.IsKeyDown(KeyboardKey.A);
        CBool forward = Raylib.IsKeyDown(KeyboardKey.W) - Raylib.IsKeyDown(KeyboardKey.S);
        bool crouching = Raylib.IsKeyDown(KeyboardKey.LeftControl);

        // Updates the player with rotation, moving side-to-side and forward/backward
        // as well as checking for jump and crouching
        // it also handles the jump and crouch too.
        UpdateBody(lookRotation.X, sideway, forward, Raylib.IsKeyPressed(KeyboardKey.Space), crouching);


        float delta = Raylib.GetFrameTime();
        // Determines the headLerp based on if crouching is true(use CROUCH_HEIGHT), or false(Use STAND_HEIGHT)
        headLerp = Single.Lerp(headLerp, crouching ? CROUCH_HEIGHT : STAND_HEIGHT, 20 * delta);

        camera.Position = new Vector3(
            body.Position.X,
            body.Position.Y + headLerp,
            body.Position.Z
        );

        // player is grounded and moving, apply head lerp
        if (isGrounded && ((forward != 0) || (sideway != 0)))
        {
            headTimer += delta * 3;
            walkLerp = Single.Lerp(walkLerp, 1, 10 * delta);
            camera.FovY = Single.Lerp(camera.FovY, 55, 5 * delta);
        }
        else
        // player is in the air
        {
            walkLerp = Single.Lerp(walkLerp, 0, 10 * delta);
            camera.FovY = Single.Lerp(camera.FovY, 60, 5 * delta);
        }
        // controls the "lean" of the camera when the head bobbing is active.
        lean.X = Single.Lerp(lean.X, sideway * 0.02f, 10 * delta);
        lean.Y = Single.Lerp(lean.Y, forward * 0.015f, 10 * delta);

        // applies the head bobbing to the player with the updated values from above.
        UpdateCameraFPS(ref camera);
    }


    private void UpdateBody(float rot, CBool side, CBool forward, bool jumpPressed, bool crouchHold)
    {
        float delta = Raylib.GetFrameTime();

        // if the player moves, normalize the input
        Vector2 input = new((float)side, -(float)forward);
        if ((side !=0) && (forward != 0)) input = Vector2.Normalize(input);

        JVector vel = body.Velocity; // player velocity

        // Everything below this comment is about checking if the player is grounded or not.
        // Using a raycast shooting downwards, if it returns true, player is grounded, false not grounded.
        JVector rayOrigin = body.Position; //starting point for raycast
        JVector rayDirection = new(0, -1, 0); 

        float capsuleBottomDistance = CAPSULE_HEIGHT / 2f + CAPSULE_RADIUS; // distance from the player's feet
        // small additional distance below the feet of the player.
        float groundCheckDist = 0.12f;

        // Shoots a raycast downwards using a small distance for the player to determine if the player is grounded or not.
        // Grounded means the raycast hit the floor, and not grounded is no raycast hit.
        isGrounded = world.DynamicTree.RayCast(
            rayOrigin, rayDirection,
            capsuleBottomDistance + groundCheckDist,
            proxy =>
            {
                // Ignore the player's own collision shape.
                return proxy is not RigidBodyShape shape || shape.RigidBody != body;
            },
            null, out var hitProxy,
            out JVector normal,
            out float groundDist
        );

        //Apply the "gravity" while in the air.
        if (!isGrounded) vel.Y -= GRAVITY * delta;
        // jumping is only allowed while player is grounded.
        if (isGrounded && jumpPressed)
        {
            vel.Y = JUMP_FORCE;
            isGrounded = false;
        }


        // This is the front and right directions of the player, later applied to player movement.
        JVector front = new(MathF.Sin(rot), 0f, MathF.Cos(rot));
        JVector right = new(MathF.Cos(-rot), 0f, MathF.Sin(-rot));
        // Direction the player is moving towards.
        JVector moveDir = input.X * right + input.Y * front;

        float maxSpeed = crouchHold ? CROUCH_SPEED : MAX_SPEED;
        vel.X = moveDir.X * maxSpeed; // left and right movement
        vel.Z = moveDir.Z * maxSpeed;// forward and backward movement.

        // This actually applies the velocity to the player allowing movement.
        body.Velocity = vel;
    }
    private void UpdateCameraFPS(ref Camera3D camera)
    {
        // equivalent to var up = new Vector3(0, 1, 0);
        Vector3 up = new(0, 1, 0);
        Vector3 targetOffset = new(0, 0, -1);

        // up and down mouse motion for the player camera 
        Vector3 yaw = Vector3.Transform(targetOffset, Quaternion.CreateFromAxisAngle(up, lookRotation.X));
        // Sets the maximum look up angle.
        float maxAngleUp = Vector3Angle(up, yaw);
        maxAngleUp -= 0.0001f;
        if (-(lookRotation.Y) > maxAngleUp) lookRotation.Y = -maxAngleUp;
        // sets minimum look up angle.
        float maxAngleDown = Vector3Angle(-up, yaw);
        maxAngleDown *= -1.0f;
        maxAngleDown += 0.001f;
        if (-(lookRotation.Y) < maxAngleDown) lookRotation.Y = -maxAngleDown;

        // controls the pitch/left-right motion of the player camera
        Vector3 right = Vector3.Normalize(Vector3.Cross(yaw, up));
        float pitchAngle = -lookRotation.Y - lean.Y;
        pitchAngle = Math.Clamp(pitchAngle, -MathF.PI / 2 + 0.0001f, MathF.PI / 2 - 0.0001f);
        Vector3 pitch = Vector3.Transform(yaw, Quaternion.CreateFromAxisAngle(right, pitchAngle));


        // Head bobbing math
        float headSin = MathF.Sin(headTimer * MathF.PI);
        float headCos = MathF.Cos(headTimer * MathF.PI);
        const float stepRotation = 0.01f;
        camera.Up = Vector3.Transform(up, Quaternion.CreateFromAxisAngle(pitch, headSin * stepRotation + lean.X));

        // More math for headbobbing, but this time it's applied to the bobbing variable
        // which later is added to the player camera.
        const float bobSide = 0.1f;
        const float bobUp = 0.15f;
        Vector3 bobbing = Vector3.Multiply(right, headSin * bobSide);
        bobbing.Y = MathF.Abs(headCos * bobUp);


        // updates the target and position with head bob 
        camera.Position += bobbing * walkLerp;
        camera.Target = camera.Position + pitch;
    }

    float Vector3Angle(Vector3 a, Vector3 b) => MathF.Acos(Math.Clamp(Vector3.Dot(Vector3.Normalize(a), Vector3.Normalize(b)), -1f, 1f));
}