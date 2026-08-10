using System.Diagnostics;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Abstractions.Messaging;

public abstract class BaseQueryHandler<TRequest, TResponse>(
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : IQueryHandler<TRequest, TResponse>
    where TRequest : IQuery<TResponse>
{
    public abstract Task<Result<TResponse>> Handle(TRequest request, CancellationToken cancellationToken);

    protected async Task<Result<TResponse>> ExecuteWithLogAsync(
        string className,
        string methodName,
        string? ipAddress,
        Func<UserIdBox, Task<Result<TResponse>>> action)
    {
        var stopwatch = Stopwatch.StartNew();
        var userIdBox = new UserIdBox();
        var isSuccess = true;
        string? errorMessage = null;
        string? stackTrace = null;

        try
        {
            var result = await action(userIdBox);
            isSuccess = !result.IsFailure;
            if (result.IsFailure)
            {
                errorMessage = result.Error.Message;
            }
            return result;
        }
        catch (Exception ex)
        {
            isSuccess = false;
            errorMessage = ex.Message;
            stackTrace = ex.StackTrace;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var log = new LogTracker(0)
            {
                AppUserId = userIdBox.Value,
                DirectoryName = "Application/Queries",
                ClassName = className,
                MethodName = methodName,
                IsSuccess = isSuccess,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = errorMessage,
                StackTrace = stackTrace,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            try
            {
                await logRepository.AddAsync(log);
                await unitOfWork.CommitAsync();
            }
            catch
            {
                // Evita falhas de log
            }
        }
    }

    protected class UserIdBox
    {
        public long? Value { get; set; }
    }
}