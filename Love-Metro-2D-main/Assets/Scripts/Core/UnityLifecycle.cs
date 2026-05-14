using UnityEngine;

namespace LoveMetro.Core
{
    public static class UnityLifecycle
    {
        public static void SafeDestroy(GameObject go)
        {
            if (go == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(go);
            else
                Object.DestroyImmediate(go);
        }
    }
}
