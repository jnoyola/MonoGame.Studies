using Study1.ContentFramework.Models;

namespace Study1.Content.WritableContent;

/// <summary>
/// An intermediate data structure to store writable pipeline content for <see cref="ContentFramework.Models.Model"/>.
/// </summary>
public class WritableModel(IReadOnlyList<WritableMesh> meshes, IReadOnlyList<Bone> bones, AnimationSet? animations)
{
    public IReadOnlyList<WritableMesh> Meshes => meshes;
    public IReadOnlyList<Bone> Bones => bones;
    public AnimationSet? Animations => animations;
}
