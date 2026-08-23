using System.Diagnostics;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Abstractions.Messaging;

// =====================================================================
// 1. VERSÃO PARA COMANDOS QUE RETORNAM UM VALOR (Ex: Result<long>)
// =====================================================================
public abstract class BaseCommandHandler<TRequest, TResponse>(
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
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

            // Achado de revisão: isto rodava em um `Task.Run(...)` "fire-and-forget" usando o
            // MESMO IUnitOfWork/AppDbContext com escopo de requisição (logRepository/unitOfWork
            // acima). Como a task não era aguardada, o handler retornava e o pipeline HTTP podia
            // finalizar (e descartar o escopo de DI/DbContext) ENQUANTO essa task ainda rodava em
            // paralelo tentando salvar o log no MESMO DbContext — DbContext não é thread-safe e
            // não sobrevive ao fim do escopo. Essa corrida é a causa mais provável do
            // NullReferenceException relatado em ChangeDetector.DetectChanges/CommitAsync ao
            // "Fechar conta" (e, por estar aqui na base compartilhada, podia afetar qualquer
            // comando). Awaiting aqui garante que o log é escrito com o DbContext ainda válido,
            // dentro do mesmo escopo da requisição — o try/catch abaixo garante que uma falha ao
            // gravar o log nunca mascara o resultado real do comando nem derruba a requisição.
            try
            {
                var log = new LogTracker(0)
                {
                    AppUserId = userIdBox.Value,
                    DirectoryName = "Application/Features",
                    ClassName = className,
                    MethodName = methodName,
                    IsSuccess = isSuccess,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    ErrorMessage = errorMessage,
                    StackTrace = stackTrace,
                    IpAddress = ipAddress,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                await logRepository.AddAsync(log);
                await unitOfWork.CommitAsync();
            }
            catch
            {
                // Falhas de log nunca devem propagar exceções nem mascarar o resultado do comando.
            }
        }
    }

    protected class UserIdBox
    {
        public long? Value { get; set; }
    }
}

// =====================================================================
// 2. VERSÃO PARA COMANDOS QUE NÃO RETORNAM DADOS (Ex: Result)
// =====================================================================
public abstract class BaseCommandHandler<TRequest>(
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<TRequest>
    where TRequest : ICommand
{
    public abstract Task<Result> Handle(TRequest request, CancellationToken cancellationToken);

    protected async Task<Result> ExecuteWithLogAsync(
        string className,
        string methodName,
        string? ipAddress,
        Func<UserIdBox, Task<Result>> action)
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

            // Mesmo achado do outro overload acima: era um Task.Run fire-and-forget reusando o
            // AppDbContext/IUnitOfWork de escopo de requisição, que corre risco de ser descartado
            // pelo pipeline antes da task terminar — causa mais provável do
            // NullReferenceException em ChangeDetector.DetectChanges relatado ao "Fechar conta"
            // (CloseOrderCommandHandler usa exatamente este overload, de único parâmetro).
            // Awaiting inline elimina a corrida; o try/catch evita que falha de log derrube o
            // resultado real do comando.
            try
            {
                var log = new LogTracker(0)
                {
                    AppUserId = userIdBox.Value,
                    DirectoryName = "Application/Features",
                    ClassName = className,
                    MethodName = methodName,
                    IsSuccess = isSuccess,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    ErrorMessage = errorMessage,
                    StackTrace = stackTrace,
                    IpAddress = ipAddress,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

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