using Assets.SingaPort;
using UnityEngine;

namespace Assets
{
    internal class BlockDrawing
    {
        public static GameObject GetObject(Block block)
        {
            var go = new GameObject("YardBlock");

            var position = new Vector3(0, 0, 0);
            foreach (int i in block.Bays.Keys)
            {
                if (i % 2 == 1) position[0] = Block.SlotLength * (i - 1) / 2;
                else position[0] = Block.SlotLength * (i / 2 - 1);
                var bay = block.Bays[i];
                for(int j = 1; j <= block.NumRows; j++)
                {
                    var stackHeight = bay.Stacks[j].Count;
                    for (int k = 0; k < stackHeight; k++)
                    {
                        var ctn = bay.ContainerSize == ContainerSize.TwentyFeet ?
                            IsoContainer.GetObject(IsoContainer.Size.TwentyFeet) :
                            IsoContainer.GetObject(IsoContainer.Size.FortyFeet);
                        ctn.transform.position += position;
                        ctn.transform.SetParent(go.transform, true);
                        position[1] += Block.SlotHeight;
                    }
                    position[1] = 0;
                    position[2] += Block.SlotWidth;
                }
                position[2] = 0;
            }
            
            return go;
        }
    }
}
