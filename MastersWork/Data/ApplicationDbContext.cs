using MastersWork.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;

namespace MastersWork.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<UserState> UserStates { get; set; }
        public DbSet<BotCreationData> BotCreationDatas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var qaConverter = new ValueConverter<List<QuestionAnswer>, string>(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<List<QuestionAnswer>>(v ?? "[]")!);

            var qaComparer = new ValueComparer<List<QuestionAnswer>>(
                (c1, c2) => JsonConvert.SerializeObject(c1) == JsonConvert.SerializeObject(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => JsonConvert.DeserializeObject<List<QuestionAnswer>>(JsonConvert.SerializeObject(c))!);

            modelBuilder.Entity<BotCreationData>()
                .Property(b => b.QA)
                .HasConversion(qaConverter!)
                .Metadata
                .SetValueComparer(qaComparer);

            base.OnModelCreating(modelBuilder);
        }
    }
}
