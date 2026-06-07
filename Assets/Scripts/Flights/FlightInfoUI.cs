using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FlightInfoUI : MonoBehaviour
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
    }

    public void SetFlightData(Flight flight)
    {
        currentFlight = flight;
        flightNameText.text = flight.flightName;
        routeText.text = $"{flight.fromAirport} → {flight.toAirport}";
        statusText.text = flight.status;
    }

    private void OnFlightClicked()
    {
        if (currentFlight != null)
        {
            FlightDetailsPanel.ShowFlightDetails(currentFlight);
            Debug.Log($"Clicked flight: {currentFlight.flightName}");
        }
    }
}

