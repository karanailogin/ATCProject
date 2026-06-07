using UnityEngine;

[System.Serializable]
public class TimeSlot
{
    public int hours;
    public int minutes;
    public bool isBooked;
    public string bookedByFlight; // Which flight booked this slot

    public TimeSlot(int h, int m)
    {
        hours = h;
        minutes = m;
        isBooked = false;
        bookedByFlight = "";
    }

    public string GetTimeString()
    {
        return $"{hours:D2}:{minutes:D2}";
    }

    public int GetTotalMinutes()
    {
        return hours * 60 + minutes;
    }
}
