using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ActiveFlightsPanel : MonoBehaviour
{
    public static ActiveFlightsPanel Instance;

    [Header("References")]
    public Transform flightListContent;
    public GameObject flightInfoPrefab;
    public GameObject airportInfoPanel;
    public GameObject pendingFlightsPanel;
    public Button closeButton;

    private void Awake()
    {
        if (Instance == null || Instance == this)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    private void OnEnable()
    {
        DisplayActiveFlights();
    }

    private void Update()
    {
        // If AirportInfoPanel is active, automatically hide this panel
        if (airportInfoPanel != null && airportInfoPanel.activeSelf)
        {
            gameObject.SetActive(false);
            return;
        }

        // Dynamically refresh list of active flights
        RefreshListIfNeeded();
    }

    public void ShowPanel()
    {
        if (airportInfoPanel != null)
        {
            airportInfoPanel.SetActive(false);
        }
        if (pendingFlightsPanel != null)
        {
            pendingFlightsPanel.SetActive(false);
        }
        else if (PendingFlightsPanel.Instance != null)
        {
            PendingFlightsPanel.Instance.gameObject.SetActive(false);
        }
        gameObject.SetActive(true);
        DisplayActiveFlights();
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
        if (airportInfoPanel != null && ATCManager.Instance != null && ATCManager.Instance.CurrentAirport != null)
        {
            airportInfoPanel.SetActive(true);
            ATCManager.Instance.CurrentAirport.DisplayFlights();
        }
    }

    private int lastActiveCount = -1;
    private HashSet<string> lastStatesHash = new HashSet<string>();

    private void RefreshListIfNeeded()
    {
        if (FlightManager.Instance == null) return;

        List<Flight> activeFlights = GetActiveFlights();
        bool needsRefresh = false;

        if (activeFlights.Count != lastActiveCount)
        {
            needsRefresh = true;
        }
        else
        {
            // Check if any flight's status string changed
            foreach (var f in activeFlights)
            {
                string key = f.flightName + "_" + f.state + "_" + f.status;
                if (!lastStatesHash.Contains(key))
                {
                    needsRefresh = true;
                    break;
                }
            }
        }

        if (needsRefresh)
        {
            DisplayActiveFlights();
        }
    }

    private List<Flight> GetActiveFlights()
    {
        List<Flight> active = new List<Flight>();
        if (FlightManager.Instance != null)
        {
            foreach (var flight in FlightManager.Instance.AllFlights)
            {
                if (flight != null)
                {
                    if (flight.state != FlightState.Landed && flight.state != FlightState.Diverted)
                    {
                        active.Add(flight);
                    }
                }
            }
        }
        return active;
    }

    public void DisplayActiveFlights()
    {
        if (flightListContent == null || flightInfoPrefab == null) return;

        // Clear existing children
        foreach (Transform child in flightListContent)
        {
            Destroy(child.gameObject);
        }

        List<Flight> activeFlights = GetActiveFlights();
        lastActiveCount = activeFlights.Count;
        lastStatesHash.Clear();

        foreach (Flight flight in activeFlights)
        {
            GameObject flightUI = Instantiate(flightInfoPrefab, flightListContent);
            FlightInfoUI flightInfo = flightUI.GetComponent<FlightInfoUI>();
            if (flightInfo != null)
            {
                flightInfo.SetFlightData(flight);
            }

            // Track state for next update checks
            string key = flight.flightName + "_" + flight.state + "_" + flight.status;
            lastStatesHash.Add(key);
        }
    }
}
