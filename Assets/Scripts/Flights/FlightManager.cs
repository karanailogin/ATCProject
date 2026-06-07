using UnityEngine;
using System.Collections.Generic;

public class FlightManager : MonoBehaviour
{
    public static FlightManager Instance;
    private List<Flight> allFlights = new List<Flight>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddFlightToAirport(Flight flight, Airport airport)
    {
        flight.currentAirport = airport;
        allFlights.Add(flight);
    }

    public List<Flight> GetFlightsAtAirport(Airport airport)
    {
        return allFlights.FindAll(f => f.currentAirport == airport);
    }

    public void RemoveFlightFromAirport(Flight flight)
    {
        allFlights.Remove(flight);
    }
}
