namespace InventoryService.Services;

public enum ReservationMode
{
    Naive,
    Optimistic,
    Pessimistic,
    Distributed
}

public static class ReservationModeParser
{
    public static bool TryParse(string? value, out ReservationMode mode)
    {
        mode = ReservationMode.Naive;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Enum.TryParse(value, true, out mode);
    }
}
