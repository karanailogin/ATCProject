using UnityEngine;

public enum FlightState
{
    FlightCreated,
    ArrivalApproved,
    EnRoute,
    Arriving,
    Landed,
    Holding,
    Diverted,
    SlotConflict
}

[System.Serializable]
public class Flight
{
    public string flightName;
    public string flightID;
    public string fromAirport;
    public string toAirport;
    public string status; // Dynamically updated string based on state
    public Airport currentAirport; // Which airport the flight is at

    // Demo / UI details
    public string aircraftType = "Boeing 737";
    public int flightDurationMinutes = 120;
    public string departureTime = "NA";
    public string arrivalTime = "NA";
    public string expectedDeparture = "NA";
    public string expectedArrival = "NA";
    public string gate = "NA";
    public string runway = "NA";
    public string priority = "Normal";

    // Time management
    public TimeSlot takeoffSlot; // When flight takes off
    public TimeSlot landingSlot; // When flight lands

    private bool _landingApproved;
    public bool landingApproved
    {
        get { return _landingApproved; }
        set
        {
            _landingApproved = value;
            if (_landingApproved && _state == FlightState.FlightCreated)
            {
                state = FlightState.ArrivalApproved;
            }
            else if (!_landingApproved && _state == FlightState.ArrivalApproved)
            {
                state = FlightState.FlightCreated;
            }
            else if (_landingApproved && _state == FlightState.Holding)
            {
                state = FlightState.EnRoute;
            }
        }
    }

    private FlightState _state = FlightState.FlightCreated;
    public FlightState state
    {
        get { return _state; }
        set
        {
            _state = value;
            UpdateStatus();
        }
    }

    public float holdingStartTime = -1f; // Simulated clock minutes when holding started

    public Flight()
    {
        flightID = System.Guid.NewGuid().ToString();
        landingApproved = false;
        _state = FlightState.FlightCreated;
        status = "Awaiting Destination Approval";
    }

    public void UpdateStatus()
    {
        string dest = string.IsNullOrEmpty(toAirport) ? "Destination" : toAirport;
        switch (_state)
        {
            case FlightState.FlightCreated:
                status = $"Awaiting {dest} Approval";
                break;
            case FlightState.ArrivalApproved:
                status = "Approved for Departure";
                break;
            case FlightState.EnRoute:
                status = "En Route";
                break;
            case FlightState.Arriving:
                status = $"Arriving at {dest}";
                break;
            case FlightState.Landed:
                status = "Landed";
                break;
            case FlightState.Holding:
                status = "Holding for Clearance";
                break;
            case FlightState.Diverted:
                status = "Diverted";
                break;
            case FlightState.SlotConflict:
                status = "DEP Slot Conflict";
                break;
        }
    }
}
