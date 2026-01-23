using System.Collections;

namespace Study1.Content.Processors;

public class BoneMaskBuilder
{
    private readonly List<string> _boneNames = [];
    private readonly List<string> _boneSubtreeRoots = [];

    public BoneMaskBuilder Add(string boneName)
    {
        _boneNames.Add(boneName);
        return this;
    }

    public BoneMaskBuilder AddSubtree(string rootBoneName)
    {
        _boneSubtreeRoots.Add(rootBoneName);
        return this;
    }

    public BitArray Build(SharpGLTF.Schema2.ModelRoot input, IReadOnlyDictionary<string, int> boneIndices)
    {
        var boneMask = new BitArray(boneIndices.Count);

        foreach (var boneName in _boneNames)
        {
            if (boneIndices.TryGetValue(boneName, out var index))
            {
                boneMask[index] = true;
            }
            else
            {
                throw new Exception($"Bone '{boneName}' not found in model when building bone mask.");
            }
        }

        foreach (var rootBoneName in _boneSubtreeRoots)
        {
            var nodeQueue = new Queue<SharpGLTF.Schema2.Node>();
            bool foundRoot = false;
            foreach (var skin in input.LogicalSkins)
            {
                foreach (var node in skin.Joints)
                {
                    if (node.Name == rootBoneName)
                    {
                        nodeQueue.Enqueue(node);
                        foundRoot = true;
                        break;
                    }
                }

                if (foundRoot)
                {
                    break;
                }
            }

            while (nodeQueue.Count > 0)
            {
                var currentNode = nodeQueue.Dequeue();
                if (boneIndices.TryGetValue(currentNode.Name, out var index))
                {
                    boneMask[index] = true;
                }
                else
                {
                    throw new Exception($"Bone '{currentNode.Name}' not found in model when building bone mask.");
                }

                foreach (var child in currentNode.VisualChildren)
                {
                    nodeQueue.Enqueue(child);
                }
            }
        }

        return boneMask;
    }
}
