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
    private readonly IDiningTableRepository _diningTableRepository;
    private readonly IComandaRepository _comandaRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IJobTitleRepository _jobTitleRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly string[] DefaultCategoryNames =
    [
        "Bebidas",
        "Petiscos",
        "Pratos Principais",
        "Drinks",
        "Sobremesas"
    ];

    public RegisterCompanyCommandHandler(
        ICompanyRepository companyRepository,
        IBranchRepository branchRepository,
        IRoleRepository roleRepository,
        IAppUserRepository userRepository,
        IUserRoleRepository userRoleRepository,
        IDiningTableRepository diningTableRepository,
        IComandaRepository comandaRepository,
        ICategoryRepository categoryRepository,
        IJobTitleRepository jobTitleRepository,
        IEmployeeRepository employeeRepository,
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
        _diningTableRepository = diningTableRepository;
        _comandaRepository = comandaRepository;
        _categoryRepository = categoryRepository;
        _jobTitleRepository = jobTitleRepository;
        _employeeRepository = employeeRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result<RegisterCompanyResponse>> Handle(RegisterCompanyCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(RegisterCompanyCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var uniquenessResult = await ValidateUniquenessAsync(request, cancellationToken);
                if (uniquenessResult.IsFailure)
                    return Result.Failure<RegisterCompanyResponse>(uniquenessResult.Error);

                var structureResult = await SetupCompanyStructureAsync(request, cancellationToken);
                if (structureResult.IsFailure)
                    return Result.Failure<RegisterCompanyResponse>(structureResult.Error);
                var (company, branch) = structureResult.Value;

                var adminResult = await SetupAdminAccountAsync(request, company.Id, branch.Id, cancellationToken);
                if (adminResult.IsFailure)
                    return Result.Failure<RegisterCompanyResponse>(adminResult.Error);
                var user = adminResult.Value;

                // Adiciona o Id do usuário recém-criado ao log de auditoria
                userIdBox.Value = user.Id;

                return Result.Success(new RegisterCompanyResponse(company.Id, branch.Id, user.Id));
            });
    }

    // Fase Sonar HIGH (2026-08-24): extraído do Handle para reduzir Cognitive Complexity de
    // 16 para o limite de 15 — mesma sequência de passos, sem mudança de comportamento.
    private async Task<Result<(Company Company, Branch Branch)>> SetupCompanyStructureAsync(
        RegisterCompanyCommand request, CancellationToken cancellationToken)
    {
        var companyResult = await CreateCompanyAsync(request, cancellationToken);
        if (companyResult.IsFailure)
            return Result.Failure<(Company, Branch)>(companyResult.Error);
        var company = companyResult.Value;

        await CreateDefaultCategoriesAsync(company.Id, cancellationToken);

        var branchResult = await CreateBranchAsync(request, company.Id, cancellationToken);
        if (branchResult.IsFailure)
            return Result.Failure<(Company, Branch)>(branchResult.Error);
        var branch = branchResult.Value;

        await CreateDefaultDiningTablesAsync(branch.Id, cancellationToken);
        await CreateDefaultComandasAsync(branch.Id, cancellationToken);

        return Result.Success((company, branch));
    }

    private async Task<Result<AppUser>> SetupAdminAccountAsync(
        RegisterCompanyCommand request, long companyId, long branchId, CancellationToken cancellationToken)
    {
        var jobTitleResult = await CreateAdminJobTitleAsync(companyId, cancellationToken);
        if (jobTitleResult.IsFailure)
            return Result.Failure<AppUser>(jobTitleResult.Error);
        var jobTitle = jobTitleResult.Value;

        var employeeResult = await CreateAdminEmployeeAsync(request, branchId, jobTitle.Id, cancellationToken);
        if (employeeResult.IsFailure)
            return Result.Failure<AppUser>(employeeResult.Error);
        var employee = employeeResult.Value;

        var roleResult = await CreateAdminRoleAsync(companyId, cancellationToken);
        if (roleResult.IsFailure)
            return Result.Failure<AppUser>(roleResult.Error);
        var role = roleResult.Value;

        var userResult = await CreateAdminUserAsync(request, companyId, employee.Id, cancellationToken);
        if (userResult.IsFailure)
            return Result.Failure<AppUser>(userResult.Error);
        var user = userResult.Value;

        var linkResult = await LinkUserToRoleAsync(companyId, user.Id, role.Id, cancellationToken);
        if (linkResult.IsFailure)
            return Result.Failure<AppUser>(linkResult.Error);

        return Result.Success(user);
    }

    private async Task<Result> ValidateUniquenessAsync(RegisterCompanyCommand request, CancellationToken cancellationToken)
    {
        if (await _companyRepository.ExistsByCnpjAsync(request.Cnpj, cancellationToken))
            return Result.Failure(
                new Error("Company.AlreadyExists", "A company with this CNPJ is already registered."));

        if (await _userRepository.ExistsAsync(request.AdminUserName, request.AdminEmail, cancellationToken))
            return Result.Failure(
                new Error("AppUser.AlreadyExists", "User name or e-mail already in use."));

        if (await _employeeRepository.ExistsByCpfAsync(request.AdminCpf, cancellationToken))
            return Result.Failure(
                new Error("Employee.AlreadyExists", "A employee with this CPF is already registered."));

        return Result.Success();
    }

    private async Task<Result<Company>> CreateCompanyAsync(RegisterCompanyCommand request, CancellationToken cancellationToken)
    {
        var companyResult = Company.Create(
            request.LegalName, request.TradeName, request.Cnpj, request.CompanyEmail, request.CompanyPhone);
        if (companyResult.IsFailure)
            return companyResult;

        await _companyRepository.AddAsync(companyResult.Value, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return companyResult;
    }

    private async Task CreateDefaultCategoriesAsync(long companyId, CancellationToken cancellationToken)
    {
        var displayOrder = 0;
        foreach (var categoryName in DefaultCategoryNames)
        {
            var categoryResult = Category.Create(companyId, categoryName, displayOrder++);
            if (categoryResult.IsSuccess)
                await _categoryRepository.AddAsync(categoryResult.Value, cancellationToken);
        }
    }

    private async Task<Result<Branch>> CreateBranchAsync(RegisterCompanyCommand request, long companyId, CancellationToken cancellationToken)
    {
        var branchResult = Branch.Create(
            companyId, request.BranchName, request.BranchCnpj, request.CompanyPhone,
            request.AddressStreet, request.AddressNumber, request.AddressDistrict,
            request.AddressCity, request.AddressState, request.AddressZipCode);
        if (branchResult.IsFailure)
            return branchResult;

        await _branchRepository.AddAsync(branchResult.Value, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken); // precisa do Branch.Id para mesas/comandas/funcionário

        return branchResult;
    }

    private async Task CreateDefaultDiningTablesAsync(long branchId, CancellationToken cancellationToken)
    {
        for (var number = 1; number <= 5; number++)
        {
            var tableResult = DiningTable.Create(branchId, tableStatusId: 1, number: number, capacity: 4);
            if (tableResult.IsSuccess)
                await _diningTableRepository.AddAsync(tableResult.Value, cancellationToken);
        }
    }

    private async Task CreateDefaultComandasAsync(long branchId, CancellationToken cancellationToken)
    {
        for (var number = 1; number <= 5; number++)
        {
            var comandaResult = Comanda.Create(branchId, comandaStatusId: 1, code: number.ToString("D3"));
            if (comandaResult.IsSuccess)
                await _comandaRepository.AddAsync(comandaResult.Value, cancellationToken);
        }
    }

    private async Task<Result<JobTitle>> CreateAdminJobTitleAsync(long companyId, CancellationToken cancellationToken)
    {
        var jobTitleResult = JobTitle.Create(companyId, "Administrador");
        if (jobTitleResult.IsFailure)
            return jobTitleResult;

        await _jobTitleRepository.AddAsync(jobTitleResult.Value, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return jobTitleResult;
    }

    private async Task<Result<Employee>> CreateAdminEmployeeAsync(
        RegisterCompanyCommand request, long branchId, long jobTitleId, CancellationToken cancellationToken)
    {
        var employeeResult = Employee.Create(
            branchId, jobTitleId, request.AdminName, request.AdminCpf,
            request.AdminEmail, request.CompanyPhone, DateTime.Now, null, null);
        if (employeeResult.IsFailure)
            return employeeResult;

        await _employeeRepository.AddAsync(employeeResult.Value, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return employeeResult;
    }

    private async Task<Result<Role>> CreateAdminRoleAsync(long companyId, CancellationToken cancellationToken)
    {
        var roleResult = Role.Create(companyId, "Administrador", "Acesso total — criado no onboarding.");
        if (roleResult.IsFailure)
            return roleResult;

        await _roleRepository.AddAsync(roleResult.Value, cancellationToken);

        return roleResult;
    }

    private async Task<Result<AppUser>> CreateAdminUserAsync(
        RegisterCompanyCommand request, long companyId, long employeeId, CancellationToken cancellationToken)
    {
        var passwordHash = _passwordHasher.Hash(request.AdminPassword);
        var userResult = AppUser.Create(companyId, employeeId, request.AdminUserName, request.AdminEmail, passwordHash);
        if (userResult.IsFailure)
            return userResult;

        await _userRepository.AddAsync(userResult.Value, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken); // precisa dos Ids de Role/AppUser para o vínculo

        return userResult;
    }

    private async Task<Result> LinkUserToRoleAsync(long companyId, long userId, long roleId, CancellationToken cancellationToken)
    {
        var linkResult = UserRole.Create(companyId, userId, roleId);
        if (linkResult.IsFailure)
            return Result.Failure(linkResult.Error);

        await _userRoleRepository.AddAsync(linkResult.Value, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}