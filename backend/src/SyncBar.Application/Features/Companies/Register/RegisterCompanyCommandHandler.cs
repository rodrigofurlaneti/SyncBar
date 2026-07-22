using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Companies.Register;

internal sealed class RegisterCompanyCommandHandler(
    ICompanyRepository companyRepository,
    IBranchRepository branchRepository,
    IRoleRepository roleRepository,
    IAppUserRepository userRepository,
    IUserRoleRepository userRoleRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterCompanyCommand, RegisterCompanyResponse>
{
    public async Task<Result<RegisterCompanyResponse>> Handle(RegisterCompanyCommand request, CancellationToken cancellationToken)
    {
        if (await companyRepository.ExistsByCnpjAsync(request.Cnpj, cancellationToken))
            return Result.Failure<RegisterCompanyResponse>(
                new Error("Company.AlreadyExists", "A company with this CNPJ is already registered."));

        if (await userRepository.ExistsAsync(request.AdminUserName, request.AdminEmail, cancellationToken))
            return Result.Failure<RegisterCompanyResponse>(
                new Error("AppUser.AlreadyExists", "User name or e-mail already in use."));

        var companyResult = Company.Create(
            request.LegalName, request.TradeName, request.Cnpj, request.CompanyEmail, request.CompanyPhone);
        if (companyResult.IsFailure)
            return Result.Failure<RegisterCompanyResponse>(companyResult.Error);

        var company = companyResult.Value;
        await companyRepository.AddAsync(company, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken); // precisa do Company.Id para a filial

        var branchResult = Branch.Create(
            company.Id, request.BranchName, request.BranchCnpj, request.CompanyPhone,
            request.AddressStreet, request.AddressNumber, request.AddressDistrict,
            request.AddressCity, request.AddressState, request.AddressZipCode);
        if (branchResult.IsFailure)
            return Result.Failure<RegisterCompanyResponse>(branchResult.Error);

        var branch = branchResult.Value;
        await branchRepository.AddAsync(branch, cancellationToken);

        // Role "Administrador" — o nome precisa bater com o que o JWT usa para o bypass
        // de manager (GetMyFeaturesQueryHandler / IsManager), senão o admin recém-criado
        // fica sem acesso a nada até alguém liberar telas manualmente.
        var roleResult = Role.Create(company.Id, "Administrador", "Acesso total — criado no onboarding.");
        if (roleResult.IsFailure)
            return Result.Failure<RegisterCompanyResponse>(roleResult.Error);

        var role = roleResult.Value;
        await roleRepository.AddAsync(role, cancellationToken);

        var passwordHash = passwordHasher.Hash(request.AdminPassword);
        var userResult = AppUser.Create(company.Id, null, request.AdminUserName, request.AdminEmail, passwordHash);
        if (userResult.IsFailure)
            return Result.Failure<RegisterCompanyResponse>(userResult.Error);

        var user = userResult.Value;
        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken); // precisa dos Ids de Role/AppUser para o vínculo

        var linkResult = UserRole.Create(user.Id, role.Id);
        if (linkResult.IsFailure)
            return Result.Failure<RegisterCompanyResponse>(linkResult.Error);

        await userRoleRepository.AddAsync(linkResult.Value, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(new RegisterCompanyResponse(company.Id, branch.Id, user.Id));
    }
}
