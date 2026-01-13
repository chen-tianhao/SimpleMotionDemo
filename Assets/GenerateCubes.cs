using UnityEngine;

public class GenerateCubes : MonoBehaviour
{
    public int count = 100;        // 生成数量
    public float spacing = 10f;    // 间距（米）

    void Start()
    {
        for (int i = 0; i < count; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

            // 位置：沿 X 轴，每两个 cube 相距 spacing
            cube.transform.position = new Vector3(i * spacing, 0f, 0f);

            // （可选）给个名字，方便调试
            cube.name = $"Cube_{i}";
        }
    }
}
