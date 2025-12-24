using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Study1.ContentFramework.Models;

namespace Study1.Content.Processors;

[ContentProcessor(DisplayName = "GLB Animation Set - MonoGame.Studies")]
public class GlbAnimationSetProcessor : ContentProcessor<SharpGLTF.Schema2.ModelRoot, AnimationSet>
{
    [Flags]
    public enum FreezeRootBoneOption
    {
        None = 0,
        X = 1 << 0,
        Y = 1 << 1,
        Z = 1 << 2,
        XY = X | Y,
        XZ = X | Z,
        YZ = Y | Z,
        XYZ = X | Y | Z,
    }

    public FreezeRootBoneOption FreezeRootBone { get; set; } = FreezeRootBoneOption.None;

    public override AnimationSet Process(SharpGLTF.Schema2.ModelRoot input, ContentProcessorContext context)
    {
        var rootBones = FindAllRootBones(input);

        var animationDict = new Dictionary<string, Animation>();
        foreach (var animation in input.LogicalAnimations)
        {
            var bones = new List<SharpGLTF.Schema2.Node>();
            var translationChannels = new Dictionary<string, SharpGLTF.Schema2.AnimationChannel>();
            var rotationChannels = new Dictionary<string, SharpGLTF.Schema2.AnimationChannel>();
            var scaleChannels = new Dictionary<string, SharpGLTF.Schema2.AnimationChannel>();
            foreach (var channel in animation.Channels)
            {
                var bone = channel.TargetNode;
                if (!bones.Contains(bone))
                {
                    bones.Add(bone);
                }

                switch (channel.TargetNodePath)
                {
                    case SharpGLTF.Schema2.PropertyPath.translation:
                        translationChannels[bone.Name] = channel;
                        break;
                    case SharpGLTF.Schema2.PropertyPath.rotation:
                        rotationChannels[bone.Name] = channel;
                        break;
                    case SharpGLTF.Schema2.PropertyPath.scale:
                        scaleChannels[bone.Name] = channel;
                        break;
                    default:
                        throw new Exception($"Unknown animation channel TargetNodePath {channel.TargetNodePath} for {bone.Name}");
                }
            }

            var boneChannels = new List<BoneChannel>();
            foreach (var bone in bones)
            {
                var translationKeys = translationChannels[bone.Name].GetTranslationSampler().GetLinearKeys();
                var rotationKeys = rotationChannels[bone.Name].GetRotationSampler().GetLinearKeys();
                var scaleKeys = scaleChannels[bone.Name].GetScaleSampler().GetLinearKeys();

                var freezeX = rootBones.Contains(bone) && (FreezeRootBone & FreezeRootBoneOption.X) != 0;
                var freezeY = rootBones.Contains(bone) && (FreezeRootBone & FreezeRootBoneOption.Y) != 0;
                var freezeZ = rootBones.Contains(bone) && (FreezeRootBone & FreezeRootBoneOption.Z) != 0;

                boneChannels.Add(
                    new BoneChannel(
                        bone.Name,
                        translationKeys.Select(
                            k => new Keyframe<Vector3>(
                                k.Key,
                                new Vector3(
                                    freezeX ? 0 : k.Value.X,
                                    freezeY ? 0 : k.Value.Y,
                                    freezeZ ? 0 : k.Value.Z
                                )
                            )
                        ).ToArray(),
                        rotationKeys.Select(k => new Keyframe<Quaternion>(k.Key, k.Value)).ToArray(),
                        scaleKeys.Select(k => new Keyframe<Vector3>(k.Key, k.Value)).ToArray()
                    )
                );
            }

            animationDict[animation.Name] = new Animation(animation.Name, animation.Duration, boneChannels);
        }

        return new AnimationSet(animationDict);
    }

    private static HashSet<SharpGLTF.Schema2.Node> FindAllRootBones(SharpGLTF.Schema2.ModelRoot input)
    {
        var rootBones = new HashSet<SharpGLTF.Schema2.Node>();
        foreach (var skin in input.LogicalSkins)
        {
            var joints = skin.Joints.ToHashSet();
            foreach (var node in skin.Joints)
            {
                if (node.VisualParent == null || !joints.Contains(node.VisualParent))
                {
                    rootBones.Add(node);
                    break;
                }
            }
        }

        return rootBones;
    }
}
