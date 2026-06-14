using UnityEngine;
using System.Collections.Generic;

public class FlightManager : MonoBehaviour
{
    public static FlightManager Instance;
    private List<Flight> allFlights = new List<Flight>();

    public List<Flight> AllFlights => allFlights;

    [Header("Virtual Time Simulation")]
    public float currentVirtualTime = 360f; // Starts at 06:00 (6 * 60)
    public float timeSpeed = 10f; // 10 simulated minutes per real-time second

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

    private void Update()
    {
        if (!Application.isPlaying) return;

        // Recalculate slot conflicts dynamically
        RecalculateSlotConflicts();

        // Advance virtual time from centralized WorldClockManager if available
        if (WorldClockManager.Instance != null)
        {
            currentVirtualTime = WorldClockManager.Instance.currentVirtualTime;
        }
        else
        {
            currentVirtualTime += timeSpeed * Time.deltaTime;
            if (currentVirtualTime >= 1440f) // Wrap at 24 hours
            {
                currentVirtualTime -= 1440f;
            }
        }

        // Update each flight state
        foreach (var flight in allFlights)
        {
            UpdateFlightSimulation(flight);
        }
    }

    public void RecalculateSlotConflicts()
    {
        HashSet<Flight> conflictingFlights = new HashSet<Flight>();

        for (int i = 0; i < allFlights.Count; i++)
        {
            Flight f1 = allFlights[i];
            if (f1 == null || f1.takeoffSlot == null) continue;

            // We only check departure slot conflicts for flights that haven't departed yet
            if (f1.state != FlightState.FlightCreated && 
                f1.state != FlightState.ArrivalApproved && 
                f1.state != FlightState.SlotConflict)
            {
                continue;
            }

            string depAirport = f1.fromAirport;
            int depHour = f1.takeoffSlot.hours;
            int depMin = f1.takeoffSlot.minutes;

            bool hasConflict = false;

            for (int j = 0; j < allFlights.Count; j++)
            {
                if (i == j) continue;
                Flight f2 = allFlights[j];
                if (f2 == null) continue;

                // Check departure conflict at the same airport (only if f2 has not departed yet)
                if (f2.fromAirport == depAirport && f2.takeoffSlot != null)
                {
                    if (f2.state == FlightState.FlightCreated || 
                        f2.state == FlightState.ArrivalApproved || 
                        f2.state == FlightState.SlotConflict)
                    {
                        if (f2.takeoffSlot.hours == depHour && f2.takeoffSlot.minutes == depMin)
                        {
                            hasConflict = true;
                            break;
                        }
                    }
                }

                // Check arrival conflict at the same airport (only if f2 is still active and hasn't landed/diverted)
                if (f2.toAirport == depAirport && f2.landingSlot != null)
                {
                    if (f2.state != FlightState.Landed && f2.state != FlightState.Diverted)
                    {
                        if (f2.landingSlot.hours == depHour && f2.landingSlot.minutes == depMin)
                        {
                            hasConflict = true;
                            break;
                        }
                    }
                }
            }

            if (hasConflict)
            {
                conflictingFlights.Add(f1);
            }
        }

        // Apply states
        foreach (var f in allFlights)
        {
            if (f == null) continue;

            // Only affect flights in pre-departure states
            if (f.state == FlightState.FlightCreated || 
                f.state == FlightState.ArrivalApproved || 
                f.state == FlightState.SlotConflict)
            {
                if (conflictingFlights.Contains(f))
                {
                    if (f.state != FlightState.SlotConflict)
                    {
                        f.state = FlightState.SlotConflict;
                    }
                }
                else
                {
                    if (f.state == FlightState.SlotConflict)
                    {
                        f.state = f.landingApproved ? FlightState.ArrivalApproved : FlightState.FlightCreated;
                    }
                }
            }
        }
    }

    public string GetConflictingFlightInfo(Flight f)
    {
        if (f == null || f.takeoffSlot == null) return "";

        string depAirport = f.fromAirport;
        int depHour = f.takeoffSlot.hours;
        int depMin = f.takeoffSlot.minutes;

        for (int j = 0; j < allFlights.Count; j++)
        {
            Flight f2 = allFlights[j];
            if (f2 == null || f2 == f) continue;

            // Check departure conflict at the same airport
            if (f2.fromAirport == depAirport && f2.takeoffSlot != null)
            {
                if (f2.state == FlightState.FlightCreated || 
                    f2.state == FlightState.ArrivalApproved || 
                    f2.state == FlightState.SlotConflict)
                {
                    if (f2.takeoffSlot.hours == depHour && f2.takeoffSlot.minutes == depMin)
                    {
                        return $"{f2.flightName} DEP {f2.takeoffSlot.GetTimeString()}";
                    }
                }
            }

            // Check arrival conflict at the same airport
            if (f2.toAirport == depAirport && f2.landingSlot != null)
            {
                if (f2.state != FlightState.Landed && f2.state != FlightState.Diverted)
                {
                    if (f2.landingSlot.hours == depHour && f2.landingSlot.minutes == depMin)
                    {
                        return $"{f2.flightName} ARR {f2.landingSlot.GetTimeString()}";
                    }
                }
            }
        }

        return "";
    }

    private void UpdateFlightSimulation(Flight flight)
    {
        if (flight == null) return;

        int depMinutes = flight.takeoffSlot != null ? flight.takeoffSlot.GetTotalMinutes() : 360;
        int landingMinutes = depMinutes + flight.flightDurationMinutes;

        switch (flight.state)
        {
            case FlightState.FlightCreated:
                // Stays in Awaiting Approval until landingApproved is true
                if (flight.landingApproved)
                {
                    flight.state = FlightState.ArrivalApproved;
                }
                break;

            case FlightState.ArrivalApproved:
                // Transitions at scheduled departure time
                if (currentVirtualTime >= depMinutes)
                {
                    if (flight.landingApproved)
                    {
                        flight.state = FlightState.EnRoute;
                    }
                    else
                    {
                        flight.state = FlightState.Holding;
                        flight.holdingStartTime = currentVirtualTime;
                    }
                }
                break;

            case FlightState.Holding:
                // Transitions if approved while holding
                if (flight.landingApproved)
                {
                    flight.state = FlightState.EnRoute;
                }
                else
                {
                    // If holding exceeds timeout (30 virtual minutes), transition to Diverted
                    float heldDuration = currentVirtualTime - flight.holdingStartTime;
                    if (heldDuration < 0f) heldDuration += 1440f; // Handle midnight wrap

                    if (heldDuration >= 30f)
                    {
                        flight.state = FlightState.Diverted;
                    }
                }
                break;

            case FlightState.EnRoute:
                // Transitions to Arriving when reaching destination airspace (15 virtual minutes remaining)
                if (currentVirtualTime >= landingMinutes - 15f)
                {
                    flight.state = FlightState.Arriving;
                }
                break;

            case FlightState.Arriving:
                // Transitions to Landed after arrival processing (reaching landing time)
                if (currentVirtualTime >= landingMinutes)
                {
                    flight.state = FlightState.Landed;
                }
                break;

            case FlightState.Landed:
            case FlightState.Diverted:
                // Terminal states
                break;
        }
    }

    public void AddFlightToAirport(Flight flight, Airport airport)
    {
        flight.currentAirport = airport;
        allFlights.Add(flight);
    }

    public void ClearAllFlights()
    {
        allFlights.Clear();
        Debug.Log("Flight list cleared.");
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
