using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class TopBarController : MonoBehaviour
{
    public static TopBarController Instance;

    [Header("UI Text References")]
    public TMP_Text fundsText;
    public TMP_Text clockText;
    public TMP_Text clockDateText;
    public TMP_Text clockTimeText;
    public TMP_Text activeFlightsText;
    public TMP_Text activeFlightsCountText;
    public TMP_Text activeFlightsLabelText;
    public Button activeFlightsButton;
    public TMP_Text attentionText;

    [Header("Attention Required Container")]
    public UnityEngine.UI.Image attentionBg;
    public Button attentionButton;

    [Header("Time Control Buttons")]
    public Button speedButton;
    public Button pauseButton;

    [Header("Button Colors")]
    public Color activeBtnColor = new Color(0.13f, 0.77f, 0.36f); // Modern Green (#22C55E)
    public Color inactiveBtnColor = new Color(0.18f, 0.18f, 0.20f); // Dark Charcoal
    public Color activeTextColor = Color.white;
    public Color inactiveTextColor = new Color(0.65f, 0.65f, 0.68f); // Dim Grey

    private float savedSpeed = 1f; // Cache the current non-paused speed (1f, 2f, 4f)
    private readonly TMP_Text[] splitFlapDigits = new TMP_Text[4];
    private string lastSplitFlapTime = string.Empty;
    private RawImage conflictIconImage;

    private void Awake()
    {
        if (Instance == null)
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
        BuildSplitFlapClock();
        PolishTopBarLayout();

        // Add Button click listeners
        if (speedButton != null) speedButton.onClick.AddListener(CycleSpeed);
        if (pauseButton != null) pauseButton.onClick.AddListener(TogglePause);
        if (attentionButton != null) attentionButton.onClick.AddListener(ToggleAttentionPanel);
        if (activeFlightsButton != null) activeFlightsButton.onClick.AddListener(ToggleActiveFlightsPanel);

        // Initial visual sync
        UpdateButtonVisuals();

        // Keep the ActiveFlightsPanel open by default on scene play
        ToggleActiveFlightsPanel();
    }

    private void Update()
    {
        RefreshTopBarData();
    }

    public void CycleSpeed()
    {
        if (WorldClockManager.Instance == null) return;

        // Cycle speed: 1x -> 2x -> 4x -> 1x
        if (savedSpeed == 1f)
        {
            savedSpeed = 2f;
        }
        else if (savedSpeed == 2f)
        {
            savedSpeed = 4f;
        }
        else
        {
            savedSpeed = 1f;
        }

        // Set simulation speed
        WorldClockManager.Instance.TimeScale = savedSpeed;
        UpdateButtonVisuals();
    }

    public void TogglePause()
    {
        if (WorldClockManager.Instance == null) return;

        if (Mathf.Approximately(WorldClockManager.Instance.TimeScale, 0f))
        {
            // Resume to last saved speed
            WorldClockManager.Instance.TimeScale = savedSpeed;
        }
        else
        {
            // Pause
            WorldClockManager.Instance.TimeScale = 0f;
        }
        UpdateButtonVisuals();
    }

    public void SetTimeScale(float scale)
    {
        if (WorldClockManager.Instance != null)
        {
            if (scale > 0f)
            {
                savedSpeed = scale;
            }
            WorldClockManager.Instance.TimeScale = scale;
            UpdateButtonVisuals();
        }
    }

    private void RefreshTopBarData()
    {
        // 1. Refresh Funds
        if (fundsText != null && GameManager.Instance != null)
        {
            fundsText.text = GameManager.Instance.GetFormattedFunds();
        }

        // 2. Refresh Date & Time
        if (WorldClockManager.Instance != null)
        {
            if (clockText != null)
            {
                clockText.text = WorldClockManager.Instance.GetFormattedTime();
            }
            if (clockDateText != null)
            {
                clockDateText.text = WorldClockManager.Instance.CurrentTime
                    .ToString("dd MMM", System.Globalization.CultureInfo.InvariantCulture)
                    .ToUpperInvariant();
            }
            if (clockTimeText != null)
            {
                clockTimeText.text = WorldClockManager.Instance.CurrentTime.ToString("HH:mm");
            }

            RefreshSplitFlapDigits(WorldClockManager.Instance.CurrentTime.ToString("HHmm"));
        }

        // 3. Refresh Active Flights
        int activeCount = 0;
        if (FlightManager.Instance != null)
        {
            foreach (var flight in FlightManager.Instance.AllFlights)
            {
                if (ActiveFlightsPanel.IsOperationalFlight(flight))
                {
                    activeCount++;
                }
            }
        }
        if (activeFlightsText != null)
        {
            activeFlightsText.text = $"{activeCount} Flights";
        }
        if (activeFlightsCountText != null)
        {
            activeFlightsCountText.text = activeCount.ToString();
        }
        if (activeFlightsLabelText != null)
        {
            activeFlightsLabelText.text = "✈";
        }

        // 4. Refresh Attention Required Counter
        int attentionCount = 0;
        if (FlightManager.Instance != null)
        {
            foreach (var flight in FlightManager.Instance.AllFlights)
            {
                if (flight != null)
                {
                    if (PendingFlightsPanel.NeedsAttention(flight))
                    {
                        attentionCount++;
                    }
                }
            }
        }

        if (attentionText != null)
        {
            attentionText.text = conflictIconImage != null ? attentionCount.ToString() : $"! {attentionCount}";
        }

        // Update Attention Required background color dynamically based on issues
        if (attentionBg != null)
        {
            if (attentionCount > 0)
            {
                attentionBg.color = new Color(0.94f, 0.27f, 0.27f); // Alert Red (#EF4444)
                if (attentionText != null) attentionText.color = Color.white;
            }
            else
            {
                attentionBg.color = inactiveBtnColor;
                if (attentionText != null) attentionText.color = inactiveTextColor;
            }
        }
    }

    private void BuildSplitFlapClock()
    {
        if (clockDateText == null) return;

        RectTransform clockBox = clockDateText.transform.parent as RectTransform;
        if (clockBox == null) return;

        LayoutElement clockLayout = clockBox.GetComponent<LayoutElement>();
        if (clockLayout != null)
        {
            clockLayout.minWidth = 420f;
            clockLayout.preferredWidth = 470f;
        }

        Image clockBoxImage = clockBox.GetComponent<Image>();
        Sprite roundedSprite = clockBoxImage != null ? clockBoxImage.sprite : null;
        if (clockBoxImage != null)
        {
            clockBoxImage.color = new Color(0.045f, 0.055f, 0.075f, 0.94f);
        }

        RectTransform dateRect = clockDateText.rectTransform;
        dateRect.anchorMin = new Vector2(0f, 0f);
        dateRect.anchorMax = new Vector2(0f, 1f);
        dateRect.pivot = new Vector2(0f, 0.5f);
        dateRect.anchoredPosition = new Vector2(24f, 0f);
        dateRect.sizeDelta = new Vector2(118f, 0f);
        clockDateText.fontSize = 27f;
        clockDateText.fontStyle = FontStyles.Bold;
        clockDateText.alignment = TextAlignmentOptions.Center;
        clockDateText.textWrappingMode = TextWrappingModes.NoWrap;
        clockDateText.color = new Color(0.82f, 0.85f, 0.9f, 1f);

        if (clockTimeText != null)
        {
            clockTimeText.gameObject.SetActive(false);
        }

        Transform existingClock = clockBox.Find("SplitFlapClock");
        if (existingClock != null)
        {
            Destroy(existingClock.gameObject);
        }

        GameObject clockGo = new GameObject("SplitFlapClock", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        RectTransform clockRect = clockGo.GetComponent<RectTransform>();
        clockRect.SetParent(clockBox, false);
        clockRect.anchorMin = new Vector2(1f, 0.5f);
        clockRect.anchorMax = new Vector2(1f, 0.5f);
        clockRect.pivot = new Vector2(1f, 0.5f);
        clockRect.anchoredPosition = new Vector2(-18f, 0f);
        clockRect.sizeDelta = new Vector2(292f, 74f);

        HorizontalLayoutGroup layout = clockGo.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 7f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        splitFlapDigits[0] = CreateSplitFlapDigit(clockRect, "HourTens", roundedSprite);
        splitFlapDigits[1] = CreateSplitFlapDigit(clockRect, "HourOnes", roundedSprite);
        CreateClockColon(clockRect);
        splitFlapDigits[2] = CreateSplitFlapDigit(clockRect, "MinuteTens", roundedSprite);
        splitFlapDigits[3] = CreateSplitFlapDigit(clockRect, "MinuteOnes", roundedSprite);

        if (activeFlightsLabelText != null)
        {
            activeFlightsLabelText.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }

    private void PolishTopBarLayout()
    {
        HorizontalLayoutGroup topLayout = GetComponent<HorizontalLayoutGroup>();
        if (topLayout != null)
        {
            topLayout.padding = new RectOffset(20, 20, 12, 12);
            topLayout.spacing = 14f;
            topLayout.childAlignment = TextAnchor.MiddleLeft;
        }

        Image topBarImage = GetComponent<Image>();
        if (topBarImage != null)
        {
            topBarImage.color = new Color(0.025f, 0.055f, 0.085f, 0.34f);
        }

        Transform fundsBox = fundsText != null ? fundsText.transform.parent : null;
        Transform clockBox = clockDateText != null ? clockDateText.transform.parent : null;
        Transform flightsBox = activeFlightsCountText != null ? activeFlightsCountText.transform.parent : null;
        Transform attentionBox = attentionBg != null ? attentionBg.transform : null;
        Transform controlsBox = speedButton != null ? speedButton.transform.parent : null;

        StyleTopBarCard(fundsBox, 160f);
        StyleTopBarCard(clockBox, 470f);
        StyleTopBarCard(flightsBox, 150f);
        StyleTopBarCard(attentionBox, 112f);

        if (fundsText != null)
        {
            fundsText.fontSize = 32f;
            fundsText.fontStyle = FontStyles.Bold;
            fundsText.color = new Color(0.18f, 0.9f, 0.52f, 1f);
            fundsText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        if (activeFlightsCountText != null)
        {
            activeFlightsCountText.fontSize = 42f;
            activeFlightsCountText.fontStyle = FontStyles.Bold;
            RectTransform countRect = activeFlightsCountText.rectTransform;
            countRect.anchorMin = new Vector2(0.08f, 0f);
            countRect.anchorMax = new Vector2(0.48f, 1f);
            countRect.offsetMin = Vector2.zero;
            countRect.offsetMax = Vector2.zero;
        }
        if (activeFlightsLabelText != null)
        {
            activeFlightsLabelText.text = "✈";
            activeFlightsLabelText.fontSize = 34f;
            activeFlightsLabelText.fontStyle = FontStyles.Normal;
            activeFlightsLabelText.color = new Color(0.42f, 0.78f, 1f, 1f);
            activeFlightsLabelText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        if (attentionText != null)
        {
            attentionText.fontSize = 32f;
            attentionText.fontStyle = FontStyles.Bold;
            attentionText.textWrappingMode = TextWrappingModes.NoWrap;
            RectTransform attentionRect = attentionText.rectTransform;
            attentionRect.anchorMin = new Vector2(0.44f, 0f);
            attentionRect.anchorMax = new Vector2(0.94f, 1f);
            attentionRect.offsetMin = Vector2.zero;
            attentionRect.offsetMax = Vector2.zero;
        }

        Texture2D planeTexture = Resources.Load<Texture2D>("TopBarIcons/plane");
        Texture2D conflictTexture = Resources.Load<Texture2D>("TopBarIcons/conflict");
        Texture2D menuTexture = Resources.Load<Texture2D>("TopBarIcons/menu");
        Texture2D speedTexture = Resources.Load<Texture2D>("TopBarIcons/speed");

        if (planeTexture != null && flightsBox != null)
        {
            if (activeFlightsLabelText != null) activeFlightsLabelText.gameObject.SetActive(false);
            CreateTopBarIcon(flightsBox, "FlightsIcon", planeTexture,
                new Vector2(0.56f, 0.23f), new Vector2(0.88f, 0.77f),
                new Color(0.42f, 0.78f, 1f, 1f));
        }

        if (conflictTexture != null && attentionBox != null)
        {
            conflictIconImage = CreateTopBarIcon(attentionBox, "ConflictIcon", conflictTexture,
                new Vector2(0.1f, 0.25f), new Vector2(0.4f, 0.75f), Color.white);
        }

        if (controlsBox != null)
        {
            LayoutElement controlsLayout = controlsBox.GetComponent<LayoutElement>();
            if (controlsLayout != null)
            {
                controlsLayout.minWidth = 222f;
                controlsLayout.preferredWidth = 222f;
            }

            if (pauseButton != null) pauseButton.transform.SetSiblingIndex(0);
            if (speedButton != null) speedButton.transform.SetSiblingIndex(1);
            SizeControlButton(pauseButton, 106f);
            SizeControlButton(speedButton, 106f);

            TMP_Text menuLabel = pauseButton != null ? pauseButton.GetComponentInChildren<TMP_Text>() : null;
            if (menuTexture != null && pauseButton != null)
            {
                if (menuLabel != null) menuLabel.gameObject.SetActive(false);
                CreateTopBarIcon(pauseButton.transform, "MenuIcon", menuTexture,
                    new Vector2(0.31f, 0.25f), new Vector2(0.69f, 0.75f),
                    new Color(0.86f, 0.9f, 0.95f, 1f));
            }
            else if (menuLabel != null)
            {
                menuLabel.text = "☰";
                menuLabel.fontSize = 34f;
                menuLabel.fontStyle = FontStyles.Bold;
            }

            if (speedButton != null)
            {
                TMP_Text speedLabel = speedButton.GetComponentInChildren<TMP_Text>();
                if (speedLabel != null)
                {
                    RectTransform speedLabelRect = speedLabel.rectTransform;
                    speedLabelRect.anchorMin = new Vector2(0.45f, 0f);
                    speedLabelRect.anchorMax = new Vector2(0.96f, 1f);
                    speedLabelRect.offsetMin = Vector2.zero;
                    speedLabelRect.offsetMax = Vector2.zero;
                }

                if (speedTexture != null)
                {
                    CreateTopBarIcon(speedButton.transform, "SpeedIcon", speedTexture,
                        new Vector2(0.1f, 0.29f), new Vector2(0.4f, 0.71f), Color.white);
                }
            }
        }

        if (controlsBox != null && transform.Find("TopBarSpacer") == null)
        {
            GameObject spacer = new GameObject("TopBarSpacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(transform, false);
            spacer.transform.SetSiblingIndex(controlsBox.GetSiblingIndex());
            LayoutElement spacerLayout = spacer.GetComponent<LayoutElement>();
            spacerLayout.minWidth = 0f;
            spacerLayout.preferredWidth = 0f;
            spacerLayout.flexibleWidth = 1f;
        }

        if (transform.Find("BottomSeparator") == null)
        {
            GameObject separator = new GameObject("BottomSeparator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            RectTransform separatorRect = separator.GetComponent<RectTransform>();
            separatorRect.SetParent(transform, false);
            separatorRect.anchorMin = new Vector2(0f, 0f);
            separatorRect.anchorMax = new Vector2(1f, 0f);
            separatorRect.pivot = new Vector2(0.5f, 0f);
            separatorRect.anchoredPosition = Vector2.zero;
            separatorRect.sizeDelta = new Vector2(0f, 2f);
            Image separatorImage = separator.GetComponent<Image>();
            separatorImage.color = new Color(0.15f, 0.72f, 0.95f, 0.28f);
            separatorImage.raycastTarget = false;
            separator.GetComponent<LayoutElement>().ignoreLayout = true;
        }
    }

    private RawImage CreateTopBarIcon(Transform parent, string objectName, Texture texture,
        Vector2 anchorMin, Vector2 anchorMax, Color tint)
    {
        if (parent == null || texture == null) return null;

        Transform existing = parent.Find(objectName);
        RawImage icon;
        if (existing != null)
        {
            icon = existing.GetComponent<RawImage>();
        }
        else
        {
            GameObject iconGo = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            iconGo.transform.SetParent(parent, false);
            icon = iconGo.GetComponent<RawImage>();
        }

        if (icon == null) return null;

        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = anchorMin;
        iconRect.anchorMax = anchorMax;
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        icon.texture = texture;
        icon.color = tint;
        icon.raycastTarget = false;
        return icon;
    }

    private void StyleTopBarCard(Transform card, float preferredWidth)
    {
        if (card == null) return;

        LayoutElement layout = card.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.minWidth = preferredWidth;
            layout.preferredWidth = preferredWidth;
        }

        Image image = card.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.045f, 0.055f, 0.075f, 0.88f);
        }
    }

    private void SizeControlButton(Button button, float width)
    {
        if (button == null) return;

        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.minWidth = width;
            layout.preferredWidth = width;
        }

        Image image = button.GetComponent<Image>();
        if (image != null && button != speedButton)
        {
            image.color = new Color(0.045f, 0.055f, 0.075f, 0.88f);
        }
    }

    private TMP_Text CreateSplitFlapDigit(Transform parent, string objectName, Sprite roundedSprite)
    {
        GameObject tile = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement), typeof(Shadow));
        tile.transform.SetParent(parent, false);

        LayoutElement tileLayout = tile.GetComponent<LayoutElement>();
        tileLayout.minWidth = 54f;
        tileLayout.preferredWidth = 54f;
        tileLayout.minHeight = 68f;
        tileLayout.preferredHeight = 68f;

        Image tileImage = tile.GetComponent<Image>();
        tileImage.color = new Color(0.105f, 0.115f, 0.14f, 1f);
        tileImage.sprite = roundedSprite;
        tileImage.type = roundedSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        tileImage.raycastTarget = false;

        Shadow shadow = tile.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        shadow.effectDistance = new Vector2(0f, -3f);

        GameObject topShade = new GameObject("TopShade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform topShadeRect = topShade.GetComponent<RectTransform>();
        topShadeRect.SetParent(tile.transform, false);
        topShadeRect.anchorMin = new Vector2(0.06f, 0.5f);
        topShadeRect.anchorMax = new Vector2(0.94f, 0.94f);
        topShadeRect.offsetMin = Vector2.zero;
        topShadeRect.offsetMax = Vector2.zero;
        Image topShadeImage = topShade.GetComponent<Image>();
        topShadeImage.color = new Color(1f, 1f, 1f, 0.035f);
        topShadeImage.raycastTarget = false;

        GameObject textGo = new GameObject("Digit", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.SetParent(tile.transform, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        TextMeshProUGUI digitText = textGo.GetComponent<TextMeshProUGUI>();
        if (clockTimeText != null) digitText.font = clockTimeText.font;
        digitText.text = "0";
        digitText.fontSize = 44f;
        digitText.fontStyle = FontStyles.Bold;
        digitText.alignment = TextAlignmentOptions.Center;
        digitText.color = new Color(0.96f, 0.97f, 1f, 1f);
        digitText.raycastTarget = false;

        GameObject seam = new GameObject("CentreSeam", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform seamRect = seam.GetComponent<RectTransform>();
        seamRect.SetParent(tile.transform, false);
        seamRect.anchorMin = new Vector2(0.04f, 0.5f);
        seamRect.anchorMax = new Vector2(0.96f, 0.5f);
        seamRect.anchoredPosition = Vector2.zero;
        seamRect.sizeDelta = new Vector2(0f, 2f);
        Image seamImage = seam.GetComponent<Image>();
        seamImage.color = new Color(0f, 0f, 0f, 0.72f);
        seamImage.raycastTarget = false;

        return digitText;
    }

    private void CreateClockColon(Transform parent)
    {
        GameObject colonGo = new GameObject("Colon", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        colonGo.transform.SetParent(parent, false);
        LayoutElement colonLayout = colonGo.GetComponent<LayoutElement>();
        colonLayout.minWidth = 22f;
        colonLayout.preferredWidth = 22f;
        colonLayout.minHeight = 68f;

        TextMeshProUGUI colon = colonGo.GetComponent<TextMeshProUGUI>();
        if (clockTimeText != null) colon.font = clockTimeText.font;
        colon.text = ":";
        colon.fontSize = 38f;
        colon.fontStyle = FontStyles.Bold;
        colon.alignment = TextAlignmentOptions.Center;
        colon.color = new Color(0.7f, 0.74f, 0.8f, 1f);
        colon.raycastTarget = false;
    }

    private void RefreshSplitFlapDigits(string digits)
    {
        if (digits == lastSplitFlapTime || digits.Length != 4) return;

        lastSplitFlapTime = digits;
        for (int i = 0; i < splitFlapDigits.Length; i++)
        {
            if (splitFlapDigits[i] != null)
            {
                splitFlapDigits[i].text = digits[i].ToString();
            }
        }
    }

    private void UpdateButtonVisuals()
    {
        float currentScale = 1f;
        if (WorldClockManager.Instance != null)
        {
            currentScale = WorldClockManager.Instance.TimeScale;
        }

        bool isPaused = Mathf.Approximately(currentScale, 0f);

        // Update the speedButton label text dynamically to show the active simulation speed scale
        if (speedButton != null)
        {
            var txt = speedButton.GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                txt.text = $"{savedSpeed:0}×";
            }
        }

        // Speed button is active (Green) if unpaused; otherwise inactive (Charcoal)
        StyleButton(speedButton, !isPaused);

        // Pause button is active (Green) if paused; otherwise inactive (Charcoal)
        StyleButton(pauseButton, isPaused);
    }

    private void StyleButton(Button button, bool isActive)
    {
        if (button == null) return;

        var img = button.GetComponent<UnityEngine.UI.Image>();
        if (img != null)
        {
            img.color = isActive ? activeBtnColor : inactiveBtnColor;
        }

        var txt = button.GetComponentInChildren<TMP_Text>();
        if (txt != null)
        {
            txt.color = isActive ? activeTextColor : inactiveTextColor;
            txt.fontStyle = isActive ? FontStyles.Bold : FontStyles.Normal;
        }
    }

    private void ToggleAttentionPanel()
    {
        if (PendingFlightsPanel.Instance == null)
        {
            var panel = FindAnyObjectByType<PendingFlightsPanel>(FindObjectsInactive.Include);
            if (panel != null)
            {
                PendingFlightsPanel.Instance = panel;
            }
        }

        if (PendingFlightsPanel.Instance != null)
        {
            if (PendingFlightsPanel.Instance.gameObject.activeSelf)
            {
                PendingFlightsPanel.Instance.ClosePanel();
            }
            else
            {
                PendingFlightsPanel.Instance.ShowPanel();
            }
        }
        else
        {
            Debug.LogWarning("PendingFlightsPanel.Instance is not available!");
        }
    }

    private void ToggleActiveFlightsPanel()
    {
        if (ActiveFlightsPanel.Instance == null)
        {
            var panel = FindAnyObjectByType<ActiveFlightsPanel>(FindObjectsInactive.Include);
            if (panel != null)
            {
                ActiveFlightsPanel.Instance = panel;
            }
        }

        if (ActiveFlightsPanel.Instance != null)
        {
            if (ActiveFlightsPanel.Instance.gameObject.activeSelf)
            {
                ActiveFlightsPanel.Instance.ClosePanel();
            }
            else
            {
                ActiveFlightsPanel.Instance.ShowPanel();
            }
        }
        else
        {
            Debug.LogWarning("ActiveFlightsPanel.Instance is not available!");
        }
    }
}
