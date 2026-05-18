using UnityEngine;

public class StartupLog : MonoBehaviour
{
    [ContextMenu("Log Startup Marker")]
    private void LogStartupMarker()
    {
        LogMarker();
    }

    public static void LogMarker()
    {
        Debug.Log("===============================================================");
        Debug.Log("<<<<< SCRIPT COMPILATION AND EXECUTION IS WORKING >>>>>");
        Debug.Log("===============================================================");
    }
}


