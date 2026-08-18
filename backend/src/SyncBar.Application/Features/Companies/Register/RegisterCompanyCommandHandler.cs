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
                if (await _companyRepository.ExistsByCnpjAsync(request.Cnpj, cancellationToken))
                    return Result.Failure<RegisterCompanyResponse>(
                        new Error("Company.AlreadyExists", "A company with this CNPJ is already registered."));

                if (await _userRepository.ExistsAsync(request.AdminUserName, request.AdminEmail, cancellationToken))
                    return Result.Failure<RegisterCompanyResponse>(
                        new Error("AppUser.AlreadyExists", "User name or e-mail already in use."));

                if (await _employeeRepository.ExistsByCpfAsync(request.AdminCpf, cancellationToken))
                    return Result.Failure<RegisterCompanyResponse>(
                        new Error("Employee.AlreadyExists", "A employee with this CPF is already registered."));

                var companyResult = Company.Create(
                    request.LegalName, request.TradeName, request.Cnpj, request.CompanyEmail, request.CompanyPhone);
                if (companyResult.IsFailure)
                    return Result.Failure<RegisterCompanyResponse>(companyResult.Error);

                var company = companyResult.Value;
                await _companyRepository.AddAsync(company, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken); 

                var displayOrder = 0;
                foreach (var categoryName in DefaultCategoryNames)
                {
                    var categoryResult = Category.Create(company.Id, categoryName, displayOrder++);
                    if (categoryResult.IsSuccess)
                        await _categoryRepository.AddAsync(categoryResult.Value, cancellationToken);
                }

                var branchResult = Branch.Create(
                    company.Id, request.BranchName, request.BranchCnpj, request.CompanyPhone,
                    request.AddressStreet, request.AddressNumber, request.AddressDistrict,
                    request.AddressCity, request.AddressState, request.AddressZipCode);
                if (branchResult.IsFailure)
                    return Result.Failure<RegisterCompanyResponse>(branchResult.Error);

                var branch = branchResult.Value;
                await _branchRepository.AddAsync(branch, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken); // precisa do Branch.Id para mesas/comandas/funcionário

                for (var number = 1; number <= 5; number++)
                {
                    var tableResult = DiningTable.Create(branch.Id, tableStatusId: 1, number: number, capacity: 4);
                    if (tableResult.IsSuccess)
                        await _diningTableRepository.AddAsync(tableResult.Value, cancellationToken);
                }

                for (var number = 1; number <= 5; number++)
                {
                    var comandaResult = Comanda.Create(branch.Id, comandaStatusId: 1, code: number.ToString("D3"));
                    if (comandaResult.IsSuccess)
                        await _comandaRepository.AddAsync(comandaResult.Value, cancellationToken);
                }

                var jobTitleResult = JobTitle.Create(company.Id, "Administrador");
                if (jobTitleResult.IsFailure)
                    return Result.Failure<RegisterCompanyResponse>(jobTitleResult.Error);

                var jobTitle = jobTitleResult.Value;
                await _jobTitleRepository.AddAsync(jobTitle, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken); 

                var employeeResult = Employee.Create(
                    branch.Id, jobTitle.Id, request.AdminName, request.AdminCpf,
                    request.AdminEmail, request.CompanyPhone, DateTime.Now, null, null);
                if (employeeResult.IsFailure)
                    return Result.Failure<RegisterCompanyResponse>(employeeResult.Error);

                var employee = employeeResult.Value;
                await _employeeRepository.AddAsync(employee, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken); 

                var roleResult = Role.Create(company.Id, "Administrador", "Acesso total — criado no onboarding.");
                if (roleResult.IsFailure)
                    return Result.Failure<RegisterCompanyResponse>(roleResult.Error);

                var role = roleResult.Value;
                await _roleRepository.AddAsync(role, cancellationToken);

                var passwordHash = _passwordHasher.Hash(request.AdminPassword);
                var userResult = AppUser.Create(
                    company.Id, employee.Id, request.AdminUserName, request.AdminEmail, passwordHash);
                if (userResult.IsFailure)
                    return Result.Failure<RegisterCompanyResponse>(userResult.Error);

                var user = userResult.Value;
                await _userRepository.AddAsync(user, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken); // precisa dos Ids de Role/AppUser para o vínculo

                var linkResult = UserRole.Create(company.Id, user.Id, role.Id);
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