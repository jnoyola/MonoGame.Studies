namespace Study1.ContentFramework.Models;

public class Model(Mesh[] meshes, Bone[] bones, AnimationSet? animations)
{
    public Mesh[] Meshes => meshes;
    public Bone[] Bones => bones;
    public AnimationSet? Animations => animations;
}
