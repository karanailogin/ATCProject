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

    [Header("Redesigned Time Picker Fields")]
    public TMP_Text airportTitleText;
    public TMP_Text pickerModeText;
    public TMP_Text currentTimeText;
    public TMP_Text selectedSlotText;

    [Header("Panel Containment")]
    public GameObject panelContainer;

    private static FlightDetailsPanel instance;
    private static Sprite roundedUiSprite;
    private static Sprite roundedBorderSprite;
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
    private int modalSelectedHourIndex;
    private int pickerWindowStartMinutes;
    private int pickerWindowEndMinutes;
    private readonly List<int> pickerHourValues = new List<int>();
    private GameObject selectedTimeSlotRow;
    private TMP_Text timeSlotsHeaderText;
    private TMP_Text airportInfoValueText;

    private readonly Color pickerBackgroundColor = new Color(0.045f, 0.055f, 0.07f, 0.96f);
    private readonly Color pickerPanelColor = new Color(0.075f, 0.09f, 0.115f, 0.98f);
    private readonly Color pickerRowColor = new Color(0.12f, 0.145f, 0.18f, 1.0f);
    private readonly Color pickerBorderColor = new Color(0.22f, 0.27f, 0.34f, 1.0f);
    private readonly Color pickerMutedTextColor = new Color(0.72f, 0.74f, 0.78f, 1.0f);
    private readonly Color pickerGreenColor = new Color(0.11f, 0.72f, 0.32f, 1.0f);
    private readonly Color pickerRedColor = new Color(1.0f, 0.31f, 0.31f, 1.0f);
    private readonly Color pickerOrangeColor = new Color(1.0f, 0.53f, 0.16f, 1.0f);
    private readonly Color pickerYellowColor = new Color(1.0f, 0.82f, 0.22f, 1.0f);
    private readonly Color pickerSelectionFillColor = new Color(0.07f, 0.19f, 0.27f, 1.0f);
    private readonly Color pickerSelectionBorderColor = new Color(0.22f, 0.74f, 0.97f, 1.0f);
    private const float HourRowHeight = 66f;
    private const float HourRowSpacing = 8f;
    private const float HourFontSize = 30f;
    private const float SlotRowHeight = 68f;
    private const float SlotRowSpacing = 9f;
    private const float SlotTimeFontSize = 29f;
    private const float SlotStatusFontSize = 29f;
    private const float SlotFlightFontSize = 29f;

    private const int PickerWindowMinutes = 120;

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

        BuildTimePickerScheduleLayout();

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

        if (currentFlight != null)
        {
            string airportCode = GetPickerAirportCode();
            if (airportTitleText != null) airportTitleText.text = $"{airportCode} Schedule";
            if (pickerModeText != null) pickerModeText.text = isDeparture ? "Select Departure Time" : "Select Arrival Time";
            if (airportInfoValueText != null) airportInfoValueText.text = airportCode;
            if (currentTimeText != null)
            {
                currentTimeText.text = WorldClockManager.Instance != null
                    ? WorldClockManager.Instance.CurrentTime.ToString("HH:mm")
                    : "--:--";
            }
            if (selectedSlotText != null)
            {
                selectedSlotText.text = "None";
                selectedSlotText.color = pickerYellowColor;
            }
        }

        if (timePickerTitleText != null && currentFlight != null)
        {
            timePickerTitleText.text = $"{GetPickerAirportCode()} Schedule";
            
            var rt = timePickerTitleText.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
            }
        }

        InitializePickerWindow(isDeparture);

        CanvasGroup popupCanvasGroup = null;
        if (timePickerPopup != null)
        {
            popupCanvasGroup = timePickerPopup.GetComponent<CanvasGroup>();
            if (popupCanvasGroup == null)
            {
                popupCanvasGroup = timePickerPopup.AddComponent<CanvasGroup>();
            }
            popupCanvasGroup.alpha = 0f;
            timePickerPopup.SetActive(true);
            timePickerPopup.transform.SetAsLastSibling();
        }

        // Regenerate Hour Tabs & Minute Grid
        RedrawHourTabs();
        RedrawTimeSlotsGrid();
        ForceTimePickerLayoutRebuild();
        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.alpha = 1f;
        }
    }

    private void BuildTimePickerScheduleLayout()
    {
        if (timePickerPopup == null) return;

        var canvas = GameObject.Find("MainCanvas");
        if (canvas != null && timePickerPopup.transform.parent != canvas.transform)
        {
            timePickerPopup.transform.SetParent(canvas.transform, false);
        }
        timePickerPopup.transform.SetAsLastSibling();

        RectTransform popupRt = EnsureRectTransform(timePickerPopup);
        StretchToParent(popupRt, Vector2.zero);
        EnsureImage(timePickerPopup, pickerBackgroundColor);

        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in timePickerPopup.transform)
        {
            childrenToDestroy.Add(child.gameObject);
        }
        foreach (var child in childrenToDestroy)
        {
            SafeDestroy(child);
        }

        GameObject modal = CreatePanel("ScheduleFrame", timePickerPopup.transform, pickerBackgroundColor);
        RectTransform modalRt = EnsureRectTransform(modal);
        StretchToParent(modalRt, new Vector2(8f, 8f));
        VerticalLayoutGroup modalLayout = modal.AddComponent<VerticalLayoutGroup>();
        modalLayout.padding = new RectOffset(30, 30, 24, 26);
        modalLayout.spacing = 12f;
        modalLayout.childControlWidth = true;
        modalLayout.childControlHeight = true;
        modalLayout.childForceExpandWidth = true;
        modalLayout.childForceExpandHeight = false;

        BuildPickerHeader(modal.transform);
        BuildPickerInfoBar(modal.transform);
        BuildPickerBody(modal.transform);
        BuildPickerFooter(modal.transform);
    }

    private void BuildPickerHeader(Transform parent)
    {
        GameObject header = CreateLayoutObject("Header", parent, 100f, -1f, false);

        timePickerTitleText = CreateText("AirportTitleText", header.transform, "MUM Schedule", 42f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        RectTransform titleRt = EnsureRectTransform(timePickerTitleText.gameObject);
        titleRt.anchorMin = new Vector2(0f, 0.46f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.offsetMin = new Vector2(120f, 0f);
        titleRt.offsetMax = new Vector2(-120f, 0f);
        airportTitleText = timePickerTitleText;

        pickerModeText = CreateText("PickerModeText", header.transform, "Select Departure Time", 27f, FontStyles.Normal, TextAlignmentOptions.Center, Color.white);
        RectTransform subtitleRt = EnsureRectTransform(pickerModeText.gameObject);
        subtitleRt.anchorMin = new Vector2(0f, 0f);
        subtitleRt.anchorMax = new Vector2(1f, 0.48f);
        subtitleRt.offsetMin = new Vector2(120f, 0f);
        subtitleRt.offsetMax = new Vector2(-120f, 0f);

        Button closeButton = CreateButton("CloseButton", header.transform, "X", pickerBackgroundColor, Color.white, 34f);
        RectTransform closeRt = EnsureRectTransform(closeButton.gameObject);
        closeRt.anchorMin = new Vector2(1f, 0.5f);
        closeRt.anchorMax = new Vector2(1f, 0.5f);
        closeRt.sizeDelta = new Vector2(64f, 64f);
        closeRt.anchoredPosition = new Vector2(-18f, 8f);
        closeButton.onClick.AddListener(() => { if (timePickerPopup != null) timePickerPopup.SetActive(false); });
    }

    private void BuildPickerInfoBar(Transform parent)
    {
        GameObject infoBar = CreatePanel("InfoBar", parent, new Color(0.095f, 0.11f, 0.14f, 1.0f));
        AddLayoutElement(infoBar, 92f, -1f, false);
        HorizontalLayoutGroup layout = infoBar.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(120, 120, 12, 12);
        layout.spacing = 54f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        airportInfoValueText = CreateInfoBlock(infoBar.transform, "Airport", "MUM", Color.white);
        currentTimeText = CreateInfoBlock(infoBar.transform, "Current Time", "11:07", Color.white);
        selectedSlotText = CreateInfoBlock(infoBar.transform, "Selected Slot", "None", pickerYellowColor);
    }

    private TMP_Text CreateInfoBlock(Transform parent, string label, string value, Color valueColor)
    {
        GameObject block = CreatePanel($"{label.Replace(" ", "")}Block", parent, new Color(0f, 0f, 0f, 0f));
        HorizontalLayoutGroup blockLayout = block.AddComponent<HorizontalLayoutGroup>();
        blockLayout.spacing = 18f;
        blockLayout.childControlWidth = true;
        blockLayout.childControlHeight = true;
        blockLayout.childForceExpandWidth = false;
        blockLayout.childForceExpandHeight = true;

        TMP_Text icon = CreateText("Icon", block.transform, GetInfoIcon(label), 36f, FontStyles.Bold, TextAlignmentOptions.Center, pickerGreenColor);
        AddLayoutElement(icon.gameObject, -1f, 58f, false);

        GameObject textStack = CreateLayoutObject("TextStack", block.transform, -1f, -1f, true);
        VerticalLayoutGroup stackLayout = textStack.AddComponent<VerticalLayoutGroup>();
        stackLayout.spacing = 1f;
        stackLayout.childControlWidth = true;
        stackLayout.childControlHeight = true;
        stackLayout.childForceExpandWidth = true;
        stackLayout.childForceExpandHeight = true;

        CreateText("Label", textStack.transform, label, 18f, FontStyles.Normal, TextAlignmentOptions.Left, pickerMutedTextColor);
        TMP_Text valueText = CreateText("Value", textStack.transform, value, 24f, FontStyles.Bold, TextAlignmentOptions.Left, valueColor);
        return valueText;
    }

    private void BuildPickerBody(Transform parent)
    {
        GameObject body = CreateLayoutObject("Body", parent, -1f, -1f, true);
        HorizontalLayoutGroup bodyLayout = body.AddComponent<HorizontalLayoutGroup>();
        bodyLayout.spacing = 10f;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = false;
        bodyLayout.childForceExpandHeight = true;
        AddLayoutElement(body, -1f, -1f, true);

        BuildHourPanel(body.transform);
        BuildSlotPanel(body.transform);
    }

    private void BuildHourPanel(Transform parent)
    {
        GameObject panel = CreatePanel("HourPickerPanel", parent, pickerPanelColor);
        LayoutElement panelLayout = AddLayoutElement(panel, -1f, 0f, true);
        panelLayout.flexibleWidth = 3f;
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 10, 12);
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text heading = CreateText("Heading", panel.transform, "SELECTABLE HOURS", 22f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        AddLayoutElement(heading.gameObject, 38f, -1f, false);

        ScrollRect scrollRect = CreateScrollRect("HourScrollView", panel.transform, out RectTransform content);
        SetScrollContentSpacing(content, HourRowSpacing);
        hourTabsContainer = content;
        AddLayoutElement(scrollRect.gameObject, -1f, -1f, true);
    }

    private void BuildSlotPanel(Transform parent)
    {
        GameObject panel = CreatePanel("SlotListPanel", parent, pickerPanelColor);
        LayoutElement panelLayout = AddLayoutElement(panel, -1f, 0f, true);
        panelLayout.flexibleWidth = 7f;
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 10, 20);
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        timeSlotsHeaderText = CreateText("Heading", panel.transform, "TIME SLOTS FOR 11:00", 22f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        AddLayoutElement(timeSlotsHeaderText.gameObject, 38f, -1f, false);

        ScrollRect scrollRect = CreateScrollRect("SlotScrollView", panel.transform, out RectTransform content);
        SetScrollContentSpacing(content, SlotRowSpacing);
        timeSlotsGridContainer = content;
        AddLayoutElement(scrollRect.gameObject, -1f, -1f, true);
    }

    private void BuildPickerFooter(Transform parent)
    {
        GameObject footer = CreateLayoutObject("Footer", parent, 86f, -1f, false);
        HorizontalLayoutGroup layout = footer.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 44f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        timePickerCancelButton = CreateButton("CancelButton", footer.transform, "CANCEL", pickerRowColor, Color.white, 26f);
        timePickerConfirmButton = CreateButton("ConfirmButton", footer.transform, "CONFIRM", pickerGreenColor, Color.white, 26f);
    }

    private void SafeDestroy(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
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

    private void RedrawHourTabs()
    {
        if (hourTabsContainer == null) return;

        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in hourTabsContainer)
        {
            childrenToDestroy.Add(child.gameObject);
        }

        foreach (var child in childrenToDestroy)
        {
            SafeDestroy(child);
        }

        for (int i = 0; i < pickerHourValues.Count; i++)
        {
            int index = i;
            int absoluteHour = pickerHourValues[i];
            string label = FormatPickerHour(absoluteHour);
            Button hourButton = CreateButton($"HourButton_{label.Replace("+", "plus_").Replace(":", "_")}", hourTabsContainer, label, pickerRowColor, Color.white, HourFontSize);
            AddLayoutElement(hourButton.gameObject, HourRowHeight, -1f, false);
            ConfigureStretchRow(hourButton.gameObject, HourRowHeight);
            StyleHourButton(hourButton.gameObject, index == modalSelectedHourIndex);
            hourButton.onClick.AddListener(() =>
            {
                modalSelectedHourIndex = index;
                modalSelectedHour = NormalizeHour(absoluteHour);
                modalSelectedMinute = GetFirstSelectableMinute(absoluteHour);
                if (selectedSlotText != null)
                {
                    selectedSlotText.text = "None";
                    selectedSlotText.color = pickerYellowColor;
                }
                RefreshHourButtonStyles();
                RedrawTimeSlotsGrid();
            });
        }

        RefreshScrollContent(hourTabsContainer as RectTransform, HourRowHeight, HourRowSpacing);
    }

    private void RefreshHourButtonStyles()
    {
        if (hourTabsContainer == null) return;

        int index = 0;
        foreach (Transform child in hourTabsContainer)
        {
            StyleHourButton(child.gameObject, index == modalSelectedHourIndex);
            index++;
        }
    }

    private void RedrawTimeSlotsGrid()
    {
        if (timeSlotsGridContainer == null) return;

        selectedTimeSlotRow = null;

        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in timeSlotsGridContainer)
        {
            childrenToDestroy.Add(child.gameObject);
        }

        foreach (var child in childrenToDestroy)
        {
            SafeDestroy(child);
        }

        if (pickerHourValues.Count == 0) return;

        int selectedAbsoluteHour = pickerHourValues[Mathf.Clamp(modalSelectedHourIndex, 0, pickerHourValues.Count - 1)];
        string hourLabel = FormatPickerHour(selectedAbsoluteHour);
        if (timeSlotsHeaderText != null)
        {
            timeSlotsHeaderText.text = $"TIME SLOTS FOR {hourLabel}";
        }

        int hourStartMinutes = selectedAbsoluteHour * 60;
        for (int minute = 0; minute < 60; minute += 5)
        {
            int slotTotalMinutes = hourStartMinutes + minute;
            if (slotTotalMinutes < pickerWindowStartMinutes || slotTotalMinutes > pickerWindowEndMinutes)
            {
                continue;
            }

            AddWindowSlotRow(slotTotalMinutes);
        }

        RefreshScrollContent(timeSlotsGridContainer as RectTransform, SlotRowHeight, SlotRowSpacing);
    }

    private void AddWindowSlotRow(int totalMinutes)
    {
        string time = FormatPickerTime(totalMinutes);

        string airportCode = GetPickerAirportCode();
        TimeSlot slot = TimeSlotManager.Instance != null
            ? TimeSlotManager.Instance.GetTimeSlot(airportCode, totalMinutes / 60, totalMinutes % 60)
            : null;

        if (slot == null || !slot.isBooked)
        {
            AddSlotRow(time, "Available", "", "", pickerGreenColor);
            return;
        }

        Flight bookedFlight = null;
        if (FlightManager.Instance != null)
        {
            bookedFlight = FlightManager.Instance.AllFlights.Find(
                flight => flight != null && flight.flightName == slot.bookedByFlight);
        }

        bool isDeparture = bookedFlight != null &&
            bookedFlight.fromAirport == airportCode &&
            bookedFlight.takeoffSlot != null &&
            bookedFlight.takeoffSlot.hours == slot.hours &&
            bookedFlight.takeoffSlot.minutes == slot.minutes;

        string status = isDeparture ? "Booked DEP" : "Booked ARR";
        string routeAirport = bookedFlight == null
            ? ""
            : (isDeparture ? bookedFlight.toAirport : bookedFlight.fromAirport);
        Color statusColor = isDeparture ? pickerOrangeColor : pickerRedColor;

        AddSlotRow(time, status, slot.bookedByFlight, routeAirport, statusColor);
    }

    private string GetPickerAirportCode()
    {
        if (currentFlight == null) return "---";
        return isPickingDepartureTime ? currentFlight.fromAirport : currentFlight.toAirport;
    }

    private void AddSlotRow(string time, string status, string flightId, string routeAirport, Color statusColor)
    {
        Button rowButton = CreateButton($"SlotRow_{time.Replace(":", "_")}", timeSlotsGridContainer, "", pickerRowColor, Color.white, 20f);
        AddLayoutElement(rowButton.gameObject, SlotRowHeight, -1f, false);
        ConfigureStretchRow(rowButton.gameObject, SlotRowHeight);

        HorizontalLayoutGroup layout = rowButton.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 0, 0);
        layout.spacing = 28f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        TMP_Text timeText = CreateText("Time", rowButton.transform, time, SlotTimeFontSize, FontStyles.Normal, TextAlignmentOptions.Left, Color.white);
        AddLayoutElement(timeText.gameObject, -1f, 220f, false);

        TMP_Text dotText = CreateText("StatusDot", rowButton.transform, "●", 24f, FontStyles.Bold, TextAlignmentOptions.Center, statusColor);
        AddLayoutElement(dotText.gameObject, -1f, 32f, false);

        TMP_Text statusText = CreateText("Status", rowButton.transform, status, SlotStatusFontSize, FontStyles.Bold, TextAlignmentOptions.Left, statusColor);
        AddLayoutElement(statusText.gameObject, -1f, 1f, true);

        TMP_Text flightText = CreateText("FlightId", rowButton.transform, flightId, SlotFlightFontSize, FontStyles.Normal, TextAlignmentOptions.Right, statusColor);
        AddLayoutElement(flightText.gameObject, -1f, 180f, false);

        TMP_Text routeText = CreateText("RouteAirport", rowButton.transform, routeAirport, SlotFlightFontSize, FontStyles.Normal, TextAlignmentOptions.Right, statusColor);
        AddLayoutElement(routeText.gameObject, -1f, 100f, false);

        rowButton.onClick.AddListener(() =>
        {
            SelectTimeSlotRow(rowButton.gameObject);

            int separatorIndex = time.LastIndexOf(':');
            if (separatorIndex >= 0 && int.TryParse(time.Substring(separatorIndex + 1), out int minute))
            {
                modalSelectedMinute = minute;
            }
            if (selectedSlotText != null)
            {
                selectedSlotText.text = string.IsNullOrEmpty(flightId)
                    ? $"{time} {status}"
                    : $"{time} {status} {flightId} {routeAirport}";
                selectedSlotText.color = statusColor;
            }
        });
    }

    private void SelectTimeSlotRow(GameObject row)
    {
        if (selectedTimeSlotRow != null && selectedTimeSlotRow != row)
        {
            ApplyTimeSlotSelectionVisual(selectedTimeSlotRow, false);
        }

        selectedTimeSlotRow = row;
        ApplyTimeSlotSelectionVisual(selectedTimeSlotRow, true);
    }

    private void ApplyTimeSlotSelectionVisual(GameObject row, bool isSelected)
    {
        if (row == null) return;

        Image image = row.GetComponent<Image>();
        if (image != null)
        {
            image.color = isSelected ? pickerSelectionFillColor : pickerRowColor;
        }

        SetRoundedSelectionBorder(row, isSelected);
    }

    private void StyleHourButton(GameObject element, bool isSelected)
    {
        var img = element.GetComponent<Image>();
        if (img != null)
        {
            img.color = isSelected ? pickerSelectionFillColor : pickerRowColor;
        }

        SetRoundedSelectionBorder(element, isSelected);

        var txt = element.GetComponentInChildren<TMP_Text>();
        if (txt != null)
        {
            txt.color = Color.white;
            txt.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
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

    private void RefreshScrollContent(RectTransform content, float rowHeight, float spacing)
    {
        if (content == null) return;

        int childCount = 0;
        foreach (Transform child in content)
        {
            if (child.gameObject.activeSelf)
            {
                childCount++;
            }
        }

        float contentHeight = Mathf.Max(1f, childCount * rowHeight + Mathf.Max(0, childCount - 1) * spacing);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = new Vector2(0f, -contentHeight);
        content.offsetMax = Vector2.zero;
        content.sizeDelta = new Vector2(0f, contentHeight);
        content.anchoredPosition = Vector2.zero;

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        if (content.parent is RectTransform viewport)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);
        }
    }

    private void ConfigureStretchRow(GameObject row, float height)
    {
        if (row == null) return;

        row.SetActive(true);
        RectTransform rt = EnsureRectTransform(row);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(0f, rt.offsetMin.y);
        rt.offsetMax = new Vector2(0f, rt.offsetMax.y);
        rt.sizeDelta = new Vector2(0f, height);

        Image image = row.GetComponent<Image>();
        if (image == null)
        {
            image = row.AddComponent<Image>();
        }
        if (image.color.a <= 0.01f)
        {
            image.color = pickerRowColor;
        }
        image.raycastTarget = true;

        CanvasGroup canvasGroup = row.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            text.gameObject.SetActive(true);
            if (text.color.a <= 0.01f)
            {
                text.color = Color.white;
            }
        }
    }

    private void ForceTimePickerLayoutRebuild()
    {
        if (timePickerPopup == null) return;

        Canvas.ForceUpdateCanvases();
        RectTransform popupRt = timePickerPopup.GetComponent<RectTransform>();
        if (popupRt != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(popupRt);
        }
        if (hourTabsContainer is RectTransform hourContent)
        {
            RefreshScrollContent(hourContent, HourRowHeight, HourRowSpacing);
        }
        if (timeSlotsGridContainer is RectTransform slotContent)
        {
            RefreshScrollContent(slotContent, SlotRowHeight, SlotRowSpacing);
        }
        Canvas.ForceUpdateCanvases();
    }

    private void SetScrollContentSpacing(RectTransform content, float spacing)
    {
        if (content == null) return;

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }
    }

    private void InitializePickerWindow(bool isDeparture)
    {
        int expectedMinutes = GetExpectedTimeMinutes(isDeparture);
        pickerWindowStartMinutes = ((expectedMinutes + 4) / 5) * 5;
        pickerWindowEndMinutes = pickerWindowStartMinutes + PickerWindowMinutes;

        pickerHourValues.Clear();
        int firstHour = pickerWindowStartMinutes / 60;
        int lastHour = pickerWindowEndMinutes / 60;
        for (int hour = firstHour; hour <= lastHour; hour++)
        {
            pickerHourValues.Add(hour);
        }

        modalSelectedHourIndex = 0;
        modalSelectedHour = NormalizeHour(firstHour);
        modalSelectedMinute = pickerWindowStartMinutes % 60;
    }

    private int GetExpectedTimeMinutes(bool isDeparture)
    {
        if (currentFlight != null)
        {
            string expectedTime = isDeparture ? currentFlight.expectedDeparture : currentFlight.expectedArrival;
            if (TryParseTime(expectedTime, out int parsedMinutes))
            {
                return parsedMinutes;
            }

            TimeSlot slot = isDeparture ? currentFlight.takeoffSlot : currentFlight.landingSlot;
            if (slot != null)
            {
                return slot.GetTotalMinutes();
            }
        }

        int hour = isDeparture ? tempDepHour : tempArrHour;
        int minute = isDeparture ? tempDepMinute : tempArrMinute;
        return hour * 60 + minute;
    }

    private bool TryParseTime(string value, out int totalMinutes)
    {
        totalMinutes = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;

        string[] parts = value.Split(':');
        if (parts.Length < 2 || !int.TryParse(parts[0], out int hour)) return false;

        string minuteText = parts[1].Length >= 2 ? parts[1].Substring(0, 2) : parts[1];
        if (!int.TryParse(minuteText, out int minute)) return false;
        if (hour < 0 || hour > 23 || minute < 0 || minute > 59) return false;

        totalMinutes = hour * 60 + minute;
        return true;
    }

    private string FormatPickerHour(int absoluteHour)
    {
        int normalizedHour = NormalizeHour(absoluteHour);
        return absoluteHour >= 24 ? $"+{normalizedHour:D2}:00" : $"{normalizedHour:D2}:00";
    }

    private string FormatPickerTime(int totalMinutes)
    {
        int absoluteHour = totalMinutes / 60;
        int minute = totalMinutes % 60;
        int normalizedHour = NormalizeHour(absoluteHour);
        return absoluteHour >= 24 ? $"+{normalizedHour:D2}:{minute:D2}" : $"{normalizedHour:D2}:{minute:D2}";
    }

    private int GetFirstSelectableMinute(int absoluteHour)
    {
        return Mathf.Max(pickerWindowStartMinutes, absoluteHour * 60) % 60;
    }

    private int NormalizeHour(int absoluteHour)
    {
        return ((absoluteHour % 24) + 24) % 24;
    }

    private RectTransform EnsureRectTransform(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null)
        {
            rt = go.AddComponent<RectTransform>();
        }
        return rt;
    }

    private void StretchToParent(RectTransform rt, Vector2 inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = inset;
        rt.offsetMax = -inset;
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private Image EnsureImage(GameObject go, Color color)
    {
        Image image = go.GetComponent<Image>();
        if (image == null)
        {
            image = go.AddComponent<Image>();
        }
        image.color = color;
        image.raycastTarget = true;
        return image;
    }

    private GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject go = CreateLayoutObject(name, parent, -1f, -1f, false);
        Image image = EnsureImage(go, color);
        if (color.a > 0.02f)
        {
            image.sprite = GetRoundedUiSprite();
            image.type = Image.Type.Sliced;
        }
        return go;
    }

    private Sprite GetRoundedUiSprite()
    {
        if (roundedUiSprite != null) return roundedUiSprite;

        const int size = 40;
        const float radius = 10f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "TimePickerRoundedUiTexture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha = GetRoundedRectAlpha(x + 0.5f, y + 0.5f, 0f, size, radius);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        roundedUiSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
        roundedUiSprite.name = "TimePickerRoundedUiSprite";
        roundedUiSprite.hideFlags = HideFlags.HideAndDontSave;
        return roundedUiSprite;
    }

    private Sprite GetRoundedBorderSprite()
    {
        if (roundedBorderSprite != null) return roundedBorderSprite;

        const int size = 40;
        const float radius = 10f;
        const float thickness = 3f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "TimePickerRoundedBorderTexture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float outerAlpha = GetRoundedRectAlpha(px, py, 0f, size, radius);
                float innerAlpha = GetRoundedRectAlpha(px, py, thickness, size - thickness, radius - thickness);
                pixels[y * size + x] = new Color(1f, 1f, 1f, outerAlpha * (1f - innerAlpha));
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        roundedBorderSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
        roundedBorderSprite.name = "TimePickerRoundedBorderSprite";
        roundedBorderSprite.hideFlags = HideFlags.HideAndDontSave;
        return roundedBorderSprite;
    }

    private float GetRoundedRectAlpha(float x, float y, float min, float max, float radius)
    {
        float nearestX = Mathf.Clamp(x, min + radius, max - radius);
        float nearestY = Mathf.Clamp(y, min + radius, max - radius);
        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(nearestX, nearestY));
        return Mathf.Clamp01(radius + 0.5f - distance);
    }

    private void SetRoundedSelectionBorder(GameObject row, bool isSelected)
    {
        if (row == null) return;

        Outline oldOutline = row.GetComponent<Outline>();
        if (oldOutline != null)
        {
            oldOutline.enabled = false;
        }

        Transform borderTransform = row.transform.Find("SelectionBorder");
        GameObject borderGo;
        if (borderTransform == null)
        {
            borderGo = new GameObject("SelectionBorder", typeof(RectTransform));
            borderGo.layer = row.layer;
            borderGo.transform.SetParent(row.transform, false);

            RectTransform borderRt = EnsureRectTransform(borderGo);
            StretchToParent(borderRt, Vector2.zero);
            LayoutElement borderLayout = borderGo.AddComponent<LayoutElement>();
            borderLayout.ignoreLayout = true;

            Image borderImage = borderGo.AddComponent<Image>();
            borderImage.sprite = GetRoundedBorderSprite();
            borderImage.type = Image.Type.Sliced;
            borderImage.color = pickerSelectionBorderColor;
            borderImage.raycastTarget = false;
        }
        else
        {
            borderGo = borderTransform.gameObject;
        }

        borderGo.transform.SetAsLastSibling();
        borderGo.SetActive(isSelected);
    }

    private GameObject CreateLayoutObject(string name, Transform parent, float preferredHeight, float preferredWidth, bool flexible)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        RectTransform rt = EnsureRectTransform(go);
        rt.localScale = Vector3.one;
        AddLayoutElement(go, preferredHeight, preferredWidth, flexible);
        return go;
    }

    private LayoutElement AddLayoutElement(GameObject go, float preferredHeight, float preferredWidth, bool flexible)
    {
        LayoutElement layout = go.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = go.AddComponent<LayoutElement>();
        }

        if (preferredHeight >= 0f) layout.preferredHeight = preferredHeight;
        if (preferredWidth >= 0f) layout.preferredWidth = preferredWidth;
        layout.flexibleWidth = flexible ? 1f : 0f;
        layout.flexibleHeight = flexible ? 1f : 0f;
        return layout;
    }

    private TMP_Text CreateText(string name, Transform parent, string text, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    private Button CreateButton(string name, Transform parent, string label, Color backgroundColor, Color textColor, float fontSize)
    {
        GameObject go = CreatePanel(name, parent, backgroundColor);
        Image background = go.GetComponent<Image>();
        Button button = go.AddComponent<Button>();
        button.targetGraphic = background;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.94f, 0.96f, 0.98f, 1f);
        colors.pressedColor = new Color(0.82f, 0.85f, 0.88f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.06f;
        button.colors = colors;

        if (!string.IsNullOrEmpty(label))
        {
            TMP_Text text = CreateText("Text", go.transform, label, fontSize, FontStyles.Bold, TextAlignmentOptions.Center, textColor);
            StretchToParent(EnsureRectTransform(text.gameObject), new Vector2(8f, 4f));
        }

        return button;
    }

    private ScrollRect CreateScrollRect(string name, Transform parent, out RectTransform content)
    {
        GameObject scrollGo = CreatePanel(name, parent, new Color(0f, 0f, 0f, 0f));
        StretchToParent(EnsureRectTransform(scrollGo), Vector2.zero);
        ScrollRect scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 35f;

        GameObject viewportGo = CreatePanel("Viewport", scrollGo.transform, new Color(1f, 1f, 1f, 0.01f));
        RectTransform viewportRt = EnsureRectTransform(viewportGo);
        StretchToParent(viewportRt, Vector2.zero);
        Image viewportImage = EnsureImage(viewportGo, new Color(1f, 1f, 1f, 0.01f));
        viewportImage.raycastTarget = false;
        RectMask2D rectMask = viewportGo.GetComponent<RectMask2D>();
        if (rectMask == null)
        {
            rectMask = viewportGo.AddComponent<RectMask2D>();
        }

        GameObject contentGo = CreateLayoutObject("Content", viewportGo.transform, -1f, -1f, false);
        content = EnsureRectTransform(contentGo);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = new Vector2(0f, 0f);
        content.offsetMax = Vector2.zero;
        content.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout = contentGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRt;
        scrollRect.content = content;
        return scrollRect;
    }

    private string GetInfoIcon(string label)
    {
        switch (label)
        {
            case "Airport": return "◎";
            case "Current Time": return "◷";
            case "Selected Slot": return "▣";
            default: return "";
        }
    }

    private void ConfirmTimePickerSelection()
    {
        if (isPickingDepartureTime)
        {
            tempDepHour = modalSelectedHour;
            tempDepMinute = modalSelectedMinute;
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
