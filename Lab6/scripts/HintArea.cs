using Godot;

public partial class HintArea : Area3D
{
    [Export] public Node3D HintTarget;
    [Export] public float HintFOV = 50f;
    [Export] public CameraController Camera;
    

    public override void _Ready()
    {
        BodyEntered += OnEnter;
        BodyExited += OnExit;
    }

    private void OnEnter(Node3D body)
    {
        if (body.IsInGroup("player"))
        {
            Camera.EnterHint(HintTarget, HintFOV);
        }
    }

    private void OnExit(Node3D body)
    {
        if (body.IsInGroup("player"))
        {
            Camera.ExitHint();
        }
    }
}
