using Carter;
using CsvHelper;
using CsvHelper.Configuration;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Polly.Registry;
using PreScreen_API.Extensions;
using PreScreen_API.Features.Interfaces;
using PreScreen_API.Models;
using PreScreen_API.Services.Implementations;
using System.Globalization;
using System.Text;

namespace PreScreen_API.Features;

public record CsvParserQuery(IFormFile? File);

public class CsvParserQueryValidator : AbstractValidator<CsvParserQuery>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/csv",
        "application/csv",
        "application/vnd.ms-excel"  // sometimes CSV is submitted as excel
    };

    private const long MaxSizeBytes = 15L * 1024 * 1024; // example: 15 MB

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".csv"
    };

    public CsvParserQueryValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("File is required.")
            .Must(f => f.Length > 0).WithMessage("File is empty.")
            .Must(f => f.Length <= MaxSizeBytes)
                .WithMessage($"Maximum allowed size: {MaxSizeBytes} bytes.")
            .Must(f => string.IsNullOrWhiteSpace(f.ContentType) || AllowedContentTypes.Contains(f.ContentType!))
                .WithMessage("Content-Type not allowed for CSV file.")
            .Must(f => AllowedExtensions.Contains(Path.GetExtension(f.FileName)))
                .WithMessage("File extension not allowed, please upload a \".csv\" file.")
            .MustAsync(async (formFile, cancellationToken) =>
            {
                if (formFile == null) return false;
                try
                {
                    // Opens a separate read stream (OpenReadStream normally returns an independent stream)
                    using var stream = formFile.OpenReadStream();
                    using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);

                    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = true,
                        MissingFieldFound = null,
                        PrepareHeaderForMatch = args => args.Header?.Replace(" ", "").ToLowerInvariant()
                    };

                    using var csv = new CsvReader(reader, config);

                    // Tries to read the header
                    if (!await csv.ReadAsync().ConfigureAwait(false))
                        return false;

                    csv.ReadHeader();

                    // Checks if there is at least one data row
                    return await csv.ReadAsync().ConfigureAwait(false);
                }
                catch
                {
                    // If parsing fails, consider it invalid (generic message)
                    return false;
                }
            }).WithMessage("The file contains no records or is in an invalid format.");
    }
}

public sealed class CsvParserQueryHandler(
    ILogger<CsvParserQueryHandler> _logger,
    ResiliencePipelineProvider<string> _pipelineProvider) : ICommandQueryHandler<CsvParserQuery, ResultDto<SalesSummaryDto>>
{
    public async Task<ResultDto<SalesSummaryDto>> HandleAsync(CsvParserQuery request, CancellationToken cancellationToken)
    {
        var pipeline = _pipelineProvider.GetPipeline("default");
        if (pipeline == null)
        {
            _logger.LogError("Resilience pipeline \"default\" not found.");
            return ResultDto<SalesSummaryDto>.Failure(new[] { "Resilience pipeline not found." });
        }

        try
        {
            var csvParserProcessor = new SalesProcessor();
            //var csvParserProcessor = new SalesProcessorWithQuickSelect();

            var result = await pipeline.ExecuteAsync(
                async ct => await csvParserProcessor.ProcessAsync(request, cancellationToken),
                cancellationToken
            );

            return ResultDto<SalesSummaryDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while parsing \".csv\" file: {request}", request);

            return ResultDto<SalesSummaryDto>.Failure(new[] { $"Error while parsing \".csv\" file." });
        }
    }
}

public sealed class CsvFileParser : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/CsvFileParser", async (
            [FromForm] CsvParserQuery request,
            [FromServices] IValidator<CsvParserQuery> validator,
            [FromServices] CsvParserQueryHandler handler,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                return Results.BadRequest(validationResult.ToResultDtoFailure<ResultDto<SalesSummaryDto>>());

            var result = await handler.HandleAsync(request, cancellationToken);

            if (result == null || !result.Success)
                return Results.BadRequest(result);
            return Results.Ok(result);
        })
        .WithTags("Csv Endpoints")
        .WithName("CsvFileParser")
        .WithSummary("CSV file parsing")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .ProducesValidationProblem()
        .DisableAntiforgery();
    }
}
