using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Study1.ContentFramework.Models;

namespace Study1.Content.Processors;

[ContentProcessor(DisplayName = "GLB Animation Set - MonoGame.Studies")]
public class GlbAnimationSetProcessor : ContentProcessor<SharpGLTF.Schema2.ModelRoot, AnimationSet>
{

    public IList<AnimationInfo> AnimationInfo { get; set; } = [];
    public IList<AnimationLayer> AnimationLayers { get; set; } = [];

    public override AnimationSet Process(SharpGLTF.Schema2.ModelRoot input, ContentProcessorContext context)
    {
        var animationLayers = new AnimationLayers(AnimationLayers);
        var animationInfoDict = AnimationInfo.ToDictionary(a => a.Name, a => a);

        var rootBones = FindAllRootBones(input);

        var animationDict = new Dictionary<string, Animation>();
        foreach (var animation in input.LogicalAnimations)
        {
            if (!animationInfoDict.TryGetValue(animation.Name, out var animationInfo))
            {
                context.Logger.Log(LogLevel.Warning, $"No AnimationInfo found for animation '{animation.Name}'. Skipping.");
                continue;
            }

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

            // var expectedTranslationKeyCount = boneChannels[0].Translations.Keyframes.Length;
            // var expectedRotationKeyCount = boneChannels[0].Rotations.Keyframes.Length;
            // var expectedScaleKeyCount = boneChannels[0].Scales.Keyframes.Length;
            // if (
            //     boneChannels.Any(
            //         boneChannel =>
            //             boneChannel.Translations.Keyframes.Length != expectedTranslationKeyCount ||
            //             boneChannel.Rotations.Keyframes.Length != expectedRotationKeyCount ||
            //             boneChannel.Scales.Keyframes.Length != expectedScaleKeyCount
            //     )
            // )
            // {
            //     throw new Exception($"Animation '{animation.Name}' has mismatched keyframe counts for some bones. All bones are expected to have the same number of keyframes (translations: {expectedTranslationKeyCount}, rotations: {expectedRotationKeyCount}, scales: {expectedScaleKeyCount}).");
            // }

            // foreach (var boneChannel in boneChannels)
            // {
            //     for (int keyIndex = 0; keyIndex < boneChannel.Translations.Keyframes.Length; ++keyIndex)
            //     {
            //         if (boneChannel.Translations.Keyframes[keyIndex].Time != boneChannels[0].Translations.Keyframes[keyIndex].Time)
            //         {
            //             throw new Exception($"Animation '{animation.Name}' has mismatched translation keyframe times for bone '{boneChannel.BoneName}' at key index {keyIndex}. All bones are expected to have keyframes at the same times.");
            //         }
            //     }
            //     for (int keyIndex = 0; keyIndex < boneChannel.Rotations.Keyframes.Length; ++keyIndex)
            //     {
            //         if (boneChannel.Rotations.Keyframes[keyIndex].Time != boneChannels[0].Rotations.Keyframes[keyIndex].Time)
            //         {
            //             throw new Exception($"Animation '{animation.Name}' has mismatched rotation keyframe times for bone '{boneChannel.BoneName}' at key index {keyIndex}. All bones are expected to have keyframes at the same times.");
            //         }
            //     }
            //     for (int keyIndex = 0; keyIndex < boneChannel.Scales.Keyframes.Length; ++keyIndex)
            //     {
            //         if (boneChannel.Scales.Keyframes[keyIndex].Time != boneChannels[0].Scales.Keyframes[keyIndex].Time)
            //         {
            //             throw new Exception($"Animation '{animation.Name}' has mismatched scale keyframe times for bone '{boneChannel.BoneName}' at key index {keyIndex}. All bones are expected to have keyframes at the same times.");
            //         }
            //     }
            // }

            if (!animationLayers.HasLayer(animationInfo.DefaultLayer))
            {
                throw new Exception($"Animation '{animation.Name}' specifies default layer '{animationInfo.DefaultLayer}' which is not present in the provided AnimationLayers ({string.Join(", ", AnimationLayers.Select(l => l.Identifier))}.");
            }

            animationDict[animation.Name] = new Animation(
                animation.Name,
                animation.Duration,
                boneChannels,
                animationInfo.DefaultLayer
            );
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
