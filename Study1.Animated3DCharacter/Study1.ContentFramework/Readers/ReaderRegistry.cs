using Microsoft.Xna.Framework.Content;

namespace Study1.ContentFramework.Readers;

public static class ReaderRegistry
{
    public static void Initialize()
    {
        AddDefaultInstance<AnimationReader>();
        AddDefaultInstance<AnimationSetReader>();
        AddDefaultInstance<BitArrayReader>();
        AddDefaultInstance<ModelReader>();
    }

    private static void AddDefaultInstance<T>() where T : ContentTypeReader, new()
    {
        var reader = new T();
        ContentTypeReaderManager.AddTypeCreator(typeof(T).FullName, () => reader);
    }
}
