using UnityEngine;

[System.Serializable]
public class Flight
{
    public string flightName;
    public string fromAirport;
    public string toAirport;
    public string status; // "Landed", "Boarding", "Departed", "In Flight"
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
    public bool landingApproved; // Has the landing airport approved the landing slot?

    public Flight()
    {
        landingApproved = false;
    }
}
