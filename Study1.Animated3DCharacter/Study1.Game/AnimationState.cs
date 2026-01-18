using System.Runtime.CompilerServices;
using Study1.ContentFramework.Models;

namespace Study1.Game;

/// <summary>
/// Holds the current state of all animation layers for an animated character.
/// </summary>
public struct AnimationState
{
    [InlineArray(AnimationLayerDefinitions.MaxOverrideLayerCount)]
    public struct OverrideLayerStates
    {
        private OverrideLayerState _element0;
    }

    [InlineArray(AnimationLayerDefinitions.MaxAdditiveLayerCount)]
    public struct AdditiveLayerStates
    {
        private AdditiveLayerState _element0;
    }

    public OverrideLayerStates OverrideLayers;
    public AdditiveLayerStates AdditiveLayers;
}

/// <summary>
/// Holds the current state of a single override animation layer.
/// This can contain info about temporal transitions within this layer, as well as inter-layer blending.
/// </summary>
public struct OverrideLayerState
{
    // The current animation playing on this layer.
    public Animation? Animation { get; set; }
    public float Time { get; set; }
    public float PlaybackSpeed { get; set; }

    // Intra-layer transition state (e.g. idle -> walking).
    public Animation? TransitionAnimation { get; set; }
    public float TransitionTime { get; set; }
    public float TransitionPlaybackSpeed { get; set; }  // TODO: optimize this by baking into TransitionTime
    public float TransitionTotalDuration { get; set; }
    public float TransitionRemainingDuration { get; set; }

    // Inter-layer blending state (e.g. arms half-raised -> lowered).
    public float Weight { get; set; }
    public float TargetWeight { get; set; }
    public float WeightVelocity { get; set; }
}

/// <summary>
/// Holds the current state of a single additive animation layer.
/// This can contain multiple additive clips contributing simultaneously.
/// </summary>
public struct AdditiveLayerState
{
    public const int MaxAdditiveClipCount = 3;

    [InlineArray(MaxAdditiveClipCount)]
    public struct AdditiveClipStates
    {
        private AdditiveClipState _element0;
    }

    [InlineArray(MaxAdditiveClipCount)]
    public struct ClipRequests
    {
        private AdditiveClipRequest _element0;
    }

    [InlineArray(MaxAdditiveClipCount)]
    public struct ClipFlags
    {
        private bool _element0;
    }

    // Currently playing clips.
    public AdditiveClipStates Clips;

    // Staging state for clips requested this frame.
    public ClipRequests RequestedClips;
    public int RequestedClipIndex;
}

public struct AdditiveClipState
{
    public Animation? Animation { get; set; }
    public float Time { get; set; }
    public float PlaybackSpeed { get; set; }

    public float Weight { get; set; }
    public float TargetWeight { get; set; }
    public float WeightVelocity { get; set; }
}

public struct AdditiveClipRequest
{
    public Animation Animation { get; set; }
    public float PlaybackSpeed { get; set; }
    public float Weight { get; set; }
    public float FadeDuration { get; set; }
}
