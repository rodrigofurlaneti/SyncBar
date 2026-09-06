using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class RefreshTokenRepositoryTests : RepositoryTestBase
    {
        private readonly RefreshTokenRepository _repository;

        public RefreshTokenRepositoryTests()
        {
            _repository = new RefreshTokenRepository(Context);
        }

        private static RefreshToken CreateToken(long appUserId = 1, string token = "token-1") =>
            RefreshToken.Create(appUserId, token, DateTime.UtcNow.AddDays(7)).Value;

        private async Task<RefreshToken> SeedAsync(RefreshToken refreshToken)
        {
            await Context.AddAsync(refreshToken);
            await Context.SaveChangesAsync();
            Context.Entry(refreshToken).State = EntityState.Detached;
            return refreshToken;
        }

        [Fact]
        public async Task GetByTokenForUpdateAsync_ExistingToken_ReturnsTrackedToken()
        {
            var refreshToken = await SeedAsync(CreateToken(token: "token-abc"));

            var result = await _repository.GetByTokenForUpdateAsync("token-abc");

            result.Should().NotBeNull();
            result!.Id.Should().Be(refreshToken.Id);
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByTokenForUpdateAsync_NonExistingToken_ReturnsNull()
        {
            var result = await _repository.GetByTokenForUpdateAsync("token-missing");

            result.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_ValidToken_PersistsToDatabase()
        {
            var refreshToken = CreateToken(token: "token-added");

            await _repository.AddAsync(refreshToken);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<RefreshToken>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Token == "token-added");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
