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
using TryBets.Bets.Models;
using TryBets.Bets.Services;
using TryBets.Bets.Repository;

namespace TryBets.Test.Test
{
    // 3. Implemente o microsserviço TryBets.Bets

public class BetResponseJson
{
    public int BetId { get; set; }
    public int MatchId { get; set; }
    public int TeamId { get; set; }
    public decimal BetValue { get; set; }
    public DateTime MatchDate { get; set; }
    public string? TeamName { get; set; }
    public string? Email { get; set; }
}

public class ErrorMessage
{
    public string? message { get; set; }
}
    public class TestReq03 : IClassFixture<WebApplicationFactory<TryBets.BetsProgram.Program>>
    {
        public HttpClient _client;
        public TestReq03(WebApplicationFactory<TryBets.BetsProgram.Program> factory)
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

                    
                    var descriptorService = services.SingleOrDefault(d => d.ServiceType == typeof(IOddService));
                    if (descriptorService != null)
                    {
                        services.Remove(descriptorService);
                    }
                    

                    services.AddDbContext<ContextBetsTest>(options =>
                    {
                        options.UseInMemoryDatabase("InMemoryTestBets");
                    });
                    services.AddScoped<ITryBetsContext, ContextBetsTest>();
                    services.AddScoped<IBetRepository, BetRepository>();
                    services.AddScoped<IOddService, OddServiceTest>();

                    var sp = services.BuildServiceProvider();
                    using (var scope = sp.CreateScope())
                    using (var appContext = scope.ServiceProvider.GetRequiredService<ContextBetsTest>())
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

        [Trait("Category", "3. Implemente o microsserviço TryBets.Bets")]
        [Theory(DisplayName = "Será validado que é possível cadastrar uma nova aposta")]
        [InlineData("/bet", 3, 1, 250, 4, 3, "Dragons", 250, "maria@trybets.com", "maria@trybets.com")]
        [InlineData("/bet", 3, 2, 260, 4, 3, "Tigers", 260, "maria@trybets.com", "maria@trybets.com")]
        [InlineData("/bet", 4, 3, 270, 4, 4, "Wolves", 270, "maria@trybets.com", "maria@trybets.com")]
        [InlineData("/bet", 4, 4, 280, 4, 4, "Lions", 280, "maria@trybets.com", "maria@trybets.com")]
        public async Task TestBetPostRoute(string url, int pMatchId, int pTeamId, decimal pBetValue, int expectedBetId, int expectedMatchId, string expectedTeamName, decimal expectedBetValue, string pEmail, string expectedEmail)
        {

            var claims = new ClaimsIdentity();
            claims.AddClaim(new Claim(ClaimTypes.Email, pEmail));

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
            var inputObj = new
            {
                MatchId = pMatchId,
                TeamId = pTeamId,
                BetValue = pBetValue
            };

            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(inputObj), System.Text.Encoding.UTF8, "application/json"));
            var responseString = await response.Content.ReadAsStringAsync();
            BetResponseJson jsonResponse = JsonConvert.DeserializeObject<BetResponseJson>(responseString);

