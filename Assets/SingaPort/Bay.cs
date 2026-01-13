using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.SingaPort
{
    public class Bay
    {
        public ContainerSize ContainerSize { get;private set; }
        public Dictionary<int,List<Container>> Stacks { get; private set; }
        public Block Block { get; private set; }
        public int NumContainers { get { return Stacks.Values.Sum(v => v.Count); } }
        public int NumTEUs { get { return NumContainers * (ContainerSize == ContainerSize.FortyFeet ? 2 : 1); } }
        public bool IsFull
        {
            get
            {
                foreach (var stack in Stacks.Values) 
                    if (stack.Count < Block.MaxNumTiers) return false;
                return true;
            }
        }
        public Bay(Block block, ContainerSize size)
        {
            Block = block;
            ContainerSize = size;
            Stacks = Enumerable.Range(1, block.NumRows).ToDictionary(i => i, i => new List<Container>());
        }

        public int? GetRowIndexToStack(Random rs)
        {
            var indices = new List<int>();
            for (var i = 1; i <= Stacks.Count; i++)
            {
                // avoid exceeding max tiers
                if (Stacks[i].Count >= Block.MaxNumTiers) continue;
                // avoid large difference
                if (Stacks[i].Count >= Stacks.Values.Min(v => v.Count) + 4) continue;
                // no steep slop on upper side
                if (i < Stacks.Count && Stacks[i].Count > Stacks[i + 1].Count) continue;
                // no steep slop on lower side
                if (i > 1 && Stacks[i].Count > Stacks[i - 1].Count) continue;
                // no hole on upper side
                if (i < Stacks.Count - 1 && Stacks[i].Count == Stacks[i + 1].Count && 
                    Enumerable.Range(i + 2, Block.NumRows - i - 1).Count(i1 => Stacks[i1].Count > Stacks[i].Count) > 0)
                    continue;
                // no hole on lower side
                if (i > 2 && Stacks[i].Count == Stacks[i - 1].Count &&
                    Enumerable.Range(1, i - 2).Count(i1 => Stacks[i1].Count > Stacks[i].Count) > 0)
                    continue;
                indices.Add(i);
            }
            if (indices.Count == 0) return null;
            return indices[rs.Next(indices.Count)];
        } 

        public bool StackContainer(Container container, Random rs, ref int? rowIndex, ref int? tierIndex)
        {
            // 当前函数的用途是返回rowIndex
            rowIndex = GetRowIndexToStack(rs);
            if (rowIndex == null) return false;
            return StackContainer(container, rowIndex.Value, ref tierIndex);
        }

        public bool StackContainer(Container container, int rowIndex, ref int? tierIndex)
        {
            // 当前函数的用途是返回tierIndex
            if (container.Size != ContainerSize || Stacks[rowIndex].Count >= Block.MaxNumTiers)
                return false;
            Stacks[rowIndex].Add(container);
            tierIndex = Stacks[rowIndex].Count;
            return true;
        }
    }
}
