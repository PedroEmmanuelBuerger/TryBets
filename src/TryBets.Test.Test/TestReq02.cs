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
using TryBets.Users.Models;
using TryBets.Users.Repository;

namespace TryBets.Test.Test
{
    // 2. Implemente o microsserviço TryBets.Users
    public class LoginJson
    {
        public string? token { get; set; }
    }
    public class TestReq02 : IClassFixture<WebApplicationFactory<TryBets.UsersProgram.Program>>
    {
        public HttpClient _client;
        public TestReq02(WebApplicationFactory<TryBets.UsersProgram.Program> factory)
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

                    services.AddDbContext<ContextUsersTest>(options =>
                    {
                        options.UseInMemoryDatabase("InMemoryTestUsers");
                    });
                    services.AddScoped<ITryBetsContext, ContextUsersTest>();
                    services.AddScoped<IUserRepository, UserRepository>();

                    var sp = services.BuildServiceProvider();
                    using (var scope = sp.CreateScope())
                    using (var appContext = scope.ServiceProvider.GetRequiredService<ContextUsersTest>())
                    {
                        appContext.Database.EnsureCreated();
                        appContext.Database.EnsureDeleted();
                        appContext.Database.EnsureCreated();

                        appContext.Users.Add(new User { UserId = 1, Name = "Maria", Email = "maria@trybets.com", Password = "123456" });
                        appContext.Users.Add(new User { UserId = 2, Name = "Joana", Email = "joana@trybets.com", Password = "123456" });
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

        [Trait("Category", "2. Implemente o microsserviço TryBets.Users")]
        [Theory(DisplayName = "Será validado que é possível cadastrar uma nova pessoa usuária")]
        [InlineData("/user/signup")]
        public async Task TestSignupRoute(string url)
        {
            var inputObj = new
            {
                Name = "Ana",
                Email = "ana@trybehotel.com",
                Password = "Senha1"
            };
            var response = await _client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(inputObj), System.Text.Encoding.UTF8, "application/json"));
            var responseString = await response.Content.ReadAsStringAsync();
            LoginJson jsonResponse = JsonConvert.DeserializeObject<LoginJson>(responseString);

            Assert.Equal(System.Net.HttpStatusCode.Created, response?.StatusCode);
            Assert.NotEmpty(jsonResponse.token);
        }

        [Trait("Category", "2. Implemente o microsserviço TryBets.Users")]
        [Theory(DisplayName = "Será validado que é possível realizar o login de pessoa usuária")]
        [InlineData("/user/login", "maria@trybets.com", "123456")]
        [InlineData("/user/login", "joana@trybets.com", "123456")]
        public async Task TestLoginRoute(string url, string pEmail, string pPassword)
        {
            var inputObj = new
            {
                Email = pEmail,
                Password = pPassword
            };
            var response = await _client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(inputObj), System.Text.Encoding.UTF8, "application/json"));
            var responseString = await response.Content.ReadAsStringAsync();
            LoginJson jsonResponse = JsonConvert.DeserializeObject<LoginJson>(responseString);

            Assert.Equal(System.Net.HttpStatusCode.OK, response?.StatusCode);
            Assert.NotEmpty(jsonResponse.token);
        }

        [Trait("Category", "2. Implemente o microsserviço TryBets.Users")]
        [Theory(DisplayName = "Será validado que não é possível realizar o login de pessoa usuária com credenciais erradas")]
        [InlineData("/user/login", "maria@trybets.com", "1234567")]
        [InlineData("/user/login", "joana@trybets.com", "1234567")]
        [InlineData("/user/login", "brenda@trybets.com", "123456")]
        [InlineData("/user/login", "fernanda@trybets.com", "123456")]
        public async Task TestLoginErrorRoute(string url, string pEmail, string pPassword)
        {
            var inputObj = new
            {
                Email = pEmail,
                Password = pPassword
            };
            var response = await _client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(inputObj), System.Text.Encoding.UTF8, "application/json"));
            var responseString = await response.Content.ReadAsStringAsync();
            LoginJson jsonResponse = JsonConvert.DeserializeObject<LoginJson>(responseString);

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response?.StatusCode);
        }

        [Trait("Category", "2. Implemente o microsserviço TryBets.Users")]
        [Theory(DisplayName = "Será validado que outras rotas não são acessíveis")]
        [InlineData("/team", "get", false)]
        [InlineData("/match/true", "get", false)]
        [InlineData("/match/false", "get", false)]
        [InlineData("/bet/1", "get", false)]
        [InlineData("/bet", "post", false)]
        [InlineData("/odd/1/1/10", "patch", false)]
        [InlineData("/team", "get", true)]
        [InlineData("/match/true", "get", true)]
        [InlineData("/match/false", "get", true)]
        [InlineData("/bet/1", "get", true)]
        [InlineData("/bet", "post", true)]
        [InlineData("/odd/1/1/10", "patch", true)]
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