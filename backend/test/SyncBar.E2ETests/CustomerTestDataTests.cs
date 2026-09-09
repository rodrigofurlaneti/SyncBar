using Xunit;

namespace SyncBar.E2ETests;

// Offline checks always run: no browser, network or registration.
public sealed class CustomerTestDataTests
{
    [Fact]
    public void CpfGenerator_ProducesElevenDigitsWithBothCheckDigits()
    {
        for (var sample = 0; sample < 200; sample++)
        {
            var cpf = CustomerTestData.ValidCpf();
            Assert.Matches("^[0-9]{11}$", cpf);
            Assert.True(cpf.Distinct().Count() > 1);
            for (var length = 9; length <= 10; length++)
            {
                var sum = cpf.Take(length).Select((digit, index) => (digit - '0') * (length + 1 - index)).Sum();
                var check = sum * 10 % 11;
                Assert.Equal(check == 10 ? 0 : check, cpf[length] - '0');
            }
        }
    }

    [Fact]
    public async Task Counter_PersistsAndReservesUniqueSequentialNamesAcrossConcurrentCalls()
    {
        var file = Path.Combine(Path.GetTempPath(), $"syncbar-customer-sequence-{Guid.NewGuid():N}.txt");
        try
        {
            var first = CustomerTestData.Create(file);
            Assert.Equal("João Furlaneti Teste 1", first.Name);
            Assert.Equal("João Furlaneti Teste 2", CustomerTestData.Create(file).Name);
            var numbers = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => CustomerTestData.NextNumber(file))));
            Assert.Equal(Enumerable.Range(3, 8).Select(n => (long)n), numbers.Order());
            Assert.Equal("João Furlaneti Teste 11", CustomerTestData.Create(file).Name);
            Assert.Equal("07025020", CustomerTestData.ZipCode);
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public void Counter_CorruptedStateFailsInsteadOfReusingNames()
    {
        var file = Path.Combine(Path.GetTempPath(), $"syncbar-customer-sequence-{Guid.NewGuid():N}.txt");
        try
        {
            File.WriteAllText(file, "invalid-counter");
            Assert.Throws<FormatException>(() => CustomerTestData.NextNumber(file));
            Assert.Equal("invalid-counter", File.ReadAllText(file));
        }
        finally { File.Delete(file); }
    }
}
