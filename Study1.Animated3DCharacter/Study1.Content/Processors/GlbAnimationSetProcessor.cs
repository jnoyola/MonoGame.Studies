using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Study1.ContentFramework.Models;

namespace Study1.Content.Processors;

[ContentProcessor(DisplayName = "GLB Animation Set - MonoGame.Studies")]
public class GlbAnimationSetProcessor : ContentProcessor<SharpGLTF.Schema2.ModelRoot, AnimationSet>
{
    public required IList<AnimationInfo> Animations { get; set; }
    public required IList<AnimationLayerBuilder> AnimationLayerDefinitions { get; set; }

    public override AnimationSet Process(SharpGLTF.Schema2.ModelRoot input, ContentProcessorContext context)
    {
        var boneIndices = BuildBoneIndices(input);
        var animationInfoDict = Animations.ToDictionary(a => a.Name, a => a);

        var animationLayers = new AnimationLayerDefinitions();
        for (int i = 0; i < AnimationLayerDefinitions.Count; ++i)
        {
            AnimationLayerDefinitions[i].Build(input, boneIndices, animationLayers);
        }

        var rootBones = FindAllRootBones(input);

        var animationDict = new Dictionary<string, Animation>();
        foreach (var animation in input.LogicalAnimations)
        {
            if (!animationInfoDict.TryGetValue(animation.Name, out var animationInfo))
            {
                context.Logger.Log(LogLevel.Warning, $"No AnimationInfo found for animation '{animation.Name}'. Skipping.");
                continue;
            }

            // Construct a consistent ordering of the bones controlled by this animation.
            var bones = new List<SharpGLTF.Schema2.Node>();
            var boneSet = new HashSet<SharpGLTF.Schema2.Node>();
            foreach (var channel in animation.Channels)
            {
                var bone = channel.TargetNode;
                if (!boneSet.Contains(bone))
                {
                    bones.Add(bone);
                    boneSet.Add(bone);
                }
            }

            // Construct an index from all bones in the animation set to all bones in this animation.
            var boneIndexMapping = new int[boneIndices.Count];
            foreach (var (boneName, boneIndex) in boneIndices)
            {
                var bone = bones.FirstOrDefault(b => b.Name == boneName);
                if (bone != null)
                {
                    boneIndexMapping[boneIndex] = bones.IndexOf(bone);
                }
                else
                {
                    boneIndexMapping[boneIndex] = -1;
                }
            }

            // Map channels by bone name.
            var translationChannels = new Dictionary<string, SharpGLTF.Schema2.AnimationChannel>();
            var rotationChannels = new Dictionary<string, SharpGLTF.Schema2.AnimationChannel>();
            var scaleChannels = new Dictionary<string, SharpGLTF.Schema2.AnimationChannel>();
            foreach (var channel in animation.Channels)
            {
                var bone = channel.TargetNode;
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

            // Get keyframes for all animated bones in order.
            // TODO (optimize): filter out any channels where values are near zero.
            //      Many translation and scale channels may only exist due to floating point rounding errors.
            var boneChannels = new List<BoneChannel>();
            foreach (var bone in bones)
            {
                var translationKeys = translationChannels[bone.Name].GetTranslationSampler().GetLinearKeys();
                var rotationKeys = rotationChannels[bone.Name].GetRotationSampler().GetLinearKeys();
                var scaleKeys = scaleChannels[bone.Name].GetScaleSampler().GetLinearKeys();

                var freezeX = rootBones.Contains(bone) && (animationInfo.FreezeRootBone & FreezeRootBoneOption.X) != 0;
                var freezeY = rootBones.Contains(bone) && (animationInfo.FreezeRootBone & FreezeRootBoneOption.Y) != 0;
                var freezeZ = rootBones.Contains(bone) && (animationInfo.FreezeRootBone & FreezeRootBoneOption.Z) != 0;

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

            if (!animationLayers.HasLayer(animationInfo.DefaultLayer))
            {
                throw new Exception($"Animation '{animation.Name}' specifies default layer '{animationInfo.DefaultLayer}' which is not present in the provided AnimationLayerDefinitions ({string.Join(", ", AnimationLayerDefinitions.Select(l => l.Identifier))}.");
            }

            animationDict[animation.Name] = new Animation
            {
                Name = animation.Name,
                DurationInSeconds = animation.Duration,
                WrapMode = animationInfo.WrapMode,
                BoneIndexMapping = boneIndexMapping,
                BoneChannels = boneChannels,
                DefaultLayer = animationInfo.DefaultLayer,
            };
        }

        return new AnimationSet(boneIndices, animationDict, animationLayers);
    }

    private static Dictionary<string, int> BuildBoneIndices(SharpGLTF.Schema2.ModelRoot input)
    {
        var boneIndices = new Dictionary<string, int>();
        foreach (var skin in input.LogicalSkins)
        {
            foreach (var node in skin.Joints)
            {
                if (boneIndices.ContainsKey(node.Name))
                {
                    throw new Exception($"Duplicate bone name detected: {node.Name}");
                }

                boneIndices[node.Name] = boneIndices.Count;
            }
        }

        return boneIndices;
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
