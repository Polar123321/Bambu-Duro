using System.ComponentModel.DataAnnotations;

namespace ConsoleApp4.Configuration;

public sealed class EconomyConfiguration
{
    [Range(1, 10000)]
    public int DailyReward { get; init; } = 200;

    [Range(1, 10000)]
    public int WorkMinReward { get; init; } = 20;

    [Range(1, 10000)]
    public int WorkMaxReward { get; init; } = 80;

    [Range(0, 100)]
    public int CrimeSuccessChancePercent { get; init; } = 45;

    [Range(1, 20000)]
    public int CrimeMinReward { get; init; } = 50;

    [Range(1, 20000)]
    public int CrimeMaxReward { get; init; } = 200;

    [Range(1, 20000)]
    public int CrimeMinFine { get; init; } = 30;

    [Range(1, 20000)]
    public int CrimeMaxFine { get; init; } = 120;

    [Range(1, 5000)]
    public int ExpMinWork { get; init; } = 5;

    [Range(1, 5000)]
    public int ExpMaxWork { get; init; } = 15;

    [Range(1, 5000)]
    public int ExpMinCrime { get; init; } = 10;

    [Range(1, 5000)]
    public int ExpMaxCrime { get; init; } = 30;

    [Range(1, 5000)]
    public int ExpMinHunt { get; init; } = 8;

    [Range(1, 5000)]
    public int ExpMaxHunt { get; init; } = 20;
}
