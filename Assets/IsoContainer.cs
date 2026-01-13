using UnityEngine;

namespace Assets
{
    internal class IsoContainer : MonoBehaviour
    {
        public enum Size { FortyFeet, TwentyFeet }
        public enum Color { Red, Blue, Yellow, White }

        public static GameObject GetObject()
        {
            var sizes = new Size[] { Size.FortyFeet, Size.TwentyFeet };
            return GetObject(sizes[Random.Range(0, sizes.Length)]);
        }

        public static GameObject GetObject(Size size, int groupIdx = -1)
        {
            var colors = new Color[]
            {
                Color.White,
                Color.Blue,
                Color.Red,
                Color.Yellow
            };
            var color = groupIdx < 0
                ? colors[Random.Range(0, colors.Length)]
                : colors[groupIdx % colors.Length];

            var go = GetObject(size, color);

            if (groupIdx >= 0)
            {
                AddGroupLabel(go, groupIdx);
            }

            return go;
        }

        public static GameObject GetObject(Size size, Color color)
        {
            string pathUnderResources = "";
            switch (color)
            {
                case Color.Blue:
                    pathUnderResources = "container_1";
                    break;
                case Color.Red:
                    pathUnderResources = "container_2";
                    break;
                case Color.Yellow:
                    pathUnderResources = "container_3";
                    break;
                case Color.White:
                    pathUnderResources = "container_4";
                    break;
            }

            float length = 6.1f;            
            switch (size)
            {
                case Size.TwentyFeet:
                    length = 6.1f;
                    break;
                case Size.FortyFeet:
                    length = 12.2f;
                    break;
                default:
                    Debug.LogError($"Container Size Not Specified: {size}");
                    break;
            }

            var position = new Vector3(length / 2, 2.44f / 2f , 2.59f / 2);
            var rotation = new Vector3(-90f, 0, 0);
            var scale = new Vector3(length / 12, 2.44f / 4f, 2.59f / 4);

            GameObject go = new GameObject();
            var prefab = Resources.Load<GameObject>(pathUnderResources);
            if (prefab == null)
            {
                Debug.LogError($"Resources prefab not found: {pathUnderResources}");
            }
            else
            {
                var ctn = Instantiate(prefab, position, Quaternion.Euler(rotation));
                ctn.transform.localScale = scale;
                ctn.transform.SetParent(go.transform, true);
            }
            return go;
        }

        static void AddGroupLabel(GameObject parent, int groupIdx)
        {
            var label = new GameObject("GroupLabel");
            label.transform.SetParent(parent.transform, true);
            label.transform.localPosition = new Vector3(0f, 1.6f, 1.6f);
            //label.transform.localRotation = Quaternion.identity;
            label.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            var textMesh = label.AddComponent<TextMesh>();
            textMesh.text = $"G{groupIdx}";
            textMesh.color = UnityEngine.Color.yellowNice;
            textMesh.fontSize = 50;
            textMesh.characterSize = 0.2f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
        }
    }
}
