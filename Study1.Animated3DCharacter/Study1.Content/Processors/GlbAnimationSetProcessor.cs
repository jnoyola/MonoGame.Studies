using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Study1.ContentFramework.Models;

namespace Study1.Content.Processors;

[ContentProcessor(DisplayName = "GLB Animation Set - MonoGame.Studies")]
public class GlbAnimationSetProcessor : ContentProcessor<SharpGLTF.Schema2.ModelRoot, AnimationSet>
{
    private const float KeyframeValueEpsilon = 0.0001f;

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
            // It is important that this index is in ascending order for performant iteration through channels.
            var bones = animation.Channels.Select(channel => channel.TargetNode).ToHashSet().ToList();
            bones.Sort((a, b) => boneIndices[a.Name] - boneIndices[b.Name]);

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

            // Process channels.
            var translationChannels = new List<BoneChannel<Vector3>>();
            var rotationChannels = new List<BoneChannel<Quaternion>>();
            var scaleChannels = new List<BoneChannel<Vector3>>();
            foreach (var channel in animation.Channels)
            {
                var bone = channel.TargetNode;
                var boneIndex = bones.IndexOf(bone);
                switch (channel.TargetNodePath)
                {
                    case SharpGLTF.Schema2.PropertyPath.translation:
                        var freezeX = rootBones.Contains(bone) && (animationInfo.FreezeRootBone & FreezeRootBoneOption.X) != 0;
                        var freezeY = rootBones.Contains(bone) && (animationInfo.FreezeRootBone & FreezeRootBoneOption.Y) != 0;
                        var freezeZ = rootBones.Contains(bone) && (animationInfo.FreezeRootBone & FreezeRootBoneOption.Z) != 0;
                        var translationKeys = channel.GetTranslationSampler().GetLinearKeys().Select(
                            k => new Keyframe<Vector3>(
                                k.Key,
                                new Vector3(
                                    freezeX ? 0 : EpsilonCheck(k.Value.X, 0),
                                    freezeY ? 0 : EpsilonCheck(k.Value.Y, 0),
                                    freezeZ ? 0 : EpsilonCheck(k.Value.Z, 0)
                                )
                            )
                        );
                        var isTranslationFixed = translationKeys.All(
                            k => (bone.LocalTransform.Translation - k.Value).Length() < KeyframeValueEpsilon);
                        if (isTranslationFixed)
                        {
                            break;
                        }
                        // TODO: also collapse adjacent keyframes or do the sampling index.
                        translationChannels.Add(
                            new BoneChannel<Vector3>
                            {
                                BoneIndex = boneIndex,
                                Keyframes = translationKeys.ToArray(),
                            }
                        );
                        break;
                    case SharpGLTF.Schema2.PropertyPath.rotation:
                        var rotationKeys = channel.GetRotationSampler().GetLinearKeys().Select(
                            k => new Keyframe<Quaternion>(
                                k.Key,
                                new Quaternion(
                                    EpsilonCheck(k.Value.X, 0, 1),
                                    EpsilonCheck(k.Value.Y, 0, 1),
                                    EpsilonCheck(k.Value.Z, 0, 1),
                                    EpsilonCheck(k.Value.W, 0, 1)
                                )
                            )
                        );
                        var isRotationFixed = rotationKeys.All(
                            k => (bone.LocalTransform.Rotation - k.Value).Length() < KeyframeValueEpsilon);
                        if (isRotationFixed)
                        {
                            break;
                        }
                        rotationChannels.Add(
                            new BoneChannel<Quaternion>
                            {
                                BoneIndex = boneIndex,
                                Keyframes = rotationKeys.ToArray(),
                            }
                        );
                        break;
                    case SharpGLTF.Schema2.PropertyPath.scale:
                        var scaleKeys = channel.GetScaleSampler().GetLinearKeys().Select(
                            k => new Keyframe<Vector3>(
                                k.Key,
                                new Vector3(
                                    EpsilonCheck(k.Value.X, 1),
                                    EpsilonCheck(k.Value.Y, 1),
                                    EpsilonCheck(k.Value.Z, 1)
                                )
                            )
                        );
                        var isScaleFixed = scaleKeys.All(
                            k => (bone.LocalTransform.Scale - k.Value).Length() < KeyframeValueEpsilon);
                        if (isScaleFixed)
                        {
                            break;
                        }
                        scaleChannels.Add(
                            new BoneChannel<Vector3>
                            {
                                BoneIndex = boneIndex,
                                Keyframes = scaleKeys.ToArray(),
                            }
                        );
                        break;
                    default:
                        throw new Exception($"Unknown animation channel TargetNodePath {channel.TargetNodePath} for {bone.Name}");
                }
            }

            // Sort by bone index so channels can be performantly iterated in lockstep while iterating over bones.
            translationChannels.Sort((a, b) => a.BoneIndex - b.BoneIndex);
            rotationChannels.Sort((a, b) => a.BoneIndex - b.BoneIndex);
            scaleChannels.Sort((a, b) => a.BoneIndex - b.BoneIndex);

            animationDict[animation.Name] = new Animation
            {
                Name = animation.Name,
                DurationInSeconds = animation.Duration,
                WrapMode = animationInfo.WrapMode,
                BoneIndexMapping = boneIndexMapping,
                TranslationChannels = translationChannels.ToArray(),
                RotationChannels = rotationChannels.ToArray(),
                ScaleChannels = scaleChannels.ToArray(),
            };
        }

        return new AnimationSet(animationDict, animationLayers);
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

    private static float EpsilonCheck(float input, params float[] targets)
    {
        foreach (var target in targets)
        {
            if (Math.Abs(target - input) < KeyframeValueEpsilon)
            {
                return target;
            }
        }

        return input;
    }
}
