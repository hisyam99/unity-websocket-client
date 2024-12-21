using UnityEngine;

public class GPUCheck : MonoBehaviour
{
    void Start()
    {
        print(SystemInfo.graphicsDeviceName);
    }
}