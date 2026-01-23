using System.Collections;
using Microsoft.Xna.Framework.Content;

namespace Study1.ContentFramework.Readers;

public class BitArrayReader : ContentTypeReader<BitArray>
{
    public override bool CanDeserializeIntoExistingObject => true;

    protected override BitArray Read(ContentReader input, BitArray? existingInstance)
    {
        var count = input.ReadUInt32();
        var value = existingInstance ?? new BitArray((int)count);
        var byteCount = (count + 7) / 8;
        for (int byteIndex = 0; byteIndex < byteCount; ++byteIndex)
        {
            var b = input.ReadByte();
            for (int bitIndex = 0; bitIndex < 8; ++bitIndex)
            {
                int index = byteIndex * 8 + bitIndex;
                if (index < count && (b & (1 << bitIndex)) != 0)
                {
                    value[index] = true;
                }
            }
        }
        return value;
    }
}
