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
                clockDateText.text = WorldClockManager.Instance.CurrentTime.ToString("dd MMM yyyy");
            }
            if (clockTimeText != null)
            {
                clockTimeText.text = WorldClockManager.Instance.CurrentTime.ToString("HH:mm");
            }
        }

        // 3. Refresh Active Flights
        int activeCount = 0;
        if (FlightManager.Instance != null)
        {
            foreach (var flight in FlightManager.Instance.AllFlights)
            {
                if (flight != null && flight.state != FlightState.Landed && flight.state != FlightState.Diverted)
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
            activeFlightsLabelText.text = activeCount == 1 ? "Flight" : "Flights";
        }

        // 4. Refresh Attention Required Counter
        int attentionCount = 0;
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
                        attentionCount++;
                    }
                }
            }
        }

        if (attentionText != null)
        {
            attentionText.text = $"[!] {attentionCount}";
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
                txt.text = $"{savedSpeed}x";
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