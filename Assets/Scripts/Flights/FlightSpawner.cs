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

        if (mumbaiAirport == null || delhiAirport == null)
        {
            Debug.LogError("FlightSpawner requires Mumbai and Delhi airport references assigned in the Inspector.");
            return;
        }

        // Determine starting base hour/minute from World Clock if available (otherwise default to 6:00)
        int baseHour = 6;
        int baseMinute = 0;
        if (WorldClockManager.Instance != null)
        {
            // First flight scheduled 30 minutes after current world clock time
            System.DateTime firstSpawnTime = WorldClockManager.Instance.CurrentTime.AddMinutes(30);
            baseHour = firstSpawnTime.Hour;
            baseMinute = firstSpawnTime.Minute;
            Debug.Log($"[FlightSpawner] World clock found. First flight planned at {baseHour:D2}:{baseMinute:D2} (30 mins from {WorldClockManager.Instance.CurrentTime:HH:mm})");
        }
        else
        {
            // Fallback: 30 minutes after FlightManager's baseline (06:00) -> 06:30
            baseHour = 6;
            baseMinute = 30;
            Debug.Log("[FlightSpawner] WorldClockManager.Instance not found. Defaulting first flight to 06:30.");
        }

        // For now, spawn only one test flight 30 minutes after the game world clock start time.
        AddFlightWithTime(0, "AI101", "MUM", "DEL", "On Time", 120, "Boeing 737", "NA", "NA", "NA", "NA", mumbaiAirport, baseHour, baseMinute);

        if (FlightManager.Instance != null)
        {
            FlightManager.Instance.RecalculateSlotConflicts();
        }

        Debug.Log($"Spawned single test flight AI101 MUM -> DEL with expected DEP {baseHour:D2}:{baseMinute:D2}.");
    }

    private static void AddFlightWithTime(int index, string flightName, string fromAirport, string toAirport, string status,
        int durationMinutes, string aircraftType, string departureTime, string arrivalTime,
        string gate, string runway, Airport airport, int hour, int minute)
    {
        Flight flight = new Flight
        {
            flightName = flightName,
            fromAirport = fromAirport,
            toAirport = toAirport,
            flightDurationMinutes = durationMinutes,
            aircraftType = aircraftType,
            departureTime = departureTime,
            arrivalTime = arrivalTime,
            gate = gate,
            runway = runway
        };
        flight.state = FlightState.FlightCreated;

        // Ensure we handle hour roll-overs safely
        if (minute >= 60)
        {
            hour += minute / 60;
            minute = minute % 60;
        }
        hour = hour % 24;

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
        Debug.Log("Added demo flight: " + flightName + " to " + airport.airportName + " at " + flight.departureTime);
    }

    private static void AddFlight(int index, string flightName, string fromAirport, string toAirport, string status,
        int durationMinutes, string aircraftType, string departureTime, string arrivalTime,
        string gate, string runway, Airport airport)
    {
        int hour = 6 + (index % 8);
        int minute = (index % 4) * 15;
        AddFlightWithTime(index, flightName, fromAirport, toAirport, status, durationMinutes, aircraftType, departureTime, arrivalTime, gate, runway, airport, hour, minute);
    }

    public void SpawnDemoFlights()
    {
        SpawnDemoFlights(mumbaiAirport, delhiAirport, bangaloreAirport);
    }
}
