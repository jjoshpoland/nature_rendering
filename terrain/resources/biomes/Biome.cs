using Godot;
using System;

[GlobalClass]
public partial class Biome : Resource
{
    [Export]
    public Image SurfaceTexture;
    [Export]
    public float GrassChance;
    [Export]
    public MeshDetail[] MeshDetails;
}
