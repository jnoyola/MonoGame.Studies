using System.Collections;

namespace Study1.ContentFramework.Models;

public class BoneMaskSet(int boneCount, int maskCount)
{
    private readonly Dictionary<string, int> _boneIndices = new(boneCount);
    private readonly BitArray[] _boneMasks = new BitArray[maskCount];

    

    public void SetBoneIndex(string boneName, int index)
    {
        _boneIndices[boneName] = index;
    }

    public void SetBoneMask(int maskIndex, BitArray bitArray)
    {
        _boneMasks[maskIndex] = bitArray;
    }

    public bool TryGetBoneIndex(string boneName, out int index)
    {
        return _boneIndices.TryGetValue(boneName, out index);
    }

    public bool IsBoneActive(int maskIndex, string boneName)
    {
        if (!_boneIndices.TryGetValue(boneName, out var boneIndex))
            return false;

        return _boneMasks[maskIndex][boneIndex];
    }
}
