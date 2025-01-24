using MudBlazor;

namespace HashKorea.DTOs.Shared;

public class SearchParameters
{
    private DateRange _dateRange;
    public DateRange dateRange
    {
        get => _dateRange;
        set
        {
            _dateRange = value;
            StartDate = value.Start.HasValue ? DateOnly.FromDateTime(value.Start.Value.Date) : null;
            EndDate = value.End.HasValue ? DateOnly.FromDateTime(value.End.Value.Date) : null;
        }
    }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string LocationCD { get; set; } = string.Empty;
    public string CategoryCD { get; set; } = string.Empty;
    public string SearchWord { get; set; } = string.Empty;
}