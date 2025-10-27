using Microsoft.AspNetCore.Http;
using PreScreen_API.Features;
using PreScreen_API.Services.Implementations;
using System.Text;

namespace Test_PreScreen_API;

public class SalesProcessorTests
{
    private static IFormFile CreateFormFile(string content, string fileName = "test.csv")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        stream.Position = 0;

        return new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv"
        };
    }

    [Fact]
    public async Task ProcessAsync_ReturnsCorrectSummary_ForOddCount()
    {
        var csv = new StringBuilder();
        csv.AppendLine("OrderDate,Region,UnitCost,TotalRevenue");
        csv.AppendLine("2020-01-01,North,10.00,100.00");
        csv.AppendLine("2020-01-03,South,30.00,300.00");
        csv.AppendLine("2020-01-05,North,20.00,200.00");

        var file = CreateFormFile(csv.ToString());
        var processor = new SalesProcessor();

        var result = await processor.ProcessAsync(new CsvParserQuery(file), CancellationToken.None);

        // median of [10.00, 20.00, 30.00] = position (n+1)/2 = (3+1)/2 = ordered middle value = 20.00
        Assert.Equal(20.00m, result.MedianUnitCost);
        Assert.Equal("North", result.MostCommonRegion);
        Assert.Equal(new DateTime(2020, 1, 1), result.FirstOrderDate.Date);
        Assert.Equal(new DateTime(2020, 1, 5), result.LastOrderDate.Date);
        Assert.Equal(4, result.DaysBetween);
        Assert.Equal(600.00m, result.TotalRevenue);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsCorrectMedian_ForEvenCount()
    {
        var csv = new StringBuilder();
        csv.AppendLine("OrderDate,Region,UnitCost,TotalRevenue");
        csv.AppendLine("2020-01-01,East,639.20,752.00");
        csv.AppendLine("2020-01-02,East,116.45,137.00");
        csv.AppendLine("2020-01-03,West,189.55,223.00");
        csv.AppendLine("2020-01-04,West,413.10,486.00");

        var file = CreateFormFile(csv.ToString());
        var processor = new SalesProcessor();

        var result = await processor.ProcessAsync(new CsvParserQuery(file), CancellationToken.None);

        // median of [639.20,116.45,189.55,413.10] = (116.45 + 189.55) / 2 = 301.33
        Assert.Equal(301.33m, result.MedianUnitCost);
        Assert.True(result.MostCommonRegion == "East"); // If counts tie, the first alphabetically is chosen
        Assert.Equal(new DateTime(2020, 1, 1), result.FirstOrderDate.Date);
        Assert.Equal(new DateTime(2020, 1, 4), result.LastOrderDate.Date);
        Assert.Equal(3, result.DaysBetween);
        Assert.Equal(1598.00m, result.TotalRevenue);
    }

    [Fact]
    public async Task ProcessAsync_ThrowsArgumentException_WhenNoRecords()
    {
        // header only -> zero records
        var csv = "OrderDate,Region,UnitCost,TotalRevenue\n";
        var file = CreateFormFile(csv);
        var processor = new SalesProcessor();

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await processor.ProcessAsync(new CsvParserQuery(file), CancellationToken.None));
    }
}
