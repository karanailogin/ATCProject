using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class FlightInfoUI : MonoBehaviour, IPointerClickHandler
{
    public TextMeshProUGUI flightNameText;
    public TextMeshProUGUI routeText;
    public TextMeshProUGUI statusText;

    private Flight currentFlight;
    private Button clickButton;

    private void Awake()
    {
        clickButton = GetComponent<Button>();
        if (clickButton != null)
        {
            clickButton.onClick.AddListener(OnFlightClicked);
        }
        else
        {
            Debug.LogWarning("FlightInfoUI card has no Button component. Add a Button to the flight card prefab for reliable clicks.");
        }
    }

    public void SetFlightData(Flight flight)
    {
        currentFlight = flight;
        flightNameText.text = flight.flightName;
        routeText.text = $"{flight.fromAirport} → {flight.toAirport}";
        statusText.text = flight.status;

        // Dynamic styling based on flight status matching Flightradar24 live indicators
        Color statusColor = new Color(1.0f, 0.8f, 0.0f, 1.0f); // Amber default (Boarding, Delayed)
        if (flight.status == "On Time" || flight.status == "Approved" || flight.status == "Landed")
        {
            statusColor = new Color(0.3f, 0.75f, 0.3f, 1.0f); // Modern Green
        }
        else if (flight.status == "Departed" || flight.status == "In Flight")
        {
            statusColor = new Color(0.2f, 0.6f, 1.0f, 1.0f); // Sky Blue
        }
        else if (flight.status == "Cancelled")
        {
            statusColor = new Color(0.9f, 0.3f, 0.3f, 1.0f); // Red
        }

        if (statusText != null)
        {
            statusText.color = statusColor;
        }

        var barTrans = transform.Find("StateColorBar");
        if (barTrans != null)
        {
            var barImg = barTrans.GetComponent<UnityEngine.UI.Image>();
            if (barImg != null)
            {
                barImg.color = statusColor;
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnFlightClicked();
    }

    private void OnFlightClicked()
    {
        if (currentFlight != null)
        {
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
}

