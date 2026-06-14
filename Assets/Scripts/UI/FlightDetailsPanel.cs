using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class FlightDetailsPanel : MonoBehaviour
{
    [Header("Flight Header Details")]
    public TMP_Text flightNumberText;
    public TMP_Text routeText;
    public TMP_Text depCountryText;
    public TMP_Text arrCountryText;
    public TMP_Text expectedDepText;
    public TMP_Text expectedArrText;
    public TMP_Text aircraftTypeText;
    public TMP_Text durationText;

    [Header("Separate Route Details")]
    public TMP_Text depAirportCodeText;
    public TMP_Text arrAirportCodeText;
    public TMP_Text routeArrowText;

    [Header("Flight Status Display")]
    public TMP_Text flightStatusText;

    [Header("Status Styling Colors")]
    public Color chipActiveBgColor = new Color(0.14f, 0.65f, 0.31f); // Emerald Green
    public Color chipInactiveBgColor = new Color(0.18f, 0.18f, 0.20f); // Dark Charcoal
    public Color chipActiveTextColor = Color.white;
    public Color chipInactiveTextColor = new Color(0.65f, 0.65f, 0.68f);

    [Header("Editable Info Rows")]
    public Button scheduledDepRowButton;
    public TMP_Text scheduledDepValueText;
    public UnityEngine.UI.Image scheduledDepValueBg;
    public TMP_Text conflictWarningText;

    public Button scheduledArrRowButton;
    public TMP_Text scheduledArrValueText;
    public UnityEngine.UI.Image scheduledArrValueBg;

    [Header("Slot Status Colors")]
    public Color statusApprovedColor = new Color(0.13f, 0.77f, 0.36f); // Green
    public Color statusPendingColor = new Color(0.92f, 0.70f, 0.08f);  // Yellow
    public Color statusRejectedColor = new Color(0.94f, 0.27f, 0.27f); // Red
    public Color statusConflictColor = new Color(0.98f, 0.45f, 0.09f); // Orange
    public Color statusNoSlotColor = new Color(0.55f, 0.55f, 0.58f);   // Gray

    [Header("Bottom Action Bar Buttons")]
    public Button discardActionBarButton;
    public Button approveActionBarButton;

    [Header("Time Picker Popup Modal")]
    public GameObject timePickerPopup;
    public TMP_Text timePickerTitleText;
    public Transform hourTabsContainer;
    public Transform timeSlotsGridContainer;
    public GameObject hourTabPrefab;
    public GameObject timeSlotPrefab;
    public Button timePickerCancelButton;
    public Button timePickerConfirmButton;

    [Header("Panel Containment")]
    public GameObject panelContainer;

    private static FlightDetailsPanel instance;
    private Flight currentFlight;

    // Temporary values for pending changes
    private string tempStatus;
    private int tempDepHour;
    private int tempDepMinute;
    private int tempArrHour;
    private int tempArrMinute;

    // Internal state management for modals
    private bool isPickingDepartureTime;
    private int modalSelectedHour;
    private int modalSelectedMinute;

    private void Awake()
    {
        if (instance == null || instance == this)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize button listeners
        SetupInfoRowListeners();
        SetupPopupActionListeners();
    }

    private void Start()
    {
        if (currentFlight == null)
        {
            ClosePanel();
        }
    }

    public static void ShowFlightDetails(Flight flight)
    {
        if (instance == null)
        {
            var panels = FindObjectsByType<FlightDetailsPanel>(FindObjectsInactive.Include);
            if (panels.Length > 0)
            {
                instance = panels[0];
            }
        }

        if (instance == null)
        {
            Debug.LogError("FlightDetailsPanel not found in scene.");
            return;
        }

        instance.DisplayFlight(flight);
    }

    public static void CloseFlightDetails()
    {
        if (instance == null)
        {
            var panels = FindObjectsByType<FlightDetailsPanel>(FindObjectsInactive.Include);
            if (panels.Length > 0)
            {
                instance = panels[0];
            }
        }

        if (instance != null)
        {
            instance.ClosePanel();
        }
    }

    private void DisplayFlight(Flight flight)
    {
        currentFlight = flight;

        // Initialize temporary draft values from flight model
        tempStatus = flight.status;

        // Extract departure hours/minutes
        if (flight.takeoffSlot != null)
        {
            tempDepHour = flight.takeoffSlot.hours;
            tempDepMinute = flight.takeoffSlot.minutes;
        }
        else
        {
            tempDepHour = 12;
            tempDepMinute = 30;
        }

        // Extract arrival hours/minutes
        if (flight.landingSlot != null)
        {
            tempArrHour = flight.landingSlot.hours;
            tempArrMinute = flight.landingSlot.minutes;
        }
        else
        {
            tempArrHour = 14;
            tempArrMinute = 30;
        }

        // Update Text Elements in Header Section
        if (flightNumberText != null) flightNumberText.text = flight.flightName;
        if (routeText != null) routeText.text = $"{flight.fromAirport}  →  {flight.toAirport}";

        string depCountry = "";
        string arrCountry = "";

        var depAirport = FindAirportByName(flight.fromAirport);
        if (depAirport != null)
        {
            depCountry = depAirport.countryName;
        }
        else
        {
            depCountry = GetCountryName(flight.fromAirport);
        }

        var arrAirport = FindAirportByName(flight.toAirport);
        if (arrAirport != null)
        {
            arrCountry = arrAirport.countryName;
        }
        else
        {
            arrCountry = GetCountryName(flight.toAirport);
        }

        if (depCountryText != null) depCountryText.text = depCountry;
        if (arrCountryText != null) arrCountryText.text = arrCountry;
        if (depAirportCodeText != null) depAirportCodeText.text = flight.fromAirport;
        if (arrAirportCodeText != null) arrAirportCodeText.text = flight.toAirport;
        if (routeArrowText != null) routeArrowText.text = "→";

        if (aircraftTypeText != null) aircraftTypeText.text = flight.aircraftType;
        if (durationText != null) durationText.text = $"{flight.flightDurationMinutes} mins";

        RefreshHeaderExpectedTimes();
        RefreshFlightStatusDisplay();
        RefreshInfoRowsDisplay();

        // Close any residual open popups
        CloseAllPopups();

        if (panelContainer != null)
        {
            panelContainer.SetActive(true);
        }
    }

    private Airport FindAirportByName(string code)
    {
        if (string.IsNullOrEmpty(code)) return null;
        var airports = FindObjectsByType<Airport>(FindObjectsInactive.Include);
        foreach (var ap in airports)
        {
            if (ap.airportName.Equals(code, System.StringComparison.OrdinalIgnoreCase))
            {
                return ap;
            }
        }
        return null;
    }

    private void RefreshHeaderExpectedTimes()
    {
        if (expectedDepText != null)
        {
            expectedDepText.text = $"Expected Departure: {tempDepHour:D2}:{tempDepMinute:D2}";
        }
        if (expectedArrText != null)
        {
            expectedArrText.text = $"Expected Arrival: {tempArrHour:D2}:{tempArrMinute:D2}";
        }
    }

    private string GetCountryName(string airportCode)
    {
        if (string.IsNullOrEmpty(airportCode)) return "UNKNOWN";
        switch (airportCode.ToUpper())
        {
            case "MUM":
            case "DEL":
            case "BLR":
                return "INDIA";
            case "LHR": return "UNITED KINGDOM";
            case "JFK": return "UNITED STATES";
            case "DXB": return "UNITED ARAB EMIRATES";
            case "SIN": return "SINGAPORE";
            case "HND": return "JAPAN";
            default: return "INDIA";
        }
    }

    private void Update()
    {
        if (panelContainer != null && panelContainer.activeSelf)
        {
            RefreshFlightStatusDisplay();
            RefreshHeaderExpectedTimes();
        }
    }

    private void RefreshFlightStatusDisplay()
    {
        if (flightStatusText != null && currentFlight != null)
        {
            currentFlight.UpdateStatus();
            flightStatusText.text = currentFlight.status;

            // State styling colors matching the aviation layout
            Color statusColor = statusPendingColor; // Yellow/Amber default
            if (currentFlight.state == FlightState.Landed || currentFlight.state == FlightState.ArrivalApproved)
            {
                statusColor = statusApprovedColor; // Green
            }
            else if (currentFlight.state == FlightState.EnRoute || currentFlight.state == FlightState.Arriving)
            {
                statusColor = new Color(0.2f, 0.6f, 1.0f, 1.0f); // Sky Blue
            }
            else if (currentFlight.state == FlightState.Diverted)
            {
                statusColor = statusRejectedColor; // Red
            }
            flightStatusText.color = statusColor;
        }
    }

    private void RefreshInfoRowsDisplay()
    {
        if (scheduledDepValueText != null) scheduledDepValueText.text = $"{tempDepHour:D2}:{tempDepMinute:D2}";
        if (scheduledArrValueText != null) scheduledArrValueText.text = $"{tempArrHour:D2}:{tempArrMinute:D2}";

        if (scheduledDepRowButton != null && currentFlight != null)
        {
            var label = scheduledDepRowButton.transform.Find("Label")?.GetComponent<TMP_Text>();
            if (label != null)
            {
                label.text = $"Request DEP ({currentFlight.fromAirport})";
            }
        }
        if (scheduledArrRowButton != null && currentFlight != null)
        {
            var label = scheduledArrRowButton.transform.Find("Label")?.GetComponent<TMP_Text>();
            if (label != null)
            {
                label.text = $"Request ARR ({currentFlight.toAirport})";
            }
        }

        // Update badge/box colors and styles dynamically
        if (currentFlight != null)
        {
            UpdateSlotValueBadge(scheduledDepValueBg, scheduledDepValueText, currentFlight.fromAirport, tempDepHour, tempDepMinute, true);
            UpdateSlotValueBadge(scheduledArrValueBg, scheduledArrValueText, currentFlight.toAirport, tempArrHour, tempArrMinute, false);
        }
        else
        {
            if (scheduledDepValueBg != null) scheduledDepValueBg.color = statusNoSlotColor;
            if (scheduledDepValueText != null) scheduledDepValueText.color = Color.white;

            if (scheduledArrValueBg != null) scheduledArrValueBg.color = statusNoSlotColor;
            if (scheduledArrValueText != null) scheduledArrValueText.color = Color.white;
        }

        // Dynamic warning message and layout adjustments
        bool hasDepConflict = false;
        if (currentFlight != null && FlightManager.Instance != null)
        {
            hasDepConflict = (currentFlight.state == FlightState.SlotConflict && tempDepHour == currentFlight.takeoffSlot?.hours && tempDepMinute == currentFlight.takeoffSlot?.minutes);
        }

        if (hasDepConflict)
        {
            // Position ArrRow lower to make room for warning
            if (scheduledArrRowButton != null)
            {
                var rtArr = scheduledArrRowButton.GetComponent<RectTransform>();
                rtArr.anchoredPosition = new Vector2(rtArr.anchoredPosition.x, -118f);
            }

            // Show conflict warning text
            if (conflictWarningText != null)
            {
                conflictWarningText.gameObject.SetActive(true);
                string conflictInfo = FlightManager.Instance.GetConflictingFlightInfo(currentFlight);
                conflictWarningText.text = $"<color=#EF4444>[CONFLICT] with {conflictInfo}</color>";
            }

            // Also make sure the value bg is red
            if (scheduledDepValueBg != null)
            {
                scheduledDepValueBg.color = statusRejectedColor;
            }
            if (scheduledDepValueText != null)
            {
                scheduledDepValueText.color = Color.white;
            }
        }
        else
        {
            // Reset ArrRow position
            if (scheduledArrRowButton != null)
            {
                var rtArr = scheduledArrRowButton.GetComponent<RectTransform>();
                rtArr.anchoredPosition = new Vector2(rtArr.anchoredPosition.x, -98f);
            }

            // Hide warning
            if (conflictWarningText != null)
            {
                conflictWarningText.gameObject.SetActive(false);
            }
        }

        // Adjust DataSection sizeDelta based on conflict presence
        var dataSection = transform.Find("DataSection");
        if (dataSection != null)
        {
            var rtData = dataSection.GetComponent<RectTransform>();
            rtData.sizeDelta = new Vector2(rtData.sizeDelta.x, hasDepConflict ? 210f : 190f);
        }

        // Disable requesting approval if there is a slot conflict
        if (approveActionBarButton != null)
        {
            approveActionBarButton.interactable = !hasDepConflict;
        }
    }

    private void UpdateSlotValueBadge(UnityEngine.UI.Image bg, TMP_Text txt, string airportCode, int hour, int minute, bool isDeparture)
    {
        if (bg == null || txt == null) return;

        if (currentFlight == null)
        {
            bg.color = statusNoSlotColor;
            txt.color = Color.white;
            return;
        }

        Color bgColor = statusNoSlotColor;
        Color textColor = Color.white;

        // Determine if there is a conflict:
        bool hasConflict = false;
        if (isDeparture && currentFlight != null)
        {
            if (currentFlight.state == FlightState.SlotConflict && hour == currentFlight.takeoffSlot?.hours && minute == currentFlight.takeoffSlot?.minutes)
            {
                hasConflict = true;
            }
        }

        if (hasConflict)
        {
            bgColor = statusRejectedColor; // Red
            textColor = Color.white;
        }
        else if (isDeparture)
        {
            if (currentFlight.takeoffSlot == null)
            {
                bgColor = statusNoSlotColor;
                textColor = Color.white;
            }
            else if (currentFlight.takeoffSlot.hours == hour && currentFlight.takeoffSlot.minutes == minute)
            {
                bgColor = statusApprovedColor;
                textColor = Color.white;
            }
            else
            {
                bgColor = statusPendingColor;
                textColor = new Color(0.08f, 0.08f, 0.10f); // Dark contrast for yellow
            }
        }
        else
        {
            if (currentFlight.status == "Rejected")
            {
                bgColor = statusRejectedColor;
                textColor = Color.white;
            }
            else if (currentFlight.landingSlot == null)
            {
                bgColor = statusNoSlotColor;
                textColor = Color.white;
            }
            else if (currentFlight.landingSlot.hours == hour && currentFlight.landingSlot.minutes == minute && currentFlight.landingApproved)
            {
                bgColor = statusApprovedColor;
                textColor = Color.white;
            }
            else
            {
                bgColor = statusPendingColor;
                textColor = new Color(0.08f, 0.08f, 0.10f); // Dark contrast for yellow
            }
        }

        bg.color = bgColor;
        txt.color = textColor;
    }

    private void CloseAllPopups()
    {
        if (timePickerPopup != null) timePickerPopup.SetActive(false);
    }

    // ==========================================
    // LISTENERS & EVENT HANDLERS
    // ==========================================

    private void SetupInfoRowListeners()
    {
        if (scheduledDepRowButton != null) scheduledDepRowButton.onClick.AddListener(() => { Debug.Log("[FlightDetailsPanel] Request DEP row CLICKED."); OpenTimePicker(true); });
        if (scheduledArrRowButton != null) scheduledArrRowButton.onClick.AddListener(() => { Debug.Log("[FlightDetailsPanel] Request ARR row CLICKED."); OpenTimePicker(false); });

        if (discardActionBarButton != null) discardActionBarButton.onClick.AddListener(() => { Debug.Log("[FlightDetailsPanel] DISCARD action button CLICKED."); ClosePanel(); });
        if (approveActionBarButton != null) approveActionBarButton.onClick.AddListener(() => { Debug.Log("[FlightDetailsPanel] REQUEST action button CLICKED."); ApproveAllChanges(); });
    }

    private void SetupPopupActionListeners()
    {
        // Time Picker Buttons
        if (timePickerCancelButton != null) timePickerCancelButton.onClick.AddListener(() => { Debug.Log("[FlightDetailsPanel] Time Picker CANCEL CLICKED."); timePickerPopup.SetActive(false); });
        if (timePickerConfirmButton != null) timePickerConfirmButton.onClick.AddListener(() => { Debug.Log("[FlightDetailsPanel] Time Picker CONFIRM CLICKED."); ConfirmTimePickerSelection(); });
    }

    // ==========================================
    // TIME PICKER POPUP FLOW
    // ==========================================

    private void OpenTimePicker(bool isDeparture)
    {
        isPickingDepartureTime = isDeparture;
        CloseAllPopups();

        if (timePickerTitleText != null && currentFlight != null)
        {
            string airportCode = isDeparture ? currentFlight.fromAirport : currentFlight.toAirport;
            timePickerTitleText.text = isDeparture ? $"{airportCode} Schedule\nSelect Departure Time" : $"{airportCode} Schedule\nSelect Arrival Time";
            
            var rt = timePickerTitleText.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -10.0f);
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, 50.0f);
            }
        }

        // Set initial selected values in picker modal
        modalSelectedHour = isDeparture ? tempDepHour : tempArrHour;
        modalSelectedMinute = isDeparture ? tempDepMinute : tempArrMinute;

        // Regenerate Hour Tabs & Minute Grid
        RedrawHourTabs();
        RedrawTimeSlotsGrid();

        if (timePickerPopup != null)
        {
            timePickerPopup.SetActive(true);
        }
    }

    private void SafeDestroy(GameObject go)
    {
        if (go == null) return;
        go.transform.SetParent(null); // Detach immediately so it doesn't affect active layouts
        if (Application.isPlaying)
        {
            Destroy(go);
        }
        else
        {
            DestroyImmediate(go);
        }
    }

    private GameObject CreateHourControlObject(string name, string text)
    {
        GameObject go;
        if (hourTabPrefab != null)
        {
            go = Instantiate(hourTabPrefab, hourTabsContainer);
            go.SetActive(true);
            go.name = name;
        }
        else
        {
            go = new GameObject(name);
            go.transform.SetParent(hourTabsContainer, false);
            go.AddComponent<Image>().color = chipInactiveBgColor;
            var txt = new GameObject("Text").AddComponent<TextMeshProUGUI>();
            txt.transform.SetParent(go.transform, false);
            txt.fontSize = 16;
            txt.alignment = TextAlignmentOptions.Center;
            go.AddComponent<Button>();
        }

        var textComp = go.GetComponentInChildren<TMP_Text>();
        if (textComp != null)
        {
            textComp.text = text;
        }
        return go;
    }

    private void RedrawHourTabs()
    {
        if (hourTabsContainer == null) return;

        // Safely clear previous elements, leaving template if it is in-hierarchy
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in hourTabsContainer)
        {
            if (hourTabPrefab != null && child.gameObject == hourTabPrefab) continue;
            childrenToDestroy.Add(child.gameObject);
        }

        foreach (var child in childrenToDestroy)
        {
            SafeDestroy(child);
        }

        // Create Left Arrow (<)
        GameObject leftArrowGo = CreateHourControlObject("LeftArrow", "<");
        var leftBtn = leftArrowGo.GetComponent<Button>();
        var leftLayout = leftArrowGo.GetComponent<LayoutElement>() ?? leftArrowGo.AddComponent<LayoutElement>();
        leftLayout.preferredWidth = 80;
        leftLayout.preferredHeight = 40;
        StylePickerElement(leftArrowGo, false);

        leftBtn.onClick.RemoveAllListeners();
        leftBtn.onClick.AddListener(() => {
            if (modalSelectedHour > 6)
            {
                modalSelectedHour--;
                RedrawHourTabs();
                RedrawTimeSlotsGrid();
            }
        });

        // Create Center Hour Text (HOUR: XX:00)
        GameObject centerHourGo = CreateHourControlObject("CenterHour", $"{modalSelectedHour:D2}:00");
        var centerBtn = centerHourGo.GetComponent<Button>();
        if (centerBtn != null) centerBtn.interactable = false;
        var centerLayout = centerHourGo.GetComponent<LayoutElement>() ?? centerHourGo.AddComponent<LayoutElement>();
        centerLayout.preferredWidth = 180;
        centerLayout.preferredHeight = 40;
        StylePickerElement(centerHourGo, true);

        // Create Right Arrow (>)
        GameObject rightArrowGo = CreateHourControlObject("RightArrow", ">");
        var rightBtn = rightArrowGo.GetComponent<Button>();
        var rightLayout = rightArrowGo.GetComponent<LayoutElement>() ?? rightArrowGo.AddComponent<LayoutElement>();
        rightLayout.preferredWidth = 80;
        rightLayout.preferredHeight = 40;
        StylePickerElement(rightArrowGo, false);

        rightBtn.onClick.RemoveAllListeners();
        rightBtn.onClick.AddListener(() => {
            if (modalSelectedHour < 21)
            {
                modalSelectedHour++;
                RedrawHourTabs();
                RedrawTimeSlotsGrid();
            }
        });
    }

    private void RedrawTimeSlotsGrid()
    {
        if (timeSlotsGridContainer == null) return;

        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in timeSlotsGridContainer)
        {
            if (timeSlotPrefab != null && child.gameObject == timeSlotPrefab) continue;
            childrenToDestroy.Add(child.gameObject);
        }

        foreach (var child in childrenToDestroy)
        {
            SafeDestroy(child);
        }

        // Generate dynamic 5-minute slots for the selected hour
        for (int m = 0; m < 60; m += 5)
        {
            int minVal = m;
            GameObject slotGo;
            if (timeSlotPrefab != null)
            {
                slotGo = Instantiate(timeSlotPrefab, timeSlotsGridContainer);
                slotGo.SetActive(true);
            }
            else
            {
                slotGo = new GameObject($"Slot_{minVal:D2}");
                slotGo.transform.SetParent(timeSlotsGridContainer, false);
                slotGo.AddComponent<Image>().color = chipInactiveBgColor;
                var txt = new GameObject("Text").AddComponent<TextMeshProUGUI>();
                txt.transform.SetParent(slotGo.transform, false);
                txt.fontSize = 14;
                txt.alignment = TextAlignmentOptions.Center;
                slotGo.AddComponent<Button>();
            }

            var btn = slotGo.GetComponent<Button>();
            var textComp = slotGo.GetComponentInChildren<TMP_Text>();
            if (textComp != null)
            {
                textComp.text = $"{modalSelectedHour:D2}:{minVal:D2}";
            }

            bool isSlotSelected = (minVal == modalSelectedMinute);
            StylePickerElement(slotGo, isSlotSelected);

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                modalSelectedMinute = minVal;
                RedrawTimeSlotsGrid();
            });
        }
    }

    private void StylePickerElement(GameObject element, bool isSelected)
    {
        var img = element.GetComponent<Image>();
        if (img != null)
        {
            img.color = isSelected ? chipActiveBgColor : chipInactiveBgColor;
        }
        var txt = element.GetComponentInChildren<TMP_Text>();
        if (txt != null)
        {
            txt.color = isSelected ? chipActiveTextColor : chipInactiveTextColor;
            txt.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
        }
    }

    private void ConfirmTimePickerSelection()
    {
        if (isPickingDepartureTime)
        {
            tempDepHour = modalSelectedHour;
            tempDepMinute = modalSelectedMinute;

            // Maintain modern gameplay flow: Auto-adjust recommended Arrival slot when Departure is modified!
            if (TimeSlotManager.Instance != null && currentFlight != null)
            {
                TimeSlot depSlot = TimeSlotManager.Instance.GetTimeSlot(currentFlight.fromAirport, tempDepHour, tempDepMinute);
                if (depSlot == null) depSlot = new TimeSlot(tempDepHour, tempDepMinute);

                TimeSlot suggestedArrival = TimeSlotManager.Instance.GetSuggestedLandingSlot(currentFlight.toAirport, depSlot, currentFlight.flightDurationMinutes);
                if (suggestedArrival != null)
                {
                    tempArrHour = suggestedArrival.hours;
                    tempArrMinute = suggestedArrival.minutes;
                }
            }
        }
        else
        {
            tempArrHour = modalSelectedHour;
            tempArrMinute = modalSelectedMinute;
        }

        RefreshHeaderExpectedTimes();
        RefreshInfoRowsDisplay();
        timePickerPopup.SetActive(false);
    }

    // ==========================================
    // FINAL SAVE AND CANCEL ACTIONS
    // ==========================================

    private void ApproveAllChanges()
    {
        if (currentFlight == null) return;

        // Update main data model
        currentFlight.state = FlightState.FlightCreated;
        currentFlight.landingApproved = false; // Must be approved by destination airport

        // Update Time Management with TimeSlotManager bookings
        if (TimeSlotManager.Instance != null)
        {
            // Release original takeoff booking if any
            if (currentFlight.takeoffSlot != null)
            {
                TimeSlotManager.Instance.ReleaseTimeSlot(currentFlight.fromAirport, currentFlight.takeoffSlot);
            }
            // Release original landing booking if any
            if (currentFlight.landingSlot != null)
            {
                TimeSlotManager.Instance.ReleaseTimeSlot(currentFlight.toAirport, currentFlight.landingSlot);
            }

            // 1. Immediately reserve and book the takeoff slot at origin
            TimeSlot takeoffSlot = TimeSlotManager.Instance.GetTimeSlot(currentFlight.fromAirport, tempDepHour, tempDepMinute);
            if (takeoffSlot == null)
            {
                takeoffSlot = new TimeSlot(tempDepHour, tempDepMinute);
            }
            TimeSlotManager.Instance.BookTimeSlot(currentFlight.fromAirport, takeoffSlot, currentFlight.flightName);
            currentFlight.takeoffSlot = takeoffSlot;
            currentFlight.departureTime = takeoffSlot.GetTimeString();
            currentFlight.expectedDeparture = takeoffSlot.GetTimeString();

            // 2. Request the landing slot at destination (assign reference but DO NOT book it officially yet)
            TimeSlot landingSlot = TimeSlotManager.Instance.GetTimeSlot(currentFlight.toAirport, tempArrHour, tempArrMinute);
            if (landingSlot == null)
            {
                landingSlot = new TimeSlot(tempArrHour, tempArrMinute);
            }
            currentFlight.landingSlot = landingSlot;
            currentFlight.arrivalTime = landingSlot.GetTimeString();
            currentFlight.expectedArrival = landingSlot.GetTimeString();
        }

        // Instantly refresh the airport's scroll list of flight cards
        if (currentFlight.currentAirport != null)
        {
            currentFlight.currentAirport.DisplayFlights();
        }

        // Also refresh active Schedule panel if open
        if (AirportSchedulePanel.Instance != null && AirportSchedulePanel.Instance.panelContainer.activeSelf)
        {
            AirportSchedulePanel.Instance.RefreshAll();
        }

        ClosePanel();
    }

    public void ClosePanel()
    {
        CloseAllPopups();
        if (panelContainer != null)
        {
            panelContainer.SetActive(false);
        }

        // Clear the selected flight selection and refresh active flight cards
        FlightInfoUI.selectedFlight = null;
        var activeCards = FindObjectsByType<FlightInfoUI>(FindObjectsInactive.Exclude);
        foreach (var card in activeCards)
        {
            card.UpdateSelectionVisuals();
        }
    }
}
