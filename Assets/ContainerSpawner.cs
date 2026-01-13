using Assets.SingaPort;
using UnityEngine;

public class ContainerSpawner : MonoBehaviour
{
    public enum Size { FortyFeet, TwentyFeet }

    [SerializeField]
    private GameObject[] containerPrefabs;

    void Start()
    {
        // // ① 选一个 Prefab（这里用随机）
        // int index = Random.Range(0, containerPrefabs.Length);
        // GameObject selectedPrefab = containerPrefabs[index];

        // // ② 真正生成“实际 GameObject”
        // GameObject containerInstance = Instantiate(selectedPrefab, Vector3.zero, Quaternion.identity);
        // containerInstance.name = "Container_Runtime";
    }

    public GameObject SpawnOne()
    {
        GameObject selectedPrefab = containerPrefabs[0];
        GameObject containerInstance = Instantiate(selectedPrefab, Vector3.zero, Quaternion.identity);
        // 初始设为不可见，直到被放置到目标位置，避免在原点短暂或持续可见
        containerInstance.SetActive(false);
        var rotation = new Vector3(0, 90f, 0);
        containerInstance.transform.rotation = Quaternion.Euler(rotation);
        return containerInstance;
    }

    public GameObject SpawnOne(Assets.SingaPort.Group group)
    {
        int colorIdx = group.Index < 0 ? Random.Range(0, containerPrefabs.Length/2) : group.Index % (containerPrefabs.Length/2);
        
        float teuLength = 6.1f;
        float containerHeight = 2.44f;
        float containerWidth = 2.59f;
        float containerLength = teuLength;
        switch (group.TEUs)
        {
            case 1:
                containerLength = teuLength;
                break;
            case 2:
                containerLength = teuLength * 2;
                colorIdx += containerPrefabs.Length/2; // using 40ft prefab
                break;
            default:
                Debug.LogError($"Container Size (group.TEUs) Not Specified: {group.TEUs}");
                break;
        }
        GameObject prefab = containerPrefabs[colorIdx];

        GameObject instance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        // 初始设为不可见，等被放置时由上层激活
        instance.SetActive(false);
        Renderer rend = instance.GetComponentInChildren<Renderer>(); 
        if (rend != null)
        {
            // boundSize 就是素材自带的原始“长宽高”
            Vector3 originalSize = rend.bounds.size;

            float scaleX, scaleY, scaleZ;

            // 假设 Y 轴总是高度（这通常没错）
            scaleY = containerHeight / originalSize.y;

            // 比较 X 和 Z 谁更长，谁长谁就是 Length，短的就是 Width
            if (originalSize.x > originalSize.z)
            {
                // 原始模型 X 是长边
                scaleX = containerLength / originalSize.x; 
                scaleZ = containerWidth / originalSize.z;
            }
            else
            {
                // 原始模型 Z 是长边
                scaleZ = containerLength / originalSize.z;
                scaleX = containerWidth / originalSize.x;
            }

            Debug.Log($"======> Original Size: {originalSize}, Scale: ({scaleX}, {scaleY}, {scaleZ})");
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

        // if (group.Index >= 0)
        // {
        //     AddGroupLabel(instance, group);
        // }
        
        return instance; // 返回生成的 container
    }

    private void AddGroupLabel(GameObject parent, Group group)
    {
        var label = new GameObject("GroupLabel");
        float offsetX = group.TEUs == 1 ? Block.SlotLength / 2 : Block.SlotLength;
        label.transform.SetParent(parent.transform, true);
        label.transform.localPosition = new Vector3(offsetX, 1f, 0f);
        label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        var textMesh = label.AddComponent<TextMesh>();
        textMesh.text = $"G{group.Index}";
        textMesh.color = UnityEngine.Color.yellowNice;
        textMesh.fontSize = 50;
        textMesh.characterSize = 0.2f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
    }
}
