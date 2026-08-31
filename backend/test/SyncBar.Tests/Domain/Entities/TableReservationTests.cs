using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class TableReservationTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long branchId = 1;
            long? diningTableId = null;
            string customerName = "John Doe";
            string customerPhone = "11999999999";
            int partySize = 4;
            DateTime reservedFor = DateTime.Now.AddDays(1);
            string notes = "Window seat please";

            // Act
            var result = TableReservation.Create(branchId, diningTableId, customerName, customerPhone, partySize, reservedFor, notes);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.BranchId.Should().Be(branchId);
            result.Value.DiningTableId.Should().Be(diningTableId);
            result.Value.CustomerName.Should().Be(customerName);
            result.Value.CustomerPhone.Should().Be(customerPhone);
            result.Value.PartySize.Should().Be(partySize);
            result.Value.ReservedFor.Should().Be(reservedFor);
            result.Value.Notes.Should().Be(notes);
            result.Value.ReservationStatusId.Should().Be(ReservationStatusIds.Pending);
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceCustomerName_ShouldReturnFailureResult(string invalidName)
        {
            // Act
            var result = TableReservation.Create(1, null, invalidName, "11999999999", 4, DateTime.Now.AddDays(1), null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("TableReservation.EmptyCustomerName");
            result.Error.Message.Should().Be("Customer name is required.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_WithInvalidPartySize_ShouldReturnFailureResult(int invalidPartySize)
        {
            // Act
            var result = TableReservation.Create(1, null, "John Doe", "11999999999", invalidPartySize, DateTime.Now.AddDays(1), null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("TableReservation.InvalidPartySize");
            result.Error.Message.Should().Be("Party size must be greater than zero.");
        }

        [Fact]
        public void Create_WithPastReservationDate_ShouldReturnFailureResult()
        {
            // Arrange
            DateTime pastDate = DateTime.Now.AddMinutes(-5);

            // Act
            var result = TableReservation.Create(1, null, "John Doe", "11999999999", 4, pastDate, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("TableReservation.PastDate");
            result.Error.Message.Should().Be("Reservation date must be in the future.");
        }

        [Fact]
        public void Confirm_WhenStatusIsPending_ShouldSucceedAndSetStatusToConfirmed()
        {
            // Arrange
            var reservation = TableReservation.Create(1, null, "John Doe", null, 2, DateTime.Now.AddHours(2), null).Value;
            long targetTableId = 10;

            // Act
            var result = reservation.Confirm(targetTableId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            reservation.ReservationStatusId.Should().Be(ReservationStatusIds.Confirmed);
            reservation.DiningTableId.Should().Be(targetTableId);
            reservation.UpdatedAt.Should().NotBeNull();
            reservation.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Confirm_WhenStatusIsNotPending_ShouldReturnFailureResult()
        {
            // Arrange
            var reservation = TableReservation.Create(1, null, "John Doe", null, 2, DateTime.Now.AddHours(2), null).Value;
            reservation.Confirm(10); // Now it's Confirmed

            // Act (trying to confirm again)
            var result = reservation.Confirm(15);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("TableReservation.NotPending");
            result.Error.Message.Should().Be("Only a pending reservation can be confirmed.");
        }

        [Fact]
        public void MarkSeated_WhenStatusIsConfirmed_ShouldSucceedAndSetStatusToSeated()
        {
            // Arrange
            var reservation = TableReservation.Create(1, null, "John Doe", null, 2, DateTime.Now.AddHours(2), null).Value;
            reservation.Confirm(10);

            // Act
            var result = reservation.MarkSeated();

            // Assert
            result.IsSuccess.Should().BeTrue();
            reservation.ReservationStatusId.Should().Be(ReservationStatusIds.Seated);
            reservation.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void MarkSeated_WhenStatusIsNotConfirmed_ShouldReturnFailureResult()
        {
            // Arrange
            var reservation = TableReservation.Create(1, null, "John Doe", null, 2, DateTime.Now.AddHours(2), null).Value;
            // Status is Pending, not Confirmed

            // Act
            var result = reservation.MarkSeated();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("TableReservation.NotConfirmed");
            result.Error.Message.Should().Be("Only a confirmed reservation can be seated.");
        }

        [Theory]
        [InlineData(true)] // Pending
        [InlineData(false)] // Confirmed
        public void Cancel_WhenStatusIsPendingOrConfirmed_ShouldSucceedAndSetStatusToCancelled(bool isPendingOnly)
        {
            // Arrange
            var reservation = TableReservation.Create(1, null, "John Doe", null, 2, DateTime.Now.AddHours(2), null).Value;
            if (!isPendingOnly)
            {
                reservation.Confirm(10);
            }

            // Act
            var result = reservation.Cancel();

            // Assert
            result.IsSuccess.Should().BeTrue();
            reservation.ReservationStatusId.Should().Be(ReservationStatusIds.Cancelled);
            reservation.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void Cancel_WhenAlreadyCancelled_ShouldReturnFailureResult()
        {
            // Arrange
            var reservation = TableReservation.Create(1, null, "John Doe", null, 2, DateTime.Now.AddHours(2), null).Value;
            reservation.Cancel(); // Status is now Cancelled

            // Act
            var result = reservation.Cancel();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("TableReservation.CannotCancel");
        }

        [Fact]
        public void MarkNoShow_WhenStatusIsConfirmed_ShouldSucceedAndSetStatusToNoShow()
        {
            // Arrange
            var reservation = TableReservation.Create(1, null, "John Doe", null, 2, DateTime.Now.AddHours(2), null).Value;
            reservation.Confirm(10);

            // Act
            var result = reservation.MarkNoShow();

            // Assert
            result.IsSuccess.Should().BeTrue();
            reservation.ReservationStatusId.Should().Be(ReservationStatusIds.NoShow);
            reservation.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void MarkNoShow_WhenStatusIsNotConfirmed_ShouldReturnFailureResult()
        {
            // Arrange
            var reservation = TableReservation.Create(1, null, "John Doe", null, 2, DateTime.Now.AddHours(2), null).Value;
            // Status is Pending

            // Act
            var result = reservation.MarkNoShow();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("TableReservation.NotConfirmed");
            result.Error.Message.Should().Be("Only a confirmed reservation can be marked as no-show.");
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var reservation = TableReservation.Create(1, null, "John Doe", null, 2, DateTime.Now.AddHours(2), null).Value;

            // Act
            reservation.Deactivate();

            // Assert
            reservation.IsActive.Should().BeFalse();
            reservation.UpdatedAt.Should().NotBeNull();
            reservation.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(TableReservation), true) as TableReservation;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
