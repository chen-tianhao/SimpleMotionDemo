#nullable enable
using Assets.SingaPort;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using MathNet.Numerics.Distributions;

namespace Assets
{
    public class CmdPlayer : MonoBehaviour
    {
        public class Cmd
        {
            public float Time;
            public Action? Action;
        }

        readonly List<Cmd> cmds = new List<Cmd>();
        readonly Dictionary<string, GameObject> ObjectsById = new Dictionary<string, GameObject>();
        readonly Dictionary<string, Tuple<Vector3, Vector3>> VelocitiesById = new Dictionary<string, Tuple<Vector3, Vector3>>();
        readonly Dictionary<string, Tuple<Vector3, Vector3>> AccelerationsById = new Dictionary<string, Tuple<Vector3, Vector3>>();
        [SerializeField]
        private ContainerSpawner containerSpawner = null!;
        public double AveragePswcGroupSize { get; set; } = 6.5;//13.0;
        public int NumberOfGroupArrivals { get; private set; } = 0;
        public int NumberOfGroup { get; private set; } = 12;
        public Dictionary<int, Group> DwellingGroupsByIndex = new Dictionary<int, Group>();
        internal Inventory inventory = new Inventory();
        Simulator sim = new Simulator();
        bool simStarted = false;
        
        float StartTime;

