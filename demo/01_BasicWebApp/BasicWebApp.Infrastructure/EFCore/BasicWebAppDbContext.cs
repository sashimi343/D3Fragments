using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Books;
using BasicWebApp.Domain.Loans;
using BasicWebApp.Domain.Members;

using D3Fragments.EFCore;

using Microsoft.EntityFrameworkCore;

namespace BasicWebApp.Infrastructure.EFCore;

public class BasicWebAppDbContext : D3DbContext
{
    public BasicWebAppDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Book> Books { get; set; } = null!;
    public DbSet<Loan> Loans { get; set; } = null!;
    public DbSet<Member> Members { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(builder =>
        {
            builder.ComplexValueObject(x => x.Isbn);
            builder.ComplexValueObject(x => x.Title);
        });
        modelBuilder.Entity<Member>(builder =>
        {
            builder.ComplexValueObject(x => x.Name);
            builder.ComplexValueObject(x => x.EmailAddress);
        });
    }
}
