using FluentAssertions;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class ShiftClosingTests
    {
        [Fact]
        public void Open_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long branchId = 1;
            long openedByEmployeeId = 7;

            // Act
            var result = ShiftClosing.Open(branchId, openedByEmployeeId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var shift = result.Value;
            shift.Should().NotBeNull();
            shift.BranchId.Should().Be(branchId);
            shift.OpenedByEmployeeId.Should().Be(openedByEmployeeId);
            shift.ShiftClosingStatusId.Should().Be(ShiftClosingStatusIds.Aberto);
            shift.ClosedByEmployeeId.Should().BeNull();
            shift.PeriodEnd.Should().BeNull();
            shift.CashSessionsCount.Should().Be(0);
            shift.TotalOpeningAmount.Should().Be(0);
            shift.TotalExpectedAmount.Should().Be(0);
            shift.TotalRealizedAmount.Should().Be(0);
            shift.TotalDifferenceAmount.Should().Be(0);
            shift.PeriodStart.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            shift.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            shift.UpdatedAt.Should().BeNull();
            shift.IsActive.Should().BeTrue();
            shift.IsOpen().Should().BeTrue();
        }

        [Fact]
        public void Open_WithInvalidBranchId_ShouldReturnFailureResult()
        {
            // Act
            var result = ShiftClosing.Open(0, 7);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ShiftClosing.InvalidBranch");
        }

        [Fact]
        public void Open_WithInvalidEmployeeId_ShouldReturnFailureResult()
        {
            // Act
            var result = ShiftClosing.Open(1, 0);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ShiftClosing.InvalidEmployee");
        }

        [Fact]
        public void Close_WithOpenCashSessionPending_ShouldReturnFailureAndNotConsolidate()
        {
            // Arrange
            var shift = ShiftClosing.Open(1, 7).Value;
            var openSession = CashSession.Open(cashRegisterId: 1, openedByEmployeeId: 10, openingAmount: 100m).Value;
            var closedSession = CashSession.Open(cashRegisterId: 2, openedByEmployeeId: 10, openingAmount: 100m).Value;
            closedSession.Close(closedByEmployeeId: 10, closingAmount: 150m, expectedAmount: 150m);

            // Act — impede o fechamento do turno enquanto houver caixa aberto pendente.
            var result = shift.Close(9, DateTime.Now, new[] { openSession, closedSession }, notes: null);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("ShiftClosing.OpenCashSessionsPending");
            shift.IsOpen().Should().BeTrue();
            shift.CashSessionsCount.Should().Be(0);
        }

        [Fact]
        public void Close_WithAllCashSessionsClosed_ShouldConsolidateTotalsAndReturnSuccess()
        {
            // Arrange
            var shift = ShiftClosing.Open(1, 7).Value;

            var session1 = CashSession.Open(cashRegisterId: 1, openedByEmployeeId: 10, openingAmount: 100m).Value;
            session1.Close(closedByEmployeeId: 10, closingAmount: 520m, expectedAmount: 500m); // +20

            var session2 = CashSession.Open(cashRegisterId: 2, openedByEmployeeId: 11, openingAmount: 50m).Value;
            session2.Close(closedByEmployeeId: 11, closingAmount: 280m, expectedAmount: 300m); // -20

            var periodEnd = DateTime.Now;

            // Act
            var result = shift.Close(9, periodEnd, new[] { session1, session2 }, notes: "Fechamento sem divergências relevantes");

            // Assert
            result.IsSuccess.Should().BeTrue();
            shift.IsOpen().Should().BeFalse();
            shift.ShiftClosingStatusId.Should().Be(ShiftClosingStatusIds.Fechado);
            shift.ClosedByEmployeeId.Should().Be(9);
            shift.PeriodEnd.Should().Be(periodEnd);
            shift.CashSessionsCount.Should().Be(2);
            shift.TotalOpeningAmount.Should().Be(150m); // 100 + 50
            shift.TotalExpectedAmount.Should().Be(800m); // 500 + 300
            shift.TotalRealizedAmount.Should().Be(800m); // 520 + 280
            shift.TotalDifferenceAmount.Should().Be(0m); // +20 e -20 se cancelam na diferença geral
            shift.Notes.Should().Be("Fechamento sem divergências relevantes");
            shift.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void Close_WithNoCashSessionsInPeriod_ShouldConsolidateWithZeroTotals()
        {
            // Arrange — filial sem nenhum caixa operado no período: não há o que consolidar,
            // mas isso não deve bloquear o fechamento do turno.
            var shift = ShiftClosing.Open(1, 7).Value;

            // Act
            var result = shift.Close(9, DateTime.Now, Array.Empty<CashSession>(), notes: null);

            // Assert
            result.IsSuccess.Should().BeTrue();
            shift.CashSessionsCount.Should().Be(0);
            shift.TotalOpeningAmount.Should().Be(0);
            shift.TotalExpectedAmount.Should().Be(0);
            shift.TotalRealizedAmount.Should().Be(0);
            shift.TotalDifferenceAmount.Should().Be(0);
        }

        [Fact]
        public void Close_WhenAlreadyClosed_ShouldReturnFailureResult()
        {
            // Arrange
            var shift = ShiftClosing.Open(1, 7).Value;
            shift.Close(9, DateTime.Now, Array.Empty<CashSession>(), notes: null);

            // Act
            var result = shift.Close(9, DateTime.Now, Array.Empty<CashSession>(), notes: null);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("ShiftClosing.NotOpen");
        }

        [Fact]
        public void Close_WithPeriodEndBeforePeriodStart_ShouldReturnFailureResult()
        {
            // Arrange
            var shift = ShiftClosing.Open(1, 7).Value;

            // Act
            var result = shift.Close(9, shift.PeriodStart.AddMinutes(-5), Array.Empty<CashSession>(), notes: null);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("ShiftClosing.InvalidPeriod");
        }

        [Fact]
        public void Close_ShouldIgnoreInactiveCashSessionsWhenValidatingAndConsolidating()
        {
            // Arrange — um CashSession desativado (soft delete) não deve bloquear o fechamento
            // do turno mesmo que ainda esteja "Aberto" logicamente, nem entrar nos totais.
            var shift = ShiftClosing.Open(1, 7).Value;
            var inactiveOpenSession = CashSession.Open(cashRegisterId: 1, openedByEmployeeId: 10, openingAmount: 100m).Value;
            inactiveOpenSession.Deactivate();

            // Act
            var result = shift.Close(9, DateTime.Now, new[] { inactiveOpenSession }, notes: null);

            // Assert
            result.IsSuccess.Should().BeTrue();
            shift.CashSessionsCount.Should().Be(0);
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var shift = ShiftClosing.Open(1, 7).Value;

            // Act
            shift.Deactivate();

            // Assert
            shift.IsActive.Should().BeFalse();
            shift.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(ShiftClosing), true) as ShiftClosing;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
