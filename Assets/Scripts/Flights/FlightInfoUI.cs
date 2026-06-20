using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FlightInfoUI : MonoBehaviour
{
    public TextMeshProUGUI flightNameText;
    public TextMeshProUGUI routeText;
    public TextMeshProUGUI statusText;

    [Header("Selection Colors")]
    public Color normalColor = new Color(0.15f, 0.15f, 0.17f, 0.95f);
    public Color selectedColor = new Color(0.11f, 0.38f, 0.73f, 0.98f); // Bright elegant blue highlight

    [Header("Enhancements for Prefab Customization")]
    public UnityEngine.UI.Button acceptButton;
    public UnityEngine.UI.Button rejectButton;
    public UnityEngine.UI.Image stateColorBar;

    [Header("Flight Icon Customization")]
    public FlightIconConfig iconConfig;
    private UnityEngine.UI.Image iconImage;
    private UnityEngine.UI.RawImage iconRawImage;

    [Header("Suggested Arrival/Departure Fields (New Prefabs)")]
    public TextMeshProUGUI suggestedArrText;
    public TextMeshProUGUI suggestedDepText;

    public static Flight selectedFlight;

    private Flight currentFlight;
    private Button clickButton;

    private void Awake()
    {
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (clickButton == null) clickButton = GetComponent<Button>();
        if (flightNameText == null) flightNameText = transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (routeText == null) routeText = transform.Find("Route")?.GetComponent<TextMeshProUGUI>();
        if (statusText == null) statusText = transform.Find("Status")?.GetComponent<TextMeshProUGUI>();
        if (stateColorBar == null) stateColorBar = transform.Find("StateColorBar")?.GetComponent<UnityEngine.UI.Image>();
        if (acceptButton == null) acceptButton = transform.Find("AcceptButton")?.GetComponent<UnityEngine.UI.Button>();
        if (rejectButton == null) rejectButton = transform.Find("RejectButton")?.GetComponent<UnityEngine.UI.Button>();

        // Cache icon components on the child named "Icon"
        var iconTrans = transform.Find("Icon");
        if (iconTrans != null)
        {
            iconImage = iconTrans.GetComponent<UnityEngine.UI.Image>();
            iconRawImage = iconTrans.GetComponent<UnityEngine.UI.RawImage>();
        }

        // Cache suggested arrival/departure fields if present
        if (suggestedArrText == null) suggestedArrText = transform.Find("SuggestedARR")?.GetComponent<TextMeshProUGUI>();
        if (suggestedDepText == null) suggestedDepText = transform.Find("SuggestedDEP")?.GetComponent<TextMeshProUGUI>();

        // Dynamically load the FlightIconConfig asset if not manually assigned in inspector
        if (iconConfig == null)
        {
            iconConfig = Resources.Load<FlightIconConfig>("FlightIconConfig");
        }

        // Disable badge bg image by default to avoid bleeding
        if (statusText != null)
        {
            var badgeImg = statusText.GetComponent<UnityEngine.UI.Image>();
            if (badgeImg != null)
            {
                badgeImg.enabled = false;
            }
        }
    }

    public void UpdateSelectionVisuals()
    {
        var image = GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            bool isSelected = (selectedFlight != null && currentFlight == selectedFlight);
            image.color = isSelected ? selectedColor : normalColor;
        }
    }

    // Mode 1: Standard Flight Card
    public void SetFlightData(Flight flight)
    {
        EnsureReferences();
        currentFlight = flight;

        if (flightNameText != null) { flightNameText.gameObject.SetActive(true); flightNameText.text = flight.flightName; }
        if (routeText != null) { routeText.gameObject.SetActive(true); routeText.text = $"{flight.fromAirport} → {flight.toAirport}"; }
        if (statusText != null) { statusText.gameObject.SetActive(true); statusText.text = flight.status; }
        if (stateColorBar != null) stateColorBar.gameObject.SetActive(true);
        if (acceptButton != null) acceptButton.gameObject.SetActive(false);
        if (rejectButton != null) rejectButton.gameObject.SetActive(false);

        // Update the flight icon based on state
        UpdateIconForState(flight.state);

        // Click interaction
        if (clickButton != null)
        {
            clickButton.interactable = true;
            clickButton.onClick.RemoveAllListeners();
            clickButton.onClick.AddListener(OnFlightClicked);
        }

        // Dynamic styling based on flight state
        Color statusColor = GetStatusColor(flight);
        if (statusText != null) statusText.color = statusColor;
        if (stateColorBar != null) stateColorBar.color = statusColor;

        UpdateSelectionVisuals();
    }

    // Mode 2: Header Row
    public void SetHeaderData(string headerTitle)
    {
        EnsureReferences();
        currentFlight = null;

        if (flightNameText != null)
        {
            flightNameText.gameObject.SetActive(true);
            flightNameText.text = $"<color=#EAB308><b>{headerTitle.ToUpper()}</b></color>";
        }
        if (routeText != null) routeText.gameObject.SetActive(false);
        if (statusText != null) statusText.gameObject.SetActive(false);
        if (stateColorBar != null) stateColorBar.gameObject.SetActive(false);
        if (acceptButton != null) acceptButton.gameObject.SetActive(false);
        if (rejectButton != null) rejectButton.gameObject.SetActive(false);

        // Hide flight icon for header rows
        SetIconActive(false);

        if (clickButton != null)
        {
            clickButton.interactable = false;
        }
    }

    // Mode 3: Request Card
    public void SetRequestData(Flight flight, System.Action onApprove, System.Action onReject)
    {
        EnsureReferences();
        currentFlight = flight;

        // Set the flight icon based on state (Ground)
        UpdateIconForState(flight.state);

        if (flightNameText != null) { flightNameText.gameObject.SetActive(true); flightNameText.text = $"<color=#EAB308><b>[{flight.flightName}]</b></color>"; }
        if (routeText != null) { routeText.gameObject.SetActive(true); routeText.text = $"<b>{flight.fromAirport} → {flight.toAirport}</b>"; }
        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = $"Aircraft: <color=#94A3B8>{flight.aircraftType}</color>\n" +
                              $"DEP: <color=#3B82F6>{flight.departureTime}</color>   ARR: <color=#10B981>{flight.arrivalTime}</color>";
        }
        if (stateColorBar != null)
        {
            stateColorBar.gameObject.SetActive(true);
            stateColorBar.color = new Color(0.92f, 0.70f, 0.08f, 1.0f); // Amber (Pending)
        }

        // Programmatically fallback to creating the buttons if they were removed from the prefab
        if (acceptButton == null)
        {
            CreateAcceptButton();
        }
        if (rejectButton == null)
        {
            CreateRejectButton();
        }

        if (acceptButton != null)
        {
            acceptButton.gameObject.SetActive(true);
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(() => onApprove?.Invoke());
        }
        if (rejectButton != null)
        {
            rejectButton.gameObject.SetActive(true);
            rejectButton.onClick.RemoveAllListeners();
            rejectButton.onClick.AddListener(() => onReject?.Invoke());
        }

        if (clickButton != null)
        {
            clickButton.interactable = false; // Disable clicking the card itself to prevent conflict with buttons
        }
    }

    private void CreateAcceptButton()
    {
        var acceptGo = new GameObject("AcceptButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        acceptGo.transform.SetParent(this.transform, false);

        RectTransform acceptRt = acceptGo.GetComponent<RectTransform>();
        acceptRt.anchorMin = new Vector2(1, 0.5f);
        acceptRt.anchorMax = new Vector2(1, 0.5f);
        acceptRt.pivot = new Vector2(1, 0.5f);
        acceptRt.anchoredPosition = new Vector2(-20, 20);
        acceptRt.sizeDelta = new Vector2(100, 32);

        UnityEngine.UI.Image acceptImg = acceptGo.GetComponent<UnityEngine.UI.Image>();
        acceptImg.color = new Color(0.13f, 0.77f, 0.36f, 1.0f); // Green

        Sprite roundedSprite = null;
        var sceneImgs = Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsInactive.Include);
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
            acceptImg.sprite = roundedSprite;
            acceptImg.type = UnityEngine.UI.Image.Type.Sliced;
        }

        var acceptTextGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        acceptTextGo.transform.SetParent(acceptGo.transform, false);
        RectTransform acceptTextRt = acceptTextGo.GetComponent<RectTransform>();
        acceptTextRt.anchorMin = Vector2.zero;
        acceptTextRt.anchorMax = Vector2.one;
        acceptTextRt.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI acceptTmp = acceptTextGo.GetComponent<TextMeshProUGUI>();
        acceptTmp.text = "APPROVE";
        acceptTmp.fontSize = 11;
        acceptTmp.fontStyle = FontStyles.Bold;
        acceptTmp.alignment = TextAlignmentOptions.Center;
        acceptTmp.color = Color.white;

        acceptButton = acceptGo.GetComponent<UnityEngine.UI.Button>();
    }

    private void CreateRejectButton()
    {
        var rejectGo = new GameObject("RejectButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        rejectGo.transform.SetParent(this.transform, false);

        RectTransform rejectRt = rejectGo.GetComponent<RectTransform>();
        rejectRt.anchorMin = new Vector2(1, 0.5f);
        rejectRt.anchorMax = new Vector2(1, 0.5f);
        rejectRt.pivot = new Vector2(1, 0.5f);
        rejectRt.anchoredPosition = new Vector2(-20, -20);
        rejectRt.sizeDelta = new Vector2(100, 32);

        UnityEngine.UI.Image rejectImg = rejectGo.GetComponent<UnityEngine.UI.Image>();
        rejectImg.color = new Color(0.94f, 0.27f, 0.27f, 1.0f); // Red

        Sprite roundedSprite = null;
        var sceneImgs = Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsInactive.Include);
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
            rejectImg.sprite = roundedSprite;
            rejectImg.type = UnityEngine.UI.Image.Type.Sliced;
        }

        var rejectTextGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        rejectTextGo.transform.SetParent(rejectGo.transform, false);
        RectTransform rejectTextRt = rejectTextGo.GetComponent<RectTransform>();
        rejectTextRt.anchorMin = Vector2.zero;
        rejectTextRt.anchorMax = Vector2.one;
        rejectTextRt.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI rejectTmp = rejectTextGo.GetComponent<TextMeshProUGUI>();
        rejectTmp.text = "REJECT";
        rejectTmp.fontSize = 11;
        rejectTmp.fontStyle = FontStyles.Bold;
        rejectTmp.alignment = TextAlignmentOptions.Center;
        rejectTmp.color = Color.white;

        rejectButton = rejectGo.GetComponent<UnityEngine.UI.Button>();
    }

    // Mode 4: Schedule Event Row
    public void SetEventData(AirportScheduleEvent ev, System.Action onClick)
    {
        EnsureReferences();
        currentFlight = null;

        bool isArrival = ev.EventType == AirportEventType.Arrival;

        // Set specific TakeOff or Landing icons for schedule entries
        if (isArrival)
        {
            UpdateIconWithSprite(iconConfig != null ? iconConfig.landingIcon : null);
        }
        else
        {
            UpdateIconWithSprite(iconConfig != null ? iconConfig.takeoffIcon : null);
        }

        string typeString = isArrival ? "<color=#3B82F6><b>ARR</b></color>" : "<color=#22C55E><b>DEP</b></color>";
        string relationText = isArrival ? $"from <b>{ev.OtherAirportCode}</b>" : $"to <b>{ev.OtherAirportCode}</b>";

        if (flightNameText != null) { flightNameText.gameObject.SetActive(true); flightNameText.text = $"<size=14><b>{ev.EventTime}</b></size>    {typeString}"; }
        if (routeText != null) { routeText.gameObject.SetActive(true); routeText.text = $"<b>{ev.FlightNumber}</b> {relationText}"; }
        if (statusText != null) statusText.gameObject.SetActive(false);

        Color typeColor = isArrival ? new Color(0.23f, 0.51f, 0.96f) : new Color(0.13f, 0.77f, 0.36f);
        if (stateColorBar != null)
        {
            stateColorBar.gameObject.SetActive(true);
            stateColorBar.color = typeColor;
        }

        if (acceptButton != null) acceptButton.gameObject.SetActive(false);
        if (rejectButton != null) rejectButton.gameObject.SetActive(false);

        if (clickButton != null)
        {
            clickButton.interactable = true;
            clickButton.onClick.RemoveAllListeners();
            clickButton.onClick.AddListener(() => onClick?.Invoke());
        }
    }

    private Color GetStatusColor(Flight flight)
    {
        Color statusColor = new Color(1.0f, 0.8f, 0.0f, 1.0f); // Amber default (FlightCreated, Holding)
        if (flight.state == FlightState.Landed || flight.state == FlightState.ArrivalApproved)
        {
            statusColor = new Color(0.3f, 0.75f, 0.3f, 1.0f); // Modern Green
        }
        else if (flight.state == FlightState.EnRoute || flight.state == FlightState.Arriving)
        {
            statusColor = new Color(0.2f, 0.6f, 1.0f, 1.0f); // Sky Blue
        }
        else if (flight.state == FlightState.Diverted)
        {
            statusColor = new Color(0.9f, 0.3f, 0.3f, 1.0f); // Red
        }
        return statusColor;
    }

    private void OnFlightClicked()
    {
        if (currentFlight != null)
        {
            selectedFlight = currentFlight;

            // Instantly refresh the selection color for this card and all sibling cards
            if (transform.parent != null)
            {
                foreach (Transform child in transform.parent)
                {
                    var flightUI = child.GetComponent<FlightInfoUI>();
                    if (flightUI != null)
                    {
                        flightUI.UpdateSelectionVisuals();
                    }
                }
            }

            Debug.Log($"Button clicked: Flight Card click event for flight: {currentFlight.flightName}");
            FlightDetailsPanel.ShowFlightDetails(currentFlight);

            // Also try the dedicated sidebar if present
            FlightDetailsSidebar sidebar = FindAnyObjectByType<FlightDetailsSidebar>();
            if (sidebar != null)
            {
                sidebar.ShowFlightDetails(currentFlight);
            }

            Debug.Log("Flight details clicked:\n" +
                      "FlightName: " + currentFlight.flightName + "\n" +
                      "Route: " + currentFlight.fromAirport + " → " + currentFlight.toAirport + "\n" +
                      "AircraftType: " + currentFlight.aircraftType + "\n" +
                      "Duration: " + currentFlight.flightDurationMinutes + " mins\n" +
                      "Status: " + currentFlight.status + "\n" +
                      "DepartureTime: " + (string.IsNullOrWhiteSpace(currentFlight.departureTime) ? "NA" : currentFlight.departureTime) + "\n" +
                      "ArrivalTime: " + (string.IsNullOrWhiteSpace(currentFlight.arrivalTime) ? "NA" : currentFlight.arrivalTime) + "\n" +
                      "Gate: " + (string.IsNullOrWhiteSpace(currentFlight.gate) ? "NA" : currentFlight.gate) + "\n" +
                      "Runway: " + (string.IsNullOrWhiteSpace(currentFlight.runway) ? "NA" : currentFlight.runway));
        }
    }

    private void UpdateIconForState(FlightState state)
    {
        if (iconConfig != null)
        {
            Sprite sprite = iconConfig.GetFlightIcon(state);
            UpdateIconWithSprite(sprite);
        }
    }

    private void UpdateIconWithSprite(Sprite sprite)
    {
        if (sprite != null)
        {
            SetIconActive(true);
            if (iconImage != null)
            {
                iconImage.sprite = sprite;
            }
            else if (iconRawImage != null)
            {
                iconRawImage.texture = sprite.texture;
            }
        }
        else
        {
            SetIconActive(false);
        }
    }

    private void SetIconActive(bool active)
    {
        if (iconImage != null) iconImage.gameObject.SetActive(active);
        if (iconRawImage != null) iconRawImage.gameObject.SetActive(active);
    }

    // Mode 5: Compact Departing Flight Card
    public void SetDepartingFlightData(Flight flight, string overrideDepartureTime = null)
    {
        EnsureReferences();
        currentFlight = flight;

        string depTime = overrideDepartureTime ?? flight.departureTime;

        if (flightNameText != null) 
        { 
            flightNameText.gameObject.SetActive(true); 
            flightNameText.text = $"<b>{flight.flightName}</b>"; 
        }

        if (routeText != null) 
        { 
            routeText.gameObject.SetActive(true); 
            if (suggestedDepText != null)
            {
                routeText.text = $"To {flight.toAirport}";
            }
            else
            {
                routeText.text = $"To: <b>{flight.toAirport}</b>      DEP: <b>{depTime}</b>"; 
            }
        }

        if (suggestedDepText != null)
        {
            suggestedDepText.gameObject.SetActive(true);
            suggestedDepText.text = depTime;
        }

        // Configure Status as a small rounded badge
        string statusStr = GetOutboundStatusText(flight);
        Color badgeColor = GetOutboundStatusColor(flight);
        SetupStatusBadge(statusStr, badgeColor);

        // Departure-airport cards always use the takeoff icon.
        UpdateIconWithSprite(iconConfig != null ? iconConfig.takeoffIcon : null);

        // Click interaction to open details
        if (clickButton != null)
        {
            clickButton.interactable = true;
            clickButton.onClick.RemoveAllListeners();
            clickButton.onClick.AddListener(OnFlightClicked);
        }

        // Use the colored state indicator line
        if (stateColorBar != null)
        {
            stateColorBar.gameObject.SetActive(true);
            stateColorBar.color = badgeColor;
        }

        UpdateSelectionVisuals();
    }

    // Mode 6: Compact Arriving Flight Card
    public void SetArrivingFlightData(Flight flight, string overrideArrivalTime = null)
    {
        EnsureReferences();
        currentFlight = flight;

        string arrTime = overrideArrivalTime ?? flight.arrivalTime;

        if (flightNameText != null) 
        { 
            flightNameText.gameObject.SetActive(true); 
            flightNameText.text = $"<b>{flight.flightName}</b>"; 
        }

        if (routeText != null) 
        { 
            routeText.gameObject.SetActive(true); 
            if (suggestedArrText != null)
            {
                routeText.text = $"From {flight.fromAirport}";
            }
            else
            {
                routeText.text = $"From: <b>{flight.fromAirport}</b>      ARR: <b>{arrTime}</b>"; 
            }
        }

        if (suggestedArrText != null)
        {
            suggestedArrText.gameObject.SetActive(true);
            suggestedArrText.text = arrTime;
        }

        // Configure Status as a small rounded badge
        string statusStr = GetInboundStatusText(flight);
        Color badgeColor = GetInboundStatusColor(flight);
        SetupStatusBadge(statusStr, badgeColor);

        // Arrival-airport cards always use the landing icon.
        UpdateIconWithSprite(iconConfig != null ? iconConfig.landingIcon : null);

        // Click interaction to open details
        if (clickButton != null)
        {
            clickButton.interactable = true;
            clickButton.onClick.RemoveAllListeners();
            clickButton.onClick.AddListener(OnFlightClicked);
        }

        // Use the colored state indicator line
        if (stateColorBar != null)
        {
            stateColorBar.gameObject.SetActive(true);
            stateColorBar.color = badgeColor;
        }

        UpdateSelectionVisuals();
    }

    private string GetOutboundStatusText(Flight flight)
    {
        if (flight.state == FlightState.SlotConflict) return "Conflict";
        if (flight.landingApproved) return "Approved";
        return "Pending";
    }

    private Color GetOutboundStatusColor(Flight flight)
    {
        if (flight.state == FlightState.SlotConflict) return new Color(0.94f, 0.27f, 0.27f, 1.0f); // Red
        if (flight.landingApproved) return new Color(0.13f, 0.77f, 0.36f, 1.0f); // Green
        return new Color(0.95f, 0.61f, 0.07f, 1.0f); // Orange/Yellow
    }

    private string GetInboundStatusText(Flight flight)
    {
        if (flight.state == FlightState.SlotConflict) return "Conflict";
        if (flight.landingApproved) return "Approved";
        return "Requested";
    }

    private Color GetInboundStatusColor(Flight flight)
    {
        if (flight.state == FlightState.SlotConflict) return new Color(0.94f, 0.27f, 0.27f, 1.0f); // Red
        if (flight.landingApproved) return new Color(0.13f, 0.77f, 0.36f, 1.0f); // Green
        return new Color(0.95f, 0.61f, 0.07f, 1.0f); // Orange/Yellow
    }

    private void SetupStatusBadge(string text, Color badgeColor)
    {
        if (statusText == null) return;

        statusText.gameObject.SetActive(true);
        statusText.text = $"<b>{text.ToUpper()}</b>";
        statusText.color = badgeColor; // Set text color directly (e.g. Green, Yellow, Red) to support custom prefabs
        statusText.alignment = TextAlignmentOptions.Left;

        // If there's an existing Image on statusText's GameObject (e.g. from design), configure it.
        // We will NOT dynamically add an Image component to avoid "Can't add Image because TextMeshProUGUI is already added" error.
        var badgeImg = statusText.GetComponent<UnityEngine.UI.Image>();
        if (badgeImg != null)
        {
            badgeImg.enabled = true;
            badgeImg.color = badgeColor;
            statusText.color = Color.white; // Make text white if it is rendered on top of a colored image background

            // Load the RoundedRect sprite if not assigned
            if (badgeImg.sprite == null)
            {
                Sprite roundedSprite = null;
                var sceneImgs = Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsInactive.Include);
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
                    badgeImg.sprite = roundedSprite;
                    badgeImg.type = UnityEngine.UI.Image.Type.Sliced;
                }
            }
        }
    }
}

