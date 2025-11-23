using UnityEngine;

public class StartupLog : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void LogOnStartup()
    {
        Debug.Log("===============================================================");
        Debug.Log("<<<<< SCRIPT COMPILATION AND EXECUTION IS WORKING >>>>>");
        Debug.Log("===============================================================");
    }
}


