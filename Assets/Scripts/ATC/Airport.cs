using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class Airport : MonoBehaviour
{
    public string airportName;
    public string countryName = "INDIA";
    private SpriteRenderer spriteRenderer;
    
    // References for flight display
    public Transform flightListContent;
    public GameObject flightInfoPrefab;

    [Header("Airport Custom Panel Prefabs")]
    public GameObject airportPanelArrivalPrefab;
    public GameObject airportPanelDeparturePrefab;

    [Header("Airport Data Collections")]
    public List<Flight> PendingFlightRequests = new List<Flight>();
    public List<AirportScheduleEvent> AirportSchedule = new List<AirportScheduleEvent>();

    private float lastCheckTime = 0f;
    private int lastFlightCount = -1;
    private int lastPendingCount = -1;
    private string lastStatesHash = "";

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnMouseDown()
    {
        ATCManager.Instance.SelectAirport(this);
        DisplayFlights();
    }

    private void Update()
    {
        if (ATCManager.Instance != null && ATCManager.Instance.CurrentAirport == this)
        {
            // Only refresh if the panel is active in hierarchy
            if (flightListContent != null && flightListContent.gameObject.activeInHierarchy)
            {
                if (Time.time - lastCheckTime > 0.5f) // Throttle checks for high efficiency
                {
                    lastCheckTime = Time.time;
                    string currentHash = GetStatesHash();
                    int pendingCount = PendingFlightRequests.Count;
                    int scheduleCount = AirportSchedule.Count;

                    if (pendingCount != lastPendingCount || scheduleCount != lastFlightCount || currentHash != lastStatesHash)
                    {
                        lastPendingCount = pendingCount;
                        lastFlightCount = scheduleCount;
                        lastStatesHash = currentHash;
                        DisplayFlights();
                    }
                }
            }
        }
    }

    public void SetSelected(bool selected)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = selected ? Color.green : Color.white;
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

        // 1. Resolve Filter Index from Dropdown: 0 = All Flights, 1 = Departure, 2 = Arrival
        int filterIndex = 0;
        var dropdownGo = GameObject.Find("MainCanvas/AirportInfoPanel/AirportNamePanel/FlightFilterDropdown");
        if (dropdownGo != null)
        {
            var dropdown = dropdownGo.GetComponent<TMP_Dropdown>();
            if (dropdown != null)
            {
                filterIndex = dropdown.value;
            }
        }

        // 2. Gather and sort timeline events
        List<TimelineEvent> events = new List<TimelineEvent>();

        foreach (var f in FlightManager.Instance.AllFlights)
        {
            if (f != null)
            {
                if (f.fromAirport == airportName && (filterIndex == 0 || filterIndex == 1))
                {
                    events.Add(new TimelineEvent
                    {
                        flight = f,
                        isArrival = false,
                        sortMinutes = GetFlightTakeoffMinutes(f),
                        displayTime = f.departureTime
                    });
                }
                if (f.toAirport == airportName && (filterIndex == 0 || filterIndex == 2))
                {
                    int arrMins = GetFlightLandingMinutes(f);
                    int depMins = GetFlightTakeoffMinutes(f);
                    bool isNextDay = arrMins < depMins;

                    events.Add(new TimelineEvent
                    {
                        flight = f,
                        isArrival = true,
                        sortMinutes = isNextDay ? (arrMins + 1440) : arrMins,
                        displayTime = isNextDay ? $"+{f.arrivalTime}" : f.arrivalTime
                    });
                }
            }
        }

        // Sort events chronologically by sortMinutes ascending
        events.Sort((e1, e2) => e1.sortMinutes.CompareTo(e2.sortMinutes));

        // 3. Render sorted timeline events
        if (events.Count > 0)
        {
            foreach (var ev in events)
            {
                if (ev.isArrival)
                {
                    GameObject usePrefab = airportPanelArrivalPrefab != null ? airportPanelArrivalPrefab : flightInfoPrefab;
                    GameObject cardGo = Instantiate(usePrefab, flightListContent);
                    cardGo.name = $"ArrivingCard_{ev.flight.flightName}";
                    var cardUI = cardGo.GetComponent<FlightInfoUI>();
                    if (cardUI != null)
                    {
                        cardUI.SetArrivingFlightData(ev.flight, ev.displayTime);
                    }
                }
                else
                {
                    GameObject usePrefab = airportPanelDeparturePrefab != null ? airportPanelDeparturePrefab : flightInfoPrefab;
                    GameObject cardGo = Instantiate(usePrefab, flightListContent);
                    cardGo.name = $"DepartingCard_{ev.flight.flightName}";
                    var cardUI = cardGo.GetComponent<FlightInfoUI>();
                    if (cardUI != null)
                    {
                        cardUI.SetDepartingFlightData(ev.flight, ev.displayTime);
                    }
                }
            }
        }

        // Force rebuild of vertical layout and content fitter
        var rt = flightListContent.GetComponent<RectTransform>();
        if (rt != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }

    private void CreateHeader(string titleText)
    {
        GameObject headerUI = Instantiate(flightInfoPrefab, flightListContent);
        FlightInfoUI info = headerUI.GetComponent<FlightInfoUI>();
        if (info != null)
        {
            info.SetHeaderData(titleText);
        }
    }

    private void CreateRequestRow(Flight req)
    {
        GameObject requestUI = Instantiate(flightInfoPrefab, flightListContent);
        FlightInfoUI info = requestUI.GetComponent<FlightInfoUI>();
        if (info != null)
        {
            info.SetRequestData(req, () => ProcessAccept(req), () => ProcessReject(req));
        }
    }

    private void CreateScheduleEventRow(AirportScheduleEvent ev)
    {
        GameObject eventUI = Instantiate(flightInfoPrefab, flightListContent);
        FlightInfoUI info = eventUI.GetComponent<FlightInfoUI>();
        if (info != null)
        {
            info.SetEventData(ev, () => {
                if (FlightManager.Instance != null)
                {
                    Flight matchedFlight = FlightManager.Instance.AllFlights.Find(f => f.flightName == ev.FlightNumber);
                    if (matchedFlight != null)
                    {
                        FlightInfoUI.selectedFlight = matchedFlight;
                        FlightDetailsPanel.ShowFlightDetails(matchedFlight);
                        
                        var sidebar = FindAnyObjectByType<FlightDetailsSidebar>();
                        if (sidebar != null)
                        {
                            sidebar.ShowFlightDetails(matchedFlight);
                        }
                    }
                }
            });
        }
    }

    private void CreateNoSchedulePlaceholder()
    {
        GameObject placeholderUI = Instantiate(flightInfoPrefab, flightListContent);
        FlightInfoUI info = placeholderUI.GetComponent<FlightInfoUI>();
        if (info != null)
        {
            info.SetHeaderData("No scheduled events.");
        }
    }

    public void ProcessAccept(Flight f)
    {
        if (f == null) return;
        Debug.Log($"[AirportInfoPanel] Accept clicked for flight: {f.flightName}");

        if (TimeSlotManager.Instance != null && f.landingSlot != null)
        {
            if (!f.landingSlot.isBooked)
            {
                TimeSlotManager.Instance.BookTimeSlot(f.toAirport, f.landingSlot, f.flightName);
            }
        }

        f.landingApproved = true;

        DisplayFlights();

        if (AirportSchedulePanel.Instance != null && AirportSchedulePanel.Instance.panelContainer.activeSelf)
        {
            AirportSchedulePanel.Instance.RefreshAll();
        }
    }

    public void ProcessReject(Flight f)
    {
        if (f == null) return;
        Debug.Log($"[AirportInfoPanel] Reject clicked for flight: {f.flightName}");

        if (TimeSlotManager.Instance != null && f.landingSlot != null)
        {
            TimeSlotManager.Instance.ReleaseTimeSlot(f.toAirport, f.landingSlot);
            f.landingSlot = null;
        }

        f.landingApproved = false;
        f.state = FlightState.FlightCreated;
        f.expectedArrival = "NA";
        f.arrivalTime = "NA";

        DisplayFlights();

        if (AirportSchedulePanel.Instance != null && AirportSchedulePanel.Instance.panelContainer.activeSelf)
        {
            AirportSchedulePanel.Instance.RefreshAll();
        }
    }

    private int ParseTimeToMinutes(string timeStr)
    {
        if (string.IsNullOrEmpty(timeStr) || !timeStr.Contains(":")) return 0;
        var parts = timeStr.Split(':');
        if (parts.Length >= 2 && int.TryParse(parts[0], out int h) && int.TryParse(parts[1], out int m))
        {
            return h * 60 + m;
        }
        return 0;
    }

    private string GetStatesHash()
    {
        if (FlightManager.Instance == null) return "";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var f in FlightManager.Instance.AllFlights)
        {
            if (f != null)
            {
                sb.Append(f.flightName).Append("_").Append(f.state).Append("_").Append(f.status).Append("_");
                if (f.takeoffSlot != null) sb.Append(f.takeoffSlot.GetTimeString()).Append("_");
                if (f.landingSlot != null) sb.Append(f.landingSlot.GetTimeString()).Append("_");
            }
        }
        // Append dropdown value to states hash for automatic, responsive rebuilding
        var dropdownGo = GameObject.Find("MainCanvas/AirportInfoPanel/AirportNamePanel/FlightFilterDropdown");
        if (dropdownGo != null)
        {
            var dropdown = dropdownGo.GetComponent<TMP_Dropdown>();
            if (dropdown != null)
            {
                sb.Append("_drop_").Append(dropdown.value);
            }
        }
        return sb.ToString();
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

    private int GetFlightTakeoffMinutes(Flight f)
    {
        if (f.takeoffSlot != null) return f.takeoffSlot.GetTotalMinutes();
        return ParseTimeToMinutes(f.departureTime);
    }

    private int GetFlightLandingMinutes(Flight f)
    {
        if (f.landingSlot != null) return f.landingSlot.GetTotalMinutes();
        return ParseTimeToMinutes(f.arrivalTime);
    }

    private class TimelineEvent
    {
        public Flight flight;
        public bool isArrival;
        public int sortMinutes;
        public string displayTime;
    }
}
