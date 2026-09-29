namespace Investec.SK.Application.Maine;

/// <summary>One entry of a year group. The year lives on the group, so it is not repeated here.</summary>
public sealed record MaineItem(
    string BookName,
    string Isbn,
    int Pages,
    string VillainName);

public sealed record MaineYearGroup(int Year, IReadOnlyList<MaineItem> Items);
