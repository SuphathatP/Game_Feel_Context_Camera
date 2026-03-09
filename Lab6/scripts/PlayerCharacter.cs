using Godot;
using System;

public partial class PlayerCharacter : CharacterBody3D
{
	[ExportGroup("Movement")]
	[Export] private float WalkSpeed = 2.0f;
	[Export] private float MoveSpeed = 4.0f;
	[Export] private float SprintSpeed = 8.0f;
	[Export] private float JumpVelocity = 5.0f;
	[Export] private float FallAcceleration = 10.0f;

	[ExportGroup("Player")]
	[Export] Node3D PlayerPivot;
	[Export] MeshInstance3D PlayerMesh;
	[Export] private float PushStrength = 1.0f;
	[Export] private float PlayerLerpSpeed = 0.1f;

	[ExportGroup("Camera")]
	[Export] Node3D CameraPivot;
	[Export] Node3D CameraYaw;
	[Export] Node3D CameraPitch;
	[Export] Camera3D camera3D;
	[Export] private float CameraDeg = 90.0f;

	[ExportGroup("Mouse Setting")]
	[Export] private float MouseSenitivity = 0.004f;

	// Height
	[Export] private float IdledHeight = 1.0f;
	[Export] private float WalkHeight = 0.0f;
	[Export] private float MoveHeight = 2.0f;
	[Export] private float SprintHeight = 4.0f;
	[Export] private float HeightLerpSpeed = 1.0f;

	// Tilt
	[Export] private float IdledTiltDeg = 0.0f;
	[Export] private float WalkTiltDeg = 1.0f;
	[Export] private float MoveTiltDeg = -10.0f;
	[Export] private float SprintTiltDeg = -20.0f;
	[Export] private float TiltLerpSpeed = 1.0f;

	// Distance
	[Export] private float IdledTargetDistance = 2.0f;
	[Export] private float WalkTargetDistance = 1.0f;
	[Export] private float MoveTargetDistance = 4.0f;
	[Export] private float SprintTargetDistance = 8.0f;
	[Export] private float DistanceLerpSpeed = 1.0f;

	bool IsMove = false;
	bool IsWalk = false;
	bool IsSprint = false;
	bool IsJump = false;

	private Vector3 _targetVelocity = Vector3.Zero;

	public override void _PhysicsProcess(double delta)
	{
		HandlePlayerMovement(delta);
		MoveAndSlide();
		HandleCollision();
	}

