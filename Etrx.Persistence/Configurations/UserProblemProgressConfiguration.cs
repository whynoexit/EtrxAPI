using Etrx.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Etrx.Persistence.Configurations;

public class UserProblemProgressConfiguration : IEntityTypeConfiguration<UserProblemProgress>
{
    public void Configure(EntityTypeBuilder<UserProblemProgress> builder)
    {
        builder.ToTable("user_problem_progress");
        builder.HasKey(p => new { p.UserId, p.ContestId, p.Index });
        builder.HasOne<User>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserProblemSyncConfiguration : IEntityTypeConfiguration<UserProblemSync>
{
    public void Configure(EntityTypeBuilder<UserProblemSync> builder)
    {
        builder.ToTable("user_problem_sync");
        builder.HasKey(p => p.UserId);
        builder.HasOne<User>().WithOne().HasForeignKey<UserProblemSync>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
