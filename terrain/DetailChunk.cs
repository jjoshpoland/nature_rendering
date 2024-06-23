using Godot;
using System;

public partial class DetailChunk : Node3D
{
    public Rect2 Bounds;
    public MultiMesh DetailMultiMesh;
    public MultiMeshInstance3D DetailMultiMeshInstance;
	public int InstanceCount;
    // Called when the node enters the scene tree for the first time.
    
    public DetailChunk()
    {
        DetailMultiMesh = new MultiMesh();
        DetailMultiMeshInstance = new MultiMeshInstance3D();
        Bounds = new Rect2();
    }

    public DetailChunk(Rect2 bounds)
    {
        DetailMultiMesh = new MultiMesh();
        DetailMultiMeshInstance = new MultiMeshInstance3D();
        Bounds = bounds;
    }
}
