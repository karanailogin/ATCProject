using UnityEngine;

public class FlightInitializer : MonoBehaviour
{
    public Airport mumbaiAirport;
    public Airport delhiAirport;
    public Airport bangaloreAirport;

    private void Start()
    {
        InitializeFlights();
    }

    private void InitializeFlights()
    {
        // Check if all airports are assigned
        if (mumbaiAirport == null || delhiAirport == null || bangaloreAirport == null)
        {
            Debug.LogError("Not all airports are assigned in FlightInitializer!");
            return;
        }

        // Test with 1 flight first
        Flight flight1 = new Flight()
        {
            flightName = "AI123",
            fromAirport = "MUM",
            toAirport = "DEL",
            status = "Boarding",
            flightDurationMinutes = 120 // 2 hours
        };
        FlightManager.Instance.AddFlightToAirport(flight1, mumbaiAirport);
        Debug.Log("Added flight: AI123 to Mumbai. Total flights: " + FlightManager.Instance.GetFlightsAtAirport(mumbaiAirport).Count);
    }
}
