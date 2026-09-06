using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Catalog.Pizza;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Catalog.Pizza;

public sealed class SyncIfoodPizzaCommandHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly IIfoodCatalogClient _catalogClient = Substitute.For<IIfoodCatalogClient>();
    private readonly IPizzaConfigurationRepository _pizzaConfigurationRepository = Substitute.For<IPizzaConfigurationRepository>();
    private readonly IPizzaFlavorRepository _pizzaFlavorRepository = Substitute.For<IPizzaFlavorRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IIfoodPizzaMappingRepository _ifoodPizzaMappingRepository = Substitute.For<IIfoodPizzaMappingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SyncIfoodPizzaCommandHandler _handler;

    public SyncIfoodPizzaCommandHandlerTests()
    {
        _handler = new SyncIfoodPizzaCommandHandler(
            _branchRepository, _tokenProvider, _settingRepository, _mappingRepository, _catalogClient,
            _pizzaConfigurationRepository, _pizzaFlavorRepository, _productRepository, _ifoodPizzaMappingRepository,
            _logRepository, _unitOfWork);
    }

    private static Branch CreateBranch()
        => Branch.Create(
            companyId: 1, "Loja Centro", cnpj: null, phone: null, addressStreet: null, addressNumber: null,
            addressDistrict: null, addressCity: null, addressState: null, addressZipCode: null).Value;

    private void SetupResolvedMerchant(Branch branch, string merchantId = "MERCH-1", string token = "token-1")
    {
        var setting = IfoodIntegrationSetting.Create(companyId: 1).Value;
        setting.SaveCredentials("client-1", "encrypted", enabled: true, ifoodCustomerId: null);
        var mapping = IfoodMerchantMapping.Create(branchId: 1).Value;
        mapping.SetMerchant(merchantId, "uuid-1");

        _branchRepository.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(branch);
        _settingRepository.GetByCompanyAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns(setting);
        _mappingRepository.GetByBranchAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(mapping);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns(token);
    }

    private static Product CreateProduct(long companyId = 1) =>
        Product.Create(companyId, categoryId: 1, unitOfMeasureId: 1, "Pizza Grande", null, null, 0m, null, false, null).Value;

    private static (PizzaConfiguration Configuration, PizzaFlavor Flavor) CreateValidConfiguration(long productId)
    {
        var configuration = PizzaConfiguration.Create(productId).Value;
        var size = configuration.AddSize("Grande", 8, 4, 0).Value;
        configuration.AddCrust("Catupiry", 5m, 0);
        configuration.AddEdge("Cheddar", 3m, 0);
        var flavor = PizzaFlavor.Create(1, "Calabresa", null).Value;
        configuration.SetFlavorPrice(flavor.Id, size.Id, 45m);
        return (configuration, flavor);
    }

    private void SetupValidContext(out Product product, out PizzaConfiguration configuration, out PizzaFlavor flavor)
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        product = CreateProduct();
        (configuration, flavor) = CreateValidConfiguration(product.Id);

        _pizzaConfigurationRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(configuration);
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _pizzaFlavorRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>()).Returns([flavor]);
    }

    [Fact]
    public async Task Handle_BranchNotFound_ShouldPropagateResolutionFailure()
    {
        var command = new SyncIfoodPizzaCommand(1, 1);
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.BranchNotFound");
    }

    [Fact]
    public async Task Handle_PizzaConfigurationNotFound_ShouldReturnNotFound()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var command = new SyncIfoodPizzaCommand(1, 1);
        _pizzaConfigurationRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((PizzaConfiguration?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PizzaConfiguration.NotFound");
    }

    [Fact]
    public async Task Handle_PizzaConfigurationInactive_ShouldReturnNotFound()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var command = new SyncIfoodPizzaCommand(1, 1);
        var product = CreateProduct();
        var (configuration, _) = CreateValidConfiguration(product.Id);
        configuration.Deactivate();
        _pizzaConfigurationRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(configuration);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PizzaConfiguration.NotFound");
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnProductNotFound()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var command = new SyncIfoodPizzaCommand(1, 1);
        var product = CreateProduct();
        var (configuration, _) = CreateValidConfiguration(product.Id);
        _pizzaConfigurationRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(configuration);
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }

    [Fact]
    public async Task Handle_ProductFromDifferentCompany_ShouldReturnProductNotFound()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var command = new SyncIfoodPizzaCommand(1, 1);
        var product = CreateProduct(companyId: 2);
        var (configuration, _) = CreateValidConfiguration(product.Id);
        _pizzaConfigurationRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(configuration);
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }

    [Fact]
    public async Task Handle_NoActiveSizes_ShouldReturnNoSizes()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var command = new SyncIfoodPizzaCommand(1, 1);
        var product = CreateProduct();
        var configuration = PizzaConfiguration.Create(product.Id).Value;
        _pizzaConfigurationRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(configuration);
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PizzaConfiguration.NoSizes");
    }

    [Fact]
    public async Task Handle_NoActiveFlavorPrices_ShouldReturnNoFlavors()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var command = new SyncIfoodPizzaCommand(1, 1);
        var product = CreateProduct();
        var configuration = PizzaConfiguration.Create(product.Id).Value;
        configuration.AddSize("Grande", 8, 4, 0);
        _pizzaConfigurationRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(configuration);
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PizzaConfiguration.NoFlavors");
    }

    [Fact]
    public async Task Handle_ApiCallUnsuccessful_ShouldReturnPizzaSyncFailed()
    {
        SetupValidContext(out _, out _, out _);
        var command = new SyncIfoodPizzaCommand(1, 1);
        _ifoodPizzaMappingRepository.GetByPizzaConfigurationAndBranchForUpdateAsync(1, 1, Arg.Any<CancellationToken>()).Returns((IfoodPizzaMapping?)null);
        _catalogClient.InvokeCatalogV1Async(
            "token-1", "MERCH-1", IfoodCatalogV1Operation.CreatePizza, null, null, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IfoodRawApiResult(false, 500, null, "erro remoto"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodCatalog.PizzaSyncFailed");
    }

    [Fact]
    public async Task Handle_ApiCallSuccessfulButNullBody_ShouldReturnPizzaSyncFailed()
    {
        SetupValidContext(out _, out _, out _);
        var command = new SyncIfoodPizzaCommand(1, 1);
        _ifoodPizzaMappingRepository.GetByPizzaConfigurationAndBranchForUpdateAsync(1, 1, Arg.Any<CancellationToken>()).Returns((IfoodPizzaMapping?)null);
        _catalogClient.InvokeCatalogV1Async(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IfoodCatalogV1Operation>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new IfoodRawApiResult(true, 200, null, null));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodCatalog.PizzaSyncFailed");
    }

    [Fact]
    public async Task Handle_ResponseWithoutIdProperty_ShouldReturnPizzaSyncNoId()
    {
        SetupValidContext(out _, out _, out _);
        var command = new SyncIfoodPizzaCommand(1, 1);
        _ifoodPizzaMappingRepository.GetByPizzaConfigurationAndBranchForUpdateAsync(1, 1, Arg.Any<CancellationToken>()).Returns((IfoodPizzaMapping?)null);
        _catalogClient.InvokeCatalogV1Async(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IfoodCatalogV1Operation>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new IfoodRawApiResult(true, 200, "{\"sizes\":[]}", null));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodCatalog.PizzaSyncNoId");
    }

    [Fact]
    public async Task Handle_ResponseWithEmptyId_ShouldReturnPizzaSyncNoId()
    {
        SetupValidContext(out _, out _, out _);
        var command = new SyncIfoodPizzaCommand(1, 1);
        _ifoodPizzaMappingRepository.GetByPizzaConfigurationAndBranchForUpdateAsync(1, 1, Arg.Any<CancellationToken>()).Returns((IfoodPizzaMapping?)null);
        _catalogClient.InvokeCatalogV1Async(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IfoodCatalogV1Operation>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new IfoodRawApiResult(true, 200, "{\"id\":\"\"}", null));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodCatalog.PizzaSyncNoId");
    }

    [Fact]
    public async Task Handle_NoExistingMapping_ShouldCreatePizzaAndPersistNewMappingWithElements()
    {
        SetupValidContext(out _, out var configuration, out var flavor);
        var size = configuration.Sizes.First();
        var command = new SyncIfoodPizzaCommand(1, 1);
        _ifoodPizzaMappingRepository.GetByPizzaConfigurationAndBranchForUpdateAsync(1, 1, Arg.Any<CancellationToken>()).Returns((IfoodPizzaMapping?)null);

        var responseBody = $$"""
            {
                "id": "pizza-abc",
                "sizes": [{"externalCode": "pizzasize-{{size.Id}}", "id": "size-elem-1"}],
                "toppings": [{"externalCode": "pizzaflavor-{{flavor.Id}}", "id": "topping-elem-1"}]
            }
            """;
        _catalogClient.InvokeCatalogV1Async(
            "token-1", "MERCH-1", IfoodCatalogV1Operation.CreatePizza, null, null, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IfoodRawApiResult(true, 201, responseBody, null));

        IfoodPizzaMapping? captured = null;
        _ifoodPizzaMappingRepository.When(r => r.AddAsync(Arg.Any<IfoodPizzaMapping>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => captured = callInfo.ArgAt<IfoodPizzaMapping>(0));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IfoodPizzaId.Should().Be("pizza-abc");
        captured.Should().NotBeNull();
        captured!.PizzaConfigurationId.Should().Be(1);
        captured.BranchId.Should().Be(1);
        captured.IfoodPizzaId.Should().Be("pizza-abc");
        captured.FindIfoodElementId(IfoodPizzaElementKind.Size, size.Id).Should().Be("size-elem-1");
        captured.FindIfoodElementId(IfoodPizzaElementKind.Topping, flavor.Id).Should().Be("topping-elem-1");
        await _unitOfWork.Received().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingMapping_ShouldUpdatePizzaAndSendUpdateOperationWithRouteParam()
    {
        SetupValidContext(out _, out var configuration, out _);
        var command = new SyncIfoodPizzaCommand(1, 1);
        var existingMapping = IfoodPizzaMapping.Create(1, 1, "old-pizza-id").Value;
        _ifoodPizzaMappingRepository.GetByPizzaConfigurationAndBranchForUpdateAsync(1, 1, Arg.Any<CancellationToken>()).Returns(existingMapping);

        _catalogClient.InvokeCatalogV1Async(
            "token-1", "MERCH-1", IfoodCatalogV1Operation.UpdatePizza,
            Arg.Is<IReadOnlyDictionary<string, string>?>(d => d != null && d["pizzaId"] == "old-pizza-id"),
            null, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IfoodRawApiResult(true, 200, "{\"id\":\"new-pizza-id\"}", null));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IfoodPizzaId.Should().Be("new-pizza-id");
        existingMapping.IfoodPizzaId.Should().Be("new-pizza-id");
        await _ifoodPizzaMappingRepository.DidNotReceive().AddAsync(Arg.Any<IfoodPizzaMapping>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ResponseElementWithUnrecognizedPrefix_ShouldBeSkippedWithoutError()
    {
        SetupValidContext(out _, out var configuration, out _);
        var command = new SyncIfoodPizzaCommand(1, 1);
        _ifoodPizzaMappingRepository.GetByPizzaConfigurationAndBranchForUpdateAsync(1, 1, Arg.Any<CancellationToken>()).Returns((IfoodPizzaMapping?)null);

        var responseBody = """
            {
                "id": "pizza-xyz",
                "sizes": [{"externalCode": "unknownprefix-1", "id": "size-elem-1"}]
            }
            """;
        _catalogClient.InvokeCatalogV1Async(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IfoodCatalogV1Operation>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new IfoodRawApiResult(true, 201, responseBody, null));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IfoodPizzaId.Should().Be("pizza-xyz");
    }
}
