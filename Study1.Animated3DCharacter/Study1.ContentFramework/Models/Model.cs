namespace Study1.ContentFramework.Models;

public class Model(IReadOnlyList<Mesh> meshes, IReadOnlyList<Bone> bones, AnimationSet? animations)
{
    public IReadOnlyList<Mesh> Meshes => meshes;
    public IReadOnlyList<Bone> Bones => bones;
    public AnimationSet? Animations => animations;
}
