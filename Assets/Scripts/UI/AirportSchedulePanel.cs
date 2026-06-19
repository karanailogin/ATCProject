using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class AirportSchedulePanel : MonoBehaviour
{
    public static AirportSchedulePanel Instance;
    private static Sprite scheduleRoundedSprite;

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

    [Header("Airport Info Panel Reference")]
    public GameObject airportInfoPanel;

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
    private TextMeshProUGUI pendingHeaderText;

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

        if (airportInfoPanel == null)
        {
            var canvasGo = GameObject.Find("MainCanvas");
            if (canvasGo != null)
            {
                var t = canvasGo.transform.Find("AirportInfoPanel");
                if (t != null)
                {
                    airportInfoPanel = t.gameObject;
                }
            }
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

        // Initialize the airport schedule memory layout and popup
        InitializeScheduleMemoryLayout();
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
        if (WorldClockManager.Instance != null)
        {
            System.DateTime now = WorldClockManager.Instance.CurrentTime;
            selectedHour = now.Hour;
            hourStartOffset = now.Minute <= 15 ? now.Hour - 1 : now.Hour;
        }
        else
        {
            selectedHour = 12;
            hourStartOffset = 12;
        }

        if (selectedAirportHeader != null)
        {
            selectedAirportHeader.text = $"{airport.airportName} OPERATIONS CENTER";
        }

        if (centerHeaderText != null)
        {
            centerHeaderText.text = $"SLOTS FOR {selectedHour:D2}:00";
        }

        // Close details popup on new airport display
        if (slotDetailsPopup != null)
        {
            slotDetailsPopup.SetActive(false);
        }
        selectedSlotHour = -1;
        selectedSlotMinute = -1;
        selectedSlotFlight = null;

        if (airportInfoPanel != null)
        {
            airportInfoPanel.SetActive(false);
        }
        if (PendingFlightsPanel.Instance != null)
        {
            PendingFlightsPanel.Instance.gameObject.SetActive(false);
        }
        if (ActiveFlightsPanel.Instance != null)
        {
            ActiveFlightsPanel.Instance.gameObject.SetActive(false);
        }

        if (panelContainer != null)
        {
            Debug.Log($"[AirportSchedulePanel] Activating panelContainer: '{panelContainer.name}'");
            isOpeningProgrammatically = true;
            panelContainer.SetActive(true);
            isOpeningProgrammatically = false;
            RefreshAll();
            Canvas.ForceUpdateCanvases();
        }
        else
        {
            Debug.LogWarning("[AirportSchedulePanel] Cannot activate panel because panelContainer is null!");
        }
    }

    private void SafeDestroy(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
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

    private void InitializeScheduleMemoryLayout()
    {
        Transform splitContainer = transform.Find("SplitContainer");
        if (splitContainer == null) return;

        List<GameObject> oldChildren = new List<GameObject>();
        foreach (Transform child in splitContainer)
        {
            oldChildren.Add(child.gameObject);
        }
        foreach (GameObject child in oldChildren)
        {
            SafeDestroy(child);
        }

        GameObject hourPanel = CreateScheduleSection("HourNavigatorPanel", splitContainer, new Vector2(0f, 0f), new Vector2(0.18f, 1f));
        GameObject slotPanel = CreateScheduleSection("SlotTimelinePanel", splitContainer, new Vector2(0.18f, 0f), new Vector2(0.75f, 1f));
        GameObject pendingPanel = CreateScheduleSection("PendingInboundPanel", splitContainer, new Vector2(0.75f, 0f), new Vector2(1f, 1f));

        BuildScheduleHourRail(hourPanel.transform);
        BuildScheduleSlotTimeline(slotPanel.transform);
        BuildPendingApprovalQueue(pendingPanel.transform);
    }

    private GameObject CreateScheduleSection(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(6f, 8f);
        rt.offsetMax = new Vector2(-6f, -8f);
        Image image = go.GetComponent<Image>();
        image.color = new Color(0.065f, 0.075f, 0.095f, 0.98f);
        ApplyScheduleRoundedImage(image);
        return go;
    }

    private void BuildScheduleHourRail(Transform parent)
    {
        VerticalLayoutGroup layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 14, 14);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateScheduleText(parent, "Header", "SCHEDULE HOURS", 25f, FontStyles.Bold, TextAlignmentOptions.Center, 50f);

        hourButtons.Clear();
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            Button button = CreateScheduleButton(parent, $"HourButton_{i}", "--:--", 78f, new Color(0.12f, 0.14f, 0.17f));
            button.onClick.AddListener(() => SelectHourFromIndex(index));
            hourButtons.Add(button);
        }
    }

    private void BuildScheduleSlotTimeline(Transform parent)
    {
        VerticalLayoutGroup layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        centerHeaderText = CreateScheduleText(parent, "CenterHeader", "SLOTS FOR 12:00", 26f, FontStyles.Bold, TextAlignmentOptions.Center, 50f);
        slotsGridContainer = CreateScheduleScrollContent(parent, "SlotScrollView");
    }

    private void BuildPendingApprovalQueue(Transform parent)
    {
        VerticalLayoutGroup layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        pendingHeaderText = CreateScheduleText(parent, "PendingHeader", "PENDING INBOUND (0)", 23f, FontStyles.Bold, TextAlignmentOptions.Left, 50f);
        requestsContainer = CreateScheduleScrollContent(parent, "PendingScrollView");
    }

    private TextMeshProUGUI CreateScheduleText(Transform parent, string name, string value, float fontSize, FontStyles style, TextAlignmentOptions alignment, float height)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        go.GetComponent<LayoutElement>().preferredHeight = height;
        return text;
    }

    private Button CreateScheduleButton(Transform parent, string name, string label, float height, Color backgroundColor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.color = backgroundColor;
        ApplyScheduleRoundedImage(image);
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.94f, 0.96f, 0.98f, 1f);
        colors.pressedColor = new Color(0.82f, 0.85f, 0.88f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;

        LayoutElement element = go.GetComponent<LayoutElement>();
        element.preferredHeight = height;

        TextMeshProUGUI text = CreateScheduleText(go.transform, "Text", label, 27f, FontStyles.Bold, TextAlignmentOptions.Center, height);
        RectTransform textRt = text.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(8f, 4f);
        textRt.offsetMax = new Vector2(-8f, -4f);
        text.GetComponent<LayoutElement>().ignoreLayout = true;
        return button;
    }

    private Transform CreateScheduleScrollContent(Transform parent, string name)
    {
        GameObject scrollGo = new GameObject(name, typeof(RectTransform), typeof(ScrollRect), typeof(LayoutElement));
        scrollGo.layer = parent.gameObject.layer;
        scrollGo.transform.SetParent(parent, false);
        LayoutElement scrollElement = scrollGo.GetComponent<LayoutElement>();
        scrollElement.flexibleHeight = 1f;

        GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewportGo.layer = parent.gameObject.layer;
        viewportGo.transform.SetParent(scrollGo.transform, false);
        RectTransform viewportRt = viewportGo.GetComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        viewportGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

        GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGo.layer = parent.gameObject.layer;
        contentGo.transform.SetParent(viewportGo.transform, false);
        RectTransform contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.offsetMin = Vector2.zero;
        contentRt.offsetMax = Vector2.zero;

        VerticalLayoutGroup contentLayout = contentGo.GetComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 6f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scrollGo.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportRt;
        scrollRect.content = contentRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 28f;
        return contentGo.transform;
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
        if (hourStartOffset > 0)
        {
            hourStartOffset--;
            RefreshAll();
        }
    }

    private void ScrollHoursDown()
    {
        if (hourStartOffset < 19)
        {
            hourStartOffset++;
            RefreshAll();
        }
    }

    private void SelectHourFromIndex(int index)
    {
        selectedHour = NormalizeScheduleHour(hourStartOffset + index);
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
            int hourVal = NormalizeScheduleHour(hourStartOffset + i);
            var btn = hourButtons[i];
            var img = btn.GetComponent<Image>();
            var txt = btn.GetComponentInChildren<TMP_Text>();

            bool isSelected = (hourVal == selectedHour);
            bool isPastHour = IsScheduleHourElapsed(hourVal);
            if (txt != null)
            {
                txt.text = $"{hourVal:D2}:00";
            }

            if (img != null)
            {
                img.color = isSelected
                    ? new Color(0.07f, 0.19f, 0.27f, 1f)
                    : (isPastHour ? new Color(0.18f, 0.19f, 0.21f, 1f) : new Color(0.12f, 0.14f, 0.17f, 1f));
            }
            if (txt != null)
            {
                txt.color = isPastHour && !isSelected ? new Color(0.55f, 0.57f, 0.61f, 1f) : Color.white;
                txt.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        if (centerHeaderText != null)
        {
            centerHeaderText.text = $"SLOTS FOR {selectedHour:D2}:00";
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

        // Show all 12 five-minute slots as a vertical schedule timeline.
        for (int m = 0; m < 60; m += 5)
        {
            int minuteVal = m;
            GameObject cardGo = CreateScheduleSlotRow(selectedHour, minuteVal);
            Button btn = cardGo.GetComponent<Button>();
            btn.onClick.AddListener(() => OnSlotCardClicked(selectedHour, minuteVal));
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(slotsGridContainer.GetComponent<RectTransform>());
    }

    private GameObject CreateScheduleSlotRow(int hour, int minute)
    {
        GameObject row = new GameObject($"SlotRow_{hour:D2}_{minute:D2}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
        row.layer = slotsGridContainer.gameObject.layer;
        row.transform.SetParent(slotsGridContainer, false);

        LayoutElement rowElement = row.GetComponent<LayoutElement>();
        rowElement.preferredHeight = 84f;

        bool isSelected = hour == selectedSlotHour && minute == selectedSlotMinute;
        bool isElapsed = IsScheduleSlotElapsed(hour, minute);
        Image image = row.GetComponent<Image>();
        image.color = isSelected
            ? new Color(0.07f, 0.19f, 0.27f, 1f)
            : (isElapsed ? new Color(0.18f, 0.19f, 0.21f, 1f) : new Color(0.105f, 0.12f, 0.15f, 1f));
        ApplyScheduleRoundedImage(image);

        Button button = row.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.94f, 0.96f, 0.98f, 1f);
        colors.pressedColor = new Color(0.82f, 0.85f, 0.88f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = Color.white;
        button.colors = colors;
        button.interactable = !isElapsed;

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 0, 0);
        layout.spacing = 18f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        List<Flight> matchedFlights = GetFlightsForScheduleSlot(hour, minute);
        string status = "Available";
        string flightNumber = "";
        string route = "";
        Color statusColor = colorAvailable;

        if (matchedFlights.Count > 1)
        {
            status = "Slot Conflict";
            flightNumber = $"{matchedFlights.Count} flights";
            statusColor = colorBlocked;
        }
        else if (matchedFlights.Count == 1)
        {
            Flight flight = matchedFlights[0];
            bool isDeparture = flight.fromAirport == currentAirport.airportName;
            status = isDeparture ? "Scheduled DEP" : (flight.landingApproved ? "Scheduled ARR" : "Pending ARR");
            statusColor = isDeparture ? colorConflict : (flight.landingApproved ? colorConfirmed : colorPending);
            flightNumber = flight.flightName;
            route = $"{flight.fromAirport} → {flight.toAirport}";
        }

        if (isElapsed)
        {
            status = matchedFlights.Count == 0 ? "Elapsed" : status;
            statusColor = new Color(0.52f, 0.54f, 0.58f, 1f);
        }

        Color primaryTextColor = isElapsed ? new Color(0.58f, 0.60f, 0.64f, 1f) : Color.white;
        Color routeColor = isElapsed ? new Color(0.48f, 0.50f, 0.54f, 1f) : new Color(0.76f, 0.79f, 0.84f);
        CreateScheduleRowText(row.transform, "Time", $"{hour:D2}:{minute:D2}", 30f, primaryTextColor, FontStyles.Bold, 135f, 0f, TextAlignmentOptions.Left);
        CreateScheduleRowText(row.transform, "Indicator", "●", 23f, statusColor, FontStyles.Bold, 30f, 0f, TextAlignmentOptions.Center);
        CreateScheduleRowText(row.transform, "Status", status, 28f, statusColor, FontStyles.Bold, 1f, 1f, TextAlignmentOptions.Left);
        CreateScheduleRowText(row.transform, "Flight", flightNumber, 26f, statusColor, FontStyles.Bold, 165f, 0f, TextAlignmentOptions.Right);
        CreateScheduleRowText(row.transform, "Route", route, 23f, routeColor, FontStyles.Normal, 220f, 0f, TextAlignmentOptions.Right);
        return row;
    }

    private bool IsScheduleHourElapsed(int hour)
    {
        if (WorldClockManager.Instance == null) return false;

        int hourEnd = GetRelativeScheduleMinutes(hour, 55);
        return hourEnd <= GetScheduleCutoffMinutes();
    }

    private bool IsScheduleSlotElapsed(int hour, int minute)
    {
        if (WorldClockManager.Instance == null) return false;
        return GetRelativeScheduleMinutes(hour, minute) <= GetScheduleCutoffMinutes();
    }

    private int GetScheduleCutoffMinutes()
    {
        System.DateTime now = WorldClockManager.Instance.CurrentTime;
        int roundedMinute = ((now.Minute + 4) / 5) * 5;
        return now.Hour * 60 + roundedMinute;
    }

    private int GetRelativeScheduleMinutes(int hour, int minute)
    {
        System.DateTime now = WorldClockManager.Instance.CurrentTime;
        int nowMinutes = now.Hour * 60 + now.Minute;
        int slotMinutes = hour * 60 + minute;
        int delta = slotMinutes - nowMinutes;

        if (delta < -720) slotMinutes += 1440;
        else if (delta > 720) slotMinutes -= 1440;
        return slotMinutes;
    }

    private int NormalizeScheduleHour(int hour)
    {
        return ((hour % 24) + 24) % 24;
    }

    private TMP_Text CreateScheduleRowText(Transform parent, string name, string value, float fontSize, Color color, FontStyles style, float preferredWidth, float flexibleWidth, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.fontStyle = style;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;

        LayoutElement element = go.GetComponent<LayoutElement>();
        element.preferredWidth = preferredWidth;
        element.flexibleWidth = flexibleWidth;
        return text;
    }

    private List<Flight> GetFlightsForScheduleSlot(int hour, int minute)
    {
        List<Flight> matches = new List<Flight>();
        if (FlightManager.Instance == null || currentAirport == null) return matches;

        foreach (Flight flight in FlightManager.Instance.AllFlights)
        {
            bool departureMatch = flight.fromAirport == currentAirport.airportName && flight.takeoffSlot != null && flight.takeoffSlot.hours == hour && flight.takeoffSlot.minutes == minute;
            bool arrivalMatch = flight.toAirport == currentAirport.airportName && flight.landingSlot != null && flight.landingSlot.hours == hour && flight.landingSlot.minutes == minute;
            if (departureMatch || arrivalMatch)
            {
                matches.Add(flight);
            }
        }
        return matches;
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
                matchedFlights.Add(f);
                if (primaryFlight == null) { primaryFlight = f; }
            }
        }

        // Conflict evaluation
        if (matchedFlights.Count > 1)
        {
            img.color = colorBlocked; // Mark red (#EF4444)
            
            string conflictLines = "";
            foreach (var f in matchedFlights)
            {
                string opType = (f.fromAirport == currentAirport.airportName) ? "DEP" : "ARR";
                conflictLines += $"{hour:D2}:{minute:D2} {opType} {f.flightName} [!]\n";
            }

            tmp.text = $"<size=18><b>{hour:D2}:{minute:D2}</b></size>\n\n" +
                       $"<size=12><b>{conflictLines}</b></size>\n" +
                       $"<color=#FFFFFF><size=11><b>DEP SLOT CONFLICT</b></size></color>" +
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
                selectedSlotFlight = f;
                break;
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
                else
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

            selectedSlotFlight.state = FlightState.FlightCreated; // Reset flight state back to takeoff preparation
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
        if (FlightManager.Instance != null)
        {
            allFlights.AddRange(FlightManager.Instance.AllFlights);
        }

        List<Flight> incomingRequests = new List<Flight>();
        foreach (Flight f in allFlights)
        {
            if (f.toAirport == currentAirport.airportName && 
                !f.landingApproved && f.state != FlightState.Diverted)
            {
                incomingRequests.Add(f);
            }
        }

        if (incomingRequests.Count == 0)
        {
            if (pendingHeaderText != null) pendingHeaderText.text = "PENDING INBOUND (0)";
            CreateNoRequestsPlaceholder();
            return;
        }

        if (pendingHeaderText != null) pendingHeaderText.text = $"PENDING INBOUND ({incomingRequests.Count})";

        foreach (Flight reqFlight in incomingRequests)
        {
            Flight f = reqFlight;
            GameObject cardGo = CreateCompactRequestCard(f);

            var rejectBtn = cardGo.transform.Find("Actions/RejectButton")?.GetComponent<Button>();
            var acceptBtn = cardGo.transform.Find("Actions/AcceptButton")?.GetComponent<Button>();
            var viewBtn = cardGo.transform.Find("Actions/ViewButton")?.GetComponent<Button>();

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

            if (viewBtn != null)
            {
                viewBtn.onClick.RemoveAllListeners();
                viewBtn.onClick.AddListener(() => FocusArrivalSlot(f));
            }
        }

        // Force layout rebuild so requestsContainer (Content) scales correctly to show the card
        if (requestsContainer != null)
        {
            var rt = requestsContainer.GetComponent<RectTransform>();
            if (rt != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
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
        LayoutElement element = go.AddComponent<LayoutElement>();
        element.preferredHeight = 110f;
    }

    private GameObject CreateCompactRequestCard(Flight flight)
    {
        GameObject card = new GameObject($"RequestCard_{flight.flightName}", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup));
        card.layer = requestsContainer.gameObject.layer;
        card.transform.SetParent(requestsContainer, false);
        Image cardImage = card.GetComponent<Image>();
        cardImage.color = new Color(0.105f, 0.12f, 0.15f, 1f);
        ApplyScheduleRoundedImage(cardImage);

        LayoutElement cardElement = card.GetComponent<LayoutElement>();
        cardElement.preferredHeight = 228f;

        VerticalLayoutGroup layout = card.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        string requestedTime = flight.landingSlot != null ? flight.landingSlot.GetTimeString() : flight.expectedArrival;
        bool hasConflict = flight.landingSlot != null && flight.landingSlot.isBooked && flight.landingSlot.bookedByFlight != flight.flightName;

        CreateScheduleText(card.transform, "FlightHeader", $"{flight.flightName}     {requestedTime}", 26f, FontStyles.Bold, TextAlignmentOptions.Left, 36f);
        CreateScheduleText(card.transform, "Route", $"{flight.fromAirport} → {flight.toAirport}", 22f, FontStyles.Normal, TextAlignmentOptions.Left, 30f);
        CreateScheduleText(card.transform, "Aircraft", $"{flight.aircraftType} · ARR Request", 19f, FontStyles.Normal, TextAlignmentOptions.Left, 27f);
        TextMeshProUGUI conflictText = CreateScheduleText(card.transform, "Conflict", hasConflict ? "Slot conflict" : "No conflict", 19f, FontStyles.Bold, TextAlignmentOptions.Left, 27f);
        conflictText.color = hasConflict ? colorBlocked : colorAvailable;

        GameObject actions = new GameObject("Actions", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
        actions.layer = card.layer;
        actions.transform.SetParent(card.transform, false);
        actions.GetComponent<LayoutElement>().preferredHeight = 54f;
        HorizontalLayoutGroup actionLayout = actions.GetComponent<HorizontalLayoutGroup>();
        actionLayout.spacing = 6f;
        actionLayout.childControlWidth = true;
        actionLayout.childControlHeight = true;
        actionLayout.childForceExpandWidth = true;
        actionLayout.childForceExpandHeight = true;

        CreateApprovalActionButton(actions.transform, "RejectButton", "REJECT", new Color(0.62f, 0.16f, 0.18f, 1f));
        CreateApprovalActionButton(actions.transform, "ViewButton", "VIEW", new Color(0.12f, 0.42f, 0.68f, 1f));
        Button acceptButton = CreateApprovalActionButton(actions.transform, "AcceptButton", hasConflict ? "CONFLICT" : "ACCEPT", hasConflict ? colorConflict : colorAvailable);
        acceptButton.interactable = !hasConflict;
        return card;
    }

    private void FocusArrivalSlot(Flight flight)
    {
        if (flight == null || flight.landingSlot == null) return;

        selectedHour = flight.landingSlot.hours;
        hourStartOffset = selectedHour - 2;
        selectedSlotHour = flight.landingSlot.hours;
        selectedSlotMinute = flight.landingSlot.minutes;
        selectedSlotFlight = flight;
        RefreshAll();
    }

    private Button CreateApprovalActionButton(Transform parent, string name, string label, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        ApplyScheduleRoundedImage(image);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.94f, 0.96f, 0.98f, 1f);
        colors.pressedColor = new Color(0.82f, 0.85f, 0.88f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;

        TextMeshProUGUI text = CreateScheduleText(go.transform, "Text", label, 18f, FontStyles.Bold, TextAlignmentOptions.Center, 54f);
        RectTransform textRt = text.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        text.GetComponent<LayoutElement>().ignoreLayout = true;
        return button;
    }

    private GameObject CreateDefaultRequestCard(Flight f)
    {
        // Create the card GameObject with RectTransform from the start
        var go = new GameObject($"RequestCard_{f.flightName}", typeof(RectTransform));
        go.transform.SetParent(requestsContainer, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(340, 175); // Premium mobile request card dimensions

        // Add LayoutElement so VerticalLayoutGroup with childControlHeight knows the exact size
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 340f;
        le.preferredHeight = 175f;
        le.minWidth = 340f;
        le.minHeight = 175f;

        // Use our beautifully styled RoundedRect sprite if available
        var img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.14f, 1.0f);
        Sprite roundedSprite = null;
        var sceneImgs = FindObjectsByType<Image>(FindObjectsInactive.Include);
        foreach (var simg in sceneImgs)
        {
            if (simg.sprite != null && simg.sprite.name == "RoundedRect")
            {
                roundedSprite = simg.sprite;
                break;
            }
        }
        if (roundedSprite != null)
        {
            img.sprite = roundedSprite;
            img.type = Image.Type.Sliced;
        }

        // TEXT FIELD
        var txtGo = new GameObject("Text", typeof(RectTransform));
        txtGo.transform.SetParent(go.transform, false);
        var txtRt = txtGo.GetComponent<RectTransform>();
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
        var rBtnGo = new GameObject("RejectButton", typeof(RectTransform));
        rBtnGo.transform.SetParent(go.transform, false);
        var rRt = rBtnGo.GetComponent<RectTransform>();
        rRt.anchorMin = new Vector2(0, 0);
        rRt.anchorMax = new Vector2(0.5f, 0);
        rRt.pivot = new Vector2(0.5f, 0);
        rRt.anchoredPosition = new Vector2(90, 12); // Positioned correctly within the left half
        rRt.sizeDelta = new Vector2(130, 34); // Scaled for mobile fingers

        var rImg = rBtnGo.AddComponent<Image>();
        rImg.color = new Color(0.24f, 0.24f, 0.27f);
        if (roundedSprite != null)
        {
            rImg.sprite = roundedSprite;
            rImg.type = Image.Type.Sliced;
        }

        rBtnGo.AddComponent<Button>();
        
        var rTxtGo = new GameObject("Text", typeof(RectTransform));
        rTxtGo.transform.SetParent(rBtnGo.transform, false);
        var rTxtRt = rTxtGo.GetComponent<RectTransform>();
        rTxtRt.anchorMin = Vector2.zero;
        rTxtRt.anchorMax = Vector2.one;
        rTxtRt.sizeDelta = Vector2.zero;
        
        var rTxt = rTxtGo.AddComponent<TextMeshProUGUI>();
        rTxt.text = "REJECT";
        rTxt.fontSize = 12; // Bold, clean & readable
        rTxt.fontStyle = FontStyles.Bold;
        rTxt.alignment = TextAlignmentOptions.Center;
        rTxt.color = Color.white;

        // ACCEPT BUTTON
        var aBtnGo = new GameObject("AcceptButton", typeof(RectTransform));
        aBtnGo.transform.SetParent(go.transform, false);
        var aRt = aBtnGo.GetComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0.5f, 0);
        aRt.anchorMax = new Vector2(1, 0);
        aRt.pivot = new Vector2(0.5f, 0);
        aRt.anchoredPosition = new Vector2(-90, 12); // Positioned correctly within the right half
        aRt.sizeDelta = new Vector2(130, 34); // Scaled for mobile fingers

        var aImg = aBtnGo.AddComponent<Image>();
        aImg.color = colorAvailable;
        if (roundedSprite != null)
        {
            aImg.sprite = roundedSprite;
            aImg.type = Image.Type.Sliced;
        }

        aBtnGo.AddComponent<Button>();

        var aTxtGo = new GameObject("Text", typeof(RectTransform));
        aTxtGo.transform.SetParent(aBtnGo.transform, false);
        var aTxtRt = aTxtGo.GetComponent<RectTransform>();
        aTxtRt.anchorMin = Vector2.zero;
        aTxtRt.anchorMax = Vector2.one;
        aTxtRt.sizeDelta = Vector2.zero;

        var aTxt = aTxtGo.AddComponent<TextMeshProUGUI>();
        aTxt.text = "ACCEPT";
        aTxt.fontSize = 12; // Bold, clean & readable
        aTxt.fontStyle = FontStyles.Bold;
        aTxt.alignment = TextAlignmentOptions.Center;
        aTxt.color = Color.white;

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

        f.landingApproved = false;
        f.state = FlightState.FlightCreated;
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

        if (airportInfoPanel != null)
        {
            airportInfoPanel.SetActive(true);
            if (ATCManager.Instance != null && ATCManager.Instance.CurrentAirport != null)
            {
                ATCManager.Instance.CurrentAirport.DisplayFlights();
            }
        }
    }
}
