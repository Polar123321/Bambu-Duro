namespace ConsoleApp4.Services.Models;

public sealed record ShipCompatibilityResult(
    int Percent,
    string Title,
    string Summary,
    string SharedChannelsText,
    string ActiveHoursText,
    string ConfidenceText);

