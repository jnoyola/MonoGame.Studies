using Microsoft.Xna.Framework.Content.Pipeline;

namespace Study1.Content.Importers;

[ContentImporter(".glb", DisplayName = "GLB - MonoGame.Studies", DefaultProcessor = "GlbModelProcessor")]
public class GlbImporter : ContentImporter<SharpGLTF.Schema2.ModelRoot>
{
    public override SharpGLTF.Schema2.ModelRoot Import(string filename, ContentImporterContext context)
    {
        return SharpGLTF.Schema2.ModelRoot.Load(filename);
    }
}
