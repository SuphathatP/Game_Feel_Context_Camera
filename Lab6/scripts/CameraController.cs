using Godot;
using System;

public partial class CameraController : Node3D
{
    [ExportGroup("Camera")]
    [Export] public Node3D CameraPivot;
    [Export] public Node3D CameraYaw;
    [Export] public Node3D CameraPitch;
    [Export] public Node3D CameraOffset;
    [Export] public Node3D CameraZoom;
    [Export] public Camera3D camera;

    [ExportGroup("Dynamic Zoom")]
    [Export] public float IdleZoom = -4.0f;
    [Export] public float WalkZoom = -2.0f;
    [Export] public float CrouchZoom = -6.0f;
    [Export] public float MovingZoom = 4f;
    [Export] public float SprintZoom = 8.0f;
    [Export] public float ZoomLerpSpeed = 1.2f;
    private float targetZoomZ;

    [ExportGroup("Hint Camera")]
    [Export] public float HintBlendSpeed = 1f;
    private bool hintActive = false;
    private Node3D hintTarget = null;
    private float hintBlend = 0f;
    private float normalFOV = 75f;
    private float hintFOV = 70f;
    private Vector3 hintCameraPosition;
    private Vector3 smoothedMidpoint;
    
    [ExportGroup("Target")]
    [Export] public Node3D Player;
    [Export] public Node3D PlayerPivot;
    [Export] public Vector3 targetPosition;
    [Export] public float FollowSpeed = 6f;
    [Export] public float RotationSpeed = 8f;

    [ExportGroup("Raycast")]
    [Export] public Node3D RayOrigin;
	[Export] public RayCast3D CenterRay;
	[Export] public RayCast3D Left1Ray;
	[Export] public RayCast3D Left2Ray;
	[Export] public RayCast3D Right1Ray;
	[Export] public RayCast3D Right2Ray;
	[Export] public RayCast3D UpRay;
	[Export] public RayCast3D DownRay;
    [Export] public RayCast3D CameraCollisionRay;

    [ExportGroup("Ray Setting")]
    [Export] public float RayLength = 4.0f;
    [Export] public float MinRayHitDistance = 0.3f;
    [Export] public float MaxRayHitDistance = 4.0f;
    [Export] public float LerpSpeed = 8.0f;
    [Export] public float MinCameraDistance = 1.0f;

    private Vector3 avoidanceOffset = Vector3.Zero;

    [ExportGroup("Sensitivity")]
    [Export] public float MouseSensitivity = 0.1f;
    [Export] float JoySensitivite = 2.0f;

    [ExportGroup("Clamp")]
    [Export] public float MinPitch = -45.0f;
    [Export] public float MaxPitch = 45.0f;

    [ExportGroup("Auto Align")]
    [Export] public float AutoAlignSpeed = 1.5f;
    [Export] public float WaitAlignTime = 3.0f;
    public float WaitAlignTimer = 0f;

    private float pitchRotation = 0;
    private Vector3 lastPlayerPosition;
    private Vector3 lastMoveDirection = Vector3.Forward;
    private Vector2 mouseDelta = Vector2.Zero;

    private Vector3 baseLocalOffset;
    private Vector3 currentCameraPosition;
    bool PlayerDebug = false;

    public override void _Input(InputEvent @event)
    {
        // Capture mouse movement
        if (@event is InputEventMouseMotion motion)
        {
            mouseDelta = motion.Relative;
        }

        if (Input.IsActionJustPressed("player_debug"))
		{
			PlayerDebug = !PlayerDebug;
		}
    }

    public override void _Ready()
    {
        WaitAlignTimer = WaitAlignTime;
        lastPlayerPosition = Player.GlobalPosition;
        baseLocalOffset = CameraZoom.Position;
        currentCameraPosition = CameraZoom.GlobalPosition;

        CameraInitSync();
    }

    public override void _Process(double delta)
    {
        HandleCameraRotation(delta);
        HandleRayCast(delta);
        HandleHintBlend(delta);
        ApplyFinalCameraTransform();
    }

