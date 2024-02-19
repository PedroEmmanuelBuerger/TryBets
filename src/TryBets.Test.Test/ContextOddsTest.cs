using Microsoft.EntityFrameworkCore;
using TryBets.Odds.Models;
using TryBets.Odds.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace TryBets.Test.Test;

public class ContextOddsTest : DbContext, ITryBetsContext
{
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<Team> Teams { get; set;}
    public virtual DbSet<Match> Matches { get; set;}
    public virtual DbSet<Bet> Bets { get; set; }
    public ContextOddsTest(DbContextOptions<ContextOddsTest> options) : base(options)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {

        modelBuilder.Entity<Team>()
            .HasMany(t => t.MatchesA)
            .WithOne(m => m.MatchTeamA)
            .HasForeignKey(m => m.MatchTeamAId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Team>()
            .HasMany(t => t.MatchesB)
            .WithOne(m => m.MatchTeamB)
            .HasForeignKey(m => m.MatchTeamBId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Bets)
            .WithOne(b => b.User)
            .HasForeignKey(b => b.UserId);

        modelBuilder.Entity<Match>()
            .HasMany(m => m.Bets)
            .WithOne(b => b.Match)
            .HasForeignKey(b => b.MatchId);

        modelBuilder.Entity<Team>()
            .HasMany(t => t.Bets)
            .WithOne(b => b.Team)
            .HasForeignKey(b => b.TeamId);
            
    }

}