using UnityEngine;
using System.IO;

public class PlaymodeDiag : MonoBehaviour
{
    private int frames = 0;

    private void Start()
    {
        Debug.Log("[PlaymodeDiag] Started.");
    }

    private void Update()
    {
        frames++;
        if (frames == 30) // Wait for scene to settle
        {
            RunCheck();
        }
    }

    private void RunCheck()
    {
        string logPath = Path.Combine(Application.dataPath, "../diag_output.txt");
        using (StreamWriter sw = new StreamWriter(logPath, false))
        {
            sw.WriteLine("--- RUNTIME DIAGNOSIS START ---");

            var tbc = TopBarController.Instance;
            if (tbc == null)
            {
                sw.WriteLine("TopBarController.Instance is null!");
            }
            else
            {
                sw.WriteLine("TopBarController.Instance is found.");
                sw.WriteLine($"  attentionButton field: {tbc.attentionButton != null}");
            }

            var panel = Object.FindAnyObjectByType<PendingFlightsPanel>(FindObjectsInactive.Include);
            if (panel == null)
            {
                sw.WriteLine("FindAnyObjectByType<PendingFlightsPanel>(Include) is null!");
            }
            else
            {
                sw.WriteLine($"FindAnyObjectByType<PendingFlightsPanel>(Include) found: '{panel.name}'");
                sw.WriteLine($"  activeSelf: {panel.gameObject.activeSelf}");
                sw.WriteLine($"  activeInHierarchy: {panel.gameObject.activeInHierarchy}");
            }

            sw.WriteLine("--- RUNTIME DIAGNOSIS END ---");
        }

// #if UNITY_EDITOR
//         UnityEditor.EditorApplication.ExitPlaymode();
// #endif
    }
}
