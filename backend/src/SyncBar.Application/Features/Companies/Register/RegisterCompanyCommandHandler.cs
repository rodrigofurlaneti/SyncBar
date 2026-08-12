using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Companies.Register;

internal sealed class RegisterCompanyCommandHandler : BaseCommandHandler<RegisterCompanyCommand, RegisterCompanyResponse>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IAppUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCompanyCommandHandler(
        ICompanyRepository companyRepository,
        IBranchRepository branchRepository,
        IRoleRepository roleRepository,
        IAppUserRepository userRepository,
        IUserRoleRepository userRoleRepository,
        IPasswordHasher passwordHasher,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _companyRepository = companyRepository;
        _branchRepository = branchRepository;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result<RegisterCompanyResponse>> Handle(RegisterCompanyCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(RegisterCompanyCommandHandler),
            nameof(Handle),
            null, // Se houver a captura de IP no request, substitua o null aqui
            async (userIdBox) =>
            {
                if (await _companyRepository.ExistsByCnpjAsync(request.Cnpj, cancellationToken))
                    return Result.Failure<RegisterCompanyResponse>(
                        new Error("Company.AlreadyExists", "A company with this CNPJ is already registered."));

                if (await _userRepository.ExistsAsync(request.AdminUserName, request.AdminEmail, cancellationToken))
                    return Result.Failure<RegisterCompanyResponse>(
                        new Error("AppUser.AlreadyExists", "User name or e-mail already in use."));

                var companyResult = Company.Create(
                    request.LegalName, request.TradeName, request.Cnpj, request.CompanyEmail, request.CompanyPhone);
                if (companyResult.IsFailure)
                    return Result.Failure<RegisterCompanyResponse>(companyResult.Error);

                var company = companyResult.Value;
                await _companyRepository.AddAsync(company, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken); // precisa do Company.Id para a filial

                var branchResult = Branch.Create(
                    company.Id, request.BranchName, request.BranchCnpj, request.CompanyPhone,
                    request.AddressStreet, request.AddressNumber, request.AddressDistrict,
                    request.AddressCity, request.AddressState, request.AddressZipCode);
                if (branchResult.IsFailure)
                    return Result.Failure<RegisterCompanyResponse>(branchResult.Error);

                var branch = branchResult.Value;
                await _branchRepository.AddAsync(branch, cancellationToken);

                // Role "Administrador" — o nome precisa bater com o que o JWT usa para o bypass
                // de manager (GetMyFeaturesQueryHandler / IsManager), senão o admin recém-criado
                // fica sem acesso a nada até alguém liberar telas manualmente.
                var roleResult = Role.Create(company.Id, "Administrador", "Acesso total — criado no onboarding.");
                if (roleResult.IsFailure)
                    return Result.Failure<RegisterCompanyResponse>(roleResult.Error);

                var role = roleResult.Value;
                await _roleRepository.AddAsync(role, cancellationToken);

                var passwordHash = _passwordHasher.Hash(request.AdminPassword);
                var userResult = AppUser.Create(company.Id, null, request.AdminUserName, request.AdminEmail, passwordHash);
                if (userResult.IsFailure)
                    return Result.Failure<RegisterCompanyResponse>(userResult.Error);

                var user = userResult.Value;
                await _userRepository.AddAsync(user, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken); // precisa dos Ids de Role/AppUser para o vínculo

                var linkResult = UserRole.Create(user.Id, role.Id);
                if (linkResult.IsFailure)
                    return Result.Failure<RegisterCompanyResponse>(linkResult.Error);

                await _userRoleRepository.AddAsync(linkResult.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                // Adiciona o Id do usuário recém-criado ao log de auditoria
                userIdBox.Value = user.Id;

                return Result.Success(new RegisterCompanyResponse(company.Id, branch.Id, user.Id));
            });
    }
}