using System.Collections;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Study1.ContentFramework.Readers;

namespace Study1.Content.Writers;

[ContentTypeWriter]
public class BitArrayWriter : ContentTypeWriter<BitArray>
{
    public override bool CanDeserializeIntoExistingObject => true;

    public override string GetRuntimeReader(TargetPlatform targetPlatform) =>
        typeof(BitArrayReader).AssemblyQualifiedName ?? throw new Exception("Failed to find BitArrayReader.");

    protected override void Write(ContentWriter output, BitArray value)
    {
        output.Write((uint)value.Count);
        var byteCount = (value.Count + 7) / 8;
        for (int byteIndex = 0; byteIndex < byteCount; ++byteIndex)
        {
            byte b = 0;
            for (int bitIndex = 0; bitIndex < 8; ++bitIndex)
            {
                int index = byteIndex * 8 + bitIndex;
                if (index < value.Count && value[index])
                {
                    b |= (byte)(1 << bitIndex);
                }
            }
            output.Write(b);
        }
    }
}
