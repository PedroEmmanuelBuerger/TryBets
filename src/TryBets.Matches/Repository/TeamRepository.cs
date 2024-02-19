using TryBets.Matches.DTO;

namespace TryBets.Matches.Repository;

public class TeamRepository : ITeamRepository
{
    protected readonly ITryBetsContext _context;
    public TeamRepository(ITryBetsContext context)
    {
        _context = context;
    }

    public IEnumerable<TeamDTOResponse> Get()
    {
        var allTeams = _context.Teams
             .Select(t => new TeamDTOResponse
             {
                 TeamId = t.TeamId,
                 TeamName = t.TeamName
             });
        return allTeams.ToList();
    }
}