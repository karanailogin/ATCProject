using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class FlightDetailsPanel : MonoBehaviour
{
    [Header("Flight Header")]
    public TextMeshProUGUI flightNameDisplay;
    public TextMeshProUGUI routeDisplay;
    public TextMeshProUGUI aircraftTypeDisplay;

    [Header("Timeline & Status")]
    public TextMeshProUGUI departureTimeDisplay;
    public TextMeshProUGUI arrivalTimeDisplay;
    public TextMeshProUGUI statusDisplay;
    public TextMeshProUGUI flightDurationDisplay;

    [Header("Passenger Info")]
    public TextMeshProUGUI passengerCountDisplay;
    public TextMeshProUGUI seatAvailabilityDisplay;
    public TextMeshProUGUI bookingClassDisplay;

    [Header("Advanced Section")]
    public GameObject advancedPanel; // Collapsible panel
    public TextMeshProUGUI gateDisplay;
    public TextMeshProUGUI runwayDisplay;
    public TextMeshProUGUI luggageInfoDisplay;
    public Button advancedToggleButton;

    [Header("Time Slot UI")]
    public Transform takeoffSlotsContainer;
    public Transform landingSlotsContainer;
    public GameObject timeSlotButtonPrefab;

    [Header("Panel Control")]
    public GameObject panelContainer;
    public Button closeButton;

    private static FlightDetailsPanel instance;
    private Flight currentFlight;
    private bool advancedExpanded = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (panelContainer != null)
        {
            panelContainer.SetActive(false);
        }

        if (advancedPanel != null)
        {
            advancedPanel.SetActive(false);
        }

        if (advancedToggleButton != null)
        {
            advancedToggleButton.onClick.AddListener(ToggleAdvanced);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    public static void ShowFlightDetails(Flight flight)
    {
        if (instance == null)
        {
            Debug.LogError("FlightDetailsPanel not found in scene!");
            return;
        }

        instance.DisplayFlight(flight);
    }

    private void DisplayFlight(Flight flight)
    {
        currentFlight = flight;

        // Display header
        flightNameDisplay.text = flight.flightName;
        routeDisplay.text = $"{flight.fromAirport} → {flight.toAirport}";
        aircraftTypeDisplay.text = "Aircraft: Boeing 737"; // Default, can be dynamic

        // Display timeline & status
        departureTimeDisplay.text = flight.takeoffSlot != null ? $"Departure: {flight.takeoffSlot.GetTimeString()}" : "Departure: Not scheduled";
        arrivalTimeDisplay.text = flight.landingSlot != null ? $"Arrival: {flight.landingSlot.GetTimeString()}" : "Arrival: Not scheduled";
        statusDisplay.text = $"Status: {flight.status}";
        flightDurationDisplay.text = $"Flight Duration: {flight.flightDurationMinutes} mins";

        // Display passenger info (sample data)
        passengerCountDisplay.text = "Passengers: 180 / 200";
        seatAvailabilityDisplay.text = "Available Seats: 20";
        bookingClassDisplay.text = "Classes: Economy, Business, First";

        // Display advanced info (sample data)
        gateDisplay.text = "Gate: B12";
        runwayDisplay.text = "Runway: 09L";
        luggageInfoDisplay.text = "Luggage: 320 bags";

        // Reset advanced panel
        advancedExpanded = false;
        if (advancedPanel != null)
        {
            advancedPanel.SetActive(false);
        }

        panelContainer.SetActive(true);
        Debug.Log($"Showing details for flight: {flight.flightName}");
    }

    private void ToggleAdvanced()
    {
        advancedExpanded = !advancedExpanded;
        if (advancedPanel != null)
        {
            advancedPanel.SetActive(advancedExpanded);
        }
    }

    public void ClosePanel()
    {
        if (panelContainer != null)
        {
            panelContainer.SetActive(false);
        }
    }
}

