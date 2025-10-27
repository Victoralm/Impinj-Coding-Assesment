using PreScreen_API.Features;
using PreScreen_API.Models;

namespace PreScreen_API.Services.Interfaces;

public interface ISalesProcessor
{
    Task<SalesSummaryDto> ProcessAsync(CsvParserQuery request, CancellationToken cancellationToken);
}
