using Godot;

public partial class Water : Node
{
    public override void _Ready()
    {
        // Get nodes
        var cr = GetNode<ColorRect>("Simulation/ColorRect");
        var water = GetNode<Node3D>("Water");

        // Get textures
        var simTex = GetNode<SubViewport>("Simulation").GetTexture();
        var colTex = GetNode<SubViewport>("Collision").GetTexture();

        // Set shader params on the ColorRect material
        var crMat = cr.Material as ShaderMaterial;
        if (crMat != null)
        {
            crMat.SetShaderParameter("sim_tex", simTex);
            crMat.SetShaderParameter("col_tex", colTex);
        }

        // Set shader params on the water mesh material
        var meshInstance = water as MeshInstance3D;
        if (meshInstance != null)
        {
            var waterMat = meshInstance.Mesh.SurfaceGetMaterial(0) as ShaderMaterial;
            if (waterMat != null)
            {
                waterMat.SetShaderParameter("simulation", simTex);
                waterMat.SetShaderParameter("simulation2", simTex);
            }
        }
    }

    public override void _Process(double delta)
    {
        // pass
    }
}
