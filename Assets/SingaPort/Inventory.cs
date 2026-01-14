#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Assets.SingaPort
{
    public class Inventory
    {
        public enum JobType
        {
            Stacking,
            Unstacking
        }

        private sealed class GroundSlot
        {
            public List<Container> gsStack { get; } = new();   // 自底向上存储
            public int FlippedLayers { get; set; }     // 已翻动层数累计
        }

        private readonly Dictionary<(int blockId, int dim1, int dim2), GroundSlot> _map = new();

        // 将奇偶 bay 编码映射到连续的 dim1（1..NumBays）
        private static int ToDim1(int bay) => bay % 2 == 0 ? bay / 2 : (bay + 1) / 2;


        public Slot? Allocate(Container container)
        {
            // if (Config.traceAllocate) Debug.Log($"[{container.ToString()}] [Allocate] Finding slot...");
            Block block = container.Block;
            int maxBayIndex = block.NumBays * 2 - 1;
            bool IsForty(Container? j) => j != null && j.Size == ContainerSize.FortyFeet;
            bool IsTwenty(Container? j) => j != null && j.Size == ContainerSize.TwentyFeet;

            // 20 尺可继续在已放 20 的 bay 堆放；仅当该 bay 参与了一只 40 尺（跨 bay）时阻塞 20 尺
            bool IsBayBlockedFor20(int bay)
            {
                for (int row = 1; row <= block.NumRows; row++)
                {
                    var self = Peek(block, new Slot(bay, row, 1));
                    if (IsForty(self)) return true;
                }
                return false;
            }

            // bay 及 bay+1 不得存在 20 尺箱，且两侧高度对齐（若顶层为 40，要求跨到同一只箱）
            bool CanPlaceForty(int bay, int row, out int tier)
            {
                tier = -1;
                if (bay % 2 != 0) return false; // 40ft 以偶数为中心
                if (bay <= 1 || bay >= maxBayIndex) return false;
                int leftBay = bay - 1;
                int rightBay = bay + 1;
                int h1 = GetHeight(block, new Slot(leftBay, row, 1));
                int h2 = GetHeight(block, new Slot(rightBay, row, 1));
                var top1 = Peek(block, new Slot(leftBay, row, 1));
                var top2 = Peek(block, new Slot(rightBay, row, 1));
                // 不允许任一侧已有 20 尺
                if (IsTwenty(top1) || IsTwenty(top2)) return false;
                // 若任一侧存在 20 尺于其他行，需要整体阻塞
                for (int r = 1; r <= block.NumRows; r++)
                {
                    if (IsTwenty(Peek(block, new Slot(leftBay, r, 1)))) return false;
                    if (IsTwenty(Peek(block, new Slot(rightBay, r, 1)))) return false;
                }
                bool topsCrossSameForty = IsForty(top1) && ReferenceEquals(top1, top2);
                bool topsWithoutForty = !IsForty(top1) && !IsForty(top2);
                if (!(h1 == h2 && (topsCrossSameForty || topsWithoutForty))) return false;
                if (h1 >= block.MaxNumTiers) return false;
                tier = h1 + 1;
                return true;
            }

            // 1) Look for a GS with the same PSWC that is not full
            if (container.Size == ContainerSize.FortyFeet)
            {
                for (int bay = 2; bay < maxBayIndex; bay += 2)
                {
                    for (int row = 1; row <= block.NumRows; row++)
                    {
                        if (!CanPlaceForty(bay, row, out var tier)) continue;
                        var topJob = Peek(block, new Slot(bay - 1, row, 1));
                        if (topJob != null && topJob.Group.Index == container.Group.Index)
                        {
                            // if (Config.traceAllocate) Debug.Log($"[{container.ToString()}] [Allocate-1] 40ft same PSWC at Yc {yc.id}, Bay {bay}-{bay + 1}, Row {row}, Tier {tier}");
                            return new Slot(bay, row, tier);
                        }
                    }
                }
            }
            else
            {
                for (int bay = 1; bay <= maxBayIndex; bay += 2)
                {
                    for (int row = 1; row <= block.NumRows; row++)
                    {
                        var baseSlot = new Slot(bay, row, 1);
                        int tier = GetHeight(block, baseSlot);
                        if (tier <= 0) continue;
                        var topJob = Peek(block, baseSlot);
                        if (topJob != null && topJob.Group.Index == container.Group.Index && !IsBayBlockedFor20(bay))
                        {
                            if (tier < block.MaxNumTiers)
                            {
                                // if (Config.traceAllocate) Debug.Log($"[{container.ToString()}] [Allocate-1] 20ft same PSWC at Yc {yc.id}, Bay {bay}, Row {row}, Tier {tier + 1}");
                                return new Slot(bay, row, tier + 1);
                            }
                        }
                    }
                }
            }

            // 2) search empties
            if (container.Size == ContainerSize.FortyFeet)
            {
                for (int bay = 2; bay < maxBayIndex; bay += 2)
                {
                    for (int row = 1; row <= block.NumRows; row++)
                    {
                        int leftBay = bay - 1;
                        int rightBay = bay + 1;
                        // 空位限定：两侧当前高度均为 0 才视为“空”
                        if (GetHeight(block, new Slot(leftBay, row, 1)) != 0) continue;
                        if (GetHeight(block, new Slot(rightBay, row, 1)) != 0) continue;
                        if (CanPlaceForty(bay, row, out var tier))
                        {
                            // if (Config.traceAllocate) Debug.Log($"[{container.ToString()}] [Allocate-2] 40ft empty at Yc {yc.id}, Bay {bay}-{bay + 1}, Row {row}, Tier {tier}");
                            return new Slot(bay, row, tier);
                        }
                    }
                }
            }
            else
            {
                for (int bay = 1; bay <= maxBayIndex; bay += 2)
                {
                    for (int row = 1; row <= block.NumRows; row++)
                    {
                        if (IsBayBlockedFor20(bay)) continue;
                        if (GetHeight(block, new Slot(bay, row, 1)) == 0)
                        {
                            // if (Config.traceAllocate) Debug.Log($"[{container.ToString()}] [Allocate-2] 20ft empty at Yc {yc.id}, Bay {bay}, Row {row}, Tier 1");
                            return new Slot(bay, row, 1);
                        }
                    }
                }
            }

            // 3) pick highest non-full respecting size rules
            int bestBay = -1;
            int bestRow = -1;
            int bestHeight = -1;
            if (container.Size == ContainerSize.FortyFeet)
            {
                for (int bay = 2; bay < maxBayIndex; bay += 2)
                {
                    for (int row = 1; row <= block.NumRows; row++)
                    {
                        if (!CanPlaceForty(bay, row, out var tier)) continue;
                        int h = tier - 1;
                        if (h > bestHeight)
                        {
                            bestHeight = h;
                            bestBay = bay;
                            bestRow = row;
                        }
                    }
                }
            }
            else
            {
                for (int bay = 1; bay <= maxBayIndex; bay += 2)
                {
                    for (int row = 1; row <= block.NumRows; row++)
                    {
                        if (IsBayBlockedFor20(bay)) continue;
                        int tier = GetHeight(block, new Slot(bay, row, 1));
                        if (tier < block.MaxNumTiers && tier > bestHeight)
                        {
                            bestHeight = tier;
                            bestBay = bay;
                            bestRow = row;
                        }
                    }
                }
            }
            if (bestBay > 0)
            {
                int tier = bestHeight + 1;
                // if (Config.traceAllocate) Debug.Log($"[{container.ToString()}] [Allocate-3] Selected at Yc {yc.id}, Bay {bestBay}{(container.Size==ContainerSize.FortyFeet?$"-{bestBay+1}":string.Empty)}, Row {bestRow}, Tier {tier}");
                return new Slot(bestBay, bestRow, tier);
            }

            // 4) If no GS is found that is not full, return null
            // if (Config.traceAllocate) Debug.Log($"[{container.ToString()}] [Allocate-4] No empty slot found");
            return null;
        }

        public void Update(Container container, JobType opType)
        {
            if (container.Slot == null || container.Slot.Bay == 0 || container.Slot.Row == 0 || container.Slot.Tier == 0) 
            {
                // Debug.Log($"[{job.ToString()}] [ERROR] Invalid slot: {job.slot.ToString()}");
                return;
            }
            if (opType == JobType.Stacking)
            {
                // Make sure job.slot is set already
                Stack(container.Block, container.Slot, container);
            }
            else
            {
                Unstack(container.Block, container.Slot);
            }
        }

        public int GetHeight(Block block, Slot slot)  // Height of stack
        {
            return GetGroundSlot(block, slot).gsStack.Count;
        }

        public Container? GetBestJobByPswc(Block bu, Group group)
        {
            Container? best = null;
            int bestDepth = int.MaxValue; // 0 means top of stack
            int bestHeight = int.MinValue;
            int bestBay = int.MinValue;
            int bestRow = int.MinValue;

            foreach (var kv in _map)
            {
                int blockId = kv.Key.blockId;
                int bay = kv.Key.dim1;
                int row = kv.Key.dim2;
                if (blockId != bu.Index) continue;
                var stack = kv.Value.gsStack;
                if (stack.Count == 0) continue;

                // 从顶向下找第一个匹配的 job
                for (int idx = stack.Count - 1; idx >= 0; idx--)
                {
                    var container = stack[idx];
                    if (container.Size != (group.TEUs == 2 ? ContainerSize.FortyFeet : ContainerSize.TwentyFeet) 
                    || container.Group.Index != group.Index)
                    {
                        continue;
                    }

                    int depthFromTop = (stack.Count - 1) - idx; // 0 = 顶
                    int height = stack.Count; // ground slot height

                    bool better = false;
                    if (depthFromTop < bestDepth) better = true;
                    else if (depthFromTop == bestDepth)
                    {
                        if (height > bestHeight) better = true;
                        else if (height == bestHeight)
                        {
                            if (bay > bestBay) better = true;
                            else if (bay == bestBay && row > bestRow) better = true;
                        }
                    }

                    if (better)
                    {
                        best = container;
                        bestDepth = depthFromTop;
                        bestHeight = height;
                        bestBay = bay;
                        bestRow = row;
                    }

                    // 找到匹配后即可跳出当前 stack 的搜索（更深层不会更优）
                    break;
                }
            }

            return best;
        }

        private GroundSlot GetGroundSlot(Block block, Slot slot)
        {
            var gsIdx = (block.Index, ToDim1(slot.Bay), slot.Row);
            if (!_map.TryGetValue(gsIdx, out var gs))
            {
                gs = new GroundSlot();
                _map[gsIdx] = gs;
            }
            return gs;
        }

        private void Stack(Block block, Slot slot, Container container)  // Stacking
        {
            // 40尺箱占用两个相邻 bay、同 row、同 tier
            if (container.Size == ContainerSize.FortyFeet)
            {
                int leftBay = slot.Bay - 1;
                int rightBay = slot.Bay + 1;
                GetGroundSlot(block, new Slot(leftBay, slot.Row, slot.Tier)).gsStack.Add(container);
                GetGroundSlot(block, new Slot(rightBay, slot.Row, slot.Tier)).gsStack.Add(container);
            }
            else
            {
                GetGroundSlot(block, slot).gsStack.Add(container);
            }
        }

        private Container? Unstack(Block block, Slot slot)  // Unstacking
        {
            var gs = GetGroundSlot(block, slot);
            if (gs.gsStack.Count == 0) return null;
            var top = gs.gsStack[^1];
            gs.gsStack.RemoveAt(gs.gsStack.Count - 1);
            if (top.Size == ContainerSize.FortyFeet)
            {
                var gsPair = GetGroundSlot(block, new Slot(slot.Bay + 2, slot.Row, slot.Tier));
                if (gsPair.gsStack.Count > 0) gsPair.gsStack.RemoveAt(gsPair.gsStack.Count - 1);
            }
            return top;
        }

        private Container? Peek(Block block, Slot slot)
        {
            var gs = GetGroundSlot(block, slot);
            return gs.gsStack.Count == 0 ? null : gs.gsStack[^1];
        }

        // Not use in Unity demo project
        private string Insight(string tag)
        {
            // return "";
            StringBuilder sbInsight = new StringBuilder();
            var entries = _map.Where(kv => kv.Value.gsStack.Count > 0)
                               .OrderBy(kv => kv.Key.blockId)
                               .ThenBy(kv => kv.Key.dim1)
                               .ThenBy(kv => kv.Key.dim2)
                               .ToList();

            sbInsight.AppendLine($"Inventory snapshot: {tag}");
            if (entries.Count == 0)
            {
                sbInsight.AppendLine("  (empty)");
                return sbInsight.ToString();
            }

            foreach (var kv in entries)
            {
                int blockId = kv.Key.blockId;
                int bay = kv.Key.dim1;
                int row = kv.Key.dim2;
                var stack = kv.Value.gsStack;
                var items = string.Join(" | ", stack.Select(container => $"{container.Index:D6}({(container.Size == ContainerSize.FortyFeet ? "40" : "20")}/{container.Group.Index})"));
                sbInsight.AppendLine($"  YC {blockId:D2} GS({bay},{row}) h={stack.Count}: {items}");
            }
            return sbInsight.ToString();
        }
    }
}
