using FluentAssertions;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class IfoodShippingDeliveryTests
    {
        private static readonly DateTime Now = DateTime.Now;

        private static SyncBar.Domain.Primitives.Result<IfoodShippingDelivery> CreateValid(
            long branchId = 1,
            string? orderReference = "Balcao #45",
            string customerName = "  John Doe  ",
            string customerPhoneAreaCode = " 11 ",
            string customerPhoneNumber = " 998887777 ",
            string postalCode = " 01310-100 ",
            string streetName = " Av. Paulista ",
            string streetNumber = " 1000 ",
            string? complement = " Apto 12 ",
            string neighborhood = " Bela Vista ",
            string city = " Sao Paulo ",
            string state = " SP ",
            string country = "",
            string? reference = " Perto do metro ",
            double? latitude = -23.5,
            double? longitude = -46.6,
            decimal merchantFee = 8.50m,
            string quoteId = "quote-123",
            string ifoodDeliveryId = "delivery-456",
            string? trackingUrl = "https://track.ifood.com/456")
        {
            return IfoodShippingDelivery.Create(
                branchId, orderReference, customerName, customerPhoneAreaCode, customerPhoneNumber,
                postalCode, streetName, streetNumber, complement, neighborhood,
                city, state, country, reference, latitude, longitude,
                merchantFee, quoteId, ifoodDeliveryId, trackingUrl, Now);
        }

        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithTrimmedProperties()
        {
            // Act
            var result = CreateValid();

            // Assert
            result.IsSuccess.Should().BeTrue();
            var delivery = result.Value;
            delivery.Should().NotBeNull();
            delivery.BranchId.Should().Be(1);
            delivery.OrderReference.Should().Be("Balcao #45");
            delivery.CustomerName.Should().Be("John Doe");
            delivery.CustomerPhoneAreaCode.Should().Be("11");
            delivery.CustomerPhoneNumber.Should().Be("998887777");
            delivery.PostalCode.Should().Be("01310-100");
            delivery.StreetName.Should().Be("Av. Paulista");
            delivery.StreetNumber.Should().Be("1000");
            delivery.Complement.Should().Be("Apto 12");
            delivery.Neighborhood.Should().Be("Bela Vista");
            delivery.City.Should().Be("Sao Paulo");
            delivery.State.Should().Be("SP");
            delivery.Country.Should().Be("Brasil"); // blank country defaults to "Brasil"
            delivery.Reference.Should().Be("Perto do metro");
            delivery.Latitude.Should().Be(-23.5);
            delivery.Longitude.Should().Be(-46.6);
            delivery.MerchantFee.Should().Be(8.50m);
            delivery.QuoteId.Should().Be("quote-123");
            delivery.IfoodDeliveryId.Should().Be("delivery-456");
            delivery.TrackingUrl.Should().Be("https://track.ifood.com/456");
            delivery.Status.Should().Be(IfoodShippingStatuses.DriverRequested);
            delivery.RequestedAt.Should().Be(Now);
            delivery.CreatedAt.Should().Be(Now);
            delivery.IsActive.Should().BeTrue();
            delivery.CancelledAt.Should().BeNull();
            delivery.CancellationReason.Should().BeNull();
            delivery.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_WithExplicitCountry_ShouldKeepAndTrimGivenCountry()
        {
            // Act
            var result = CreateValid(country: "  Portugal  ");

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Country.Should().Be("Portugal");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceCustomerName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = CreateValid(customerName: invalidName!);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodShippingDelivery.MissingCustomerName");
            result.Error.Message.Should().Be("Customer name is required.");
        }

        [Fact]
        public void Create_WithEmptyPhoneAreaCode_ShouldReturnFailureResult()
        {
            // Act
            var result = CreateValid(customerPhoneAreaCode: "");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodShippingDelivery.MissingCustomerPhone");
            result.Error.Message.Should().Be("Customer phone is required.");
        }

        [Fact]
        public void Create_WithEmptyPhoneNumber_ShouldReturnFailureResult()
        {
            // Act
            var result = CreateValid(customerPhoneNumber: "   ");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodShippingDelivery.MissingCustomerPhone");
            result.Error.Message.Should().Be("Customer phone is required.");
        }

        [Fact]
        public void Create_WithIncompletePostalCode_ShouldReturnFailureResult()
        {
            // Act
            var result = CreateValid(postalCode: "");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodShippingDelivery.IncompleteAddress");
            result.Error.Message.Should().Be("Delivery address is incomplete.");
        }

        [Fact]
        public void Create_WithIncompleteCity_ShouldReturnFailureResult()
        {
            // Act
            var result = CreateValid(city: "   ");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodShippingDelivery.IncompleteAddress");
            result.Error.Message.Should().Be("Delivery address is incomplete.");
        }

        [Fact]
        public void Create_WithNegativeMerchantFee_ShouldReturnFailureResult()
        {
            // Act
            var result = CreateValid(merchantFee: -1m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodShippingDelivery.InvalidMerchantFee");
            result.Error.Message.Should().Be("Merchant fee cannot be negative.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceQuoteId_ShouldReturnFailureResult(string? invalidQuoteId)
        {
            // Act
            var result = CreateValid(quoteId: invalidQuoteId!);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodShippingDelivery.MissingQuoteId");
            result.Error.Message.Should().Be("A valid quote is required before requesting a driver.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceIfoodDeliveryId_ShouldReturnFailureResult(string? invalidDeliveryId)
        {
            // Act
            var result = CreateValid(ifoodDeliveryId: invalidDeliveryId!);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodShippingDelivery.MissingDeliveryId");
            result.Error.Message.Should().Be("Ifood did not return a delivery id.");
        }

        [Fact]
        public void MarkCancelled_WhenNotYetCancelled_ShouldUpdateStatusAndSetTimestamps()
        {
            // Arrange
            var delivery = CreateValid().Value;
            var cancelledAt = Now.AddMinutes(5);

            // Act
            var result = delivery.MarkCancelled("Cliente desistiu", cancelledAt);

            // Assert
            result.IsSuccess.Should().BeTrue();
            delivery.Status.Should().Be(IfoodShippingStatuses.Cancelled);
            delivery.CancellationReason.Should().Be("Cliente desistiu");
            delivery.CancelledAt.Should().Be(cancelledAt);
            delivery.UpdatedAt.Should().Be(cancelledAt);
        }

        [Fact]
        public void MarkCancelled_WhenAlreadyCancelled_ShouldReturnFailureResult()
        {
            // Arrange
            var delivery = CreateValid().Value;
            delivery.MarkCancelled("Primeiro cancelamento", Now.AddMinutes(1));

            // Act
            var result = delivery.MarkCancelled("Segunda tentativa", Now.AddMinutes(2));

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodShippingDelivery.AlreadyCancelled");
            result.Error.Message.Should().Be("Esta entrega já foi cancelada.");
            delivery.CancellationReason.Should().Be("Primeiro cancelamento"); // unchanged
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(IfoodShippingDelivery), true) as IfoodShippingDelivery;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
