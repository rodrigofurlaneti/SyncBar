using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class PurchaseTests
    {
        // Purchase.Create não tem nenhuma validação no código real (sempre retorna sucesso) —
        // por isso só há o caminho feliz, sem testes de falha (não há branch de falha a cobrir).
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long branchId = 1;
            long supplierId = 5;
            string documentNumber = "NF-1001";
            var purchasedAt = new DateTime(2026, 8, 1);
            string notes = "Compra mensal de insumos";

            // Act
            var result = Purchase.Create(branchId, supplierId, documentNumber, purchasedAt, notes);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var purchase = result.Value;
            purchase.Should().NotBeNull();
            purchase.BranchId.Should().Be(branchId);
            purchase.SupplierId.Should().Be(supplierId);
            purchase.DocumentNumber.Should().Be(documentNumber);
            purchase.PurchasedAt.Should().Be(purchasedAt);
            purchase.Notes.Should().Be(notes);
            purchase.TotalAmount.Should().Be(0m);
            purchase.Items.Should().BeEmpty();
            purchase.IsActive.Should().BeTrue();
            purchase.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            purchase.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void AddItem_WithValidArguments_ShouldAddItemAndRecalculateTotal()
        {
            // Arrange
            var purchase = Purchase.Create(1, 5, "NF-1001", DateTime.Now, null).Value;

            // Act
            var result = purchase.AddItem(10, 4m, 2.5m);

            // Assert
            // Only PurchaseItem.TotalCost/IsActive are asserted directly: PurchaseItem.cs
            // itself was not part of this assignment (not staged locally), so its other
            // property names are not guessed here — see Purchase.cs's own RecalculateTotal
            // (`_items.Where(i => i.IsActive).Sum(i => i.TotalCost)`), which is the only
            // confirmed surface of PurchaseItem visible from the source actually read.
            result.IsSuccess.Should().BeTrue();
            purchase.Items.Should().HaveCount(1);
            var item = purchase.Items.First();
            item.IsActive.Should().BeTrue();
            item.TotalCost.Should().Be(10.0m); // 4 * 2.5

            purchase.TotalAmount.Should().Be(10.0m);
            purchase.UpdatedAt.Should().NotBeNull();
            purchase.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void AddItem_WithMultipleItems_ShouldAccumulateTotal()
        {
            // Arrange
            var purchase = Purchase.Create(1, 5, "NF-1001", DateTime.Now, null).Value;

            // Act
            purchase.AddItem(10, 4m, 2.5m);   // 10.00
            var result = purchase.AddItem(11, 2m, 5.0m); // 10.00

            // Assert
            result.IsSuccess.Should().BeTrue();
            purchase.Items.Should().HaveCount(2);
            purchase.TotalAmount.Should().Be(20.0m);
        }

        [Fact]
        public void AddItem_WithNonPositiveQuantity_ShouldReturnFailureResult()
        {
            // Arrange
            var purchase = Purchase.Create(1, 5, "NF-1001", DateTime.Now, null).Value;

            // Act
            var result = purchase.AddItem(10, 0m, 2.5m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Purchase.InvalidQuantity");
            result.Error.Message.Should().Be("Quantity must be greater than zero.");
            purchase.Items.Should().BeEmpty();
        }

        [Fact]
        public void AddItem_WithNegativeUnitCost_ShouldReturnFailureResult()
        {
            // Arrange
            var purchase = Purchase.Create(1, 5, "NF-1001", DateTime.Now, null).Value;

            // Act
            var result = purchase.AddItem(10, 1m, -1.0m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Purchase.InvalidUnitCost");
            result.Error.Message.Should().Be("Unit cost cannot be negative.");
            purchase.Items.Should().BeEmpty();
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var purchase = Purchase.Create(1, 5, "NF-1001", DateTime.Now, null).Value;

            // Act
            purchase.Touch();

            // Assert
            purchase.UpdatedAt.Should().NotBeNull();
            purchase.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var purchase = Purchase.Create(1, 5, "NF-1001", DateTime.Now, null).Value;

            // Act
            purchase.Deactivate();

            // Assert
            purchase.IsActive.Should().BeFalse();
            purchase.UpdatedAt.Should().NotBeNull();
            purchase.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(Purchase), true) as Purchase;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
