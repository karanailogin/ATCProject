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

    private TMP_Text headerText;

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
        StyleHeader();
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
                string key = f.flightName + "_" + f.state + "_" + f.status + "_" + f.landingApproved;
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
                    if (NeedsAttention(flight))
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
        UpdateHeaderCount(lastPendingCount);
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
            string key = flight.flightName + "_" + flight.state + "_" + flight.status + "_" + flight.landingApproved;
            lastStatesHash.Add(key);
        }
    }

    public static bool NeedsAttention(Flight flight)
    {
        if (flight == null) return false;

        return flight.state == FlightState.SlotConflict ||
            (flight.state == FlightState.FlightCreated && !flight.landingApproved) ||
            flight.state == FlightState.Holding ||
            flight.state == FlightState.Diverted;
    }

    private void StyleHeader()
    {
        Transform header = transform.Find("HeaderPanel");
        if (header == null) return;

        headerText = header.Find("HeaderText")?.GetComponent<TMP_Text>();
        if (headerText != null)
        {
            RectTransform titleRect = headerText.rectTransform;
            titleRect.anchorMin = new Vector2(0.14f, 0f);
            titleRect.anchorMax = new Vector2(0.76f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            headerText.fontSize = 32f;
            headerText.fontStyle = FontStyles.Bold;
            headerText.alignment = TextAlignmentOptions.MidlineLeft;
            headerText.textWrappingMode = TextWrappingModes.NoWrap;
            headerText.color = new Color(1f, 0.61f, 0.18f, 1f);
        }

        CreateHeaderIcon(header, "TopBarIcons/conflict", new Color(1f, 0.61f, 0.18f, 1f));
        StyleCloseButton();
        UpdateHeaderCount(GetPendingFlights().Count);
    }

    private void CreateHeaderIcon(Transform header, string resourcePath, Color tint)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null || header.Find("HeaderIcon") != null) return;

        GameObject iconGo = new GameObject("HeaderIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        RectTransform iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.SetParent(header, false);
        iconRect.anchorMin = new Vector2(0.035f, 0.25f);
        iconRect.anchorMax = new Vector2(0.115f, 0.75f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        RawImage icon = iconGo.GetComponent<RawImage>();
        icon.texture = texture;
        icon.color = tint;
        icon.raycastTarget = false;
    }

    private void StyleCloseButton()
    {
        if (closeButton == null) return;

        RectTransform rect = closeButton.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-18f, 0f);
        rect.sizeDelta = new Vector2(68f, 68f);

        Image image = closeButton.GetComponent<Image>();
        if (image != null) image.color = new Color(0.12f, 0.15f, 0.19f, 0.95f);
        TMP_Text label = closeButton.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = "×";
            label.fontSize = 38f;
            label.fontStyle = FontStyles.Normal;
            label.color = new Color(0.78f, 0.83f, 0.9f, 1f);
        }
    }

    private void UpdateHeaderCount(int count)
    {
        if (headerText != null) headerText.text = $"ATTENTION  ·  {count}";
    }
}
