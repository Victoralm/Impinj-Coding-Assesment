using CsvHelper;
using CsvHelper.Configuration;
using PreScreen_API.Features;
using PreScreen_API.Models;
using PreScreen_API.Records;
using PreScreen_API.Services.Interfaces;
using System.Globalization;

namespace PreScreen_API.Services.Implementations;

public class SalesProcessorWithQuickSelect : ISalesProcessor
{
    private static readonly Random _rng = new();

    public async Task<SalesSummaryDto> ProcessAsync(CsvParserQuery request, CancellationToken cancellationToken)
    {
        await using var input = request.File!.OpenReadStream();
        var regionCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        DateTime firstDate = DateTime.MaxValue, lastDate = DateTime.MinValue;
        decimal totalRevenue = 0m;
        var costs = new List<decimal>();

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

            costs.Add(saleRecord.UnitCost);

            regionCounts[saleRecord.Region] = regionCounts.TryGetValue(saleRecord.Region, out var existingCount) ? existingCount + 1 : 1;

            if (saleRecord.OrderDate < firstDate)
                firstDate = saleRecord.OrderDate;
            if (saleRecord.OrderDate > lastDate)
                lastDate = saleRecord.OrderDate;

            totalRevenue += saleRecord.TotalRevenue;
        }

        int amount = costs.Count;
        if (amount == 0)
            throw new ArgumentException("No records to process");

        // materialize once and use QuickSelect to compute median
        var unitCostArray = costs.ToArray();

        decimal median = amount % 2 == 1
            ? QuickSelect(unitCostArray, amount / 2)
            : (QuickSelect(unitCostArray, (amount / 2) - 1) + QuickSelect(unitCostArray, amount / 2)) / 2m;

        var mostCommonRegion = regionCounts.Count > 0
            ? regionCounts.OrderByDescending(kvp => kvp.Value)
                .ThenBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
                .First().Key
            : string.Empty;

        return new SalesSummaryDto
        {
            MedianUnitCost = decimal.Round(median, 2, MidpointRounding.AwayFromZero),
            MostCommonRegion = mostCommonRegion,
            FirstOrderDate = firstDate,
            LastOrderDate = lastDate,
            DaysBetween = (lastDate - firstDate).Days,
            TotalRevenue = decimal.Round(totalRevenue, 2, MidpointRounding.AwayFromZero)
        };
    }

    // QuickSelect (select k-th smallest, 0-based)
    private static decimal QuickSelect(decimal[] arr, int k)
    {
        int left = 0, right = arr.Length - 1;
        while (true)
        {
            if (left == right) return arr[left];
            int pivotIndex = Partition(arr, left, right);
            if (k == pivotIndex) return arr[k];
            if (k < pivotIndex) right = pivotIndex - 1;
            else left = pivotIndex + 1;
        }
    }

    private static int Partition(decimal[] arr, int left, int right)
    {
        int pivotIndex = _rng.Next(left, right + 1);
        decimal pivotValue = arr[pivotIndex];
        Swap(arr, pivotIndex, right);
        int storeIndex = left;
        for (int i = left; i < right; i++)
        {
            if (arr[i] < pivotValue)
            {
                Swap(arr, storeIndex, i);
                storeIndex++;
            }
        }
        Swap(arr, storeIndex, right);
        return storeIndex;
    }

    private static void Swap(decimal[] arr, int i, int j)
    {
        if (i == j) return;
        (arr[i], arr[j]) = (arr[j], arr[i]);
    }
}
