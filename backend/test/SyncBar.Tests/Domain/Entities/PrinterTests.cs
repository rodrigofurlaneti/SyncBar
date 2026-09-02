using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class PrinterTests
    {
        [Fact]
        public void Create_WithValidWindowsConnection_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long branchId = 1;
            string name = "Kitchen Printer";
            int connectionType = Printer.ConnectionWindows;
            string printerName = "EPSON-TM20";

            // Act
            var result = Printer.Create(branchId, name, connectionType, printerName, null, null, true, false);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var printer = result.Value;
            printer.Should().NotBeNull();
            printer.BranchId.Should().Be(branchId);
            printer.Name.Should().Be(name);
            printer.ConnectionType.Should().Be(connectionType);
            printer.PrinterName.Should().Be(printerName);
            printer.IpAddress.Should().BeNull();
            printer.Port.Should().BeNull();
            printer.PrintsOrders.Should().BeTrue();
            printer.PrintsBills.Should().BeFalse();
            printer.IsActive.Should().BeTrue();
            printer.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            printer.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_WithValidNetworkConnection_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            string ipAddress = "192.168.0.50";
            int port = 9100;

            // Act
            var result = Printer.Create(1, "Bar Printer", Printer.ConnectionNetwork, null, ipAddress, port, false, true);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.IpAddress.Should().Be(ipAddress);
            result.Value.Port.Should().Be(port);
            result.Value.PrintsOrders.Should().BeFalse();
            result.Value.PrintsBills.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = Printer.Create(1, invalidName, Printer.ConnectionWindows, "Driver", null, null, true, false);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Printer.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(-1)]
        public void Create_WithInvalidConnectionType_ShouldReturnFailureResult(int invalidConnectionType)
        {
            // Act
            var result = Printer.Create(1, "Printer", invalidConnectionType, "Driver", null, null, true, false);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Printer.InvalidConnection");
            result.Error.Message.Should().Be("Connection type must be Windows (1) or Network (2).");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithWindowsConnectionAndMissingPrinterName_ShouldReturnFailureResult(string? invalidPrinterName)
        {
            // Act
            var result = Printer.Create(1, "Printer", Printer.ConnectionWindows, invalidPrinterName, null, null, true, false);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Printer.MissingDriver");
            result.Error.Message.Should().Be("Windows printer requires the installed driver name.");
        }

        [Fact]
        public void Create_WithNetworkConnectionAndMissingIpAddress_ShouldReturnFailureResult()
        {
            // Act
            var result = Printer.Create(1, "Printer", Printer.ConnectionNetwork, null, null, 9100, true, false);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Printer.MissingAddress");
            result.Error.Message.Should().Be("Network printer requires IP address and port.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(65536)]
        public void Create_WithNetworkConnectionAndInvalidPort_ShouldReturnFailureResult(int invalidPort)
        {
            // Act
            var result = Printer.Create(1, "Printer", Printer.ConnectionNetwork, null, "192.168.0.1", invalidPort, true, false);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Printer.MissingAddress");
        }

        [Fact]
        public void Create_WithNetworkConnectionAndNullPort_ShouldReturnFailureResult()
        {
            // Act
            var result = Printer.Create(1, "Printer", Printer.ConnectionNetwork, null, "192.168.0.1", null, true, false);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Printer.MissingAddress");
        }

        [Fact]
        public void Create_WithNeitherPrintsOrdersNorPrintsBills_ShouldReturnFailureResult()
        {
            // Act
            var result = Printer.Create(1, "Printer", Printer.ConnectionWindows, "Driver", null, null, false, false);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Printer.NoRole");
            result.Error.Message.Should().Be("Printer must print orders, bills or both.");
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var printer = Printer.Create(1, "Printer", Printer.ConnectionWindows, "Driver", null, null, true, false).Value;

            // Act
            printer.Deactivate();

            // Assert
            printer.IsActive.Should().BeFalse();
            printer.UpdatedAt.Should().NotBeNull();
            printer.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(Printer), true) as Printer;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
