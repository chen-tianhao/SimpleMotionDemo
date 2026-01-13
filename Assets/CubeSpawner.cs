using Assets;
using Assets.SingaPort;
using System;
using System.Collections;
using System.ComponentModel;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static UnityEngine.Rendering.DebugUI.Table;

public class CubeSpawner : MonoBehaviour
{
    public int totalCubes = 100;      // 总生成数量
    public float spacing = 15f;        // Cube 间距（米）
    public float spawnInterval = 5f;   // 每 5 秒生成一个

    public float minLifetime = 30f;     // 最短存在时间（秒）
    public float maxLifetime = 50f;    // 最长存在时间（秒）

    private int spawnedCount = 0;

    void Start()
    {
        StartCoroutine(SpawnCubes());

        //var rs = new System.Random(0);
        //for (int i = 0; i < 10; i++)
        //{
        //    var block = new Block(30, 8, 6);
        //    while (true)
        //    {
        //        var container = new Assets.SingaPort.Container(rs.NextDouble() < 0.4 ? ContainerSize.TwentyFeet : ContainerSize.FortyFeet);
        //        int? bayIndex = null, rowIndex = null, tierIndex = null;
        //        block.StackContainer(container, rs, ref bayIndex, ref rowIndex, ref tierIndex);
        //        if (block.NumTEUs > block.CapacityTEUs * 0.6) break;
        //    }

        //    var go = BlockDrawing.GetObject(block);
        //    go.transform.position += new Vector3(10, 0, 30 * i + 10);
        //}
    }

    IEnumerator SpawnCubes()
    {
        while (spawnedCount < totalCubes)
        {
            Vector3 position = new Vector3(
                spawnedCount * spacing,
                0f,
                0f
            );

            GameObject go;

            go = IsoContainer.GetObject();

            go.transform.position += position;
            
            go.name = $"Cube_{spawnedCount}";

            // 给这个 cube 安排一个“随机时间后消失”的事件
            StartCoroutine(DestroyAfterRandomTime(go));

            spawnedCount++;

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    IEnumerator DestroyAfterRandomTime(GameObject cube)
    {
        float lifetime = UnityEngine.Random.Range(minLifetime, maxLifetime);

        yield return new WaitForSeconds(lifetime);

        // 防止 cube 已经被其它逻辑销毁
        if (cube != null)
        {
            Destroy(cube);
        }
    }
}
