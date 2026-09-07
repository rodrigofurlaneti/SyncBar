using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class OrderOriginTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            var now = DateTime.Now;

            var result = OrderOrigin.Create(1, 2, "Local", now);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.CompanyId.Should().Be(1);
            result.Value.BranchId.Should().Be(2);
            result.Value.Name.Should().Be("LOCAL");
            result.Value.CreatedAt.Should().Be(now);
            result.Value.UpdatedAt.Should().BeNull();
            result.Value.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Create_WithNullCompanyAndBranch_ShouldReturnSuccessResultWithNullScope()
        {
            var now = DateTime.Now;

            var result = OrderOrigin.Create(null, null, "WebSite", now);

            result.IsSuccess.Should().BeTrue();
            result.Value.CompanyId.Should().BeNull();
            result.Value.BranchId.Should().BeNull();
        }

        [Fact]
        public void Create_ShouldTrimAndUpperCaseTheName()
        {
            var result = OrderOrigin.Create(null, null, "  ifood  ", DateTime.Now);

            result.IsSuccess.Should().BeTrue();
            result.Value.Name.Should().Be("IFOOD");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            var result = OrderOrigin.Create(1, 1, invalidName!, DateTime.Now);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OrderOrigin.EmptyName");
            result.Error.Message.Should().Be("Order origin name cannot be empty.");
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            var instance = Activator.CreateInstance(typeof(OrderOrigin), true) as OrderOrigin;

            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
            instance.Name.Should().Be(string.Empty);
        }
    }
}
