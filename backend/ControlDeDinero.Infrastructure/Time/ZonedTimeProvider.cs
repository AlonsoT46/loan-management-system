namespace ControlDeDinero.Infrastructure.Time;

/// <summary>
/// A clock whose "local" time is a fixed time zone instead of the server's. The app uses it so
/// "today" is the Costa Rica date even if the server runs in UTC.
/// </summary>
public sealed class ZonedTimeProvider : TimeProvider
{
    private readonly TimeZoneInfo _zone;

    public ZonedTimeProvider(TimeZoneInfo zone)
    {
        _zone = zone;
    }

    public override TimeZoneInfo LocalTimeZone => _zone;

    public static ZonedTimeProvider CostaRica()
    {
        foreach (var id in new[] { "America/Costa_Rica", "Central America Standard Time" })
        {
            try
            {
                return new ZonedTimeProvider(TimeZoneInfo.FindSystemTimeZoneById(id));
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        // Costa Rica is UTC-6 all year (no daylight saving time).
        return new ZonedTimeProvider(
            TimeZoneInfo.CreateCustomTimeZone("CR", TimeSpan.FromHours(-6), "Costa Rica", "Costa Rica"));
    }
}
