using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class AirportSchedulePanel : MonoBehaviour
{
    public static AirportSchedulePanel Instance;

    [Header("Panel Container")]
    public GameObject panelContainer;

    [Header("LEFT SIDE: Airport Schedule Grid")]
    public TMP_Text selectedAirportHeader;
    public Transform hourTabsContainer;
    public Transform slotsGridContainer;
    public GameObject hourTabPrefab;
    public GameObject slotCardPrefab;

    [Header("RIGHT SIDE: Pending Flight Requests")]
    public Transform requestsContainer;
    public GameObject requestCardPrefab;

    [Header("Back to Map Button")]
    public Button closeButton;

    [Header("Visual Colors")]
    public Color colorAvailable = new Color(0.13f, 0.77f, 0.36f); // Modern Green (#22C55E)
    public Color colorConfirmed = new Color(0.23f, 0.51f, 0.96f); // Blue (Reserved - #3B82F6)
    public Color colorPending = new Color(0.92f, 0.70f, 0.08f);   // Yellow (#EAB308)
    public Color colorBlocked = new Color(0.94f, 0.27f, 0.27f);   // Red (Maintenance - #EF4444)
    public Color colorConflict = new Color(0.98f, 0.45f, 0.09f);  // Orange (Conflict - #F97316)

    // Redesigned Layout Fields
    private Button upHourButton;
    private Button downHourButton;
    private List<Button> hourButtons = new List<Button>();
    private int hourStartOffset = 10; // Viewport of hours: displays hours from hourStartOffset to hourStartOffset + 4
    private TextMeshProUGUI centerHeaderText;

    // Slot Details Popup Modal
    private GameObject slotDetailsPopup;
    private TMP_Text popupTitleText;
    private TMP_Text popupFlightNoText;
    private TMP_Text popupOriginText;
    private TMP_Text popupDestinationText;
    private TMP_Text popupAircraftText;
    private TMP_Text popupEtaText;
    private TMP_Text popupStatusText;
    private Button popupApproveButton;
    private Button popupRejectButton;
    private Button popupRevokeButton;
    private Button popupCloseButton;

    private Flight selectedSlotFlight = null;
    private int selectedSlotHour = -1;
    private int selectedSlotMinute = -1;

    private Airport currentAirport;
    private int selectedHour = 12;
    private static bool isOpeningProgrammatically = false;

    private void Awake()
    {
        Debug.Log("[AirportSchedulePanel] Awake called.");
        if (Instance == null || Instance == this)
        {
            Instance = this;
            Debug.Log("[AirportSchedulePanel] Singleton instance registered.");
        }
        else
        {
            Debug.LogWarning("[AirportSchedulePanel] Duplicate instance detected. Destroying duplicate GameObject.");
            Destroy(gameObject);
            return;
        }

        if (panelContainer != null && !isOpeningProgrammatically)
        {
            panelContainer.SetActive(false);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() =>
            {
                Debug.Log("[AirportSchedulePanel] Close button (BACK TO MAP) CLICKED.");
                ClosePanel();
            });
        }

        // Initialize redesigned layout and popup
        InitializeLayoutRedesign();
        InitializeDetailsPopup();
    }

    public static void ShowSchedule(Airport airport)
    {
        Debug.Log($"[AirportSchedulePanel] Static ShowSchedule() requested for airport: {(airport != null ? airport.airportName : "NULL")}");
        isOpeningProgrammatically = true;
        if (Instance == null)
        {
            Debug.Log("[AirportSchedulePanel] Instance is null. Searching for panel in the scene (including inactive)...");
            var panels = FindObjectsByType<AirportSchedulePanel>(FindObjectsInactive.Include);
            if (panels.Length > 0)
            {
                Instance = panels[0];
                Debug.Log($"[AirportSchedulePanel] Found panel GameObject: '{Instance.gameObject.name}' in scene.");
            }
        }

        if (Instance == null)
        {
            Debug.LogError("[AirportSchedulePanel] AirportSchedulePanel not found in scene!");
            isOpeningProgrammatically = false;
            return;
        }

        Instance.DisplaySchedule(airport);
        isOpeningProgrammatically = false;
    }

    public void DisplaySchedule(Airport airport)
    {
        if (airport == null)
        {
            Debug.LogError("[AirportSchedulePanel] Cannot display schedule: airport reference is null.");
            return;
        }

        Debug.Log($"[AirportSchedulePanel] DisplaySchedule() called for airport: {airport.airportName}");
        currentAirport = airport;
        selectedHour = 12; // Default to 12:00 hour view
        hourStartOffset = 10; // Center around 12:00 (shows 10:00 to 14:00)

        if (selectedAirportHeader != null)
        {
            selectedAirportHeader.text = $"{airport.airportName} OPERATIONS CENTER";
        }

        if (centerHeaderText != null)
        {
            centerHeaderText.text = $"{airport.airportName} AIRPORT SCHEDULE";
        }

        // Close details popup on new airport display
        if (slotDetailsPopup != null)
        {
            slotDetailsPopup.SetActive(false);
        }
        selectedSlotHour = -1;
        selectedSlotMinute = -1;
        selectedSlotFlight = null;

        RefreshAll();

        if (panelContainer != null)
        {
            Debug.Log($"[AirportSchedulePanel] Activating panelContainer: '{panelContainer.name}'");
            isOpeningProgrammatically = true;
            panelContainer.SetActive(true);
            isOpeningProgrammatically = false;
        }
        else
        {
            Debug.LogWarning("[AirportSchedulePanel] Cannot activate panel because panelContainer is null!");
        }
    }

    private void SafeDestroy(GameObject go)
    {
        if (go == null) return;
        go.transform.SetParent(null);
        if (Application.isPlaying)
        {
            Destroy(go);
        }
        else
        {
            DestroyImmediate(go);
        }
    }

    private void InitializeLayoutRedesign()
    {
        var splitContainer = transform.Find("SplitContainer");
        if (splitContainer == null) return;

        // 1. Center Grid Panel
        var leftSideGridPanel = splitContainer.Find("CenterGridPanel") ?? splitContainer.Find("LeftSideGridPanel");
        if (leftSideGridPanel != null)
        {
            leftSideGridPanel.gameObject.name = "CenterGridPanel";
            var rt = leftSideGridPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.15f, 0.00f);
            rt.anchorMax = new Vector2(0.70f, 1.00f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Destroy the old horizontal tabs bar if it exists
            var oldTabs = leftSideGridPanel.Find("HourTabsBar");
            if (oldTabs != null)
            {
                SafeDestroy(oldTabs.gameObject);
            }

            // Expand slots grid to cover the full panel height nicely
            if (slotsGridContainer != null)
            {
                var slotsRt = slotsGridContainer.GetComponent<RectTransform>();
                slotsRt.anchorMin = new Vector2(0f, 0f);
                slotsRt.anchorMax = new Vector2(1f, 1f);
                slotsRt.offsetMin = new Vector2(30, 30);
                slotsRt.offsetMax = new Vector2(-30, -70); // Space at the top for center header

                var glg = slotsGridContainer.GetComponent<GridLayoutGroup>();
                if (glg != null)
                {
                    glg.cellSize = new Vector2(240, 150);
                    glg.spacing = new Vector2(20, 20);
                    glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    glg.constraintCount = 4;
                }
            }

            // Find existing CenterHeader or create it
            Transform centerHeaderTrans = leftSideGridPanel.Find("CenterHeader");
            GameObject centerHeaderGo;
            if (centerHeaderTrans == null)
            {
                centerHeaderGo = new GameObject("CenterHeader");
                centerHeaderGo.transform.SetParent(leftSideGridPanel, false);
                var headerRt = centerHeaderGo.AddComponent<RectTransform>();
                headerRt.anchorMin = new Vector2(0f, 1f);
                headerRt.anchorMax = new Vector2(1f, 1f);
                headerRt.pivot = new Vector2(0.5f, 1f);
                headerRt.anchoredPosition = new Vector2(0, -15);
                headerRt.sizeDelta = new Vector2(-60, 40);
            }
            else
            {
                centerHeaderGo = centerHeaderTrans.gameObject;
            }

            centerHeaderText = centerHeaderGo.GetComponent<TextMeshProUGUI>() ?? centerHeaderGo.AddComponent<TextMeshProUGUI>();
            centerHeaderText.fontSize = 24; // Nice large size
            centerHeaderText.color = Color.white;
            centerHeaderText.alignment = TextAlignmentOptions.Center;
            centerHeaderText.fontStyle = FontStyles.Bold;
            centerHeaderText.text = currentAirport != null ? $"{currentAirport.airportName} Airport Schedule" : "Airport Schedule";
        }

        // Destroy any duplicate programmatic navigator panels to clean up
        int navCount = 0;
        foreach (Transform child in splitContainer)
        {
            if (child.name == "HourNavigatorPanel")
            {
                navCount++;
                if (navCount > 1)
                {
                    SafeDestroy(child.gameObject);
                }
            }
        }

        // 2. Find or Create the Left Panel - Hour Navigator (Anchors 0% to 15%)
        Transform leftNavTrans = splitContainer.Find("HourNavigatorPanel");
        GameObject leftNavGo;
        if (leftNavTrans == null)
        {
            leftNavGo = new GameObject("HourNavigatorPanel");
            leftNavGo.transform.SetParent(splitContainer, false);
        }
        else
        {
            leftNavGo = leftNavTrans.gameObject;
        }

        var navRt = leftNavGo.GetComponent<RectTransform>() ?? leftNavGo.AddComponent<RectTransform>();
        navRt.anchorMin = new Vector2(0.00f, 0.00f);
        navRt.anchorMax = new Vector2(0.15f, 1.00f);
        navRt.offsetMin = Vector2.zero;
        navRt.offsetMax = Vector2.zero;

        // Visual layout styling: solid dark sidebar
        var navBg = leftNavGo.GetComponent<Image>() ?? leftNavGo.AddComponent<Image>();
        navBg.color = new Color(0.09f, 0.09f, 0.11f, 1.0f);

        // Find or create ButtonsContainer
        Transform buttonsContainerTrans = leftNavGo.transform.Find("ButtonsContainer");
        GameObject buttonsContainerGo;
        if (buttonsContainerTrans == null)
        {
            buttonsContainerGo = new GameObject("ButtonsContainer");
            buttonsContainerGo.transform.SetParent(leftNavGo.transform, false);
        }
        else
        {
            buttonsContainerGo = buttonsContainerTrans.gameObject;
        }

        var btnContRt = buttonsContainerGo.GetComponent<RectTransform>() ?? buttonsContainerGo.AddComponent<RectTransform>();
        btnContRt.anchorMin = new Vector2(0, 0);
        btnContRt.anchorMax = new Vector2(1, 1);
        btnContRt.offsetMin = new Vector2(10, 15);
        btnContRt.offsetMax = new Vector2(-10, -15);

        var vlg = buttonsContainerGo.GetComponent<VerticalLayoutGroup>() ?? buttonsContainerGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15;
        vlg.padding = new RectOffset(5, 5, 5, 5);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        // Find or Create UP Button
        Transform upBtnTrans = buttonsContainerGo.transform.Find("UpButton") ?? buttonsContainerGo.transform.Find("UpHourButton");
        GameObject upBtnGo;
        if (upBtnTrans == null)
        {
            upBtnGo = new GameObject("UpButton");
            upBtnGo.transform.SetParent(buttonsContainerGo.transform, false);
        }
        else
        {
            upBtnGo = upBtnTrans.gameObject;
            upBtnGo.name = "UpButton"; // Standardize name
        }

        var upImg = upBtnGo.GetComponent<Image>() ?? upBtnGo.AddComponent<Image>();
        upImg.color = new Color(0.18f, 0.18f, 0.20f);
        upHourButton = upBtnGo.GetComponent<Button>() ?? upBtnGo.AddComponent<Button>();
        upHourButton.onClick.RemoveAllListeners();
        upHourButton.onClick.AddListener(ScrollHoursUp);

        var upTxt = upBtnGo.GetComponentInChildren<TextMeshProUGUI>();
        if (upTxt == null)
        {
            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(upBtnGo.transform, false);
            upTxt = txtGo.AddComponent<TextMeshProUGUI>();
        }
        var upTxtRt = upTxt.GetComponent<RectTransform>();
        upTxtRt.anchorMin = Vector2.zero;
        upTxtRt.anchorMax = Vector2.one;
        upTxtRt.offsetMin = Vector2.zero;
        upTxtRt.offsetMax = Vector2.zero;
        upTxt.text = "▲";
        upTxt.fontSize = 32; // Taller text arrows
        upTxt.alignment = TextAlignmentOptions.Center;
        upTxt.color = Color.white;

        var upLe = upBtnGo.GetComponent<LayoutElement>() ?? upBtnGo.AddComponent<LayoutElement>();
        upLe.preferredHeight = 110; // Taller Arrow Buttons!
        upLe.preferredWidth = 180;

        // Setup the 5 Hour Selection Buttons
        hourButtons.Clear();
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            Transform hourBtnTrans = buttonsContainerGo.transform.Find($"HourButton_{i}");
            GameObject hourBtnGo;
            if (hourBtnTrans == null)
            {
                hourBtnGo = new GameObject($"HourButton_{i}");
                hourBtnGo.transform.SetParent(buttonsContainerGo.transform, false);
            }
            else
            {
                hourBtnGo = hourBtnTrans.gameObject;
            }

            var cardImg = hourBtnGo.GetComponent<Image>() ?? hourBtnGo.AddComponent<Image>();
            cardImg.color = new Color(0.24f, 0.24f, 0.27f);
            var btn = hourBtnGo.GetComponent<Button>() ?? hourBtnGo.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SelectHourFromIndex(index));

            var btnTxt = hourBtnGo.GetComponentInChildren<TextMeshProUGUI>();
            if (btnTxt == null)
            {
                var txtGo = new GameObject("Text");
                txtGo.transform.SetParent(hourBtnGo.transform, false);
                btnTxt = txtGo.AddComponent<TextMeshProUGUI>();
            }
            var btnTxtRt = btnTxt.GetComponent<RectTransform>();
            btnTxtRt.anchorMin = Vector2.zero;
            btnTxtRt.anchorMax = Vector2.one;
            btnTxtRt.offsetMin = Vector2.zero;
            btnTxtRt.offsetMax = Vector2.zero;
            btnTxt.fontSize = 20; // Larger readable text boxes as requested!
            btnTxt.alignment = TextAlignmentOptions.Center;
            btnTxt.color = Color.white;

            var btnLe = hourBtnGo.GetComponent<LayoutElement>() ?? hourBtnGo.AddComponent<LayoutElement>();
            btnLe.preferredHeight = 120; // Nice large robust boxes!
            btnLe.preferredWidth = 180;

            hourButtons.Add(btn);
        }

        // Find or Create DOWN Button
        Transform downBtnTrans = buttonsContainerGo.transform.Find("DownButton") ?? buttonsContainerGo.transform.Find("DownHourButton");
        GameObject downBtnGo;
        if (downBtnTrans == null)
        {
            downBtnGo = new GameObject("DownButton");
            downBtnGo.transform.SetParent(buttonsContainerGo.transform, false);
        }
        else
        {
            downBtnGo = downBtnTrans.gameObject;
            downBtnGo.name = "DownButton"; // Standardize name
        }

        var downImg = downBtnGo.GetComponent<Image>() ?? downBtnGo.AddComponent<Image>();
        downImg.color = new Color(0.18f, 0.18f, 0.20f);
        downHourButton = downBtnGo.GetComponent<Button>() ?? downBtnGo.AddComponent<Button>();
        downHourButton.onClick.RemoveAllListeners();
        downHourButton.onClick.AddListener(ScrollHoursDown);

        var downTxt = downBtnGo.GetComponentInChildren<TextMeshProUGUI>();
        if (downTxt == null)
        {
            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(downBtnGo.transform, false);
            downTxt = txtGo.AddComponent<TextMeshProUGUI>();
        }
        var downTxtRt = downTxt.GetComponent<RectTransform>();
        downTxtRt.anchorMin = Vector2.zero;
        downTxtRt.anchorMax = Vector2.one;
        downTxtRt.offsetMin = Vector2.zero;
        downTxtRt.offsetMax = Vector2.zero;
        downTxt.text = "▼";
        downTxt.fontSize = 32; // Taller text arrows
        downTxt.alignment = TextAlignmentOptions.Center;
        downTxt.color = Color.white;

        var downLe = downBtnGo.GetComponent<LayoutElement>() ?? downBtnGo.AddComponent<LayoutElement>();
        downLe.preferredHeight = 110; // Taller Arrow Buttons!
        downLe.preferredWidth = 180;
    }

    private void InitializeDetailsPopup()
    {
        // Fullscreen Overlay blocking Raycasts
        slotDetailsPopup = new GameObject("SlotDetailsPopup");
        slotDetailsPopup.transform.SetParent(transform, false);
        var popRt = slotDetailsPopup.AddComponent<RectTransform>();
        popRt.anchorMin = Vector2.zero;
        popRt.anchorMax = Vector2.one;
        popRt.offsetMin = Vector2.zero;
        popRt.offsetMax = Vector2.zero;

        var popBg = slotDetailsPopup.AddComponent<Image>();
        popBg.color = new Color(0f, 0f, 0f, 0.65f);

        var cg = slotDetailsPopup.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = true;

        // Central Dialog Card
        var cardGo = new GameObject("Card");
        cardGo.transform.SetParent(slotDetailsPopup.transform, false);
        var cardRt = cardGo.AddComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(460, 460);
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);

        var cardBg = cardGo.AddComponent<Image>();
        cardBg.color = new Color(0.11f, 0.11f, 0.13f, 1.0f);

        // Gold Accent top line on card
        var accentGo = new GameObject("GoldAccent");
        accentGo.transform.SetParent(cardGo.transform, false);
        var accRt = accentGo.AddComponent<RectTransform>();
        accRt.anchorMin = new Vector2(0, 1);
        accRt.anchorMax = new Vector2(1, 1);
        accRt.pivot = new Vector2(0.5f, 1);
        accRt.anchoredPosition = new Vector2(0, 0);
        accRt.sizeDelta = new Vector2(0, 4);
        accentGo.AddComponent<Image>().color = colorPending;

        // Popup Title Text
        var pTitleGo = new GameObject("Title");
        pTitleGo.transform.SetParent(cardGo.transform, false);
        var ptRt = pTitleGo.AddComponent<RectTransform>();
        ptRt.anchorMin = new Vector2(0, 1);
        ptRt.anchorMax = new Vector2(1, 1);
        ptRt.pivot = new Vector2(0.5f, 1);
        ptRt.anchoredPosition = new Vector2(0, -25);
        ptRt.sizeDelta = new Vector2(-40, 35);

        popupTitleText = pTitleGo.AddComponent<TextMeshProUGUI>();
        popupTitleText.text = "SLOT DETAILS";
        popupTitleText.fontSize = 20;
        popupTitleText.alignment = TextAlignmentOptions.Center;
        popupTitleText.color = Color.white;
        popupTitleText.fontStyle = FontStyles.Bold;

        // Content Vertical Stack inside card
        var contentGo = new GameObject("ContentPanel");
        contentGo.transform.SetParent(cardGo.transform, false);
        var cRt = contentGo.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0, 0);
        cRt.anchorMax = new Vector2(1, 1);
        cRt.offsetMin = new Vector2(40, 150); // Room at the bottom for Actions grid
        cRt.offsetMax = new Vector2(-40, -80);

        var cvlg = contentGo.AddComponent<VerticalLayoutGroup>();
        cvlg.spacing = 14;
        cvlg.childAlignment = TextAnchor.UpperLeft;
        cvlg.childControlHeight = true;
        cvlg.childControlWidth = true;
        cvlg.childForceExpandHeight = false;
        cvlg.childForceExpandWidth = true;

        // Details lines
        popupFlightNoText = CreateDetailTextLine(contentGo.transform, "Flight Number: --");
        popupOriginText = CreateDetailTextLine(contentGo.transform, "Origin: --");
        popupDestinationText = CreateDetailTextLine(contentGo.transform, "Destination: --");
        popupAircraftText = CreateDetailTextLine(contentGo.transform, "Aircraft: --");
        popupEtaText = CreateDetailTextLine(contentGo.transform, "ETA: --");
        popupStatusText = CreateDetailTextLine(contentGo.transform, "Status: --");

        // Action Buttons Grid Panel
        var buttonsGo = new GameObject("ActionsPanel");
        buttonsGo.transform.SetParent(cardGo.transform, false);
        var bRt = buttonsGo.AddComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0, 0);
        bRt.anchorMax = new Vector2(1, 0);
        bRt.pivot = new Vector2(0.5f, 0);
        bRt.anchoredPosition = new Vector2(0, 20);
        bRt.sizeDelta = new Vector2(-60, 110);

        var bglg = buttonsGo.AddComponent<GridLayoutGroup>();
        bglg.cellSize = new Vector2(190, 42);
        bglg.spacing = new Vector2(20, 12);
        bglg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        bglg.constraintCount = 2;

        // Button Layout allocations
        popupApproveButton = CreatePopupButton(buttonsGo.transform, "ApproveButton", "APPROVE SLOT", colorAvailable);
        popupApproveButton.onClick.AddListener(() => ApproveSelectedSlot());

        popupRejectButton = CreatePopupButton(buttonsGo.transform, "RejectButton", "REJECT SLOT", new Color(0.85f, 0.18f, 0.18f));
        popupRejectButton.onClick.AddListener(() => RejectSelectedSlot());

        popupRevokeButton = CreatePopupButton(buttonsGo.transform, "RevokeButton", "REVOKE SLOT", colorConflict);
        popupRevokeButton.onClick.AddListener(() => RevokeSelectedSlot());

        popupCloseButton = CreatePopupButton(buttonsGo.transform, "CloseButton", "CLOSE", new Color(0.24f, 0.24f, 0.27f));
        popupCloseButton.onClick.AddListener(() => {
            slotDetailsPopup.SetActive(false);
            selectedSlotHour = -1;
            selectedSlotMinute = -1;
            RedrawSlotsGrid();
        });

        slotDetailsPopup.SetActive(false);
    }

    private TMP_Text CreateDetailTextLine(Transform parent, string initialText)
    {
        var go = new GameObject("DetailLine");
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = initialText;
        txt.fontSize = 15;
        txt.color = new Color(0.90f, 0.90f, 0.93f);
        return txt;
    }

    private Button CreatePopupButton(Transform parent, string name, string text, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        var btn = go.AddComponent<Button>();

        var txt = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        txt.transform.SetParent(go.transform, false);
        txt.text = text;
        txt.fontSize = 12;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        txt.fontStyle = FontStyles.Bold;

        var lay = go.AddComponent<LayoutElement>();
        lay.preferredWidth = 190;
        lay.preferredHeight = 42;

        return btn;
    }

    private void ScrollHoursUp()
    {
        if (hourStartOffset > 6)
        {
            hourStartOffset--;
            RefreshAll();
        }
    }

    private void ScrollHoursDown()
    {
        if (hourStartOffset < 17)
        {
            hourStartOffset++;
            RefreshAll();
        }
    }

    private void SelectHourFromIndex(int index)
    {
        selectedHour = hourStartOffset + index;
        RefreshAll();
    }

    public void RefreshAll()
    {
        if (currentAirport == null) return;

        RedrawHourTabs();
        RedrawSlotsGrid();
        RedrawPendingRequests();
    }

    private void RedrawHourTabs()
    {
        if (hourButtons == null || hourButtons.Count < 5) return;

        for (int i = 0; i < 5; i++)
        {
            int hourVal = hourStartOffset + i;
            var btn = hourButtons[i];
            var img = btn.GetComponent<Image>();
            var txt = btn.GetComponentInChildren<TMP_Text>();

            bool isSelected = (hourVal == selectedHour);
            if (txt != null)
            {
                txt.text = isSelected ? $"► {hourVal:D2}:00" : $"{hourVal:D2}:00";
            }

            if (img != null)
            {
                img.color = isSelected ? colorAvailable : new Color(0.24f, 0.24f, 0.27f);
            }
            if (txt != null)
            {
                txt.color = isSelected ? Color.black : new Color(0.85f, 0.85f, 0.88f);
                txt.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
            }
        }
    }

    private void RedrawSlotsGrid()
    {
        if (slotsGridContainer == null || currentAirport == null || FlightManager.Instance == null) return;

        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in slotsGridContainer)
        {
            if (slotCardPrefab != null && child.gameObject == slotCardPrefab) continue;
            childrenToDestroy.Add(child.gameObject);
        }

        foreach (var child in childrenToDestroy)
        {
            SafeDestroy(child);
        }

        // Show all 12 5-minute slots for the selected hour
        for (int m = 0; m < 60; m += 5)
        {
            int minuteVal = m;
            GameObject cardGo;
            if (slotCardPrefab != null)
            {
                cardGo = Instantiate(slotCardPrefab, slotsGridContainer);
                cardGo.SetActive(true);
            }
            else
            {
                cardGo = CreateDefaultSlotCard(minuteVal);
            }

            ConfigureSlotCardState(cardGo, selectedHour, minuteVal);

            var btn = cardGo.GetComponent<Button>() ?? cardGo.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnSlotCardClicked(selectedHour, minuteVal));
        }
    }

    private GameObject CreateDefaultSlotCard(int minute)
    {
        var go = new GameObject($"SlotCard_{minute:D2}");
        go.transform.SetParent(slotsGridContainer, false);

        var img = go.AddComponent<Image>();
        img.color = colorAvailable;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var r = labelGo.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(10, 10);
        r.offsetMax = new Vector2(-10, -10);

        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 13; // Clean, premium base font size
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return go;
    }

    private void ConfigureSlotCardState(GameObject cardGo, int hour, int minute)
    {
        var img = cardGo.GetComponent<Image>();
        var tmp = cardGo.GetComponentInChildren<TextMeshProUGUI>();
        if (img == null || tmp == null) return;

        // Custom selected highlight styling
        bool isSlotSelectedCard = (hour == selectedSlotHour && minute == selectedSlotMinute && slotDetailsPopup != null && slotDetailsPopup.activeSelf);

        // Maintenance state detection
        if (minute == 55)
        {
            img.color = colorBlocked;
            tmp.text = $"<size=20><b>{hour:D2}:{minute:D2}</b></size>\n\n<color=#EF4444><size=13><b>MAINTENANCE</b></size></color>\n<size=11>Runway Closed</size>" + 
                       (isSlotSelectedCard ? "\n<b>[SELECTED]</b>" : "");
            return;
        }

        // Identify occupying flights
        List<Flight> allFlights = new List<Flight>();
        var allAirports = FindObjectsByType<Airport>(FindObjectsInactive.Include);
        foreach (var ap in allAirports)
        {
            if (FlightManager.Instance != null)
            {
                allFlights.AddRange(FlightManager.Instance.GetFlightsAtAirport(ap));
            }
        }

        List<Flight> matchedFlights = new List<Flight>();
        Flight primaryFlight = null;
        bool hasDeparture = false;

        foreach (Flight f in allFlights)
        {
            // Departures from current airport
            if (f.fromAirport == currentAirport.airportName && f.takeoffSlot != null && f.takeoffSlot.hours == hour && f.takeoffSlot.minutes == minute)
            {
                matchedFlights.Add(f);
                if (primaryFlight == null) { primaryFlight = f; hasDeparture = true; }
            }
            // Arrivals to current airport
            if (f.toAirport == currentAirport.airportName && f.landingSlot != null && f.landingSlot.hours == hour && f.landingSlot.minutes == minute)
            {
                if (f.status == "Pending Approval" || f.landingApproved)
                {
                    matchedFlights.Add(f);
                    if (primaryFlight == null) { primaryFlight = f; }
                }
            }
        }

        // Conflict evaluation
        if (matchedFlights.Count > 1)
        {
            img.color = colorConflict;
            tmp.text = $"<size=20><b>{hour:D2}:{minute:D2}</b></size>\n\n<color=#FFFFFF><size=13><b>CONFLICT ({matchedFlights.Count})</b></size></color>\n<size=11>Multiple Bookings</size>" +
                       (isSlotSelectedCard ? "\n<b>[SELECTED]</b>" : "");
            return;
        }

        if (primaryFlight != null)
        {
            if (hasDeparture)
            {
                img.color = colorConfirmed;
                tmp.text = $"<size=20><b>{hour:D2}:{minute:D2}</b></size>\n\n<b>{primaryFlight.flightName}</b>\n<size=11>{primaryFlight.fromAirport} → {primaryFlight.toAirport}</size>\n\n<size=11><b>CONFIRMED DEP</b></size>" +
                           (isSlotSelectedCard ? "\n<b>[SELECTED]</b>" : "");
            }
            else
            {
                if (primaryFlight.landingApproved)
                {
                    img.color = colorConfirmed;
                    tmp.text = $"<size=20><b>{hour:D2}:{minute:D2}</b></size>\n\n<b>{primaryFlight.flightName}</b>\n<size=11>{primaryFlight.fromAirport} → {primaryFlight.toAirport}</size>\n\n<size=11><b>RESERVED ARR</b></size>" +
                               (isSlotSelectedCard ? "\n<b>[SELECTED]</b>" : "");
                }
                else
                {
                    img.color = colorPending;
                    tmp.text = $"<size=20><b>{hour:D2}:{minute:D2}</b></size>\n\n<b>{primaryFlight.flightName}</b>\n<size=11>{primaryFlight.fromAirport} → {primaryFlight.toAirport}</size>\n\n<color=#000000><size=11><b>PENDING ARR</b></size></color>" +
                               (isSlotSelectedCard ? "\n<b>[SELECTED]</b>" : "");
                }
            }
        }
        else
        {
            // Available
            img.color = colorAvailable;
            tmp.text = $"<size=20><b>{hour:D2}:{minute:D2}</b></size>\n\n<color=#FFFFFF><size=13><b>AVAILABLE</b></size></color>\n<size=11>Free Slot</size>" +
                       (isSlotSelectedCard ? "\n<b>[SELECTED]</b>" : "");
        }
    }

    private void OnSlotCardClicked(int hour, int minute)
    {
        selectedSlotHour = hour;
        selectedSlotMinute = minute;
        selectedSlotFlight = null;

        // Find occupying flight at selected coordinates
        List<Flight> allFlights = new List<Flight>();
        var allAirports = FindObjectsByType<Airport>(FindObjectsInactive.Include);
        foreach (var ap in allAirports)
        {
            if (FlightManager.Instance != null)
            {
                allFlights.AddRange(FlightManager.Instance.GetFlightsAtAirport(ap));
            }
        }

        foreach (Flight f in allFlights)
        {
            if (f.fromAirport == currentAirport.airportName && f.takeoffSlot != null && f.takeoffSlot.hours == hour && f.takeoffSlot.minutes == minute)
            {
                selectedSlotFlight = f;
                break;
            }
            if (f.toAirport == currentAirport.airportName && f.landingSlot != null && f.landingSlot.hours == hour && f.landingSlot.minutes == minute)
            {
                if (f.status == "Pending Approval" || f.landingApproved)
                {
                    selectedSlotFlight = f;
                    break;
                }
            }
        }

        popupTitleText.text = $"SLOT DETAILS - {hour:D2}:{minute:D2}";

        if (selectedSlotFlight != null)
        {
            popupFlightNoText.text = $"<b>Flight Number:</b> {selectedSlotFlight.flightName}";
            popupOriginText.text = $"<b>Origin:</b> {selectedSlotFlight.fromAirport}";
            popupDestinationText.text = $"<b>Destination:</b> {selectedSlotFlight.toAirport}";
            popupAircraftText.text = $"<b>Aircraft:</b> {selectedSlotFlight.aircraftType}";
            popupEtaText.text = $"<b>ETA/Time:</b> {hour:D2}:{minute:D2}";

            string statusStr = selectedSlotFlight.status;
            if (selectedSlotFlight.toAirport == currentAirport.airportName)
            {
                if (selectedSlotFlight.landingApproved)
                {
                    statusStr = "Reserved / Confirmed Arrival";
                }
                else if (selectedSlotFlight.status == "Pending Approval")
                {
                    statusStr = "Awaiting Landing Approval";
                }
            }
            else
            {
                statusStr = "Confirmed Departure";
            }
            popupStatusText.text = $"<b>Status:</b> {statusStr}";

            // Enable action options based on slot reservation state
            if (selectedSlotFlight.toAirport == currentAirport.airportName)
            {
                if (selectedSlotFlight.landingApproved)
                {
                    popupApproveButton.gameObject.SetActive(false);
                    popupRejectButton.gameObject.SetActive(false);
                    popupRevokeButton.gameObject.SetActive(true);
                }
                else
                {
                    popupApproveButton.gameObject.SetActive(true);
                    popupRejectButton.gameObject.SetActive(true);
                    popupRevokeButton.gameObject.SetActive(false);
                }
            }
            else
            {
                // Departure slots can be revoked as well
                popupApproveButton.gameObject.SetActive(false);
                popupRejectButton.gameObject.SetActive(false);
                popupRevokeButton.gameObject.SetActive(true);
            }
        }
        else
        {
            popupFlightNoText.text = "<b>Slot Status:</b> Empty / Available";
            popupOriginText.text = "<b>Origin:</b> --";
            popupDestinationText.text = "<b>Destination:</b> --";
            popupAircraftText.text = "<b>Aircraft:</b> --";
            popupEtaText.text = $"<b>Time:</b> {hour:D2}:{minute:D2}";

            if (minute == 55)
            {
                popupStatusText.text = "<b>Status:</b> MAINTENANCE - Runway Closed";
            }
            else
            {
                popupStatusText.text = "<b>Status:</b> AVAILABLE - Free Slot";
            }

            popupApproveButton.gameObject.SetActive(false);
            popupRejectButton.gameObject.SetActive(false);
            popupRevokeButton.gameObject.SetActive(false);
        }

        RedrawSlotsGrid();
        slotDetailsPopup.SetActive(true);
    }

    private void ApproveSelectedSlot()
    {
        if (selectedSlotFlight != null)
        {
            ProcessAccept(selectedSlotFlight);
            slotDetailsPopup.SetActive(false);
            selectedSlotHour = -1;
            selectedSlotMinute = -1;
            RefreshAll();
        }
    }

    private void RejectSelectedSlot()
    {
        if (selectedSlotFlight != null)
        {
            ProcessReject(selectedSlotFlight);
            slotDetailsPopup.SetActive(false);
            selectedSlotHour = -1;
            selectedSlotMinute = -1;
            RefreshAll();
        }
    }

    private void RevokeSelectedSlot()
    {
        if (selectedSlotFlight != null)
        {
            Debug.Log($"[AirportSchedulePanel] Revoking slot for flight: {selectedSlotFlight.flightName}");

            if (TimeSlotManager.Instance != null)
            {
                if (selectedSlotFlight.fromAirport == currentAirport.airportName && selectedSlotFlight.takeoffSlot != null)
                {
                    TimeSlotManager.Instance.ReleaseTimeSlot(selectedSlotFlight.fromAirport, selectedSlotFlight.takeoffSlot);
                    selectedSlotFlight.takeoffSlot = null;
                }
                else if (selectedSlotFlight.toAirport == currentAirport.airportName && selectedSlotFlight.landingSlot != null)
                {
                    TimeSlotManager.Instance.ReleaseTimeSlot(selectedSlotFlight.toAirport, selectedSlotFlight.landingSlot);
                    selectedSlotFlight.landingSlot = null;
                }
            }

            selectedSlotFlight.status = "Boarding"; // Reset flight status back to takeoff preparation
            selectedSlotFlight.landingApproved = false;
            selectedSlotFlight.expectedArrival = "NA";
            selectedSlotFlight.arrivalTime = "NA";

            Debug.Log($"[AirportSchedulePanel] Flight {selectedSlotFlight.flightName} reservation successfully REVOKED.");

            slotDetailsPopup.SetActive(false);
            selectedSlotHour = -1;
            selectedSlotMinute = -1;

            RefreshAll();

            if (selectedSlotFlight.currentAirport != null)
            {
                selectedSlotFlight.currentAirport.DisplayFlights();
            }
        }
    }

    private void RedrawPendingRequests()
    {
        if (requestsContainer == null || currentAirport == null) return;

        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in requestsContainer)
        {
            if (requestCardPrefab != null && child.gameObject == requestCardPrefab) continue;
            childrenToDestroy.Add(child.gameObject);
        }

        foreach (var child in childrenToDestroy)
        {
            SafeDestroy(child);
        }

        List<Flight> allFlights = new List<Flight>();
        var allAirports = FindObjectsByType<Airport>(FindObjectsInactive.Include);
        foreach (var ap in allAirports)
        {
            if (FlightManager.Instance != null)
            {
                allFlights.AddRange(FlightManager.Instance.GetFlightsAtAirport(ap));
            }
        }

        List<Flight> incomingRequests = new List<Flight>();
        foreach (Flight f in allFlights)
        {
            if (f.toAirport == currentAirport.airportName && f.status == "Pending Approval")
            {
                incomingRequests.Add(f);
            }
        }

        if (incomingRequests.Count == 0)
        {
            CreateNoRequestsPlaceholder();
            return;
        }

        foreach (Flight reqFlight in incomingRequests)
        {
            Flight f = reqFlight;
            GameObject cardGo;
            if (requestCardPrefab != null)
            {
                cardGo = Instantiate(requestCardPrefab, requestsContainer);
                cardGo.SetActive(true);
            }
            else
            {
                cardGo = CreateDefaultRequestCard(f);
            }

            var rejectBtn = cardGo.transform.Find("RejectButton")?.GetComponent<Button>();
            var acceptBtn = cardGo.transform.Find("AcceptButton")?.GetComponent<Button>();

            if (rejectBtn != null)
            {
                rejectBtn.onClick.RemoveAllListeners();
                rejectBtn.onClick.AddListener(() => ProcessReject(f));
            }

            if (acceptBtn != null)
            {
                acceptBtn.onClick.RemoveAllListeners();
                acceptBtn.onClick.AddListener(() => ProcessAccept(f));
            }
        }
    }

    private void CreateNoRequestsPlaceholder()
    {
        var go = new GameObject("Placeholder");
        go.transform.SetParent(requestsContainer, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "No pending inbound requests.";
        tmp.fontSize = 14;
        tmp.color = new Color(0.55f, 0.55f, 0.58f);
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private GameObject CreateDefaultRequestCard(Flight f)
    {
        var go = new GameObject($"RequestCard_{f.flightName}");
        go.transform.SetParent(requestsContainer, false);

        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(340, 175); // Premium mobile request card dimensions

        var img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.14f, 1.0f);

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        var txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = new Vector2(0, 0);
        txtRt.anchorMax = new Vector2(1, 1);
        txtRt.offsetMin = new Vector2(20, 52); // Extra padding for beautiful breathing room
        txtRt.offsetMax = new Vector2(-20, -15);

        var tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 13; // Clean base detail text size
        tmp.color = Color.white;
        tmp.text = $"<size=17><b>{f.flightName}</b></size>\n" +
                   $"Route: {f.fromAirport} → {f.toAirport}\n" +
                   $"Requested Arrival: {f.expectedArrival}\n" +
                   $"Aircraft: {f.aircraftType}\n" +
                   $"Priority: {f.priority}";

        // REJECT BUTTON
        var rBtnGo = new GameObject("RejectButton");
        rBtnGo.transform.SetParent(go.transform, false);
        var rRt = rBtnGo.AddComponent<RectTransform>();
        rRt.anchorMin = new Vector2(0, 0);
        rRt.anchorMax = new Vector2(0.5f, 0);
        rRt.pivot = new Vector2(0.5f, 0);
        rRt.anchoredPosition = new Vector2(20, 12);
        rRt.sizeDelta = new Vector2(130, 34); // Scaled for mobile fingers

        rBtnGo.AddComponent<Image>().color = new Color(0.24f, 0.24f, 0.27f);
        rBtnGo.AddComponent<Button>();
        var rTxt = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        rTxt.transform.SetParent(rBtnGo.transform, false);
        rTxt.text = "REJECT";
        rTxt.fontSize = 12; // Bold, clean & readable
        rTxt.fontStyle = FontStyles.Bold;
        rTxt.alignment = TextAlignmentOptions.Center;

        // ACCEPT BUTTON
        var aBtnGo = new GameObject("AcceptButton");
        aBtnGo.transform.SetParent(go.transform, false);
        var aRt = aBtnGo.AddComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0.5f, 0);
        aRt.anchorMax = new Vector2(1, 0);
        aRt.pivot = new Vector2(0.5f, 0);
        aRt.anchoredPosition = new Vector2(-20, 12);
        aRt.sizeDelta = new Vector2(130, 34); // Scaled for mobile fingers

        aBtnGo.AddComponent<Image>().color = colorAvailable;
        aBtnGo.AddComponent<Button>();
        var aTxt = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        aTxt.transform.SetParent(aBtnGo.transform, false);
        aTxt.text = "ACCEPT";
        aTxt.fontSize = 12; // Bold, clean & readable
        aTxt.fontStyle = FontStyles.Bold;
        aTxt.alignment = TextAlignmentOptions.Center;

        return go;
    }

    private void ProcessAccept(Flight f)
    {
        if (f == null)
        {
            Debug.LogError("[AirportSchedulePanel] ProcessAccept called with null flight reference!");
            return;
        }

        Debug.Log($"[AirportSchedulePanel] ACCEPT button CLICKED for flight: {f.flightName} ({f.fromAirport} -> {f.toAirport})");

        if (TimeSlotManager.Instance == null)
        {
            Debug.LogError("[AirportSchedulePanel] TimeSlotManager.Instance is null!");
            return;
        }

        if (f.landingSlot != null)
        {
            if (f.landingSlot.isBooked && f.landingSlot.bookedByFlight != f.flightName)
            {
                Debug.LogWarning($"[AirportSchedulePanel] Cannot approve: requested arrival slot {f.landingSlot.GetTimeString()} is already booked by another flight: '{f.landingSlot.bookedByFlight}'!");
                return;
            }

            TimeSlotManager.Instance.BookTimeSlot(f.toAirport, f.landingSlot, f.flightName);
        }

        f.landingApproved = true;
        f.status = "Approved";

        Debug.Log($"[AirportSchedulePanel] Flight {f.flightName} arrival slot successfully APPROVED at {currentAirport.airportName}.");

        RefreshAll();

        if (f.currentAirport != null)
        {
            f.currentAirport.DisplayFlights();
        }
    }

    private void ProcessReject(Flight f)
    {
        if (f == null)
        {
            Debug.LogError("[AirportSchedulePanel] ProcessReject called with null flight reference!");
            return;
        }

        Debug.Log($"[AirportSchedulePanel] REJECT button CLICKED for flight: {f.flightName} ({f.fromAirport} -> {f.toAirport})");

        if (TimeSlotManager.Instance == null)
        {
            Debug.LogError("[AirportSchedulePanel] TimeSlotManager.Instance is null!");
            return;
        }

        if (f.landingSlot != null)
        {
            TimeSlotManager.Instance.ReleaseTimeSlot(f.toAirport, f.landingSlot);
            f.landingSlot = null;
        }

        f.status = "Rejected";
        f.landingApproved = false;
        f.expectedArrival = "NA";
        f.arrivalTime = "NA";

        Debug.Log($"[AirportSchedulePanel] Flight {f.flightName} arrival request successfully REJECTED at {currentAirport.airportName}.");

        RefreshAll();

        if (f.currentAirport != null)
        {
            f.currentAirport.DisplayFlights();
        }
    }

    public void ClosePanel()
    {
        Debug.Log("[AirportSchedulePanel] ClosePanel() executing.");
        if (panelContainer != null)
        {
            Debug.Log($"[AirportSchedulePanel] Deactivating panelContainer: '{panelContainer.name}'");
            panelContainer.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[AirportSchedulePanel] Cannot close panel because panelContainer is null!");
        }
    }
}