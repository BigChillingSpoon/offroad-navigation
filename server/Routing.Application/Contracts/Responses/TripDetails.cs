using Routing.Domain.ValueObjects;

public abstract record TripDetails;

public sealed record TripDetailsWithData( 
    string Polyline,
    Coordinate Start,
    Coordinate End
    //todo add bounds + turn instructions
) : TripDetails;


public sealed record EmptyTripDetails
    : TripDetails;

