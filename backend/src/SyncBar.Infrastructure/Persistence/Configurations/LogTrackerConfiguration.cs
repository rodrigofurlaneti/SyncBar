using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class LogTrackerConfiguration : IEntityTypeConfiguration<LogTracker>
{
    public void Configure(EntityTypeBuilder<LogTracker> builder)
    {
        // Nome da tabela ajustado para minúsculo conforme o padrão do MySQL
        builder.ToTable("logtracker");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        // Relacionamento com AppUser
        builder.HasIndex(x => x.AppUserId).HasDatabaseName("IX_LogTracker_AppUserId");
        builder.HasOne<AppUser>()
               .WithMany()
               .HasForeignKey(x => x.AppUserId)
               .HasConstraintName("FK_LogTracker_AppUser")
               .OnDelete(DeleteBehavior.Restrict);

        // Mapeamento de Colunas de Origem
        builder.Property(x => x.DirectoryName).HasColumnType("varchar(150)");
        builder.Property(x => x.ClassName).HasColumnType("varchar(150)").IsRequired();
        builder.Property(x => x.MethodName).HasColumnType("varchar(150)").IsRequired();

        // Mapeamento de Status e Performance
        builder.Property(x => x.IsSuccess).HasColumnType("tinyint(1)").IsRequired();
        builder.Property(x => x.ExecutionTimeMs).HasColumnType("bigint");

        // Mapeamento de Mensagens e Logs de Erro (Ajustado para 'text' compatível com MySQL)
        builder.Property(x => x.Message).HasColumnType("text");
        builder.Property(x => x.ErrorMessage).HasColumnType("text");
        builder.Property(x => x.StackTrace).HasColumnType("text");

        // Mapeamento de Rede e Auditoria
        builder.Property(x => x.IpAddress).HasColumnType("varchar(45)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.IsActive).HasColumnType("tinyint(1)").IsRequired();

        // Índice adicionado para otimização de consultas por data de criação (Escopo do Cartão)
        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_LogTracker_CreatedAt");
    }
}