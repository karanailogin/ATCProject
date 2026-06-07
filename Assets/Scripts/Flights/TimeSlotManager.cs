using UnityEngine;
using System.Collections.Generic;

public class TimeSlotManager : MonoBehaviour
{
    public static TimeSlotManager Instance;
    
    // Dictionary: Airport name -> List of time slots
    private Dictionary<string, List<TimeSlot>> airportSlots = new Dictionary<string, List<TimeSlot>>();

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

        InitializeTimeSlots();
    }

    private void InitializeTimeSlots()
    {
        // Create 5-minute slots for each airport (06:00 to 22:00)
        string[] airports = { "MUM", "DEL", "BLR" };

        foreach (string airport in airports)
        {
            List<TimeSlot> slots = new List<TimeSlot>();

            for (int hour = 6; hour < 22; hour++)
            {
                for (int minute = 0; minute < 60; minute += 5)
                {
                    slots.Add(new TimeSlot(hour, minute));
                }
            }

            airportSlots[airport] = slots;
        }
    }

    public List<TimeSlot> GetAvailableSlots(string airportCode)
    {
        if (airportSlots.ContainsKey(airportCode))
        {
            return airportSlots[airportCode];
        }

        Debug.LogWarning($"Airport {airportCode} not found in TimeSlotManager!");
        return new List<TimeSlot>();
    }

    public bool BookTimeSlot(string airportCode, TimeSlot slot, string flightName)
    {
        if (slot.isBooked)
        {
            Debug.LogWarning($"Time slot {slot.GetTimeString()} already booked!");
            return false;
        }

        slot.isBooked = true;
        slot.bookedByFlight = flightName;
        Debug.Log($"Booked slot {slot.GetTimeString()} at {airportCode} for flight {flightName}");
        return true;
    }

    public void ReleaseTimeSlot(string airportCode, TimeSlot slot)
    {
        slot.isBooked = false;
        slot.bookedByFlight = "";
        Debug.Log($"Released slot {slot.GetTimeString()} at {airportCode}");
    }

    public TimeSlot GetSuggestedLandingSlot(string destinationCode, TimeSlot takeoffSlot, int flightDurationMinutes)
    {
        int landingTotalMinutes = takeoffSlot.GetTotalMinutes() + flightDurationMinutes;
        
        // Handle day wrap (past 22:00)
        if (landingTotalMinutes >= 1320) // 22:00 = 1320 minutes
        {
            landingTotalMinutes -= 1320; // Wrap to next day
        }

        int landingHour = landingTotalMinutes / 60;
        int landingMinute = landingTotalMinutes % 60;

        // Find closest available slot
        List<TimeSlot> slots = GetAvailableSlots(destinationCode);
        foreach (TimeSlot slot in slots)
        {
            if (slot.GetTotalMinutes() >= landingTotalMinutes && !slot.isBooked)
            {
                return slot;
            }
        }

        // If not found, get the next available
        foreach (TimeSlot slot in slots)
        {
            if (!slot.isBooked)
            {
                return slot;
            }
        }

        return null;
    }
}
