using UnityEngine;

[CreateAssetMenu(fileName = "FlightIconConfig", menuName = "ATC/FlightIconConfig")]
public class FlightIconConfig : ScriptableObject
{
    [Header("Flight State Icons")]
    public Sprite takeoffIcon;
    public Sprite landingIcon;
    public Sprite onGroundIcon;
    public Sprite inAirIcon;

    public Sprite GetFlightIcon(FlightState state)
    {
        switch (state)
        {
            case FlightState.FlightCreated:
            case FlightState.ArrivalApproved:
            case FlightState.SlotConflict:
            case FlightState.Landed:
                return onGroundIcon;
            case FlightState.EnRoute:
                return inAirIcon != null ? inAirIcon : takeoffIcon;
            case FlightState.Arriving:
            case FlightState.Holding:
            case FlightState.Diverted:
                return landingIcon;
            default:
                return onGroundIcon;
        }
    }
}
