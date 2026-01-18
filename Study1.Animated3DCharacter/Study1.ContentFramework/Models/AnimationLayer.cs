using System.Collections;
using System.Runtime.CompilerServices;

namespace Study1.ContentFramework.Models;

/// <summary>
/// Semantic identifiers for animation layers. An animation set can define up to <see cref="AnimationLayerDefinitions.MaxLayerCount"/> layers,
/// each identified by a unique <see cref="AnimationLayer"/>.
/// </summary>
public enum AnimationLayer
{
    /// <summary>
    /// The base layer, typically used for full-body animations.
    /// </summary>
    Base,

    /// <summary>
    /// An upper-body layer, typically used for animations that override the upper body (e.g. waving, shooting).
    /// </summary>
    UpperBody,

    /// <summary>
    /// A head layer, typically used for animations that override the head (e.g. looking around).
    /// </summary>
    Head,

    /// <summary>
    /// An additive layer, typically used for additive animations that modify other animations (e.g. blinking, closing hands).
    /// Since additive layers can play multiple animations which are typically finer and designed to be combined,
    /// one additive layer is often sufficient.
    /// </summary>
    AdditiveBase,

    /// <summary>
    /// Not a layer. Indicates the number of identifiers.
    /// </summary>
    Count,
}

public struct OverrideLayerDefinition
{
    public required AnimationLayer Identifier { get; init; }
    public required BitArray? BoneMask { get; init; }
    public int LayerIndex { get; set; }
}

public struct AdditiveLayerDefinition
{
    public required AnimationLayer Identifier { get; init; }
    public required BitArray? BoneMask { get; init; }
    public int LayerIndex { get; set; }
}

/// <summary>
/// A list of all override and additive animation layer definitions, where a given <see cref="AnimationLayer"/> can be
/// mapped to its corresponding index or its full definition.
/// </summary>
public class AnimationLayerDefinitions
{
    public const int MaxOverrideLayerCount = 2;
    public const int MaxAdditiveLayerCount = 1;

    [InlineArray(MaxOverrideLayerCount)]
    private struct OverrideLayerDefinitionArray
    {
        private OverrideLayerDefinition _element0;
    }

    [InlineArray(MaxAdditiveLayerCount)]
    private struct AdditiveLayerDefinitionArray
    {
        private AdditiveLayerDefinition _element0;
    }

    [InlineArray((int)AnimationLayer.Count)]
    private struct AnimationLayerIndexArray
    {
        private int _element0;
    }

    private int _overrideLayerCount;
    private int _additiveLayerCount;
    private OverrideLayerDefinitionArray _overrideLayers;
    private AdditiveLayerDefinitionArray _additiveLayers;
    private AnimationLayerIndexArray _identifierIndex;
    private AnimationLayerBitmask _isAdditiveLayer;

    public AnimationLayerDefinitions()
    {
        _overrideLayerCount = 0;
        _additiveLayerCount = 0;
        for (int i = 0; i < (int)AnimationLayer.Count; ++i)
        {
            _identifierIndex[i] = -1;
        }
    }

    public void AddLayer(in OverrideLayerDefinition layer)
    {
        if (_overrideLayerCount >= MaxOverrideLayerCount)
        {
            throw new InvalidOperationException($"Cannot add more than {MaxOverrideLayerCount} override animation layers.");
        }
        if (HasLayer(layer.Identifier))
        {
            throw new ArgumentException($"An animation layer with identifier '{layer.Identifier}' already exists.");
        }

        int index = _overrideLayerCount;
        _overrideLayers[index] = layer;
        _overrideLayers[index].LayerIndex = index;
        _identifierIndex[(int)layer.Identifier] = index;
        ++_overrideLayerCount;
    }

    public void AddLayer(in AdditiveLayerDefinition layer)
    {
        if (_additiveLayerCount >= MaxAdditiveLayerCount)
        {
            throw new InvalidOperationException($"Cannot add more than {MaxAdditiveLayerCount} additive animation layers.");
        }
        if (HasLayer(layer.Identifier))
        {
            throw new ArgumentException($"An animation layer with identifier '{layer.Identifier}' already exists.");
        }

        int index = _additiveLayerCount;
        _additiveLayers[index] = layer;
        _additiveLayers[index].LayerIndex = index;
        _identifierIndex[(int)layer.Identifier] = index;
        ++_additiveLayerCount;

        _isAdditiveLayer.Set(layer.Identifier);
    }

    public int OverrideLayerCount => _overrideLayerCount;
    public int AdditiveLayerCount => _additiveLayerCount;

    public bool HasLayer(AnimationLayer identifier) => GetLayerIndex(identifier) != -1;
    public int GetLayerIndex(AnimationLayer identifier) => _identifierIndex[(int)identifier];
    public ref OverrideLayerDefinition GetOverrideLayer(int layerIndex) => ref _overrideLayers[layerIndex];
    public ref AdditiveLayerDefinition GetAdditiveLayer(int layerIndex) => ref _additiveLayers[layerIndex];

    public IEnumerable<OverrideLayerDefinition> GetAllOverrideLayers()
    {
        for (int i = 0; i < _overrideLayerCount; ++i)
        {
            yield return _overrideLayers[i];
        }
    }

    public IEnumerable<AdditiveLayerDefinition> GetAllAdditiveLayers()
    {
        for (int i = 0; i < _additiveLayerCount; ++i)
        {
            yield return _additiveLayers[i];
        }
    }

    public bool IsAdditiveLayer(AnimationLayer identifier)
    {
        return _isAdditiveLayer.Get(identifier);
    }
}

public struct AnimationLayerBitmask
{

    [InlineArray(((int)AnimationLayer.Count + 31) / 32)]
    private struct AnimationLayerBitArray
    {
        private int _element0;
    }
    
    private AnimationLayerBitArray _intArray;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Get(AnimationLayer identifier)
    {
        return (_intArray[(int)identifier >> 5] & (1 << (int)identifier)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(AnimationLayer identifier)
    {
        _intArray[(int)identifier >> 5] |= 1 << (int)identifier;
    }
}
