using System.Runtime.CompilerServices;

namespace Study1.ContentFramework.Models;

/// <summary>
/// Semantic identifiers for animation layers. An animation set can define up to <see cref="AnimationLayers.MaxLayerCount"/> layers,
/// each identified by a unique <see cref="AnimationLayerIdentifier"/>.
/// </summary>
public enum AnimationLayerIdentifier
{
    Base,
    UpperBody,
    Gesture,
    Head,
    Override,
}

public struct AnimationLayer(AnimationLayerIdentifier identifier, HashSet<string> boneMask)
{
    public readonly AnimationLayerIdentifier Identifier => identifier;
    public readonly HashSet<string> BoneMask => boneMask;
}

/// <summary>
/// A list of animation layers, where a given <see cref="AnimationLayerIdentifier"/> can be mapped to its corresponding index or
/// its full <see cref="AnimationLayer"/> definition.
/// </summary>
public class AnimationLayers
{
    [InlineArray(MaxLayerCount)]
    public struct AnimationLayerArray
    {
        private AnimationLayer _element0;
    }

    public const int MaxLayerCount = 3;

    private readonly int[] _identifierIndex;
    private readonly AnimationLayerArray _layers;

    public AnimationLayers(IList<AnimationLayer> layers)
    {
        if (layers.Count > MaxLayerCount)
        {
            throw new ArgumentException($"Cannot have more than {MaxLayerCount} animation layers.");
        }
        for (int i = 0; i < layers.Count; ++i)
        {
            for (int j = 0; j < i; ++j)
            {
                if (layers[i].Identifier == layers[j].Identifier)
                {
                    throw new ArgumentException($"Cannot have duplicate animation layer identifier: {layers[i].Identifier}");
                }
            }
        }

        _identifierIndex = new int[typeof(AnimationLayerIdentifier).GetEnumValues().Length];
        for (int i = 0; i < _identifierIndex.Length; ++i)
        {
            _identifierIndex[i] = -1;
        }

        for (int i = 0; i < layers.Count; ++i)
        {
            _layers[i] = layers[i];
            _identifierIndex[(int)layers[i].Identifier] = i;
        }
    }

    public bool HasLayer(AnimationLayerIdentifier identifier) => _identifierIndex[(int)identifier] != -1;
    public int GetLayerIndex(AnimationLayerIdentifier identifier) => _identifierIndex[(int)identifier];
    public AnimationLayer GetLayer(AnimationLayerIdentifier identifier) => _layers[GetLayerIndex(identifier)];
}
