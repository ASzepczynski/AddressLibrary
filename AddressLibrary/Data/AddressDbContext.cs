using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection;


namespace AddressLibrary.Data
{
    public class AddressDbContext : DbContext
    {
        public AddressDbContext(DbContextOptions<AddressDbContext> options) : base(options)
        {

        }

        // Istniej¹ce tabele TERYT (p³askie)
        public DbSet<TerytSimc> TerytSimc { get; set; }
        public DbSet<TerytTerc> TerytTerc { get; set; }
        public DbSet<TerytUlic> TerytUlic { get; set; }
        public DbSet<TerytWmRodz> TerytWmRodz { get; set; }
        public DbSet<Pna> Pna { get; set; }

        // S³owniki
        public DbSet<RodzajGminy> RodzajeGmin { get; set; }
        public DbSet<RodzajMiasta> RodzajeMiast { get; set; }

        // Tabele hierarchiczne
        public DbSet<Wojewodztwo> Wojewodztwa { get; set; }
        public DbSet<Powiat> Powiaty { get; set; }
        public DbSet<Gmina> Gminy { get; set; }
        public DbSet<Miasto> Miasta { get; set; }
        public DbSet<Ulica> Ulice { get; set; }
        public DbSet<KodPocztowy> KodyPocztowe { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Automatycznie zastosuj wszystkie konfiguracje z assembly
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

        
    }
}