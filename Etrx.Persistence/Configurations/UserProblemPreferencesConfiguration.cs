using Etrx.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Etrx.Persistence.Configurations;

public class UserProblemPreferencesConfiguration : IEntityTypeConfiguration<UserProblemPreferences>
{
    public void Configure(EntityTypeBuilder<UserProblemPreferences> builder)
    {
        builder.HasKey(p => p.UserId);
        builder.Property(p => p.FiltersJson).IsRequired();
        builder.HasOne<User>().WithOne().HasForeignKey<UserProblemPreferences>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