        void Start()
        {
            Debug.Log("Test Debug.Log");
            var rs = new System.Random(0);
            var block = new Block(5, 8, 4);
            float t = 0;

            ////////////////////////////////////////////////////////////////////////////////////////
            //AddCmd for stacking containers
            for (int i = 0; i < NumberOfGroup; i++)
            {
                NumberOfGroupArrivals++;
                var group = new Group
                {
                    Index = NumberOfGroupArrivals,
                    // TEUs = 1, // rs.NextDouble() < (Ratio40to20 / (Ratio40to20 + 1)) ? 2 : 1,
                    TEUs = rs.NextDouble() < 0.6 ? 2 : 1,
                    // Size = Poisson.Sample(rs, AveragePswcGroupSize),
                    Size = Poisson.Sample(rs, AveragePswcGroupSize),
                };

                for (int j = 0; j < group.Size; j++)
                {
                    //var container = new Assets.SingaPort.Container(rs.NextDouble() < 0.4 ? ContainerSize.TwentyFeet : ContainerSize.FortyFeet);
                    var container = new Container(group); // 20ft or 40ft is defined by group.TEUs
                    // int? bayIndex = null, rowIndex = null, tierIndex = null;
                    // var result = block.StackContainer(container, rs, ref bayIndex, ref rowIndex, ref tierIndex);
                    // if (!result)
                    // {
                    //     Debug.LogWarning("Cannot stack more containers.");
                    //     break;
                    // }
                    container.Block = block; // Only have one block for demo
                    group.Containers.Add(container);
                }
                DwellingGroupsByIndex[group.Index] = group;
            }

            var containersWithRandomOrder = DwellingGroupsByIndex
                .Values.SelectMany(g => g.Containers).OrderBy(_ => rs.NextDouble()).ToList();
            foreach (var container in containersWithRandomOrder)
            {
                Slot? slot = inventory.Allocate(container);
                if (slot == null)
                {
                    Debug.LogWarning("Cannot allocate more containers.");
                    break;
                }
                container.Slot = slot;
                inventory.Update(container, Inventory.JobType.Stacking);
                
                // var go = container.Size == ContainerSize.TwentyFeet ?
                //             IsoContainer.GetObject(IsoContainer.Size.TwentyFeet, container.Group.Index) :
                //             IsoContainer.GetObject(IsoContainer.Size.FortyFeet, container.Group.Index);
                if (containerSpawner == null)
                {
                    containerSpawner = GetComponent<ContainerSpawner>();
                    if (containerSpawner == null)
                        containerSpawner = FindFirstObjectByType<ContainerSpawner>();
                    if (containerSpawner == null)
                    {
                        Debug.LogError("ContainerSpawner missing: 请在 Inspector 指定或场景中添加。");
                        return;
                    }
                }

                // // 位置方向静态测试代码
                // var go = containerSpawner.SpawnOne();
                // var pos = new Vector3(10, 0, -50);
                // Place("test-1", go, pos + new Vector3(), go.transform.eulerAngles);

                var go = containerSpawner.SpawnOne(container.Group);
                var pos = new Vector3(0, 0, 0);

                if (slot.Bay % 2 == 1) 
                    pos[0] = Block.SlotLength * (slot.Bay - 1) / 2; // 奇数是20尺箱
                else 
                    pos[0] = Block.SlotLength * (slot.Bay / 2 - 0.5f); // 偶数是40尺箱，位于相邻两个奇数bay中间
                pos[1] = (slot.Tier - 1) * Block.SlotHeight;
                pos[2] = (slot.Row - 1) * Block.SlotWidth;
                AddCmd(t, () => Stack($"Ctn#{container.Index}", go, pos, 5f, 10f));
                t += 3;
                Debug.Log($"Stacking Ctn#{container.Index} at Bay {slot.Bay}, Row {slot.Row}, Tier {slot.Tier}");

                if (block.NumTEUs > block.CapacityTEUs * 0.6) break;
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            //AddCmd for unstacking containers
            for (int i = 0; i < 10; i++)
            {
                var emptyGroupKeys = DwellingGroupsByIndex.Where(kv => kv.Value.Containers.Count == 0).Select(kv => kv.Key).ToList();
                foreach (var key in emptyGroupKeys)
                {
                    DwellingGroupsByIndex.Remove(key);
                }
                // Randomly pick a non-empty group to unstack a "best" container from this group
                var nonEmptyGroups = DwellingGroupsByIndex.Values.ToList();
                if (nonEmptyGroups.Count > 0)
                {
                    var chosen = nonEmptyGroups[rs.Next(nonEmptyGroups.Count)];
                    Debug.Log($"Picked group #{chosen.Index} with {chosen.Containers.Count} containers.");
                    Container? unstackingContainer = inventory.GetBestJobByPswc(block, chosen);
                    if (unstackingContainer == null)
                    {
                        Debug.LogWarning("No container available for unstacking.");
                    }
                    else
                    {
                        inventory.Update(unstackingContainer, Inventory.JobType.Unstacking);
                        chosen.Containers.Remove(unstackingContainer);
                        AddCmd(t, () => Unstack($"Ctn#{unstackingContainer.Index}", 5f, 10f));
                        t += 3;
                        Debug.Log($"Unstacking Ctn#{unstackingContainer.Index} from Bay {unstackingContainer.Slot.Bay}, Row {unstackingContainer.Slot.Row}, Tier {unstackingContainer.Slot.Tier}");
                    }
                }
                // if (block.NumTEUs < block.CapacityTEUs * 0.2) break;
            }

            // AddCmd for reshuffling containers
            /*
            for (int i = 0; i < 10; i++)
            {
               var k = i;
               var id = $"Ctn#{k}";
               AddCmd(i, () => Stack(id, IsoContainer.GetObject(), new Vector3(0, 0, k * 15 + 5), 3f, 3f));
               //AddCmd(new Cmd
               //{
               //    Time = i ,
               //    Action = () => Place(id, IsoContainer.GetObject(), new Vector3(0, 0, k * 15 + 5), new Vector3(0, 360f * k/10f, 0))
               //});
               //AddCmd(new Cmd
               //{
               //    Time = i * 10 + 5,
               //    Action = () => SetPosition(id, new Vector3(0, 10, k * 15 + 5), new Vector3(0, 10, 0))
               //});
               //AddCmd(new Cmd
               //{
               //    Time = i * 10 + 10,
               //    Action = () => SetVelocity(id, new Vector3(1, 1, 1), new Vector3(0, 1, 0))
               //});
               //AddCmd(new Cmd
               //{
               //    Time = i * 10 + 15,
               //    Action = () => SetAcceleration(id, new Vector3(-1, -1, 1), new Vector3(0, 10, 0))
               //});
               //AddCmd(new Cmd
               //{
               //    Time = i * 10 + 30,
               //    Action = () => Remove(id)
               //});
            }
            */

            StartTime = Time.time;
        }

        void AddCmd(float time, Action action)
        {
            cmds.Add(new Cmd { Time = time, Action = action });
            cmds.Sort((a, b) => a.Time.CompareTo(b.Time));
        }

        void Update()
        {
            foreach (var id in AccelerationsById.Keys)
            {
                if (!VelocitiesById.ContainsKey(id))
                {
                    VelocitiesById[id] = new Tuple<Vector3, Vector3>(new Vector3(0, 0, 0), new Vector3(0, 0, 0));
                }
                else
                {
                    VelocitiesById[id] = new Tuple<Vector3, Vector3>(
                        VelocitiesById[id].Item1 + AccelerationsById[id].Item1 * Time.deltaTime,
                        VelocitiesById[id].Item2 + AccelerationsById[id].Item2 * Time.deltaTime
                        );
                }
            }

            foreach (var id in VelocitiesById.Keys)
            {
                var go = ObjectsById[id];
                go.transform.position += VelocitiesById[id].Item1 * Time.deltaTime;
                go.transform.rotation = Quaternion.Euler(go.transform.rotation.eulerAngles + VelocitiesById[id].Item2 * Time.deltaTime);
            }

            float elapsed = Time.time - StartTime;
            while (cmds.Count > 0 && cmds.First().Time <= elapsed)
            {
                cmds[0].Action?.Invoke();
                cmds.RemoveAt(0);
            }
        }

        void Stack(string id, GameObject go, Vector3 pos, float speed, float offset)
        {
            if (!simStarted)
            {
                simStarted = true;
                sim.Arrive(1);
                sim.Run(TimeSpan.FromMinutes(3));
            }
            Place(id, go, pos + new Vector3(0, offset, 0), go.transform.eulerAngles);
            SetVelocity(id, new Vector3(0, -speed, 0), new Vector3());
            var a = speed * speed / (2 * offset);
            SetAcceleration(id, new Vector3(0, a, 0), new Vector3());
            var t = speed / a;
            AddCmd(Time.time + t, () => SetAcceleration(id, new Vector3(), new Vector3()));
            AddCmd(Time.time + t, () => SetVelocity(id, new Vector3(), new Vector3()));
            AddCmd(Time.time + t, () => SetPosition(id, pos, go.transform.eulerAngles));
        }

        void Unstack(string id, float speed, float offset)
        {
            if (ObjectsById.TryGetValue(id, out var go) && go != null)
            {
                Vector3 pos = go.transform.position;
                SetVelocity(id, new Vector3(0, speed, 0), new Vector3());
                var a = speed * speed / (2 * offset);
                SetAcceleration(id, new Vector3(0, -a, 0), new Vector3());
                var t = speed / a;
                AddCmd(Time.time + t, () => SetAcceleration(id, new Vector3(), new Vector3()));
                AddCmd(Time.time + t, () => SetVelocity(id, new Vector3(), new Vector3()));
                AddCmd(Time.time + t, () => SetPosition(id, pos + new Vector3(0, offset, 0), go.transform.eulerAngles));
                AddCmd(Time.time + t + 0.5f, () => Remove(id));
            }
        }

        void Reshuffle(string id, Vector3 targetPos, float speed, float offset)
        {
            if (ObjectsById.TryGetValue(id, out var go) && go != null)
            {
                Vector3 pos = go.transform.position;
                // move up
                SetVelocity(id, new Vector3(0, speed, 0), new Vector3());
                var au = speed * speed / (2 * offset);
                SetAcceleration(id, new Vector3(0, -au, 0), new Vector3());
                var tu = speed / au;
                AddCmd(Time.time + tu, () => SetAcceleration(id, new Vector3(), new Vector3()));
                AddCmd(Time.time + tu, () => SetVelocity(id, new Vector3(), new Vector3()));
                AddCmd(Time.time + tu, () => SetPosition(id, pos + new Vector3(0, offset, 0), go.transform.eulerAngles));
                // move horizontal
                
                // move down
                // Place(id, go, targetPos + new Vector3(0, offset, 0), new Vector3());
                SetVelocity(id, new Vector3(0, -speed, 0), new Vector3());
                var ad = au;
                SetAcceleration(id, new Vector3(0, ad, 0), new Vector3());
                var td = tu;
                AddCmd(Time.time + td, () => SetAcceleration(id, new Vector3(), new Vector3()));
                AddCmd(Time.time + td, () => SetVelocity(id, new Vector3(), new Vector3()));
                AddCmd(Time.time + td, () => SetPosition(id, targetPos, go.transform.eulerAngles));
            }
        }

        void Place(string id, GameObject go, Vector3 pos, Vector3 dir)
        {
            ObjectsById[id] = go;
            // 激活对象（如果之前为不可见），再设置位置和朝向
            go.SetActive(true);
            go.name = $"Obj_{id}";
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(dir);
        }

        void SetPosition(string id, Vector3 pos, Vector3 dir)
        {
            if (ObjectsById.TryGetValue(id, out var go) && go != null)
            {
                go.transform.position = pos;
                go.transform.rotation = Quaternion.Euler(dir);
            }
        }

        void SetVelocity(string id, Vector3 vPos, Vector3 vDir)
        {
            if (vPos.magnitude == 0 && vDir.magnitude == 0)
            {
                VelocitiesById.Remove(id);
            }
            else
            {
                VelocitiesById[id] = new Tuple<Vector3, Vector3>(vPos, vDir);
            }
        }

        void SetAcceleration(string id, Vector3 aPos, Vector3 aDir)
        {
            if (aPos.magnitude == 0 && aDir.magnitude == 0)
            {
                AccelerationsById.Remove(id);
            }
            else
            {
                AccelerationsById[id] = new Tuple<Vector3, Vector3>(aPos, aDir);
            }
        }

        void Remove(string id)
        {
            if (ObjectsById.TryGetValue(id, out var go) && go != null)
            {
                ObjectsById.Remove(id);
                VelocitiesById.Remove(id);
                AccelerationsById.Remove(id);
                Destroy(go);
            }
        }        

    }
}
