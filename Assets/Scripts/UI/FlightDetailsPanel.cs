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

    [Header("Status Selector")]
    public Button securityChipButton;
    public Button boardingChipButton;
    public Button taxiChipButton;
    public Button readyChipButton;
    
    [Header("Status Styling Colors")]
    public Color chipActiveBgColor = new Color(0.14f, 0.65f, 0.31f); // Emerald Green
    public Color chipInactiveBgColor = new Color(0.18f, 0.18f, 0.20f); // Dark Charcoal
    public Color chipActiveTextColor = Color.white;
    public Color chipInactiveTextColor = new Color(0.65f, 0.65f, 0.68f);

    [Header("Editable Info Rows")]
    public Button scheduledDepRowButton;
    public TMP_Text scheduledDepValueText;

    public Button scheduledArrRowButton;
    public TMP_Text scheduledArrValueText;

    public Button boardingGateRowButton;
    public TMP_Text boardingGateValueText;

    public Button assignedRunwayRowButton;
    public TMP_Text assignedRunwayValueText;

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

    [Header("Gate Picker Popup Modal")]
    public GameObject gatePickerPopup;
    public TMP_Text gatePickerTitleText;
    public Transform gateCardsContainer;
    public GameObject gateCardPrefab;
    public Button gatePickerCancelButton;
    public Button gatePickerConfirmButton;

    [Header("Runway Picker Popup Modal")]
    public GameObject runwayPickerPopup;
    public TMP_Text runwayPickerTitleText;
    public Transform runwayCardsContainer;
    public GameObject runwayCardPrefab;
    public Button runwayPickerCancelButton;
    public Button runwayPickerConfirmButton;

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
    private string tempGate;
    private string tempRunway;

    // Internal state management for modals
    private bool isPickingDepartureTime;
    private int modalSelectedHour;
    private int modalSelectedMinute;
    private string modalSelectedGate;
    private string modalSelectedRunway;

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
        SetupStatusChipListeners();
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
        tempGate = string.IsNullOrWhiteSpace(flight.gate) || flight.gate == "NA" ? "Gate A4" : flight.gate;
        tempRunway = string.IsNullOrWhiteSpace(flight.runway) || flight.runway == "NA" ? "Runway 27L" : flight.runway;

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
        if (depCountryText != null) depCountryText.text = "INDIA";
        if (arrCountryText != null) arrCountryText.text = "INDIA";
        if (aircraftTypeText != null) aircraftTypeText.text = flight.aircraftType;
        if (durationText != null) durationText.text = $"{flight.flightDurationMinutes} mins";

        RefreshHeaderExpectedTimes();
        RefreshStatusChipsDisplay();
        RefreshInfoRowsDisplay();

        // Close any residual open popups
        CloseAllPopups();

        if (panelContainer != null)
        {
            panelContainer.SetActive(true);
        }
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

    private void RefreshStatusChipsDisplay()
    {
        StyleChip(securityChipButton, tempStatus == "Security");
        StyleChip(boardingChipButton, tempStatus == "Boarding");
        StyleChip(taxiChipButton, tempStatus == "Taxi");
        StyleChip(readyChipButton, tempStatus == "Ready" || tempStatus == "On Time" || tempStatus == "Approved");
    }

    private void StyleChip(Button button, bool isActive)
    {
        if (button == null) return;
        var img = button.GetComponent<Image>();
        if (img != null)
        {
            img.color = isActive ? chipActiveBgColor : chipInactiveBgColor;
        }
        var txt = button.GetComponentInChildren<TMP_Text>();
        if (txt != null)
        {
            txt.color = isActive ? chipActiveTextColor : chipInactiveTextColor;
        }
    }

    private void RefreshInfoRowsDisplay()
    {
        if (scheduledDepValueText != null) scheduledDepValueText.text = $"{tempDepHour:D2}:{tempDepMinute:D2}";
        if (scheduledArrValueText != null) scheduledArrValueText.text = $"{tempArrHour:D2}:{tempArrMinute:D2}";
        if (boardingGateValueText != null) boardingGateValueText.text = tempGate;
        if (assignedRunwayValueText != null) assignedRunwayValueText.text = tempRunway;

        if (scheduledDepRowButton != null && currentFlight != null)
        {
            var label = scheduledDepRowButton.transform.Find("Label")?.GetComponent<TMP_Text>();
            if (label != null)
            {
                label.text = $"Scheduled Departure ({currentFlight.fromAirport})";
            }
        }
        if (scheduledArrRowButton != null && currentFlight != null)
        {
            var label = scheduledArrRowButton.transform.Find("Label")?.GetComponent<TMP_Text>();
            if (label != null)
            {
                label.text = $"Scheduled Arrival ({currentFlight.toAirport})";
            }
        }
    }

    private void CloseAllPopups()
    {
        if (timePickerPopup != null) timePickerPopup.SetActive(false);
        if (gatePickerPopup != null) gatePickerPopup.SetActive(false);
        if (runwayPickerPopup != null) runwayPickerPopup.SetActive(false);
    }

    // ==========================================
    // LISTENERS & EVENT HANDLERS
    // ==========================================

    private void SetupStatusChipListeners()
    {
        if (securityChipButton != null) securityChipButton.onClick.AddListener(() => { Debug.Log("[FlightDetailsPanel] Security status chip CLICKED."); SetStatusDraft("Security"); });
        if (boardingChipButton != null) boardingChipButton.onClick.AddListener(() => { Debug.Log("[FlightDetailsPanel] Boarding status chip CLICKED."); SetStatusDraft("Boarding"); });
        if (taxiChipButton != null) taxiChipButton.onClick.AddListener(() => { Debug.Log("[FlightDetailsPanel] Taxi status chip CLICKED."); SetStatusDraft("Taxi"); });
        if (readyChipButton != null) readyChipButton.onClick.AddListener(() => { Debug.Log("[FlightDetailsPanel] Ready status chip CLICKED."); SetStatusDraft("Ready"); });
    }

    private void SetStatusDraft(string newStatus)
    {
        tempStatus = newStatus;
        RefreshStatusChipsDisplay();
    }

    private void SetupInfoRowListeners()
    {
        if (scheduledDepRowButton != null) scheduledDepRowButton.onClick.AddListener(() => { Debug.Log("[FlightDetailsPanel] Scheduled Departure row CLICKED."); OpenTimePicker(true); });
        if (scheduledArrRowButton != null) scheduledArrRowButton.onClick.AddListener(() => { Debug.Log("[FlightDetailsPanel] Scheduled Arrival row CLICKED."); OpenTimePicker(false); });
        if (boardingGateRowButton != null) boardingGateRowButton.onClick.AddListener(() => { Debug.Log("[FlightDetailsPanel] Boarding Gate row CLICKED."); OpenGatePicker(); });
        if (assignedRunwayRowButton != null) assignedRunwayRowButton.onClick.AddListener(() => { Debug.Log("[FlightDetailsPanel] Assigned Runway row CLICKED."); OpenRunwayPicker(); });

        if (discardActionBarButton != null) discardActionBarButton.onClick.AddListener(() => { Debug.Log("[FlightDetailsPanel] DISCARD action button CLICKED."); ClosePanel(); });
        if (approveActionBarButton != null) approveActionBarButton.onClick.AddListener(() => { Debug.Log("[FlightDetailsPanel] REQUEST action button CLICKED."); ApproveAllChanges(); });
    }

    private void SetupPopupActionListeners()
    {
        // Time Picker Buttons
        if (timePickerCancelButton != null) timePickerCancelButton.onClick.AddListener(() => { Debug.Log("[FlightDetailsPanel] Time Picker CANCEL CLICKED."); timePickerPopup.SetActive(false); });
        if (timePickerConfirmButton != null) timePickerConfirmButton.onClick.AddListener(() => { Debug.Log("[FlightDetailsPanel] Time Picker CONFIRM CLICKED."); ConfirmTimePickerSelection(); });

        // Gate Picker Buttons
        if (gatePickerCancelButton != null) gatePickerCancelButton.onClick.AddListener(() => { Debug.Log("[FlightDetailsPanel] Gate Picker CANCEL CLICKED."); gatePickerPopup.SetActive(false); });
        if (gatePickerConfirmButton != null) gatePickerConfirmButton.onClick.AddListener(() => { Debug.Log("[FlightDetailsPanel] Gate Picker CONFIRM CLICKED."); ConfirmGatePickerSelection(); });

        // Runway Picker Buttons
        if (runwayPickerCancelButton != null) runwayPickerCancelButton.onClick.AddListener(() => { Debug.Log("[FlightDetailsPanel] Runway Picker CANCEL CLICKED."); runwayPickerPopup.SetActive(false); });
        if (runwayPickerConfirmButton != null) runwayPickerConfirmButton.onClick.AddListener(() => { Debug.Log("[FlightDetailsPanel] Runway Picker CONFIRM CLICKED."); ConfirmRunwayPickerSelection(); });
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
    // GATE PICKER POPUP FLOW
    // ==========================================

    private void OpenGatePicker()
    {
        CloseAllPopups();
        modalSelectedGate = tempGate;
        RedrawGatePicker();
        if (gatePickerPopup != null) gatePickerPopup.SetActive(true);
    }

    private void RedrawGatePicker()
    {
        if (gateCardsContainer == null) return;

        foreach (Transform child in gateCardsContainer)
        {
            if (gateCardPrefab != null && child.gameObject == gateCardPrefab) continue;
            Destroy(child.gameObject);
        }

        string[] gates = { "Gate A1", "Gate A2", "Gate A3", "Gate A4", "Gate A5", "Gate B1", "Gate B2", "Gate B3" };
        foreach (string gate in gates)
        {
            string gateName = gate;
            GameObject cardGo;
            if (gateCardPrefab != null)
            {
                cardGo = Instantiate(gateCardPrefab, gateCardsContainer);
                cardGo.SetActive(true);
            }
            else
            {
                cardGo = new GameObject(gateName);
                cardGo.transform.SetParent(gateCardsContainer, false);
                cardGo.AddComponent<Image>().color = chipInactiveBgColor;
                var txt = new GameObject("Text").AddComponent<TextMeshProUGUI>();
                txt.transform.SetParent(cardGo.transform, false);
                txt.fontSize = 14;
                txt.alignment = TextAlignmentOptions.Center;
                cardGo.AddComponent<Button>();
            }

            var btn = cardGo.GetComponent<Button>();
            var textComp = cardGo.GetComponentInChildren<TMP_Text>();
            if (textComp != null)
            {
                textComp.text = gateName;
            }

            bool isSelected = (gateName == modalSelectedGate);
            StylePickerElement(cardGo, isSelected);

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                modalSelectedGate = gateName;
                RedrawGatePicker();
            });
        }
    }

    private void ConfirmGatePickerSelection()
    {
        tempGate = modalSelectedGate;
        RefreshInfoRowsDisplay();
        gatePickerPopup.SetActive(false);
    }

    // ==========================================
    // RUNWAY PICKER POPUP FLOW
    // ==========================================

    private void OpenRunwayPicker()
    {
        CloseAllPopups();
        modalSelectedRunway = tempRunway;
        RedrawRunwayPicker();
        if (runwayPickerPopup != null) runwayPickerPopup.SetActive(true);
    }

    private void RedrawRunwayPicker()
    {
        if (runwayCardsContainer == null) return;

        foreach (Transform child in runwayCardsContainer)
        {
            if (runwayCardPrefab != null && child.gameObject == runwayCardPrefab) continue;
            Destroy(child.gameObject);
        }

        string[] runways = { "Runway 09L", "Runway 09R", "Runway 27L", "Runway 27R" };
        foreach (string runway in runways)
        {
            string runwayName = runway;
            GameObject cardGo;
            if (runwayCardPrefab != null)
            {
                cardGo = Instantiate(runwayCardPrefab, runwayCardsContainer);
                cardGo.SetActive(true);
            }
            else
            {
                cardGo = new GameObject(runwayName);
                cardGo.transform.SetParent(runwayCardsContainer, false);
                cardGo.AddComponent<Image>().color = chipInactiveBgColor;
                var txt = new GameObject("Text").AddComponent<TextMeshProUGUI>();
                txt.transform.SetParent(cardGo.transform, false);
                txt.fontSize = 14;
                txt.alignment = TextAlignmentOptions.Center;
                cardGo.AddComponent<Button>();
            }

            var btn = cardGo.GetComponent<Button>();
            var textComp = cardGo.GetComponentInChildren<TMP_Text>();
            if (textComp != null)
            {
                textComp.text = runwayName;
            }

            bool isSelected = (runwayName == modalSelectedRunway);
            StylePickerElement(cardGo, isSelected);

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                modalSelectedRunway = runwayName;
                RedrawRunwayPicker();
            });
        }
    }

    private void ConfirmRunwayPickerSelection()
    {
        tempRunway = modalSelectedRunway;
        RefreshInfoRowsDisplay();
        runwayPickerPopup.SetActive(false);
    }

    // ==========================================
    // FINAL SAVE AND CANCEL ACTIONS
    // ==========================================

    private void ApproveAllChanges()
    {
        if (currentFlight == null) return;

        // Update main data model
        currentFlight.status = "Pending Approval"; // Waiting for destination approval
        currentFlight.gate = tempGate;
        currentFlight.runway = tempRunway;
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
