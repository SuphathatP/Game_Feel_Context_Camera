using Godot;

public partial class HintArea : Area3D
{
    [Export] public Node3D HintTarget;
    [Export] public CameraController Camera;
    [Export] public Node3D HintCameraPosition;

    public override void _Ready()
    {
        BodyEntered += OnEnter;
        BodyExited += OnExit;
    }

    private void OnEnter(Node3D body)
    {
        if (body.IsInGroup("player"))
        {
            Camera.EnterHint(HintTarget, HintCameraPosition);
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
