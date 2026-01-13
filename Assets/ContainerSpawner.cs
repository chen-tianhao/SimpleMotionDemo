using UnityEngine;

public class ContainerSpawner : MonoBehaviour
{
    public enum Size { FortyFeet, TwentyFeet }

    [SerializeField]
    private GameObject[] containerPrefabs;

    void Start()
    {
        // ① 选一个 Prefab（这里用随机）
        int index = Random.Range(0, containerPrefabs.Length);
        GameObject selectedPrefab = containerPrefabs[index];

        // ② ⭐真正生成“实际 GameObject”的代码（核心）
        GameObject containerInstance = Instantiate(
            selectedPrefab,          // Prefab（模板）
            Vector3.zero,             // 世界坐标
            Quaternion.identity       // 不旋转
        );

        containerInstance.name = "Container_Runtime";
    }

    public GameObject SpawnOne(Size size, int groupIdx = -1)
    {
        int colorIdx = groupIdx < 0 ? Random.Range(0, containerPrefabs.Length/2) : groupIdx % (containerPrefabs.Length/2);
        
        float containerLength = 6.1f;
        float containerHeight = 2.44f;
        float containerWidth = 2.59f;
        switch (size)
        {
            case Size.TwentyFeet:
                containerLength = 6.1f;
                break;
            case Size.FortyFeet:
                containerLength = 12.2f;
                colorIdx += containerPrefabs.Length/2; // using 40ft prefab
                break;
            default:
                Debug.LogError($"Container Size Not Specified: {size}");
                break;
        }
        GameObject prefab = containerPrefabs[colorIdx];

        GameObject instance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        Renderer rend = instance.GetComponentInChildren<Renderer>(); 
        if (rend != null)
        {
            // boundSize 就是素材自带的原始“长宽高”
            Vector3 originalSize = rend.bounds.size;

            // 4. 计算需要的缩放比例
            // 目标 / 原始 = 需要的 Scale
            // 注意：这里假设模型的轴向是标准的 (X=长, Y=高, Z=宽)，如果不是，需要调换 x/y/z
            float scaleX = containerWidth / originalSize.x;
            float scaleY = containerHeight / originalSize.y;
            float scaleZ = containerLength / originalSize.z;
            
            // 某些素材可能需要整体一致缩放，取平均值或最大值，这里演示分别缩放
            instance.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

            // 5. 修正位置（因为缩放是基于中心点的，可能需要把底面抬高到地面）
            // 重新计算 bounds，因为缩放后 bounds 变了
            float groundOffset = instance.transform.localScale.y * originalSize.y / 2f;
            instance.transform.position = new Vector3(0, groundOffset, 0); // 举例放在原点上方
        }
        else
        {
            Debug.LogError("Prefab 没有 Renderer，无法自动计算尺寸！");
        }

        // var position = new Vector3(containerLength / 2, containerHeight / 2f , containerWidth / 2);
        var rotation = new Vector3(0, 90f, 0);
        instance.transform.rotation = Quaternion.Euler(rotation);

        return instance; // 返回生成的 container
    }
}
