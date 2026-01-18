using MonoGame.Framework.Content.Pipeline.Builder;
using Study1.Content.Processors;
using Study1.ContentFramework.Models;

namespace Study1.Content;

public class Builder : ContentBuilder
{
    public static int Main(string[] args)
    {
        var builder = new Builder();

        if (args is not null && args.Length > 0)
        {
            builder.Run(args);
        }
        else
        {
            builder.Run(
                new ContentBuilderParams
                {
                    Mode = ContentBuilderMode.Builder,
                    WorkingDirectory = $"{AppContext.BaseDirectory}../../../",
                    SourceDirectory = "Assets",
                }
            );
        }

        return builder.FailedToBuild > 0 ? -1 : 0;
    }

    public override IContentCollection GetContentCollection()
    {
        var content = new ContentCollection();

        content.Include("Effects/SkinnedVertexColoredEffect.fx");

        content.Include("Fonts/Tahoma_14.spritefont");

        content.Include(
            "Models/man.glb",
            contentProcessor: new GlbModelProcessor { ReverseIndexWinding = true, SrgbColorCorrection = true }
        );

        content.Include(
            "Models/man_anims.glb",
            contentProcessor: new GlbAnimationSetProcessor
            {
                Animations =
                [
                    new() { Name = "idle", WrapMode = WrapMode.Loop, DefaultLayer = AnimationLayer.Base },
                    new() { Name = "run_forward", WrapMode = WrapMode.Loop, DefaultLayer = AnimationLayer.Base },
                    new() { Name = "wave", WrapMode = WrapMode.Once, DefaultLayer = AnimationLayer.UpperBody },
                    new() { Name = "hand_closed_left", WrapMode = WrapMode.Clamp, DefaultLayer = AnimationLayer.AdditiveBase },
                    new() { Name = "hand_closed_right", WrapMode = WrapMode.Clamp, DefaultLayer = AnimationLayer.AdditiveBase },
                    new() { Name = "head_down", WrapMode = WrapMode.Clamp, DefaultLayer = AnimationLayer.AdditiveBase },
                    new() { Name = "breathe_heavy", WrapMode = WrapMode.Loop, DefaultLayer = AnimationLayer.AdditiveBase },
                ],
                AnimationLayerDefinitions =
                [
                    new(AnimationLayer.Base),
                    new(AnimationLayer.UpperBody)
                    {
                        BoneMask = new BoneMaskBuilder().AddSubtree("mixamorig:Spine")
                    },
                    new(AnimationLayer.AdditiveBase),
                ],
            }
        );
        return content;
    }
}
