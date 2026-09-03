using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Ifood;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood;

public sealed class GetIfoodMerchantMappingsQueryHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetIfoodMerchantMappingsQueryHandler _handler;

    public GetIfoodMerchantMappingsQueryHandlerTests()
    {
        _handler = new GetIfoodMerchantMappingsQueryHandler(_branchRepository, _mappingRepository, _logRepository, _unitOfWork);
    }

    private static Branch CreateBranch(string name = "Loja Centro")
        => Branch.Create(
            companyId: 1, name, cnpj: null, phone: null, addressStreet: null, addressNumber: null,
            addressDistrict: null, addressCity: null, addressState: null, addressZipCode: null).Value;

    // Nem Branch nem IfoodMerchantMapping expõem forma pública de fixar o Id (só existiria após o
    // SaveChanges do EF Core). O handler casa mapping->filial por Id (Dictionary<long, ...>), então
    // sem isso não dá pra montar mais de uma filial distinta no mesmo teste. Reflection imita o Id
    // que o EF teria atribuído após persistir.
    private static void SetId(Entity entity, long id) =>
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    [Fact]
    public async Task Handle_NoBranchesForCompany_ShouldReturnEmptyCollection()
    {
        var query = new GetIfoodMerchantMappingsQuery(CompanyId: 1);
        _branchRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Branch>());
        _mappingRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<long, IfoodMerchantMapping>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BranchWithoutMapping_ShouldReturnRowWithNullMerchantFields()
    {
        var branch = CreateBranch();
        SetId(branch, 1);
        var query = new GetIfoodMerchantMappingsQuery(CompanyId: 1);
        _branchRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>())
            .Returns([branch]);
        _mappingRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<long, IfoodMerchantMapping>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value.Single();
        response.BranchId.Should().Be(branch.Id);
        response.BranchName.Should().Be(branch.Name);
        response.MerchantId.Should().BeNull();
        response.MerchantUuid.Should().BeNull();
    }

    [Fact]
    public async Task Handle_BranchWithMapping_ShouldReturnMappedMerchantFields()
    {
        var branch = CreateBranch();
        SetId(branch, 1);
        var mapping = IfoodMerchantMapping.Create(branch.Id).Value;
        mapping.SetMerchant(merchantId: "MERCH-1", merchantUuid: "uuid-1");

        var query = new GetIfoodMerchantMappingsQuery(CompanyId: 1);
        _branchRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>())
            .Returns([branch]);
        _mappingRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<long, IfoodMerchantMapping> { [branch.Id] = mapping });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value.Single();
        response.MerchantId.Should().Be("MERCH-1");
        response.MerchantUuid.Should().Be("uuid-1");
    }

    [Fact]
    public async Task Handle_InactiveBranch_ShouldBeExcludedFromResult()
    {
        var activeBranch = CreateBranch("Loja Ativa");
        SetId(activeBranch, 1);
        var inactiveBranch = CreateBranch("Loja Inativa");
        SetId(inactiveBranch, 2);
        inactiveBranch.Deactivate();

        var query = new GetIfoodMerchantMappingsQuery(CompanyId: 1);
        _branchRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>())
            .Returns([activeBranch, inactiveBranch]);
        _mappingRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<long, IfoodMerchantMapping>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(r => r.BranchId == activeBranch.Id);
    }

    [Fact]
    public async Task Handle_MultipleBranches_ShouldReturnOneRowPerActiveBranch()
    {
        var branchOne = CreateBranch("Loja 1");
        SetId(branchOne, 1);
        var branchTwo = CreateBranch("Loja 2");
        SetId(branchTwo, 2);
        var mappingTwo = IfoodMerchantMapping.Create(branchTwo.Id).Value;
        mappingTwo.SetMerchant("MERCH-2", "uuid-2");

        var query = new GetIfoodMerchantMappingsQuery(CompanyId: 1);
        _branchRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>())
            .Returns([branchOne, branchTwo]);
        _mappingRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<long, IfoodMerchantMapping> { [branchTwo.Id] = mappingTwo });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Single(r => r.BranchId == branchOne.Id).MerchantId.Should().BeNull();
        result.Value.Single(r => r.BranchId == branchTwo.Id).MerchantId.Should().Be("MERCH-2");
    }
}
