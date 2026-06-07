using UnityEngine;
using TMPro;

public class ATCManager : MonoBehaviour
{
    public static ATCManager Instance;

    private string selectedAirport = "";
    private Airport currentAirport;
    public TextMeshProUGUI airportNameText;

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
        
        Debug.Log("Selected Airport : " + airport.airportName);
    }

    public string GetSelectedAirport()
    {
        return selectedAirport;
    }
}
