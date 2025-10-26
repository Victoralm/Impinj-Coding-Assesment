namespace PreScreen_API.Features.Interfaces;

public interface ICommandHandler<TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
