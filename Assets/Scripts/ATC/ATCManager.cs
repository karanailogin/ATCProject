using UnityEngine;
using TMPro;

public class ATCManager : MonoBehaviour
{
    public static ATCManager Instance;

    private string selectedAirport = "";
    private Airport currentAirport;
    public TextMeshProUGUI airportNameText;

    public Airport CurrentAirport => currentAirport;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("[ATCManager] Start() called. Hooking up ViewScheduleButton click listener...");
        var btnGo = GameObject.Find("MainCanvas/AirportInfoPanel/AirportNamePanel/ViewScheduleButton");
        if (btnGo != null)
        {
            Debug.Log("[ATCManager] ViewScheduleButton GameObject found successfully.");
            var btn = btnGo.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                Debug.Log("[ATCManager] Button component found on ViewScheduleButton. Adding onClick listener.");
                btn.onClick.AddListener(() =>
                {
                    Debug.Log($"[ATCManager] ViewScheduleButton CLICKED! Current selected airport is: {(currentAirport != null ? currentAirport.airportName : "NULL")}");
                    if (currentAirport != null)
                    {
                        Debug.Log($"[ATCManager] Requesting AirportSchedulePanel.ShowSchedule for: {currentAirport.airportName}");
                        AirportSchedulePanel.ShowSchedule(currentAirport);
                    }
                    else
                    {
                        Debug.LogWarning("[ATCManager] Cannot show schedule panel because no airport is currently selected!");
                    }
                });
            }
            else
            {
                Debug.LogError("[ATCManager] ViewScheduleButton was found, but it is missing the UnityEngine.UI.Button component!");
            }
        }
        else
        {
            Debug.LogError("[ATCManager] Could not find ViewScheduleButton GameObject at path 'MainCanvas/AirportInfoPanel/AirportNamePanel/ViewScheduleButton'!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SelectAirport(Airport airport)
    {
        // Deselect previous airport
        if (currentAirport != null)
        {
            currentAirport.SetSelected(false);
        }

        // Store and select new airport
        currentAirport = airport;
        currentAirport.SetSelected(true);
        
        selectedAirport = airport.airportName;
        
        // Update UI text
        if (airportNameText != null)
        {
            airportNameText.text = airport.airportName;
        }

        // Close flight details when selecting a new airport
        FlightDetailsPanel.CloseFlightDetails();
        
        Debug.Log("Selected Airport : " + airport.airportName);
    }

    public string GetSelectedAirport()
    {
        return selectedAirport;
    }
}
