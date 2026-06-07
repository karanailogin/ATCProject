using UnityEngine;

[System.Serializable]
public class Flight
{
    public string flightName;
    public string fromAirport;
    public string toAirport;
    public string status; // "Landed", "Boarding", "Departed", "In Flight"
    public Airport currentAirport; // Which airport the flight is at
    
    // Time management
    public TimeSlot takeoffSlot; // When flight takes off
    public TimeSlot landingSlot; // When flight lands
    public int flightDurationMinutes; // Flight duration in minutes (e.g., 120 for 2 hours)
    public bool landingApproved; // Has the landing airport approved the landing slot?

    public Flight()
    {
        landingApproved = false;
    }
}
