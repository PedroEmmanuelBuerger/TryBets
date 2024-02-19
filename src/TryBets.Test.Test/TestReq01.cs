using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Diagnostics;
using System.Xml;
using System.IO;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TryBets.Matches.Models;
using TryBets.Matches.Repository;

namespace TryBets.Test.Test
{
    // 1. Implemente o microsserviço TryBets.Matches
    public class TeamJson {
        public int TeamId { get; set; }
        public string? TeamName { get; set; }
    }

    public class MatchJson {
         public int MatchId { get; set; }
        public DateTime MatchDate { get; set; }
        public int MatchTeamAId { get; set; }
        public int MatchTeamBId { get; set; }
        public string? TeamAName { get; set; }
        public string? TeamBName { get; set; }
        public string? MatchTeamAOdds { get; set;}
        public string? MatchTeamBOdds { get; set;}
        public bool MatchFinished { get; set;}
        public int? MatchWinnerId { get; set; }
    }

    public class TestReq01 : IClassFixture<WebApplicationFactory<TryBets.MatchesProgram.Program>>
    {
        public HttpClient _client;
        public TestReq01(WebApplicationFactory<TryBets.MatchesProgram.Program> factory)
        {
            _client = factory.WithWebHostBuilder(builder => {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TryBetsContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ContextMatchesTest>(options =>
                    {
                        options.UseInMemoryDatabase("InMemoryTestMatches");
                    });
                    services.AddScoped<ITryBetsContext, ContextMatchesTest>();
                    services.AddScoped<ITeamRepository, TeamRepository>();
                    services.AddScoped<IMatchRepository, MatchRepository>();

                    var sp = services.BuildServiceProvider();
                    using (var scope = sp.CreateScope())
                    using (var appContext = scope.ServiceProvider.GetRequiredService<ContextMatchesTest>())
                    {
                        appContext.Database.EnsureCreated();
                        appContext.Database.EnsureDeleted();
                        appContext.Database.EnsureCreated();
                    
                        appContext.Users.Add(new User { UserId = 1, Name = "Maria", Email = "maria@trybets.com", Password = "123456" });
                        appContext.SaveChanges();

                        appContext.Teams.Add(new Team { TeamId = 1, TeamName = "Dragons" });
                        appContext.Teams.Add(new Team { TeamId = 2, TeamName = "Tigers" });
                        appContext.Teams.Add(new Team { TeamId = 3, TeamName = "Wolves" });
                        appContext.Teams.Add(new Team { TeamId = 4, TeamName = "Lions" });
                        appContext.SaveChanges();

                        appContext.Matches.Add(new Match { MatchId = 1, MatchDate = new DateTime(2022, 10, 02, 10, 0, 0), MatchTeamAId = 1, MatchTeamBId = 4, MatchTeamAValue = 100, MatchTeamBValue = 100, MatchFinished = true, MatchWinnerId = 1});
                        appContext.Matches.Add(new Match { MatchId = 2, MatchDate = new DateTime(2022, 10, 03, 10, 0, 0), MatchTeamAId = 2, MatchTeamBId = 3, MatchTeamAValue = 100, MatchTeamBValue = 100, MatchFinished = true, MatchWinnerId = 2});
                        appContext.Matches.Add(new Match { MatchId = 3, MatchDate = new DateTime(2023, 07, 02, 10, 0, 0), MatchTeamAId = 1, MatchTeamBId = 2, MatchTeamAValue = 100, MatchTeamBValue = 100, MatchFinished = false });
                        appContext.Matches.Add(new Match { MatchId = 4, MatchDate = new DateTime(2023, 07, 03, 10, 0, 0), MatchTeamAId = 3, MatchTeamBId = 4, MatchTeamAValue = 100, MatchTeamBValue = 100, MatchFinished = false });
                        appContext.SaveChanges();

                        appContext.Bets.Add(new Bet { BetId = 1, UserId = 1, MatchId = 1, TeamId = 4, BetValue = 50});
                        appContext.Bets.Add(new Bet { BetId = 2, UserId = 1, MatchId = 2, TeamId = 2, BetValue = 70});
                        appContext.Bets.Add(new Bet { BetId = 3, UserId = 1, MatchId = 3, TeamId = 1, BetValue = 90});
                        appContext.SaveChanges();

                    }
                });
            }).CreateClient();
        }

        [Trait("Category", "1. Implemente o microsserviço TryBets.Matches")]
        [Theory(DisplayName = "Será validado que é possível listar todos os times")]
        [InlineData("/team", 0, 1, "Dragons")]
        [InlineData("/team", 1, 2, "Tigers")]
        [InlineData("/team", 2, 3, "Wolves")]
        [InlineData("/team", 3, 4, "Lions")]
        public async Task TestTeamRoute(string url, int positionTeam, int expectedTeamId, string expectedTeamName)
        {
            var response = await _client.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();
            List<TeamJson> jsonResponse = JsonConvert.DeserializeObject<List<TeamJson>>(responseString);
            Assert.Equal(expectedTeamId, jsonResponse[positionTeam].TeamId);
            Assert.Contains(expectedTeamName, jsonResponse[positionTeam].TeamName);
        }

        [Trait("Category", "1. Implemente o microsserviço TryBets.Matches")]
        [Theory(DisplayName = "Será validado que é possível listar todas as partidas encerradas")]
        [InlineData("/match/true", 0, 1, "Dragons", "Lions")]
        [InlineData("/match/true", 1, 2, "Tigers", "Wolves")]
        public async Task TestMatchFinishedRoute(string url, int positionMatch, int expectedMatchId, string expectedTeamAName, string expectedTeamBName)
        {
            var response = await _client.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();
            List<MatchJson> jsonResponse = JsonConvert.DeserializeObject<List<MatchJson>>(responseString);
            Assert.Equal(expectedMatchId, jsonResponse[positionMatch].MatchId);
            Assert.Contains(expectedTeamAName, jsonResponse[positionMatch].TeamAName);
            Assert.Contains(expectedTeamBName, jsonResponse[positionMatch].TeamBName);
        }

        [Trait("Category", "1. Implemente o microsserviço TryBets.Matches")]
        [Theory(DisplayName = "Será validado que é possível listar todas as partidas em aberto")]
        [InlineData("/match/false", 0, 3, "Dragons", "Tiger")]
        [InlineData("/match/false", 1, 4, "Wolves", "Lions")]
        public async Task TestMatchNotFinishedRoute(string url, int positionMatch, int expectedMatchId, string expectedTeamAName, string expectedTeamBName)
        {
            var response = await _client.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();
            List<MatchJson> jsonResponse = JsonConvert.DeserializeObject<List<MatchJson>>(responseString);
            Assert.Equal(expectedMatchId, jsonResponse[positionMatch].MatchId);
            Assert.Contains(expectedTeamAName, jsonResponse[positionMatch].TeamAName);
            Assert.Contains(expectedTeamBName, jsonResponse[positionMatch].TeamBName);
        }


        [Trait("Category", "1. Implemente o microsserviço TryBets.Matches")]
        [Theory(DisplayName = "Será validado que outras rotas não são acessíveis")]
        [InlineData("/bet/1", "get", false)]
        [InlineData("/bet", "post", false)]
        [InlineData("/odd/1/1/10", "patch", false)]
        [InlineData("/user/login", "post", false)]
        [InlineData("/user/signup", "post", false)]
        [InlineData("/bet/1", "get", true)]
        [InlineData("/bet", "post", true)]
        [InlineData("/odd/1/1/10", "patch", true)]
        [InlineData("/user/login", "post", true)]
        [InlineData("/user/signup", "post", true)]
        public async Task TestOtherRoutes(string url, string method, bool authorization)
        {
            if(authorization)
            {
                var claims = new ClaimsIdentity();
                claims.AddClaim(new Claim(ClaimTypes.Email, "maria@trybets.com"));

                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenDescriptor = new SecurityTokenDescriptor()
                {    
                    Subject = claims,
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(Encoding.ASCII.GetBytes("4d82a63bbdc67c1e4784ed6587f3730c")),
                        SecurityAlgorithms.HmacSha256Signature
                    ),
                    Expires = DateTime.Now.AddDays(1)
                };
                var tokenH = tokenHandler.CreateToken(tokenDescriptor);
                var token = tokenHandler.WriteToken(tokenH);
                _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            var inputObj = new
            {
            };

            var statusCodeFounded = System.Net.HttpStatusCode.OK;

            if (method == "get")
            {
                var response = await _client.GetAsync(url);
                statusCodeFounded = response!.StatusCode;
            } 
            else if (method == "post")
            {
                var response = await _client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(inputObj), System.Text.Encoding.UTF8, "application/json"));
                statusCodeFounded = response!.StatusCode;
            }
            else if (method == "put")
            {
                var response = await _client.PutAsync(url, new StringContent(JsonConvert.SerializeObject(inputObj), System.Text.Encoding.UTF8, "application/json"));
                statusCodeFounded = response!.StatusCode;
            }
            else if (method == "patch")
            {
                var response = await _client.PatchAsync(url, new StringContent(JsonConvert.SerializeObject(inputObj), System.Text.Encoding.UTF8, "application/json"));
                statusCodeFounded = response!.StatusCode;
            }

            Assert.Equal(System.Net.HttpStatusCode.NotFound, statusCodeFounded);
        }

    }
}