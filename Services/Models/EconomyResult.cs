namespace ConsoleApp4.Services.Models;

public sealed record EconomyResult(
    bool Success,
    string Message,
    int CoinsDelta,
    int ExpDelta,
    int NewBalance,
    int NewLevel,
    int NewExp);
