using Godot;
using System;

public partial class PoleBanner : Node3D
{
    private AnimationPlayer anim;

    public override void _Ready() 
    { 
        anim = GetNode<AnimationPlayer>("AnimationPlayer"); 
    }

    public override void _PhysicsProcess(double delta)
    {
        anim.Play("banner_sway_anim");
    }
}

