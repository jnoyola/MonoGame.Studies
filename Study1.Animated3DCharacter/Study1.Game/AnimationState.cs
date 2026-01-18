using Study1.ContentFramework.Models;

namespace Study1.Game;

public struct AnimationState
{
    public Animation? Animation { get; set; }
    public float Time { get; set; }
    public float PlaybackSpeed { get; set; }

    public Animation? TransitionAnimation { get; set; }
    public float TransitionTime { get; set; }
    public float TransitionPlaybackSpeed { get; set; }  // TODO: optimize this by baking into TransitionTime
    public float TransitionTotalDuration { get; set; }
    public float TransitionRemainingDuration { get; set; }

    public bool IsLooping { get; set; }
}
