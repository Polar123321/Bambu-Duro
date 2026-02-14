namespace ConsoleApp4.Services.Models;

public sealed record SpendResult(
    bool Success,
    string Message,
    int Amount,
    int NewBalance);
