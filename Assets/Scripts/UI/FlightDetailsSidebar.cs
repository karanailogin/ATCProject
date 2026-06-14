using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FlightDetailsSidebar : MonoBehaviour
{
    [Header("Sidebar text fields")]
    public TMP_Text flightNameText;
    public TMP_Text routeText;
    public TMP_Text aircraftTypeText;
    public TMP_Text durationText;
    public TMP_Text statusText;
    public TMP_Text departureTimeText;
    public TMP_Text arrivalTimeText;
    public TMP_Text expectedDepartureText;
    public TMP_Text expectedArrivalText;
    public TMP_Text gateText;
    public TMP_Text runwayText;

    [Header("Buttons")]
    public Button discardButton;
    public Button approveButton;

    [Header("Panel")]
    public GameObject panelContainer;

    private Flight currentFlight;

    private void Awake()
    {
        if (panelContainer != null)
        {
            panelContainer.SetActive(false);
        }

        if (discardButton != null)
        {
            discardButton.onClick.AddListener(HidePanel);
        }

        if (approveButton != null)
        {
            approveButton.onClick.AddListener(ApproveCurrentFlight);
        }
    }

    public void ShowFlightDetails(Flight flight)
    {
        currentFlight = flight;

        if (flightNameText != null) flightNameText.text = flight.flightName;
        if (routeText != null) routeText.text = $"Route: {flight.fromAirport} → {flight.toAirport}";
        if (aircraftTypeText != null) aircraftTypeText.text = "Aircraft: " + (string.IsNullOrWhiteSpace(flight.aircraftType) ? "N/A" : flight.aircraftType);
        if (durationText != null) durationText.text = $"Duration: {flight.flightDurationMinutes} mins";
        if (statusText != null) statusText.text = $"Status: {flight.status}";
        if (departureTimeText != null) departureTimeText.text = "Departure: " + (string.IsNullOrWhiteSpace(flight.departureTime) ? "NA" : flight.departureTime);
        if (arrivalTimeText != null) arrivalTimeText.text = "Arrival: " + (string.IsNullOrWhiteSpace(flight.arrivalTime) ? "NA" : flight.arrivalTime);
        if (expectedDepartureText != null) expectedDepartureText.text = "Expected DEP: " + (string.IsNullOrWhiteSpace(flight.expectedDeparture) || flight.expectedDeparture == "NA" ? (string.IsNullOrWhiteSpace(flight.departureTime) ? "NA" : flight.departureTime) : flight.expectedDeparture);
        if (expectedArrivalText != null) expectedArrivalText.text = "Expected ARR: " + (string.IsNullOrWhiteSpace(flight.expectedArrival) || flight.expectedArrival == "NA" ? (string.IsNullOrWhiteSpace(flight.arrivalTime) ? "NA" : flight.arrivalTime) : flight.expectedArrival);
        if (gateText != null) gateText.text = "Gate: " + (string.IsNullOrWhiteSpace(flight.gate) ? "NA" : flight.gate);
        if (runwayText != null) runwayText.text = "Runway: " + (string.IsNullOrWhiteSpace(flight.runway) ? "NA" : flight.runway);

        if (panelContainer != null)
        {
            panelContainer.SetActive(true);
        }
    }

    private void ApproveCurrentFlight()
    {
        if (currentFlight != null)
        {
            currentFlight.landingApproved = true;
            if (statusText != null)
            {
                statusText.text = $"Status: {currentFlight.status}";
            }
        }
    }

    private void Update()
    {
        if (panelContainer != null && panelContainer.activeSelf && currentFlight != null)
        {
            if (statusText != null)
            {
                statusText.text = $"Status: {currentFlight.status}";
            }
        }
    }

    private void HidePanel()
    {
        if (panelContainer != null)
        {
            panelContainer.SetActive(false);
        }
    }
}
