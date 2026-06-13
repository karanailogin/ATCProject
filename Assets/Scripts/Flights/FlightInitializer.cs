using UnityEngine;

public class FlightInitializer : MonoBehaviour
{
    public Airport mumbaiAirport;
    public Airport delhiAirport;
    public Airport bangaloreAirport;

    private void Start()
    {
        FlightSpawner.SpawnDemoFlights(mumbaiAirport, delhiAirport, bangaloreAirport);
    }
}
