using MonoGame.Framework.Content.Pipeline.Builder;
using Study1.Content.Processors;

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

        content.Include("Models/man.glb", contentProcessor: new GlbModelProcessor { ReverseIndexWinding = true, SrgbColorCorrection = true });
        content.Include("Models/man_anims.glb", contentProcessor: new GlbAnimationSetProcessor());

        return content;
    }
}
