namespace ConsoleApp4.Services.Models;

public sealed record ShopResult(
    bool Success,
    string Message,
    int NewBalance);
