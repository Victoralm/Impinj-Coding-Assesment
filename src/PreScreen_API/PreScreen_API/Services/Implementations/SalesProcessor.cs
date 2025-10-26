using CsvHelper;
using CsvHelper.Configuration;
using PreScreen_API.Features;
using PreScreen_API.Models;
using PreScreen_API.Records;
using PreScreen_API.Services.Interfaces;
using System.Globalization;

namespace PreScreen_API.Services.Implementations;

public class SalesProcessor : ISalesProcessor
{
    public async Task<SalesSummaryDto> ProcessAsync(CsvParserCommand request, CancellationToken cancellationToken)
    {
        await using var input = request.File!.OpenReadStream();
        var regionCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        DateTime firstDate = DateTime.MaxValue, lastDate = DateTime.MinValue;
        decimal totalRevenue = 0m;

        // Heaps for streaming median:
        // lower: max-heap (implemented using priority = -value)
        // upper: min-heap (priority = value)
        var lower = new PriorityQueue<decimal, decimal>(); // stores lower half, top = largest of the lower half
        var upper = new PriorityQueue<decimal, decimal>(); // stores upper half, top = smallest of the upper half
        int count = 0;

        using var reader = new StreamReader(input);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            PrepareHeaderForMatch = args => args.Header?.Replace(" ", "").ToLowerInvariant()
        };
        using var csv = new CsvReader(reader, config);

        var records = csv.GetRecordsAsync<SalesRecord>(cancellationToken);

        await foreach (var saleRecord in records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            decimal value = saleRecord.UnitCost;
            // insert into one of the heaps
            if (lower.Count == 0 || value <= lower.Peek())
                lower.Enqueue(value, -value);
            else
                upper.Enqueue(value, value);

            // rebalance to maintain invariant: lower.Count == upper.Count || lower.Count == upper.Count + 1
            if (lower.Count > upper.Count + 1)
            {
                var moved = lower.Dequeue();
                upper.Enqueue(moved, moved);
            }
            else if (upper.Count > lower.Count)
            {
                var moved = upper.Dequeue();
                lower.Enqueue(moved, -moved);
            }

            count++;

            regionCounts[saleRecord.Region] = regionCounts.TryGetValue(saleRecord.Region, out var existingCount) ? existingCount + 1 : 1;
            if (saleRecord.OrderDate < firstDate)
                firstDate = saleRecord.OrderDate;
            if (saleRecord.OrderDate > lastDate)
                lastDate = saleRecord.OrderDate;
            totalRevenue += saleRecord.TotalRevenue;
        }

        if (count == 0)
            throw new ArgumentException("No records to process");

        // compute median from the tops of the heaps
        decimal median;
        if (lower.Count > upper.Count)
            median = lower.Peek(); // odd count
        else
            median = (lower.Peek() + upper.Peek()) / 2m; // even count

        var mostCommonRegion = regionCounts.Count > 0
            ? regionCounts.OrderByDescending(kvp => kvp.Value)
                .ThenBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
                .First().Key
            : string.Empty;

        return new SalesSummaryDto
        {
            MedianUnitCost   = decimal.Round(median, 2, MidpointRounding.AwayFromZero),
            MostCommonRegion = mostCommonRegion,
            FirstOrderDate   = firstDate,
            LastOrderDate    = lastDate,
            DaysBetween      = (lastDate - firstDate).Days,
            TotalRevenue     = decimal.Round(totalRevenue, 2, MidpointRounding.AwayFromZero)
        };
    }
}