	public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
		else if (@event.IsActionPressed("ui_cancel"))
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
	}

	private void HandlePlayerMovement(double delta)
	{
		IsMove = true;
		
		Vector3 velocity = Velocity;
		
		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		// Get the input direction and handle movement.
		Vector2 inputDirection = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		Vector3 direction = (CameraPivot.Basis * new Vector3(inputDirection.X, 0, inputDirection.Y)).Normalized();
		Vector3 lastDirection = direction;

	

		if (direction != Vector3.Zero)
		{
			lastDirection = direction;
		}

		Basis rotationBasis = new Basis();

		rotationBasis.Z = direction.Rotated(Vector3.Up, 0) * new Vector3(1,0,1);
		//rotationBasis.Z = new Vector3(1,0,1);
		rotationBasis.X = rotationBasis.Z.Cross(Vector3.Down);
		rotationBasis.Y = rotationBasis.Z.Cross(rotationBasis.X);

		rotationBasis = rotationBasis.Orthonormalized();
		PlayerPivot.Basis = PlayerPivot.Basis.Slerp(rotationBasis, 0.3f);

		//direction.Y = 0;
		if (direction != Vector3.Zero)
		{
			
			// Handle Sprint.
			if (Input.IsActionPressed("sprint"))
			{
				IsMove = false;
				IsSprint = true;
				
				velocity.X = direction.X * SprintSpeed;
				velocity.Z = direction.Z * SprintSpeed;
				//DynamicThirdPersonCamera(delta);
				
				IsMove = true;
				IsSprint = false;
			}
			// Handle Walk
			else if (Input.IsActionPressed("walk"))
			{
				IsMove = false;
				IsWalk = true;
				
				velocity.X = direction.X * WalkSpeed;
				velocity.Z = direction.Z * WalkSpeed;
				//DynamicThirdPersonCamera(delta);
				
				IsMove = true;
				IsWalk = false;
			}
			else
			{
				IsSprint = false;
				IsWalk = false;
				IsMove = true;

				velocity.X = direction.X * MoveSpeed;
				velocity.Z = direction.Z * MoveSpeed;
				//DynamicThirdPersonCamera(delta);
			}
			

		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, MoveSpeed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, MoveSpeed);

			//PlayerPivot.Rotation = PlayerPivot.Rotation * CameraYaw.Rotation;
			/*Basis rotationBasis = new Basis();
			rotationBasis.Z = lastDirection;
			rotationBasis.X = rotationBasis.Z.Cross(Vector3.Up);
			rotationBasis.Y = rotationBasis.Z.Cross(rotationBasis.X);

			rotationBasis = rotationBasis.Orthonormalized();
			CameraPivot.Basis = CameraPivot.Basis.Slerp(rotationBasis, 0.3f);*/

			IsMove = false;
			//DynamicThirdPersonCamera(delta);
		}

		Velocity = velocity;
	}

		//Vector3 inputDirection = new Vector3(Input.GetActionStrength("move_left") - Input.GetActionStrength("move_right"), 0, Input.GetActionStrength("move_forward") - Input.GetActionStrength("move_backward"));

		/*if (Input.IsActionPressed("move_right"))
		{	
			playerDirection.X += 1.0f;
			SmoothRotatePlayer(delta);
			DynamicThirdPersonCamera(delta);

		}
		if (Input.IsActionPressed("move_left"))
		{
			playerDirection.X -= 1.0f;
			DynamicThirdPersonCamera(delta);

		}
		if (Input.IsActionPressed("move_backward"))
		{
			playerDirection.Z += 1.0f;
			DynamicThirdPersonCamera(delta);

		}
		if (Input.IsActionPressed("move_forward"))
		{
			playerDirection.Z -= 1.0f;
			DynamicThirdPersonCamera(delta);
		}

		if (playerDirection != Vector3.Zero)
		{
			playerDirection = playerDirection.Normalized();
			PlayerPivot.Basis = Basis.LookingAt(playerDirection);
		}

		_targetVelocity.X = playerDirection.X * MoveSpeed;
		_targetVelocity.Z = playerDirection.Z * MoveSpeed;

		// Gravity
		if (!IsOnFloor())
		{
			_targetVelocity.Y -= FallAcceleration * (float)delta;
		}
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
		{
			playerDirection.Y += 1f;
			_targetVelocity.Y = playerDirection.Y * JumpVelocity;
		}
		if (Input.IsActionPressed("sprint"))
		{	
			IsMove = false;
			IsSprint = true;

			_targetVelocity.X = playerDirection.X * SprintSpeed;
			_targetVelocity.Z = playerDirection.Z * SprintSpeed;
			DynamicThirdPersonCamera(delta);

			IsMove = true;
			IsSprint = false;
		}
		if (Input.IsActionPressed("walk"))
		{
			IsMove = false;
			IsWalk = true;

			while (IsWalk)
			{
				_targetVelocity.X = playerDirection.X * WalkSpeed;
				_targetVelocity.Z = playerDirection.Z * WalkSpeed;
				DynamicThirdPersonCamera(delta);

				if (Input.IsActionPressed("walk"))
				{
					IsWalk = false;
					IsMove = true;
				}
			}
		}


		Velocity = _targetVelocity;

		IsMove = false;
		DynamicThirdPersonCamera(delta);
	} */

	private void HandleCollision()
	{
		int count = GetSlideCollisionCount();

		for (int i = 0; i < count; i++)
		{
    		var collision = GetSlideCollision(i);

    		if (collision.GetCollider() is RigidBody3D rb)
    		{
				float mass = rb.Mass;

        		Vector3 push = -collision.GetNormal() * PushStrength * (1.0f / mass);
        		rb.ApplyImpulse(push);
    		}
		}
	}

	// Dynamic camera
	private void DynamicThirdPersonCamera(double delta)
	{
		// FIXED BUT NEED MORE TEST : Dynamic camera stop transition when no movement key press 
		// TO DO : Dynamic camera orbit around player
		// TO DO : Dynamic camera when jump or failing down.

		float targetHeight;
		float targetTiltDeg;
		float targetDistance;

		if (IsWalk)
		{
			targetHeight = WalkHeight;
			targetTiltDeg = WalkTiltDeg;
			targetDistance = WalkTargetDistance;
			
		}
		else if (IsMove)
		{
			targetHeight = MoveHeight;
			targetTiltDeg = MoveTiltDeg;
			targetDistance = MoveTargetDistance;
		}
		else if (IsSprint)
		{
			targetHeight = SprintHeight;
			targetTiltDeg = SprintTiltDeg;
			targetDistance = SprintTargetDistance;
		}
		else
		{
			targetHeight = IdledHeight;
			targetTiltDeg = IdledTiltDeg;
			targetDistance = IdledTargetDistance;
		}
		
		// Height
		Vector3 pivotPosition = CameraPitch.Position;
		pivotPosition.Y = Mathf.Lerp(pivotPosition.Y, targetHeight, (float)delta * HeightLerpSpeed);
		CameraPitch.Position = pivotPosition;

		// Tilt
		Vector3 rotation = CameraPitch.RotationDegrees;
		rotation.X = Mathf.Lerp(rotation.X, targetTiltDeg, (float)delta * TiltLerpSpeed);
		CameraPitch.RotationDegrees = rotation;

		// Distance
		Vector3 cameraPosition = CameraYaw.Position;
		cameraPosition.Z = Mathf.Lerp(cameraPosition.Z, targetDistance, (float)delta * DistanceLerpSpeed);
		CameraYaw.Position = cameraPosition;
	}
}
