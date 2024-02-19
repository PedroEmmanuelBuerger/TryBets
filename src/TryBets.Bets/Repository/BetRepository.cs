using TryBets.Bets.DTO;
using TryBets.Bets.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace TryBets.Bets.Repository;

public class BetRepository : IBetRepository
{
    protected readonly ITryBetsContext _context;
    public BetRepository(ITryBetsContext context)
    {
        _context = context;
    }

    public BetDTOResponse Post(BetDTORequest betRequest, string userEmail)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
        if (user == null)
        {
            throw new Exception("User not founded");
        }

        var match = _context.Matches.FirstOrDefault(m => m.MatchId == betRequest.MatchId);
        if (match == null)
        {
            throw new Exception("Match not founded");
        }

        var selectedTeam = _context.Teams.FirstOrDefault(t => t.TeamId == betRequest.TeamId);
        if (selectedTeam == null)
        {
            throw new Exception("Team not founded");
        }

        if (match.MatchFinished)
        {
            throw new Exception("Match finished");
        }

        if (match.MatchTeamAId != betRequest.TeamId && match.MatchTeamBId != betRequest.TeamId)
        {
            throw new Exception("Team is not in this match");
        }

        var newBet = new Bet
        {
            UserId = user.UserId,
            MatchId = betRequest.MatchId,
            TeamId = betRequest.TeamId,
            BetValue = betRequest.BetValue
        };

        _context.Bets.Add(newBet);
        _context.SaveChanges();

        var createdBet = _context.Bets
            .Include(b => b.Team)
            .Include(b => b.Match)
            .FirstOrDefault(b => b.BetId == newBet.BetId);

        if (createdBet == null)
        {
            throw new Exception("Failed to create bet");
        }

        if (match.MatchTeamAId == betRequest.TeamId)
        {
            match.MatchTeamAValue += betRequest.BetValue;
        }
        else
        {
            match.MatchTeamBValue += betRequest.BetValue;
        }

        _context.Matches.Update(match);
        _context.SaveChanges();

        var result = new BetDTOResponse
        {
            Email = createdBet.User?.Email,
            MatchId = createdBet.MatchId,
            MatchDate = createdBet.Match!.MatchDate,
            TeamName = createdBet.Team?.TeamName,
            TeamId = createdBet.TeamId,
            BetId = createdBet.BetId,
            BetValue = createdBet.BetValue,
        };
        return result;
    }

    public BetDTOResponse Get(int BetId, string email)
    {
        var findedUser = _context.Users.FirstOrDefault(u => u.Email == email);
        if (findedUser == null)
        {
            throw new Exception("User not founded");
        }

        var findedBet = _context.Bets
            .Include(b => b.Team)
            .Include(b => b.Match)
            .FirstOrDefault(b => b.BetId == BetId);

        if (findedBet == null)
        {
            throw new Exception("Bet not founded");
        }

        if (findedBet.User!.Email != email)
        {
            throw new Exception("Bet view not allowed");
        }

        return new BetDTOResponse
        {
            Email = findedBet.User?.Email,
            MatchId = findedBet.MatchId,
            MatchDate = findedBet.Match!.MatchDate,
            TeamName = findedBet.Team?.TeamName,
            TeamId = findedBet.TeamId,
            BetId = findedBet.BetId,
            BetValue = findedBet.BetValue,
        };
    }
}