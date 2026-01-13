using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class JsonLineEventPlayer : MonoBehaviour
{
    [Header("Input")]
    public string fileName = "commands.ndjson";

    [Header("Optional: random disappear")]
    public bool addRandomDisappear = false;
    public float disappearMin = 1f;
    public float disappearMax = 10f;

    class Cmd
    {
        public float t;
        public string evnt;
        public string id;
        public float[] pos; // length 3
        public float[] dir; // length 3
    }

    readonly List<Cmd> cmds = new List<Cmd>();
    readonly Dictionary<string, GameObject> objectsById = new Dictionary<string, GameObject>();

    float startTime;
    int idx;

    void Start()
    {
        LoadCommands();
        cmds.Sort((a, b) => a.t.CompareTo(b.t));

        startTime = Time.time;
        idx = 0;
    }

    void Update()
    {
        float elapsed = Time.time - startTime;

        while (idx < cmds.Count && cmds[idx].t <= elapsed)
        {
            Execute(cmds[idx]);
            idx++;
        }
    }

    void LoadCommands()
    {
        cmds.Clear();

        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        if (!File.Exists(path))
        {
            Debug.LogError($"Command file not found: {path}");
            return;
        }

        // 逐行读取
        foreach (var line in File.ReadLines(path))
        {
            string s = line.Trim();
            if (string.IsNullOrEmpty(s)) continue;

            try
            {
                // JsonUtility 不能直接解析数组顶层，但这里是一行一个对象，OK
                Cmd cmd = JsonUtility.FromJson<Cmd>(s);
                if (cmd == null || string.IsNullOrEmpty(cmd.evnt) || string.IsNullOrEmpty(cmd.id))
                    continue;

                cmds.Add(cmd);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Bad line: {s}\n{e.Message}");
            }
        }

        Debug.Log($"Loaded {cmds.Count} commands from {path}");
    }

    void Execute(Cmd cmd)
    {
        string evnt = cmd.evnt.ToLowerInvariant();

        if (evnt == "show")
        {
            Vector3 pos = (cmd.pos != null && cmd.pos.Length >= 3)
                ? new Vector3(cmd.pos[0], cmd.pos[1], cmd.pos[2])
                : Vector3.zero;

            Vector3 dir = (cmd.dir != null && cmd.dir.Length >= 3)
                ? new Vector3(cmd.dir[0], cmd.dir[1], cmd.dir[2])
                : Vector3.forward;

            Show(cmd.id, pos, dir);
        }
        else if (evnt == "hide")
        {
            Hide(cmd.id);
        }
        else
        {
            Debug.LogWarning($"Unknown event: {cmd.evnt}");
        }
    }

    void Show(string id, Vector3 pos, Vector3 dir)
    {        
        //var pathUnderResources = "Truck";
        //var go = Resources.Load<GameObject>(pathUnderResources);
        //if (go == null)
        //{
        //    Debug.LogError($"Resources prefab not found: {pathUnderResources}");
        //    go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        //}

        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);

        objectsById[id] = go;
        go.name = $"Obj_{id}";
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(dir[0], dir[1], dir[2]);     
    }

    void Hide(string id)
    {
        if (objectsById.TryGetValue(id, out var go) && go != null)
        {
            objectsById.Remove(id);
            Destroy(go);
        }
    }
}
