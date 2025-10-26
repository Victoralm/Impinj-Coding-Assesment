namespace PreScreen_API.Records;

public sealed record SalesRecord
(
    DateTime OrderDate,
    string Region,
    decimal UnitCost,
    decimal TotalRevenue
);
