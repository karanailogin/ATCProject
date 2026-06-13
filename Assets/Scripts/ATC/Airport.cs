using UnityEngine;
using System.Collections.Generic;

public class Airport : MonoBehaviour
{
    public string airportName;
    private SpriteRenderer spriteRenderer;
    
    // References for flight display
    public Transform flightListContent;
    public GameObject flightInfoPrefab;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnMouseDown()
    {
        ATCManager.Instance.SelectAirport(this);
        DisplayFlights();
    }

    public void SetSelected(bool selected)
    {
        if (selected)
        {
            spriteRenderer.color = Color.green;
        }
        else
        {
            spriteRenderer.color = Color.white;
        }
    }

    public void DisplayFlights()
    {
        // Validate references
        if (flightListContent == null)
        {
            Debug.LogError($"Airport '{airportName}': flightListContent is not assigned in Inspector!");
            return;
        }

        if (flightInfoPrefab == null)
        {
            Debug.LogError($"Airport '{airportName}': flightInfoPrefab is not assigned in Inspector!");
            return;
        }

        EnsurePanelVisible(flightListContent);

        if (FlightManager.Instance == null)
        {
            Debug.LogError("FlightManager instance is not available. Make sure a FlightManager exists in the scene.");
            return;
        }

        // Clear previous list
        foreach (Transform child in flightListContent)
        {
            Destroy(child.gameObject);
        }

        // Get flights at this airport
        List<Flight> flights = FlightManager.Instance.GetFlightsAtAirport(this);

        if (flights.Count == 0)
        {
            Debug.Log($"No flights at airport: {airportName}");
            return;
        }

        // Instantiate prefab for each flight
        foreach (Flight flight in flights)
        {
            GameObject flightUI = Instantiate(flightInfoPrefab, flightListContent);
            // Populate the flight info
            FlightInfoUI flightInfo = flightUI.GetComponent<FlightInfoUI>();
            if (flightInfo != null)
            {
                flightInfo.SetFlightData(flight);
            }
            else
            {
                Debug.LogError("FlightInfoUI script not found on prefab!");
            }
        }
    }

    private void EnsurePanelVisible(Transform panelTransform)
    {
        Transform current = panelTransform;

        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }

            current = current.parent;
        }
    }
}
