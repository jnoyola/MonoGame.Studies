using MonoGame.Framework.Content.Pipeline.Builder;

namespace Study2.Content;

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

        content.Include("Fonts/Tahoma_14.spritefont");

        content.Include("Effects/Particle.fx");

        return content;
    }
}
