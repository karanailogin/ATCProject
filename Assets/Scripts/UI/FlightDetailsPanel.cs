using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class FlightDetailsPanel : MonoBehaviour
{
    [Header("Flight details UI")]
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

    [Header("Slot Selection Dropdowns")]
    public TMP_Dropdown expectedDepartureDropdown;
    public TMP_Dropdown expectedArrivalDropdown;

    [Header("Buttons")]
    public Button discardButton;
    public Button approveButton;

    [Header("Panel")]
    public GameObject panelContainer;

    private static FlightDetailsPanel instance;
    private Flight currentFlight;

    // Cache lists of slots for selection
    private List<TimeSlot> departureSlots = new List<TimeSlot>();
    private List<TimeSlot> arrivalSlots = new List<TimeSlot>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (panelContainer != null)
        {
            panelContainer.SetActive(false);
        }

        if (discardButton != null)
        {
            discardButton.onClick.AddListener(ClosePanel);
        }

        if (approveButton != null)
        {
            approveButton.onClick.AddListener(ApproveFlight);
        }

        if (expectedDepartureDropdown != null)
        {
            expectedDepartureDropdown.onValueChanged.AddListener(OnDepartureSlotChanged);
        }

        if (expectedArrivalDropdown != null)
        {
            expectedArrivalDropdown.onValueChanged.AddListener(OnArrivalSlotChanged);
        }
    }

    public static void ShowFlightDetails(Flight flight)
    {
        if (instance == null)
        {
            instance = FindAnyObjectByType<FlightDetailsPanel>();
        }

        if (instance == null)
        {
            Debug.LogError("FlightDetailsPanel not found in scene. Make sure the sidebar panel exists under the Canvas and has the FlightDetailsPanel script attached.");
            return;
        }

        instance.DisplayFlight(flight);
    }

    private void DisplayFlight(Flight flight)
    {
        currentFlight = flight;

        if (flightNameText != null) flightNameText.text = flight.flightName;
        if (routeText != null) routeText.text = $"Route: {flight.fromAirport} → {flight.toAirport}";
        if (aircraftTypeText != null) aircraftTypeText.text = $"Aircraft: {flight.aircraftType}";
        if (durationText != null) durationText.text = $"Duration: {flight.flightDurationMinutes} mins";
        if (statusText != null) statusText.text = $"Status: {flight.status}" + (flight.landingApproved ? " (Approved)" : "");
        if (departureTimeText != null) departureTimeText.text = flight.takeoffSlot != null ? $"Departure: {flight.takeoffSlot.GetTimeString()}" : "Departure: Not scheduled";
        if (arrivalTimeText != null) arrivalTimeText.text = flight.landingSlot != null ? $"Arrival: {flight.landingSlot.GetTimeString()}" : "Arrival: Not scheduled";
        
        if (expectedDepartureText != null) expectedDepartureText.text = "Expected DEP Slot:";
        if (expectedArrivalText != null) expectedArrivalText.text = "Expected ARR Slot:";
        
        if (gateText != null) gateText.text = $"Gate: {(string.IsNullOrWhiteSpace(flight.gate) || flight.gate == "NA" ? "B12" : flight.gate)}";
        if (runwayText != null) runwayText.text = $"Runway: {(string.IsNullOrWhiteSpace(flight.runway) || flight.runway == "NA" ? "09L" : flight.runway)}";

        // Setup dropdowns
        PopulateDropdowns();

        if (panelContainer != null)
        {
            panelContainer.SetActive(true);
        }
    }

    private void PopulateDropdowns()
    {
        if (currentFlight == null || TimeSlotManager.Instance == null) return;

        // 1. Expected Departure Dropdown
        if (expectedDepartureDropdown != null)
        {
            expectedDepartureDropdown.ClearOptions();
            departureSlots.Clear();

            List<TimeSlot> allDepSlots = TimeSlotManager.Instance.GetAvailableSlots(currentFlight.fromAirport);
            List<string> depOptions = new List<string>();
            int selectedDepIndex = 0;

            foreach (TimeSlot slot in allDepSlots)
            {
                // Show slot if it is not booked OR if it is booked by this current flight
                if (!slot.isBooked || slot.bookedByFlight == currentFlight.flightName || (currentFlight.takeoffSlot != null && currentFlight.takeoffSlot.hours == slot.hours && currentFlight.takeoffSlot.minutes == slot.minutes))
                {
                    departureSlots.Add(slot);
                    depOptions.Add(slot.GetTimeString() + (slot.isBooked ? " (Booked)" : ""));

                    // Check if this is the flight's current expected departure
                    string currentExpected = string.IsNullOrWhiteSpace(currentFlight.expectedDeparture) || currentFlight.expectedDeparture == "NA" 
                        ? (currentFlight.takeoffSlot != null ? currentFlight.takeoffSlot.GetTimeString() : "") 
                        : currentFlight.expectedDeparture;

                    if (slot.GetTimeString() == currentExpected)
                    {
                        selectedDepIndex = depOptions.Count - 1;
                    }
                }
            }

            expectedDepartureDropdown.AddOptions(depOptions);
            expectedDepartureDropdown.SetValueWithoutNotify(selectedDepIndex);
        }

        // 2. Expected Arrival Dropdown
        PopulateArrivalDropdown();
    }

    private void PopulateArrivalDropdown()
    {
        if (currentFlight == null || TimeSlotManager.Instance == null || expectedArrivalDropdown == null) return;

        expectedArrivalDropdown.ClearOptions();
        arrivalSlots.Clear();

        List<TimeSlot> allArrSlots = TimeSlotManager.Instance.GetAvailableSlots(currentFlight.toAirport);
        List<string> arrOptions = new List<string>();
        int selectedArrIndex = 0;

        foreach (TimeSlot slot in allArrSlots)
        {
            if (!slot.isBooked || slot.bookedByFlight == currentFlight.flightName || (currentFlight.landingSlot != null && currentFlight.landingSlot.hours == slot.hours && currentFlight.landingSlot.minutes == slot.minutes))
            {
                arrivalSlots.Add(slot);
                arrOptions.Add(slot.GetTimeString() + (slot.isBooked ? " (Booked)" : ""));

                // Check if this is the flight's current expected arrival
                string currentExpected = string.IsNullOrWhiteSpace(currentFlight.expectedArrival) || currentFlight.expectedArrival == "NA" 
                    ? (currentFlight.landingSlot != null ? currentFlight.landingSlot.GetTimeString() : "") 
                    : currentFlight.expectedArrival;

                if (slot.GetTimeString() == currentExpected)
                {
                    selectedArrIndex = arrOptions.Count - 1;
                }
            }
        }

        expectedArrivalDropdown.AddOptions(arrOptions);
        expectedArrivalDropdown.SetValueWithoutNotify(selectedArrIndex);
    }

    private void OnDepartureSlotChanged(int index)
    {
        if (index < 0 || index >= departureSlots.Count || currentFlight == null) return;

        TimeSlot selectedDepSlot = departureSlots[index];
        currentFlight.expectedDeparture = selectedDepSlot.GetTimeString();

        // Automatically update the suggested arrival slot based on flight duration
        if (TimeSlotManager.Instance != null)
        {
            TimeSlot suggestedArrival = TimeSlotManager.Instance.GetSuggestedLandingSlot(currentFlight.toAirport, selectedDepSlot, currentFlight.flightDurationMinutes);
            if (suggestedArrival != null)
            {
                currentFlight.expectedArrival = suggestedArrival.GetTimeString();
                // Re-populate and select the new expected arrival slot
                PopulateArrivalDropdown();
            }
        }
    }

    private void OnArrivalSlotChanged(int index)
    {
        if (index < 0 || index >= arrivalSlots.Count || currentFlight == null) return;

        TimeSlot selectedArrSlot = arrivalSlots[index];
        currentFlight.expectedArrival = selectedArrSlot.GetTimeString();
    }

    private void ApproveFlight()
    {
        if (currentFlight != null && TimeSlotManager.Instance != null)
        {
            // Release existing bookings if any
            if (currentFlight.takeoffSlot != null)
            {
                TimeSlotManager.Instance.ReleaseTimeSlot(currentFlight.fromAirport, currentFlight.takeoffSlot);
            }
            if (currentFlight.landingSlot != null)
            {
                TimeSlotManager.Instance.ReleaseTimeSlot(currentFlight.toAirport, currentFlight.landingSlot);
            }

            // Get selected slots from dropdowns
            TimeSlot finalDepSlot = null;
            if (expectedDepartureDropdown != null && expectedDepartureDropdown.value >= 0 && expectedDepartureDropdown.value < departureSlots.Count)
            {
                finalDepSlot = departureSlots[expectedDepartureDropdown.value];
            }

            TimeSlot finalArrSlot = null;
            if (expectedArrivalDropdown != null && expectedArrivalDropdown.value >= 0 && expectedArrivalDropdown.value < arrivalSlots.Count)
            {
                finalArrSlot = arrivalSlots[expectedArrivalDropdown.value];
            }

            // Book new slots
            if (finalDepSlot != null)
            {
                TimeSlotManager.Instance.BookTimeSlot(currentFlight.fromAirport, finalDepSlot, currentFlight.flightName);
                currentFlight.takeoffSlot = finalDepSlot;
                currentFlight.departureTime = finalDepSlot.GetTimeString();
                currentFlight.expectedDeparture = finalDepSlot.GetTimeString();
            }

            if (finalArrSlot != null)
            {
                TimeSlotManager.Instance.BookTimeSlot(currentFlight.toAirport, finalArrSlot, currentFlight.flightName);
                currentFlight.landingSlot = finalArrSlot;
                currentFlight.arrivalTime = finalArrSlot.GetTimeString();
                currentFlight.expectedArrival = finalArrSlot.GetTimeString();
            }

            currentFlight.landingApproved = true;
            currentFlight.status = "Approved";

            if (statusText != null)
            {
                statusText.text = $"Status: {currentFlight.status}";
            }

            if (departureTimeText != null)
            {
                departureTimeText.text = currentFlight.takeoffSlot != null ? $"Departure: {currentFlight.takeoffSlot.GetTimeString()}" : "Departure: Not scheduled";
            }

            if (arrivalTimeText != null)
            {
                arrivalTimeText.text = currentFlight.landingSlot != null ? $"Arrival: {currentFlight.landingSlot.GetTimeString()}" : "Arrival: Not scheduled";
            }

            // Refresh the dropdown labels to show they are booked
            PopulateDropdowns();

            // Refresh flights display at current airport
            if (currentFlight.currentAirport != null)
            {
                currentFlight.currentAirport.DisplayFlights();
            }
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
