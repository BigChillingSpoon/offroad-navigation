using Routing.Domain.ValueObjects;
using System.Text.Json.Serialization;

[JsonPolymorphic]
[JsonDerivedType(typeof(TripDetailsWithData))]
[JsonDerivedType(typeof(EmptyTripDetails))]
public abstract record TripDetails;

public sealed record TripDetailsWithData( 
    string Polyline,
    Coordinate Start,
    Coordinate End
    //todo add bounds + turn instructions
) : TripDetails;


public sealed record EmptyTripDetails
    : TripDetails;

