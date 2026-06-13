using UnityEngine;

public class FlightSpawner : MonoBehaviour
{
    public Airport mumbaiAirport;
    public Airport delhiAirport;
    public Airport bangaloreAirport;

    public static void SpawnDemoFlights(Airport mumbaiAirport, Airport delhiAirport, Airport bangaloreAirport)
    {
        if (FlightManager.Instance == null)
        {
            Debug.LogError("FlightManager.Instance is not available. Add a FlightManager object to the scene.");
            return;
        }

        FlightManager.Instance.ClearAllFlights();

        if (mumbaiAirport == null || delhiAirport == null || bangaloreAirport == null)
        {
            Debug.LogError("FlightSpawner requires all 3 airport references assigned in the Inspector.");
            return;
        }

        AddFlight(0, "AI101", "MUM", "DEL", "Boarding", 120, "Boeing 737", "NA", "NA", "NA", "NA", mumbaiAirport);
        AddFlight(1, "UK202", "DEL", "BLR", "On Time", 95, "Airbus A320", "NA", "NA", "NA", "NA", delhiAirport);
        AddFlight(2, "SG303", "BLR", "MUM", "Delayed", 150, "Boeing 787", "NA", "NA", "NA", "NA", bangaloreAirport);

        AddFlight(3, "AI102", "MUM", "DEL", "Boarding", 120, "Boeing 737", "NA", "NA", "NA", "NA", mumbaiAirport);
        AddFlight(4, "UK203", "DEL", "BLR", "On Time", 95, "Airbus A320", "NA", "NA", "NA", "NA", delhiAirport);
        AddFlight(5, "SG304", "BLR", "MUM", "Delayed", 150, "Boeing 787", "NA", "NA", "NA", "NA", bangaloreAirport);

        Debug.Log("Demo flights spawned for all airports.");
    }

    private static void AddFlight(int index, string flightName, string fromAirport, string toAirport, string status,
        int durationMinutes, string aircraftType, string departureTime, string arrivalTime,
        string gate, string runway, Airport airport)
    {
        Flight flight = new Flight
        {
            flightName = flightName,
            fromAirport = fromAirport,
            toAirport = toAirport,
            status = status,
            flightDurationMinutes = durationMinutes,
            aircraftType = aircraftType,
            departureTime = departureTime,
            arrivalTime = arrivalTime,
            gate = gate,
            runway = runway
        };

        int hour = 6 + (index % 8);
        int minute = (index % 4) * 15;
        TimeSlot suggestedDeparture = null;
        
        if (TimeSlotManager.Instance != null)
        {
            suggestedDeparture = TimeSlotManager.Instance.GetTimeSlot(fromAirport, hour, minute);
            if (suggestedDeparture != null)
            {
                TimeSlotManager.Instance.BookTimeSlot(fromAirport, suggestedDeparture, flightName);
            }
        }

        if (suggestedDeparture == null)
        {
            suggestedDeparture = new TimeSlot(hour, minute);
        }

        flight.takeoffSlot = suggestedDeparture;
        flight.departureTime = suggestedDeparture.GetTimeString();
        flight.expectedDeparture = suggestedDeparture.GetTimeString();

        if (TimeSlotManager.Instance != null)
        {
            TimeSlot suggestedArrival = TimeSlotManager.Instance.GetSuggestedLandingSlot(toAirport, suggestedDeparture, durationMinutes);
            if (suggestedArrival != null)
            {
                TimeSlotManager.Instance.BookTimeSlot(toAirport, suggestedArrival, flightName);
                flight.landingSlot = suggestedArrival;
                flight.arrivalTime = suggestedArrival.GetTimeString();
                flight.expectedArrival = suggestedArrival.GetTimeString();
            }
            else
            {
                flight.expectedArrival = "NA";
            }
        }
        else
        {
            flight.expectedArrival = "NA";
        }

        FlightManager.Instance.AddFlightToAirport(flight, airport);
        Debug.Log("Added demo flight: " + flightName + " to " + airport.airportName);
    }

    public void SpawnDemoFlights()
    {
        SpawnDemoFlights(mumbaiAirport, delhiAirport, bangaloreAirport);
    }
}