    private void HandleCameraRotation(double delta)
    {
        float dt = (float)delta;
        
        WaitAlignTimer -= dt;

        // Lerp Camera to follow player.
        Vector3 targetPosition = Player.GlobalPosition;
        GlobalPosition = GlobalPosition.Lerp(targetPosition, FollowSpeed * dt);

        // Read joystick input.
        Vector2 rightJoyStick = new Vector2(Input.GetJoyAxis(0, JoyAxis.RightX), Input.GetJoyAxis(0, JoyAxis.RightY));

        // Combine mosue and joystick.
        Vector2 lookInput = Vector2.Zero;

        lookInput += mouseDelta * MouseSensitivity;
        lookInput += rightJoyStick * JoySensitivite;

        mouseDelta = Vector2.Zero;

        // Deadzone for camera input.
        bool hasCameraRotateInput = Mathf.Abs(lookInput.X) > 0.01f || Mathf.Abs(lookInput.Y) > 0.01f;

        // Disable manual rotation while hint is active
        if (hasCameraRotateInput)
        {
            WaitAlignTimer = WaitAlignTime;

            pitchRotation -= lookInput.Y * dt;
            pitchRotation = Mathf.Clamp(pitchRotation, Mathf.DegToRad(MinPitch), Mathf.DegToRad(MaxPitch));
            CameraPitch.Rotation = new Vector3(pitchRotation, 0, 0);

            CameraYaw.RotateY(-lookInput.X * dt);
        }

        // Auto align if no rotation input and waitTime is 0.
        Vector3 movement = Player.GlobalPosition - lastPlayerPosition;
        lastPlayerPosition = Player.GlobalPosition;

        bool isPlayerMoving = movement.Length() > 0.01f;
        bool isCrouching = Input.IsActionPressed("crouch");
        bool isSprinting = Input.IsActionPressed("sprint");

        if (PlayerDebug)
        {
            // Draw debug ray.
            DebugDrawRay(CenterRay, Colors.White);
            DebugDrawRay(Left1Ray, Colors.Cyan);
            DebugDrawRay(Left2Ray, Colors.Blue);
            DebugDrawRay(Right1Ray, Colors.Cyan);
            DebugDrawRay(Right2Ray, Colors.Blue);
            DebugDrawRay(UpRay, Colors.Green);
            DebugDrawRay(DownRay, Colors.LightGreen);
            DebugDrawRay(CameraCollisionRay, Colors.Pink);
            // DebugDraw3D.DrawSphere(Player.GlobalPosition, 0.1f, Colors.White);
        }

        if (isPlayerMoving)
        {
            lastMoveDirection = movement;
        }
        
        // Zoom target based on movement.
        if (isCrouching)
        {
            targetZoomZ = CrouchZoom;
        }
        else if (!isPlayerMoving)
        {
            targetZoomZ = IdleZoom;
        }
        else if (isSprinting)
        {
            targetZoomZ = SprintZoom;
        }
        else
        {
            targetZoomZ = WalkZoom;
        }

        baseLocalOffset.Z = Mathf.Lerp(baseLocalOffset.Z, targetZoomZ, ZoomLerpSpeed * dt);

        // Disable auto-align while hint is active
        if (hintBlend < 0.99f && !hasCameraRotateInput && WaitAlignTimer <= 0f)
        {
            Vector3 forwardDirection = PlayerPivot.GlobalTransform.Basis.Z;

            float targetYaw = Mathf.Atan2(-forwardDirection.X, -forwardDirection.Z);
            float currentYaw = CameraYaw.Rotation.Y;

            CameraYaw.Rotation = new Vector3(0, Mathf.LerpAngle(currentYaw, targetYaw, AutoAlignSpeed * dt), 0);
        }

        //GD.Print("WaitAlign: ", WaitAlignTimer, "  move: ", movement, "  auto align: ", !hasCameraRotateInput && isPlayerMoving && WaitAlignTimer <= 0f);
        camera.GlobalPosition = CameraZoom.GlobalPosition;

        // Normal gameplay rotation (orbit)
        Basis gameplayBasis = CameraZoom.GlobalBasis.Orthonormalized();

        // If no hint blend or no target change to gameplay camera
        if (hintBlend <= 0f || hintTarget == null)
        {
            camera.GlobalBasis = gameplayBasis;
            return;
        }

        // Midpoint between player and hint.
        Vector3 midpoint = (Player.GlobalPosition + hintTarget.GlobalPosition) * 0.5f;

        // Direction from camera to midpoint.
        Vector3 cameraPosition = camera.GlobalPosition;
        Vector3 direction = (midpoint - cameraPosition).Normalized();

        // Basis looking at midpoint.
        Basis hintBasis = Basis.LookingAt(direction, Vector3.Up).Orthonormalized();

        // Blend between normal orbit and hint.
        camera.GlobalBasis = gameplayBasis.Slerp(hintBasis, hintBlend);  
    }