            Assert.Equal(System.Net.HttpStatusCode.Created, response?.StatusCode);
            Assert.Equal(expectedBetId, jsonResponse.BetId);
            Assert.Equal(expectedMatchId, jsonResponse.MatchId);
            Assert.Equal(expectedBetValue, jsonResponse.BetValue);
            Assert.Contains(expectedTeamName, jsonResponse.TeamName);
            Assert.Contains(expectedEmail, jsonResponse.Email);
        }


        [Trait("Category", "3. Implemente o microsserviço TryBets.Bets")]
        [Theory(DisplayName = "Será validado que não é possível cadastrar uma nova aposta")]
        [InlineData("/bet", 3, 15, 250, "maria@trybets.com", "Team not founded")]
        [InlineData("/bet", 3, 4, 250, "maria@trybets.com", "Team is not in this match")]
        [InlineData("/bet", 300, 1, 250, "maria@trybets.com", "Match not founded")]
        [InlineData("/bet", 1, 1, 250, "maria@trybets.com", "Match finished")]
        public async Task TestBetPostErrorRoute(string url, int pMatchId, int pTeamId, decimal pBetValue, string pEmail, string expectedMessage)
        {
            var claims = new ClaimsIdentity();
            claims.AddClaim(new Claim(ClaimTypes.Email, pEmail));

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
            var inputObj = new
            {
                MatchId = pMatchId,
                TeamId = pTeamId,
                BetValue = pBetValue
            };

            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(inputObj), System.Text.Encoding.UTF8, "application/json"));
            var responseString = await response.Content.ReadAsStringAsync();
            ErrorMessage jsonResponse = JsonConvert.DeserializeObject<ErrorMessage>(responseString);

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response?.StatusCode);
            Assert.Contains(expectedMessage, jsonResponse.message);
        }

        [Trait("Category", "3. Implemente o microsserviço TryBets.Bets")]
        [Theory(DisplayName = "Será validado que não é possível cadastrar uma nova aposta sem um e-mail cadastrado")]
        [InlineData("/bet", 3, 1, 250, "jose@trybets.com")]
        [InlineData("/bet", 3, 1, 250, "joao@trybets.com")]
        [InlineData("/bet", 3, 1, 250, "pedro@trybets.com")]
        public async Task TestBetPostWrongEmailRoute(string url, int pMatchId, int pTeamId, decimal pBetValue, string pEmail)
        {
            var claims = new ClaimsIdentity();
            claims.AddClaim(new Claim(ClaimTypes.Email, pEmail));

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
            var inputObj = new
            {
                MatchId = pMatchId,
                TeamId = pTeamId,
                BetValue = pBetValue
            };

            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(inputObj), System.Text.Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response?.StatusCode);
        }

        [Trait("Category", "3. Implemente o microsserviço TryBets.Bets")]
        [Theory(DisplayName = "Será validado que não é possível cadastrar uma nova aposta sem autorização")]
        [InlineData("/bet", 3, 1, 250)]
        [InlineData("/bet", 3, 1, 250)]
        [InlineData("/bet", 3, 1, 250)]
        public async Task TestBetPostUnauthorizedRoute(string url, int pMatchId, int pTeamId, decimal pBetValue)
        {
            var inputObj = new
            {
                MatchId = pMatchId,
                TeamId = pTeamId,
                BetValue = pBetValue
            };

            var response = await _client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(inputObj), System.Text.Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response?.StatusCode);
        }


        [Trait("Category", "3. Implemente o microsserviço TryBets.Bets")]
        [Theory(DisplayName = "Será validado que é possível consultar uma aposta")]
        [InlineData("/bet", 1, "maria@trybets.com", 1, 1, "Lions", 50, "maria@trybets.com")]
        [InlineData("/bet", 2, "maria@trybets.com", 2, 2, "Tigers", 70, "maria@trybets.com")]
        [InlineData("/bet", 3, "maria@trybets.com", 3, 3, "Dragons", 90, "maria@trybets.com")]
        public async Task TestBetGetRoute(string url, int pBetId, string pEmail, int expectedBetId, int expectedMatchId, string expectedTeamName, decimal expectedBetValue, string expectedEmail)
        {
            var claims = new ClaimsIdentity();
            claims.AddClaim(new Claim(ClaimTypes.Email, pEmail));

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

            var response = await _client.GetAsync(url+"/"+pBetId.ToString());
            var responseString = await response.Content.ReadAsStringAsync();
            BetResponseJson jsonResponse = JsonConvert.DeserializeObject<BetResponseJson>(responseString);

            Assert.Equal(System.Net.HttpStatusCode.OK, response?.StatusCode);
            Assert.Equal(expectedBetId, jsonResponse.BetId);
            Assert.Equal(expectedMatchId, jsonResponse.MatchId);
            Assert.Equal(expectedBetValue, jsonResponse.BetValue);
            Assert.Contains(expectedTeamName, jsonResponse.TeamName);
            Assert.Contains(expectedEmail, jsonResponse.Email);
        }

        [Trait("Category", "3. Implemente o microsserviço TryBets.Bets")]
        [Theory(DisplayName = "Será validado que não é possível consultar uma aposta de um e-mail errado")]
        [InlineData("/bet", 1, "joaquim@trybets.com")]
        [InlineData("/bet", 2, "marcelo@trybets.com")]
        [InlineData("/bet", 3, "francisco@trybets.com")]
        public async Task TestBetGetWrongEmailRoute(string url, int pBetId, string pEmail)
        {
            var claims = new ClaimsIdentity();
            claims.AddClaim(new Claim(ClaimTypes.Email, pEmail));

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

            var response = await _client.GetAsync(url+"/"+pBetId.ToString());
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response?.StatusCode);
            
        }


        [Trait("Category", "3. Implemente o microsserviço TryBets.Bets")]
        [Theory(DisplayName = "Será validado que não é possível consultar uma aposta sem autorização")]
        [InlineData("/bet", 1)]
        [InlineData("/bet", 2)]
        [InlineData("/bet", 3)]
        public async Task TestBetGetUnauthorizedRoute(string url, int pBetId)
        {
            var response = await _client.GetAsync(url+"/"+pBetId.ToString());
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response?.StatusCode);
        }

        [Trait("Category", "3. Implemente o microsserviço TryBets.Bets")]
        [Theory(DisplayName = "Será validado que é possível consultar uma aposta")]
        [InlineData("/bet", 100, "maria@trybets.com", "Bet not founded")]
        [InlineData("/bet", 101, "maria@trybets.com","Bet not founded")]
        [InlineData("/bet", 102, "maria@trybets.com", "Bet not founded")]
        public async Task TestBetGetNotFoundedRoute(string url, int pBetId, string pEmail, string expectedMessage)
        {
            var claims = new ClaimsIdentity();
            claims.AddClaim(new Claim(ClaimTypes.Email, pEmail));

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

            var response = await _client.GetAsync(url+"/"+pBetId.ToString());
            var responseString = await response.Content.ReadAsStringAsync();
            ErrorMessage jsonResponse = JsonConvert.DeserializeObject<ErrorMessage>(responseString);

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response?.StatusCode);
            Assert.Contains(expectedMessage, jsonResponse.message);

        }

        [Trait("Category", "3. Implemente o microsserviço TryBets.Bets")]
        [Theory(DisplayName = "Será validado que outras rotas não são acessíveis")]
        [InlineData("/team", "get", false)]
        [InlineData("/match/true", "get", false)]
        [InlineData("/match/false", "get", false)]
        [InlineData("/odd/1/1/10", "patch", false)]
        [InlineData("/user/login", "post", false)]
        [InlineData("/user/signup", "post", false)]
        [InlineData("/team", "get", true)]
        [InlineData("/match/true", "get", true)]
        [InlineData("/match/false", "get", true)]
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