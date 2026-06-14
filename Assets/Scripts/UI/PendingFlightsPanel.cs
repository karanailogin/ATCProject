using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PendingFlightsPanel : MonoBehaviour
{
    public static PendingFlightsPanel Instance;

    [Header("References")]
    public Transform flightListContent;
    public GameObject flightInfoPrefab;
    public GameObject airportInfoPanel;
    public GameObject activeFlightsPanel;
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
        DisplayPendingFlights();
    }

    private void Update()
    {
        // If AirportInfoPanel is active, automatically hide this panel
        if (airportInfoPanel != null && airportInfoPanel.activeSelf)
        {
            gameObject.SetActive(false);
            return;
        }

        // Dynamically refresh list of pending flights
        RefreshListIfNeeded();
    }

    public void ShowPanel()
    {
        if (airportInfoPanel != null)
        {
            airportInfoPanel.SetActive(false);
        }
        if (activeFlightsPanel != null)
        {
            activeFlightsPanel.SetActive(false);
        }
        else if (ActiveFlightsPanel.Instance != null)
        {
            ActiveFlightsPanel.Instance.gameObject.SetActive(false);
        }
        gameObject.SetActive(true);
        DisplayPendingFlights();
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

    private int lastPendingCount = -1;
    private HashSet<string> lastStatesHash = new HashSet<string>();

    private void RefreshListIfNeeded()
    {
        if (FlightManager.Instance == null) return;

        List<Flight> pendingFlights = GetPendingFlights();
        bool needsRefresh = false;

        if (pendingFlights.Count != lastPendingCount)
        {
            needsRefresh = true;
        }
        else
        {
            // Check if any flight's status string changed
            foreach (var f in pendingFlights)
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
            DisplayPendingFlights();
        }
    }

    private List<Flight> GetPendingFlights()
    {
        List<Flight> pending = new List<Flight>();
        if (FlightManager.Instance != null)
        {
            foreach (var flight in FlightManager.Instance.AllFlights)
            {
                if (flight != null)
                {
                    if (flight.state == FlightState.SlotConflict ||
                        flight.state == FlightState.FlightCreated ||
                        flight.state == FlightState.Holding ||
                        flight.state == FlightState.Diverted)
                    {
                        pending.Add(flight);
                    }
                }
            }
        }
        return pending;
    }

    public void DisplayPendingFlights()
    {
        if (flightListContent == null || flightInfoPrefab == null) return;

        // Clear existing children
        foreach (Transform child in flightListContent)
        {
            Destroy(child.gameObject);
        }

        List<Flight> pendingFlights = GetPendingFlights();
        lastPendingCount = pendingFlights.Count;
        lastStatesHash.Clear();

        foreach (Flight flight in pendingFlights)
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
