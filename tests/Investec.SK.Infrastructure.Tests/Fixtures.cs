using System.Text.Json;

namespace Investec.SK.Infrastructure.Tests;

/// <summary>
/// Reads the golden payloads captured from the live feed on 2026-09-29. Tests read these files,
/// never the network.
/// </summary>
public static class Fixtures
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string Read(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", name));

    public static string Books => Read("books.json");

    public static string Villains => Read("villains.json");
}
