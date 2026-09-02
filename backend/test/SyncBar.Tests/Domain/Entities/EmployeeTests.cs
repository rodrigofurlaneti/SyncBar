using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class EmployeeTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long branchId = 1;
            long jobTitleId = 2;
            string name = "Joao Silva";
            string cpf = "12345678900";
            string email = "joao@bar.com";
            string phone = "11988887777";
            DateTime hiredAt = new DateTime(2026, 1, 10);
            DateTime? dismissedAt = null;
            decimal? salary = 2500m;

            // Act
            var result = Employee.Create(branchId, jobTitleId, name, cpf, email, phone, hiredAt, dismissedAt, salary);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.BranchId.Should().Be(branchId);
            result.Value.JobTitleId.Should().Be(jobTitleId);
            result.Value.Name.Should().Be(name);
            result.Value.Cpf.Should().Be(cpf);
            result.Value.Email.Should().Be(email);
            result.Value.Phone.Should().Be(phone);
            result.Value.HiredAt.Should().Be(hiredAt);
            result.Value.DismissedAt.Should().BeNull();
            result.Value.Salary.Should().Be(salary);
            result.Value.CommissionPercent.Should().BeNull();
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = Employee.Create(1, 2, invalidName, "12345678900", null, null, DateTime.Now, null, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Employee.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceCpf_ShouldReturnFailureResult(string? invalidCpf)
        {
            // Act
            var result = Employee.Create(1, 2, "Joao Silva", invalidCpf, null, null, DateTime.Now, null, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Employee.EmptyCpf");
            result.Error.Message.Should().Be("Cpf is required.");
        }

        [Fact]
        public void UpdateDetails_WithValidArguments_ShouldUpdatePropertiesAndSetUpdatedAt()
        {
            // Arrange
            var employee = Employee.Create(1, 2, "Joao Silva", "12345678900", null, null, DateTime.Now, null, null).Value;

            // Act
            var result = employee.UpdateDetails(3, "Joao Souza", "joao.souza@bar.com", "11977776666", 3000m);

            // Assert
            result.IsSuccess.Should().BeTrue();
            employee.JobTitleId.Should().Be(3);
            employee.Name.Should().Be("Joao Souza");
            employee.Email.Should().Be("joao.souza@bar.com");
            employee.Phone.Should().Be("11977776666");
            employee.Salary.Should().Be(3000m);
            employee.UpdatedAt.Should().NotBeNull();
            employee.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateDetails_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Arrange
            var employee = Employee.Create(1, 2, "Joao Silva", "12345678900", null, null, DateTime.Now, null, null).Value;

            // Act
            var result = employee.UpdateDetails(2, invalidName, null, null, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Employee.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(50)]
        [InlineData(100)]
        public void SetCommissionPercent_WithValidValues_ShouldReturnSuccessResultAndSetUpdatedAt(decimal validCommission)
        {
            // Arrange
            var employee = Employee.Create(1, 2, "Joao Silva", "12345678900", null, null, DateTime.Now, null, null).Value;

            // Act
            var result = employee.SetCommissionPercent(validCommission);

            // Assert
            result.IsSuccess.Should().BeTrue();
            employee.CommissionPercent.Should().Be(validCommission);
            employee.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void SetCommissionPercent_WithNull_ShouldReturnSuccessResultAndClearCommission()
        {
            // Arrange
            var employee = Employee.Create(1, 2, "Joao Silva", "12345678900", null, null, DateTime.Now, null, null).Value;
            employee.SetCommissionPercent(10m);

            // Act
            var result = employee.SetCommissionPercent(null);

            // Assert
            result.IsSuccess.Should().BeTrue();
            employee.CommissionPercent.Should().BeNull();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void SetCommissionPercent_WithOutOfRangeValues_ShouldReturnFailureResult(decimal invalidCommission)
        {
            // Arrange
            var employee = Employee.Create(1, 2, "Joao Silva", "12345678900", null, null, DateTime.Now, null, null).Value;

            // Act
            var result = employee.SetCommissionPercent(invalidCommission);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Employee.InvalidCommission");
            result.Error.Message.Should().Be("Commission percent must be between 0 and 100.");
        }

        [Fact]
        public void Dismiss_WhenNotDismissed_ShouldSetDismissedAtAndDeactivate()
        {
            // Arrange
            var employee = Employee.Create(1, 2, "Joao Silva", "12345678900", null, null, DateTime.Now, null, null).Value;

            // Act
            var result = employee.Dismiss();

            // Assert
            result.IsSuccess.Should().BeTrue();
            employee.DismissedAt.Should().NotBeNull();
            employee.DismissedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            employee.IsActive.Should().BeFalse();
            employee.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void Dismiss_WhenAlreadyDismissed_ShouldReturnFailureResult()
        {
            // Arrange
            var employee = Employee.Create(1, 2, "Joao Silva", "12345678900", null, null, DateTime.Now, null, null).Value;
            employee.Dismiss();

            // Act
            var result = employee.Dismiss();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Employee.AlreadyDismissed");
            result.Error.Message.Should().Be("Employee is already dismissed.");
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var employee = Employee.Create(1, 2, "Joao Silva", "12345678900", null, null, DateTime.Now, null, null).Value;

            // Act
            employee.Touch();

            // Assert
            employee.UpdatedAt.Should().NotBeNull();
            employee.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var employee = Employee.Create(1, 2, "Joao Silva", "12345678900", null, null, DateTime.Now, null, null).Value;

            // Act
            employee.Deactivate();

            // Assert
            employee.IsActive.Should().BeFalse();
            employee.UpdatedAt.Should().NotBeNull();
            employee.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(Employee), true) as Employee;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
