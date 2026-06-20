using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ActiveFlightsPanel : MonoBehaviour
{
    public static ActiveFlightsPanel Instance;

    [Header("References")]
    public Transform flightListContent;
    public GameObject flightInfoPrefab;
    public GameObject airportInfoPanel;
    public GameObject pendingFlightsPanel;
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
        DisplayActiveFlights();
    }

    private void Update()
    {
        // If AirportInfoPanel is active, automatically hide this panel
        if (airportInfoPanel != null && airportInfoPanel.activeSelf)
        {
            gameObject.SetActive(false);
            return;
        }

        // Dynamically refresh list of active flights
        RefreshListIfNeeded();
    }

    public void ShowPanel()
    {
        if (airportInfoPanel != null)
        {
            airportInfoPanel.SetActive(false);
        }
        if (pendingFlightsPanel != null)
        {
            pendingFlightsPanel.SetActive(false);
        }
        else if (PendingFlightsPanel.Instance != null)
        {
            PendingFlightsPanel.Instance.gameObject.SetActive(false);
        }
        gameObject.SetActive(true);
        DisplayActiveFlights();
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

    private int lastActiveCount = -1;
    private HashSet<string> lastStatesHash = new HashSet<string>();

    private void RefreshListIfNeeded()
    {
        if (FlightManager.Instance == null) return;

        List<Flight> activeFlights = GetActiveFlights();
        bool needsRefresh = false;

        if (activeFlights.Count != lastActiveCount)
        {
            needsRefresh = true;
        }
        else
        {
            // Check if any flight's status string changed
            foreach (var f in activeFlights)
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
            DisplayActiveFlights();
        }
    }

    private List<Flight> GetActiveFlights()
    {
        List<Flight> active = new List<Flight>();
        if (FlightManager.Instance != null)
        {
            foreach (var flight in FlightManager.Instance.AllFlights)
            {
                if (flight != null)
                {
                    if (IsOperationalFlight(flight))
                    {
                        active.Add(flight);
                    }
                }
            }
        }
        return active;
    }

    public void DisplayActiveFlights()
    {
        if (flightListContent == null || flightInfoPrefab == null) return;

        // Clear existing children
        foreach (Transform child in flightListContent)
        {
            Destroy(child.gameObject);
        }

        List<Flight> activeFlights = GetActiveFlights();
        lastActiveCount = activeFlights.Count;
        UpdateHeaderCount(lastActiveCount);
        lastStatesHash.Clear();

        foreach (Flight flight in activeFlights)
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

    public static bool IsOperationalFlight(Flight flight)
    {
        if (flight == null || flight.state == FlightState.Landed ||
            flight.state == FlightState.Diverted || flight.state == FlightState.SlotConflict)
        {
            return false;
        }

        return flight.landingApproved ||
            flight.state == FlightState.ArrivalApproved ||
            flight.state == FlightState.EnRoute ||
            flight.state == FlightState.Arriving;
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
            headerText.fontSize = 34f;
            headerText.fontStyle = FontStyles.Bold;
            headerText.alignment = TextAlignmentOptions.MidlineLeft;
            headerText.textWrappingMode = TextWrappingModes.NoWrap;
            headerText.color = new Color(0.28f, 0.74f, 1f, 1f);
        }

        CreateHeaderIcon(header, "TopBarIcons/plane", new Color(0.28f, 0.74f, 1f, 1f));
        StyleCloseButton();
        UpdateHeaderCount(GetActiveFlights().Count);
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
        if (headerText != null) headerText.text = $"ACTIVE  ·  {count}";
    }
}