    private void CameraInitSync()
    {
        Vector3 forward = PlayerPivot.GlobalTransform.Basis.Z;
        float initialYaw = Mathf.Atan2(-forward.X, -forward.Z);
        CameraYaw.Rotation = new Vector3(0, initialYaw, 0);

        pitchRotation = CameraPitch.Rotation.X;
        camera.GlobalTransform = CameraZoom.GlobalTransform;
    }

    private Vector3 ComputeRayCameraPosition(Vector3 playerPosition, Vector3 idealCameraPosition)
    {
        Vector3 toCamera = idealCameraPosition - playerPosition;
        Vector3 direction = toCamera.Normalized();
        float idealDistance = toCamera.Length();

        // Set up all whiskers
        UpdateRay(playerPosition, idealCameraPosition);

        // Find closest hit distance between ray.
        float closestHit = float.MaxValue;
        bool anyHit = false;

        float distance;

        if (RayHit(CenterRay, out distance) && distance < closestHit) { closestHit = distance; anyHit = true; }
        if (RayHit(Left1Ray, out distance) && distance < closestHit) { closestHit = distance; anyHit = true; }
        if (RayHit(Left2Ray, out distance) && distance < closestHit) { closestHit = distance; anyHit = true; }
        if (RayHit(Right1Ray, out distance) && distance < closestHit) { closestHit = distance; anyHit = true; }
        if (RayHit(Right2Ray, out distance) && distance < closestHit) { closestHit = distance; anyHit = true; }
        if (RayHit(UpRay, out distance) && distance < closestHit) { closestHit = distance; anyHit = true; }
        if (RayHit(DownRay, out distance) && distance < closestHit) { closestHit = distance; anyHit = true; }

        if (!anyHit)
            // If no obstacle move camera to full distance
            return playerPosition + direction * idealDistance;

        // Move camera near closest hit but never closer than MinCameraDistance
        float targetDistance = Mathf.Clamp(closestHit - 0.2f, MinCameraDistance, idealDistance);
        return playerPosition + direction * targetDistance;
    }

    private void HandleRayCast(double delta)
    {
        float dt = (float)delta;
        Vector3 playerPosition = Player.GlobalPosition;

        // Ideal camera position.
        Vector3 idealCameraPosition = CameraOffset.GlobalTransform.Origin + CameraOffset.GlobalTransform.Basis * baseLocalOffset;

        // Find best valid camera position using ray.
        Vector3 targetCameraPosition = ComputeRayCameraPosition(playerPosition, idealCameraPosition);
        
        // Prevent camera going inside player.
        Vector3 toCam = targetCameraPosition - playerPosition;
        float dist = toCam.Length();
        if (dist < MinCameraDistance)
            targetCameraPosition = playerPosition + toCam.Normalized() * MinCameraDistance;

        // Smooth lerp camera to target.
        currentCameraPosition = currentCameraPosition.Lerp(targetCameraPosition, LerpSpeed * dt);
        CameraZoom.GlobalPosition = currentCameraPosition;

        // Extra Collision ray to ensure no cliping.
        HandleCameraCollision(playerPosition);
    }


