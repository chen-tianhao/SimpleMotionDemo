#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Rendering.LookDev;


namespace Assets.SingaPort
{
    public class Block
    {
        public static int Count { get; private set; } = 0;
        public static float SlotLength = 6.5f;
        public static float SlotWidth = 2.5f;
        public static float SlotHeight = 2.6f;
        public int Index { get; private set; }
        public int NumBays { get; private set; }
        public int NumRows { get; private set; }
        public int MaxNumTiers { get; private set; }
        public int CapacityTEUs { get { return (NumBays + 1) / 2 * NumRows * MaxNumTiers; } }
        public int NumContainers { get { return Bays.Values.Sum(v => v.NumContainers); } }
        public int NumTEUs { get { return Bays.Values.Sum(v => v.NumTEUs); } }
        public Dictionary<int, Bay> Bays { get; private set; }

        // numBays must be odd, coz bayIndex: 1,2,3,... odd: 20ft even:40ft
        public Block(int numBays, int numRows, int maxNumTiers)
        {
            Index = ++Count;
            Bays = new Dictionary<int, Bay>();
            NumBays = numBays;
            NumRows = numRows;
            MaxNumTiers = maxNumTiers;
        }
        public int? GetBayIndexToStack(ContainerSize containerSize, Random rs)
        {
            var indices = new List<int>();
            var twentyFeetBays = Bays.Values.Where(b => b.ContainerSize == ContainerSize.TwentyFeet).ToList();
            var fortyFeetBays = Bays.Values.Where(b => b.ContainerSize == ContainerSize.FortyFeet).ToList();

            // condition if to create more 20-feet bays
            var lessTwentyBays =
                (twentyFeetBays.Count > 0 ? twentyFeetBays.Average(b => 1.0f * b.NumContainers) : 0f)
                >= (fortyFeetBays.Count > 0 ? fortyFeetBays.Average(b => 1.0f * b.NumContainers) : 0f);

            switch (containerSize)
            {
                case ContainerSize.TwentyFeet:
                    for (int i = 1; i < NumBays * 2; i += 2)
                    {
                        // 如果目前还没有任何 bay，直接接受第一个 20 尺 bay 位置，避免因邻位检查过严导致无法起仓
                        if (Bays.Count == 0)
                        {
                            indices.Add(i);
                            break;
                        }

                        if (Bays.ContainsKey(i))
                        {
                            if (!Bays[i].IsFull) indices.Add(i);
                        }
                        else if (twentyFeetBays.Count == 0 ||
                            ((i == 1 || Bays.ContainsKey(i - 2) || Bays.ContainsKey(i - 3)) &&
                            (Bays.ContainsKey(i + 2) || Bays.ContainsKey(i + 3) || i == NumBays * 2 - 1)) // only one twenty-feet gap
                            || lessTwentyBays)
                        {

                            if (!Bays.ContainsKey(i - 1) && !Bays.ContainsKey(i + 1))
                                // do not creat a bubble at either side
                                if (Bays.ContainsKey(i - 2) || Bays.ContainsKey(i - 3) || Bays.ContainsKey(i + 2) || Bays.ContainsKey(i + 3))
                                    indices.Add(i);
                        }
                    }
                    break;
                case ContainerSize.FortyFeet:
                    for (int i = 2; i < NumBays * 2; i += 2)
                    {
                        if (Bays.ContainsKey(i))
                        {
                            if (!Bays[i].IsFull) indices.Add(i);
                        }
                        else if (fortyFeetBays.Count == 0 || !lessTwentyBays)
                        {
                            if (!Bays.ContainsKey(i - 1) && !Bays.ContainsKey(i + 1) &&
                                !Bays.ContainsKey(i - 2) && !Bays.ContainsKey(i + 2))
                                indices.Add(i);
                        }
                    }
                    break;
                default:
                    throw new Exception();

            }
            if (indices.Count == 0) return null;
            //if (containerSize == ContainerSize.TwentyFeet) return indices.Min();
            //else return indices.Max();
            return indices[rs.Next(indices.Count)];
        }

        public bool StackContainer(Container container, Random rs, ref int? bayIndex, ref int? rowIndex, ref int? tierIndex)
        {
            // 当前函数的用途是返回bayIndex，全部调用结束后，实际返回index of bay/row/tier
            bayIndex = GetBayIndexToStack(container.Size, rs);
            if (bayIndex == null) return false;
            return StackContainer(container, rs, bayIndex.Value, ref rowIndex, ref tierIndex);
        }

        public bool StackContainer(Container container, Random rs, int bayIndex, ref int? rowIndex, ref int? tierIndex)
        {
            // 当前函数的用途是根据bayIndex找到或创建bay
            var bay = CreateBayIfNotExist(bayIndex, container.Size);
            return bay.StackContainer(container, rs, ref rowIndex, ref tierIndex);
        }

        // public bool StackContainer(Container container, int bayIndex, int rowIndex, ref int? tierIndex)
        // {
        //     var bay = CreateBayIfNotExist(bayIndex, container.Size);
        //     return bay.StackContainer(container, rowIndex, ref tierIndex);
        // }

        private Bay CreateBayIfNotExist(int bayIndex, ContainerSize size)
        {
            if (!Bays.ContainsKey(bayIndex))
            {
                if (bayIndex < 1 || bayIndex > NumBays * 2 - 1 ||
                    Bays.ContainsKey(bayIndex - 1) || Bays.ContainsKey(bayIndex + 1))
                    throw new Exception("Bay Index Infeasible.");
                if (bayIndex % 2 == 0 && (Bays.ContainsKey(bayIndex - 2) || Bays.ContainsKey(bayIndex + 2)))
                    throw new Exception("Bay Index Infeasible.");
                Bays.Add(bayIndex, new Bay(this, size));
            }
            return Bays[bayIndex];
        }
    }
}
