using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[ExportGroup("Player")]
	[Export] Node3D PlayerPivot;
	[Export] CollisionShape3D playerHeadCollision;
	[Export] SpotLight3D FlashLight;
	[Export] MeshInstance3D PlayerEyes;
	[Export] private float CrouchHeight = 0.5f;
	[Export] private float StandHeight = 1.0f;

	[ExportGroup("Movement")]
	[Export] private float MoveSpeed = 5.0f;
	[Export] private float SprintSpeed = 8.0f;
	[Export] private float JumpVelocity = 6.0f;
	[Export] private float PushStrength = 1.0f;
	[Export] private float WalkSpeed = 2.0f;
	[Export] private float CrouchSpeed = 1.0f;
	[Export] private float CrouchLerpSpeed = 22.0f;
	[Export] private float RotationSpeed = 8.0f;
	[Export] private float Gravity = 9.81f;
	[Export] private float JumpDelay = 0.1f;
	[Export] private float CoyoteTime = 0.15f;
	[Export] private float HoverTime = 0.08f;
	private float HoverTimer = 0f;
	private float CoyoteTimer = 0;
	private float JumpDelayTimer = 0f;

	[ExportGroup("Mouse Setting")]
	[Export] private float MouseSenitivity = 0.004f;

	[ExportGroup("Camera")]
	[Export] Camera3D camera;

	[ExportGroup("Check")]
	[Export] bool IsMove = false;
	[Export] bool IsWalk = false;
	[Export] bool IsSprint = false;
	[Export] bool IsJump = false;
	[Export] bool IsCrouch = false;
	[Export] bool HasJump = false;
	[Export] bool IsHover = false;
	private bool IsFlashLightOn = false;

	private bool PlayerDebug = false;

	private enum AccelerationMode
	{
		Immediate,
		Linear,
		Ease
	}

	[ExportGroup("Accelaration/Deccelaration")]
	[Export] private float LinearAccelaration = 6f;
	[Export] private float LinearDecelaration = 12f;
	[Export] private float EaseAccelaration = 6f;

	private AccelerationMode currentAccelarationMode = AccelerationMode.Immediate;

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

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("player_debug"))
		{
			PlayerDebug = !PlayerDebug;
		}

		if (Input.IsActionJustPressed("immediate_mode"))
		{
			currentAccelarationMode = AccelerationMode.Immediate;
		}

		if (Input.IsActionJustPressed("linear_mode"))
		{
			currentAccelarationMode = AccelerationMode.Linear;
		}

		if (Input.IsActionJustPressed("ease_mode"))
		{
			currentAccelarationMode = AccelerationMode.Ease;
		}
    }

	public override void _PhysicsProcess(double delta)
	{
		HandlePlayerMovement(delta);
		MoveAndSlide();
		HandleCollision();
		//GD.Print("OnFloor: ", IsOnFloor());
	}

	// Player Movement
	private void HandlePlayerMovement(double delta)
	{
		
		Vector3 velocity = Velocity;
		
		// Get the input direction.
		Vector2 inputDirection = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");

		// Joystick
		Vector2 leftJoyStick = new Vector2(Input.GetJoyAxis(0, JoyAxis.LeftX), Input.GetJoyAxis(0, JoyAxis.LeftY));

		float deadzone = 0.2f;
		if (leftJoyStick.Length() < deadzone)
		{
			leftJoyStick = Vector2.Zero;
		}

		// Combine both input.
		Vector2 moveInput = inputDirection + leftJoyStick;

		// Limit to avoid speed boost when moving use input from both keyboard and joystick.
		moveInput = moveInput.LimitLength(1.0f); 

		// Word direction.
		Vector3 direction = (camera.Basis * new Vector3(moveInput.X, 0, moveInput.Y)).Normalized();

		// Start Hovertimier and apply the gravity.
		if (!IsOnFloor())
		{
			if (velocity.Y < 0 && HoverTimer > 0f)
			{
				HoverTimer -= (float)delta;
				velocity.Y = 0;
			}
			else
			{
				velocity.Y -= Gravity * (float)delta;
			}
		}
		else
		{
			HoverTimer = HoverTime;
		}

		if (!IsJump)
		{
			if (IsOnFloor())
			{
				CoyoteTimer = CoyoteTime;
				HasJump = false;
			}
			else
			{
				CoyoteTimer -= (float)delta;
			}
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("jump") && CoyoteTimer > 0f && !HasJump)
		{
			IsJump = true;
			JumpDelayTimer = JumpDelay;
			
			Vector3 playerScale = PlayerPivot.Scale;
			playerScale.Y = CrouchHeight; // 0.5
			PlayerPivot.Scale = playerScale;	
		}

		// Handle Crouch.
		IsCrouch = Input.IsActionPressed("crouch");
		HandleCrouch(delta);

		HandleFlashLight();
		
		// Direction.Y = 0;
		if (direction != Vector3.Zero)
		{
			// Handle Sprint.
			if (IsCrouch)
			{
				float targetX = direction.X * WalkSpeed;
				float targetZ = direction.Z * WalkSpeed;

				velocity.X = ApplyAccelration(velocity.X, targetX, (float)delta);
				velocity.Z = ApplyAccelration(velocity.Z, targetZ, (float)delta);
				
				//DynamicThirdPersonCamera(delta);

				IsMove = false;
				IsSprint = false;
				IsWalk = false;
			}
			else if (Input.IsActionPressed("sprint"))
			{	
				float targetX = direction.X * SprintSpeed;
				float targetZ = direction.Z * SprintSpeed;

				velocity.X = ApplyAccelration(velocity.X, targetX, (float)delta);
				velocity.Z = ApplyAccelration(velocity.Z, targetZ, (float)delta);
				
				//DynamicThirdPersonCamera(delta);

				IsSprint = true;
				IsMove = false;
				IsWalk = false;
			}
			// Handle Walk.
			else if (Input.IsActionPressed("walk"))
			{		
				float targetX = direction.X * WalkSpeed;
				float targetZ = direction.Z * WalkSpeed;

				velocity.X = ApplyAccelration(velocity.X, targetX, (float)delta);
				velocity.Z = ApplyAccelration(velocity.Z, targetZ, (float)delta);

				//DynamicThirdPersonCamera(delta);
				
				IsWalk = true;
				IsMove = false;
				IsSprint = false;
			}
			else
			{
				float targetX = direction.X * MoveSpeed;
				float targetZ = direction.Z * MoveSpeed;

				velocity.X = ApplyAccelration(velocity.X, targetX, (float)delta);
				velocity.Z = ApplyAccelration(velocity.Z, targetZ, (float)delta);

				IsMove = true;
				IsSprint = false;
				IsWalk = false;
			}
		}
		else
		{
			velocity.X = ApplyAccelration(velocity.X, 0, (float)delta);
			velocity.Z = ApplyAccelration(velocity.Z, 0, (float)delta);
			IsMove = false;
		}

		if (direction != Vector3.Zero)
		{
			// Get pointing direction (all quadrants).
			float targetAngle = Mathf.Atan2(direction.X, direction.Z); // Return angle in radians. : Can convert into degree using Mathf.Rad2Deg

			Vector3 currentRotation = PlayerPivot.Rotation;
			currentRotation.Y = Mathf.LerpAngle(currentRotation.Y, targetAngle, (float)delta * 10f);

			PlayerPivot.Rotation = currentRotation;
		}

		if (IsJump)
		{
			JumpDelayTimer -= (float)delta;

			if (JumpDelayTimer <= 0f)
			{
				velocity.Y = JumpVelocity;
				HasJump = true;

				if (!IsCrouch)
				{
					// Reset player scale.
					Vector3 targetScale = PlayerPivot.Scale;
					targetScale.Y = StandHeight;

					Vector3 currentScale = PlayerPivot.Scale;
					currentScale.Y = Mathf.Lerp(currentScale.Y, targetScale.Y, (float)delta * CrouchLerpSpeed);
				}

				IsJump = false;
			}
		}

		Velocity = velocity;

		DrawPlayerDebug();
	}

	// Handle collision and apply push.
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
	
	// Apply different accelration mode.
	private float ApplyAccelration(float current, float target, float delta)
	{
		switch (currentAccelarationMode)
		{
			case AccelerationMode.Immediate:
				return target;
			
			case AccelerationMode.Linear:
				float AccelarationRate;
				if (Mathf.Abs(target) > Mathf.Abs(current))
				{
					AccelarationRate = LinearAccelaration;
				}
				else
				{
					AccelarationRate = LinearDecelaration;
				}
				//Gradually move a player toward a target at a linear accelaration speed.
				return Mathf.MoveToward(current, target, AccelarationRate * (float)delta);
			case AccelerationMode.Ease:
				if (Mathf.Abs(target) > Mathf.Abs(current))
				{
					// Ease in.
					return Mathf.Lerp(current, target, EaseAccelaration * (float)delta);
				}
				else
				{
					// Constant deceleration
					return Mathf.MoveToward(current, target, LinearDecelaration * (float)delta);
				}
			default:
				return target;
		}
	}

	private void HandleCrouch(double delta)
	{
		if (IsJump)
		{
			return;
		}

		Vector3 targetScale = PlayerPivot.Scale;

		Vector3 playerCrouch = PlayerPivot.Scale;

		if (IsCrouch)
		{
			playerHeadCollision.Disabled = true;
			targetScale.Y = CrouchHeight;
			playerCrouch.Y = Mathf.Lerp(playerCrouch.Y, targetScale.Y, (float)delta * CrouchLerpSpeed);
		}
		else
		{
			playerHeadCollision.Disabled = false;
			targetScale.Y = StandHeight;
			playerCrouch.Y = Mathf.Lerp(playerCrouch.Y, targetScale.Y, (float)delta * CrouchLerpSpeed);
		}

		PlayerPivot.Scale = playerCrouch;	
	}

	private void HandleFlashLight()
	{
		var mat = PlayerEyes.GetActiveMaterial(0) as StandardMaterial3D;
		
		if (Input.IsActionJustPressed("flash_light"))
		{
			IsFlashLightOn = !IsFlashLightOn;
			FlashLight.Visible = IsFlashLightOn;
			mat.EmissionEnabled = IsFlashLightOn;
		}
	}

	private string GetAccelerationModeName() 
	{ 
		return currentAccelarationMode switch 
		{ 
			AccelerationMode.Immediate => "Immediate", 
			AccelerationMode.Linear => "Linear", 
			AccelerationMode.Ease => "Ease",
			 _ => "Unknown" 
		}; 
	}

	private void DrawPlayerDebug()
	{
		if (PlayerDebug)
		{
			Vector2 inputDirection = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
			Vector3 direction = (camera.Basis * new Vector3(inputDirection.X, 0, inputDirection.Y)).Normalized();
		
			Vector3 startArrow = PlayerPivot.GlobalPosition;
			Vector3 endArrow = startArrow + PlayerPivot.Basis.Z * 1.2f;
		
			DebugDraw3D.DrawArrow(startArrow, endArrow, Colors.DarkGreen, 0.05f);
		
			if (direction != Vector3.Zero) 
			{ 
				DebugDraw3D.DrawArrow(PlayerPivot.GlobalPosition, PlayerPivot.GlobalPosition + direction * 1.8f, Colors.Green, 0.05f); 
			}

			// Draw acceleration mode text above the player 
			string modeText = $"Acceleration Mode: {GetAccelerationModeName()}"; 
			Vector3 debugTextPos = PlayerPivot.GlobalPosition + new Vector3(0, 2.5f, 0); 
			DebugDraw3D.DrawText(debugTextPos, modeText);
		}	
	}
}