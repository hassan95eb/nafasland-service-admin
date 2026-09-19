using Microsoft.Extensions.DependencyInjection;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Persistence;

namespace NafasLand.Admin.Shared.Infrastructure.Messaging;

/// <summary>
/// The fourth link in the pipeline (ADR-006). Only opens a transaction for
/// commands whose module has registered an <see cref="IUnitOfWork"/>; commands
/// with no need to write to the database pass through without a transaction.
/// </summary>
internal sealed class TransactionBehavior<TCommand, TResponse>(IServiceProvider serviceProvider)
    : IPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TCommand command,
        CommandHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var moduleName = ModuleNameResolver.ResolveFromNamespace(typeof(TCommand).Namespace);
        var unitOfWork = moduleName is null
            ? null
            : serviceProvider.GetKeyedService<IUnitOfWork>(moduleName);

        if (unitOfWork is null)
        {
            return await next();
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var response = await next();
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return response;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
