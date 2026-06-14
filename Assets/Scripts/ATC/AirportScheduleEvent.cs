public enum AirportEventType
{
    Arrival,
    Departure
}

[System.Serializable]
public class AirportScheduleEvent
{
    public string FlightID;
    public string FlightNumber;
    public AirportEventType EventType;
    public string AirportCode;
    public string OtherAirportCode;
    public string EventTime;
}