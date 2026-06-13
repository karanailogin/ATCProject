using UnityEngine;
using UnityEngine.UI;

public class ClickLogger : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("[ClickLogger] Initialized. Finding all buttons in the scene...");
        var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
        Debug.Log($"[ClickLogger] Found {buttons.Length} buttons total in the scene.");
        
        foreach (var btn in buttons)
        {
            string path = GetGameObjectPath(btn.gameObject);
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"[ClickLogger] BUTTON CLICKED: '{path}' (interactable: {btn.interactable}, activeInHierarchy: {btn.gameObject.activeInHierarchy})");
            });
        }
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = obj.name + "/" + path;
        }
        return path;
    }
}