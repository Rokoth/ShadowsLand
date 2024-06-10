using Microsoft.EntityFrameworkCore;
using Shadows.DB.Model;
using System;
using System.Reflection;

namespace Shadows.DB.Context
{
    /// <summary>
    /// Postgresql context
    /// </summary>
    public class DbPgContext : DbContext
    {        
        /// <summary>
        /// settings set
        /// </summary>
        public DbSet<Settings> Settings { get; set; }

        public DbSet<User> Users { get; set; }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="options"></param>
        public DbPgContext(DbContextOptions<DbPgContext> options) : base(options)
        {

        }

        /// <summary>
        /// create models
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasPostgresExtension("uuid-ossp");

            modelBuilder.ApplyConfiguration(new EntityConfiguration<Settings>());

            foreach (var type in Assembly.GetAssembly(typeof(Entity)).GetTypes())
            {
                if (typeof(IEntity).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    var configType = typeof(EntityConfiguration<>).MakeGenericType(type);
                    var config = Activator.CreateInstance(configType);
                    GetType().GetMethod(nameof(ApplyConf), BindingFlags.NonPublic | BindingFlags.Instance)
                        .MakeGenericMethod(type).Invoke(this, new object[] { modelBuilder, config });

                }
            }
        }

        private void ApplyConf<T>(ModelBuilder modelBuilder, EntityConfiguration<T> config) where T : class, IEntity
        {
            modelBuilder.ApplyConfiguration(config);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }
    }
}
