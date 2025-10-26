using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Jango_Travel.Models;

namespace Jango_Travel.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Country> Countries { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<BlogPost> BlogPosts { get; set; }
    }
}