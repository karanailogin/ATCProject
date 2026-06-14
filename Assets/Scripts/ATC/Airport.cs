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

        // 1. Populate Pending Flight Requests for this arrival airport
        PendingFlightRequests.Clear();
        foreach (var f in FlightManager.Instance.AllFlights)
        {
            if (f != null && f.toAirport == airportName && !f.landingApproved && f.state != FlightState.Landed && f.state != FlightState.Diverted)
            {
                PendingFlightRequests.Add(f);
            }
        }

        // 2. Populate Airport Schedule (Approved flights only)
        AirportSchedule.Clear();
        foreach (var f in FlightManager.Instance.AllFlights)
        {
            if (f != null && f.landingApproved)
            {
                if (f.fromAirport == airportName)
                {
                    AirportSchedule.Add(new AirportScheduleEvent
                    {
                        FlightID = f.flightID,
                        FlightNumber = f.flightName,
                        EventType = AirportEventType.Departure,
                        AirportCode = f.fromAirport,
                        OtherAirportCode = f.toAirport,
                        EventTime = f.departureTime
                    });
                }
                if (f.toAirport == airportName)
                {
                    AirportSchedule.Add(new AirportScheduleEvent
                    {
                        FlightID = f.flightID,
                        FlightNumber = f.flightName,
                        EventType = AirportEventType.Arrival,
                        AirportCode = f.toAirport,
                        OtherAirportCode = f.fromAirport,
                        EventTime = f.arrivalTime
                    });
                }
            }
        }

        // Sort Schedule chronologically by event time ascending
        AirportSchedule.Sort((e1, e2) => ParseTimeToMinutes(e1.EventTime).CompareTo(ParseTimeToMinutes(e2.EventTime)));

        // Organize schedule to display upcoming events first
        float currentMinutes = 360f;
        if (WorldClockManager.Instance != null)
        {
            currentMinutes = WorldClockManager.Instance.currentVirtualTime;
        }
        else if (FlightManager.Instance != null)
        {
            currentMinutes = FlightManager.Instance.currentVirtualTime;
        }

        List<AirportScheduleEvent> orderedSchedule = new List<AirportScheduleEvent>();
        List<AirportScheduleEvent> upcomingEvents = new List<AirportScheduleEvent>();
        List<AirportScheduleEvent> pastEvents = new List<AirportScheduleEvent>();

        foreach (var ev in AirportSchedule)
        {
            if (ParseTimeToMinutes(ev.EventTime) >= currentMinutes)
            {
                upcomingEvents.Add(ev);
            }
            else
            {
                pastEvents.Add(ev);
            }
        }
        orderedSchedule.AddRange(upcomingEvents);
        orderedSchedule.AddRange(pastEvents);

        // ================= BUILD UI USING PREFAB =================

        // Section A: Pending Flight Requests (Only visible when requests exist)
        if (PendingFlightRequests.Count > 0)
        {
            CreateHeader("Pending Flight Requests");
            foreach (var req in PendingFlightRequests)
            {
                CreateRequestRow(req);
            }
        }

        // Section B: Unified Chronological Schedule Section
        CreateHeader("Airport Schedule");
        if (orderedSchedule.Count == 0)
        {
            CreateNoSchedulePlaceholder();
        }
        else
        {
            foreach (var ev in orderedSchedule)
            {
                CreateScheduleEventRow(ev);
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
        if (WorldClockManager.Instance != null)
        {
            sb.Append(WorldClockManager.Instance.currentVirtualTime.ToString("F0"));
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
}
