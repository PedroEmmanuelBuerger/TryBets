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
using TryBets.Odds.Models;
using TryBets.Odds.Repository;

namespace TryBets.Test.Test
{
    // 4. Implemente o microsserviço TryBets.Odds
    public class TestReq04 : IClassFixture<WebApplicationFactory<TryBets.OddsProgram.Program>>
    {
        public HttpClient _client;
        public TestReq04(WebApplicationFactory<TryBets.OddsProgram.Program> factory)
        {
            _client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TryBetsContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ContextOddsTest>(options =>
                    {
                        options.UseInMemoryDatabase("InMemoryTestOdds");
                    });
                    services.AddScoped<ITryBetsContext, ContextOddsTest>();
                    services.AddScoped<IOddRepository, OddRepository>();

                    var sp = services.BuildServiceProvider();
                    using (var scope = sp.CreateScope())
                    using (var appContext = scope.ServiceProvider.GetRequiredService<ContextOddsTest>())
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

                        appContext.Matches.Add(new Match { MatchId = 1, MatchDate = new DateTime(2022, 10, 02, 10, 0, 0), MatchTeamAId = 1, MatchTeamBId = 4, MatchTeamAValue = 100, MatchTeamBValue = 100, MatchFinished = true, MatchWinnerId = 1 });
                        appContext.Matches.Add(new Match { MatchId = 2, MatchDate = new DateTime(2022, 10, 03, 10, 0, 0), MatchTeamAId = 2, MatchTeamBId = 3, MatchTeamAValue = 100, MatchTeamBValue = 100, MatchFinished = true, MatchWinnerId = 2 });
                        appContext.Matches.Add(new Match { MatchId = 3, MatchDate = new DateTime(2023, 07, 02, 10, 0, 0), MatchTeamAId = 1, MatchTeamBId = 2, MatchTeamAValue = 100, MatchTeamBValue = 100, MatchFinished = false });
                        appContext.Matches.Add(new Match { MatchId = 4, MatchDate = new DateTime(2023, 07, 03, 10, 0, 0), MatchTeamAId = 3, MatchTeamBId = 4, MatchTeamAValue = 100, MatchTeamBValue = 100, MatchFinished = false });
                        appContext.SaveChanges();

                        appContext.Bets.Add(new Bet { BetId = 1, UserId = 1, MatchId = 1, TeamId = 4, BetValue = 50 });
                        appContext.Bets.Add(new Bet { BetId = 2, UserId = 1, MatchId = 2, TeamId = 2, BetValue = 70 });
                        appContext.Bets.Add(new Bet { BetId = 3, UserId = 1, MatchId = 3, TeamId = 1, BetValue = 90 });
                        appContext.SaveChanges();

                    }
                });
            }).CreateClient();
        }


        
        [Trait("Category", "4. Implemente o microsserviço TryBets.Odds")]
        [Theory(DisplayName = "Será validado que é possível atualizar uma odd de uma partida")]
        [InlineData("/odd", 1, 1, 55.65, 1, 155.65, 100)]
        [InlineData("/odd", 1, 4, 55.65, 1, 100, 155.65)]
        [InlineData("/odd", 2, 2, 55.65, 2, 155.65, 100)]
        [InlineData("/odd", 2, 3, 55.65, 2, 100, 155.65)]
        [InlineData("/odd", 3, 1, 55.65, 3, 155.65, 100)]
        [InlineData("/odd", 3, 2, 55.65, 3, 100, 155.65)]
        [InlineData("/odd", 4, 3, 55.65, 4, 155.65, 100)]
        [InlineData("/odd", 4, 4, 55.65, 4, 100, 155.65)]
        public async Task TestOddRoute(string url, int pMatchId, int pTeamId, decimal pBetValue, int expectedMatchId, decimal expectedTeamAValue, decimal expectedTeamBValue)
        {
            var inputObj = new
            {
            };

            var urlFull = url + "/" + pMatchId.ToString() + "/" + pTeamId.ToString() + "/" + pBetValue.ToString().Replace('.',',');
            var response = await _client.PatchAsync(urlFull, new StringContent(JsonConvert.SerializeObject(inputObj), System.Text.Encoding.UTF8, "application/json"));
            var responseString = await response.Content.ReadAsStringAsync();
            Match jsonResponse = JsonConvert.DeserializeObject<Match>(responseString);

            Assert.Equal(System.Net.HttpStatusCode.OK, response?.StatusCode);
            Assert.Equal(expectedMatchId, jsonResponse.MatchId);
            Assert.Equal(expectedTeamAValue, jsonResponse.MatchTeamAValue);
            Assert.Equal(expectedTeamBValue, jsonResponse.MatchTeamBValue);
            
        }

        [Trait("Category", "4. Implemente o microsserviço TryBets.Odds")]
        [Theory(DisplayName = "Será validado que não é possível atualizar uma odd de uma partida")]
        [InlineData("/odd", 1, 2, 55.65, 1, 155.65, 100)]
        [InlineData("/odd", 1, 3, 55.65, 1, 100, 155.65)]
        [InlineData("/odd", 2, 1, 55.65, 2, 155.65, 100)]
        [InlineData("/odd", 2, 4, 55.65, 2, 100, 155.65)]
        [InlineData("/odd", 10, 1, 55.65, 3, 155.65, 100)]
        [InlineData("/odd", 20, 2, 55.65, 3, 100, 155.65)]
        [InlineData("/odd", 4, 30, 55.65, 4, 155.65, 100)]
        [InlineData("/odd", 4, 40, 55.65, 4, 100, 155.65)]
        public async Task TestOddErrorRoute(string url, int pMatchId, int pTeamId, decimal pBetValue, int expectedMatchId, decimal expectedTeamAValue, decimal expectedTeamBValue)
        {
            var inputObj = new
            {
            };

            var urlFull = url + "/" + pMatchId.ToString() + "/" + pTeamId.ToString() + "/" + pBetValue.ToString().Replace('.',',');
            var response = await _client.PatchAsync(urlFull, new StringContent(JsonConvert.SerializeObject(inputObj), System.Text.Encoding.UTF8, "application/json"));

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response?.StatusCode);
        }

        [Trait("Category", "4. Implemente o microsserviço TryBets.Odds")]
        [Theory(DisplayName = "Será validado que outras rotas não são acessíveis")]
        [InlineData("/team", "get", false)]
        [InlineData("/match/true", "get", false)]
        [InlineData("/match/false", "get", false)]
        [InlineData("/user/login", "post", false)]
        [InlineData("/user/signup", "post", false)]
        [InlineData("/team", "get", true)]
        [InlineData("/match/true", "get", true)]
        [InlineData("/match/false", "get", true)]
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