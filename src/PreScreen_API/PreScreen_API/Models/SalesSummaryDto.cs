namespace PreScreen_API.Models;

public class SalesSummaryDto
{
    public decimal MedianUnitCost { get; set; }
    public string MostCommonRegion { get; set; } = string.Empty;
    public DateTime FirstOrderDate { get; set; }
    public DateTime LastOrderDate { get; set; }
    public int DaysBetween { get; set; }
    public decimal TotalRevenue { get; set; }
}
