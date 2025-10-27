namespace PreScreen_API.Features.Interfaces;

public interface ICommandQueryHandler<TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
