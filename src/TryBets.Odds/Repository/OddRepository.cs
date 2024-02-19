using TryBets.Odds.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Globalization;

namespace TryBets.Odds.Repository;

public class OddRepository : IOddRepository
{
    protected readonly ITryBetsContext _context;
    public OddRepository(ITryBetsContext context)
    {
        _context = context;
    }

    public Match Patch(int MatchId, int TeamId, string BetValue)
    {
        if (!decimal.TryParse(BetValue.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal decimalBetValue))
        {
            throw new Exception("Invalid bet value format");
        }

        var match = _context.Matches.FirstOrDefault(m => m.MatchId == MatchId);

        if (match.MatchTeamAId != TeamId && match.MatchTeamBId != TeamId)
        {
            throw new Exception("Team is not in this match");
        }

        if (TeamId == match.MatchTeamAId)
        {
            match.MatchTeamAValue += decimalBetValue;
        }
        else
        {
            match.MatchTeamBValue += decimalBetValue;
        }

        return match;

    }
}