    private void UpdateRay(Vector3 playerPosition, Vector3 idealCameraPosition)
    {
        
        Vector3 toCamera = (idealCameraPosition - playerPosition).Normalized();
        Vector3 forward = toCamera; // from player to camera
        Vector3 right = CameraPivot.GlobalTransform.Basis.X;
        Vector3 up = CameraPivot.GlobalTransform.Basis.Y;

        Vector3 origin = RayOrigin.GlobalTransform.Origin;

        // Center
        SetRay(CenterRay, origin, forward);

        // Left
        SetRay(Left1Ray, origin, (forward - right * 0.3f).Normalized());
        SetRay(Left2Ray, origin, (forward - right * 0.6f).Normalized());
        
        // Right
        SetRay(Right1Ray, origin, (forward + right * 0.3f).Normalized());
        SetRay(Right2Ray, origin, (forward + right * 0.6f).Normalized());

        // Up / Down
        SetRay(UpRay, origin, (forward + up * 0.4f).Normalized());
        SetRay(DownRay, origin, (forward - up * 0.2f).Normalized());
    }

    private void SetRay(RayCast3D ray, Vector3 origin, Vector3 direction)
    {
        ray.GlobalPosition = origin;
        ray.LookAt(origin + direction, Vector3.Up);
        ray.TargetPosition = new Vector3 (0, 0, -RayLength);
    }

    private bool RayHit(RayCast3D ray, out float distance)
    {
        distance = 0f;
        if (!ray.IsColliding())
            return false;

        var collier = ray.GetCollider();

        if (collier is Node node && node.IsInGroup("props"))
            return false;

        Vector3 origin = ray.GlobalPosition;
        Vector3 hit = ray.GetCollisionPoint();
        distance = origin.DistanceTo(hit);

        return distance >= MinRayHitDistance && distance <= MaxRayHitDistance;
    }

    private void HandleCameraCollision(Vector3 playerPosition)
    {
        Vector3 toCamera = CameraZoom.GlobalPosition - playerPosition;
        float distance = toCamera.Length();
        Vector3 direction = toCamera.Normalized();
        
        CameraCollisionRay.GlobalPosition = playerPosition;
        CameraCollisionRay.TargetPosition = direction * distance;
        CameraCollisionRay.ForceRaycastUpdate();

        if (CameraCollisionRay.IsColliding())
        {
            Vector3 hit = CameraCollisionRay.GetCollisionPoint();
            
            // Put camera slightly in front of the hit point.
            Vector3 newPosition = hit - direction * 0.2f;
            CameraZoom.GlobalPosition = newPosition;
        }
    }
    
    // Hint override camera.
    public void EnterHint(Node3D target, float fov = 70f)
    {
        hintBlend = 0f;
        hintCameraPosition = CameraZoom.GlobalPosition;
        hintTarget = target;
        hintFOV = fov;
        hintActive = true;
    }

    public void ExitHint()
    {
        hintActive = false;
    }

    private void HandleHintBlend(double delta)
    {
        float dt = (float)delta;
        float target = hintActive ? 1f : 0f;
        hintBlend = Mathf.MoveToward(hintBlend, target, HintBlendSpeed * dt);
    }

    private void ApplyFinalCameraTransform()
    {
        float targetFov = Mathf.Lerp(normalFOV, hintFOV, hintBlend);
        camera.Fov = Mathf.Lerp(camera.Fov, targetFov, 8f * (float)GetProcessDeltaTime());
    }

    private void DebugDrawRay(RayCast3D ray, Color color)
    {
        if (!PlayerDebug)
            return;
        
        Vector3 start = ray.GlobalPosition;
        Vector3 end = start + ray.GlobalTransform.Basis * ray.TargetPosition;

        DebugDraw3D.DrawLine(start, end, color);

        if (ray.IsColliding())
        {
            Vector3 hit = ray.GetCollisionPoint();
            DebugDraw3D.DrawSphere(hit, 0.1f, Colors.Red);
        }
    }
}
