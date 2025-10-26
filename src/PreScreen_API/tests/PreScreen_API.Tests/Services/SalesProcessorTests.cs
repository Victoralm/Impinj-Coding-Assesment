using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using PreScreen_API.Features;
using PreScreen_API.Services.Implementations;
using Xunit;

namespace PreScreen_API.Tests.Services;

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
        csv.AppendLine("OrderDate,Region,UnitCost,TotalRevenue,UnitsSold");
        csv.AppendLine("2020-01-01,North,10.00,100.00,10");
        csv.AppendLine("2020-01-03,South,30.00,300.00,10");
        csv.AppendLine("2020-01-05,North,20.00,200.00,10");

        var file = CreateFormFile(csv.ToString());
        var sut = new SalesProcessor();

        var result = await sut.ProcessAsync(new CsvParserCommand(file), CancellationToken.None);

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
        csv.AppendLine("OrderDate,Region,UnitCost,TotalRevenue,UnitsSold");
        csv.AppendLine("2020-01-01,East,10.00,100.00,10");
        csv.AppendLine("2020-01-02,East,20.00,200.00,10");
        csv.AppendLine("2020-01-03,West,30.00,300.00,10");
        csv.AppendLine("2020-01-04,West,40.00,400.00,10");

        var file = CreateFormFile(csv.ToString());
        var sut = new SalesProcessor();

        var result = await sut.ProcessAsync(new CsvParserCommand(file), CancellationToken.None);

        // median of [10,20,30,40] = (20 + 30) / 2 = 25
        Assert.Equal(25.00m, result.MedianUnitCost);
        Assert.True(result.MostCommonRegion == "East" || result.MostCommonRegion == "West"); // either region can be returned when counts tie
        Assert.Equal(new DateTime(2020, 1, 1), result.FirstOrderDate.Date);
        Assert.Equal(new DateTime(2020, 1, 4), result.LastOrderDate.Date);
        Assert.Equal(3, result.DaysBetween);
        Assert.Equal(1000.00m, result.TotalRevenue);
    }

    [Fact]
    public async Task ProcessAsync_ThrowsArgumentException_WhenNoRecords()
    {
        // header only -> zero records
        var csv = "OrderDate,Region,UnitCost,TotalRevenue,UnitsSold\n";
        var file = CreateFormFile(csv);
        var sut = new SalesProcessor();

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await sut.ProcessAsync(new CsvParserCommand(file), CancellationToken.None));
    }
}