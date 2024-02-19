using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using TryBets.Bets.Repository;
using TryBets.Bets.DTO;
using TryBets.Bets.Services;


namespace TryBets.Bets.Controllers;

[Route("[controller]")]
public class BetController : Controller
{
    private readonly IBetRepository _repository;
    private readonly IOddService _oddService;
    public BetController(IBetRepository repository, IOddService oddService)
    {
        _repository = repository;
        _oddService = oddService;
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Policy = "Client")]
    public async Task<IActionResult> Post([FromBody] BetDTORequest request)
    {
        try {
            var Token = HttpContext.User.Identity as ClaimsIdentity;

            var Email = Token?.Claims.FirstOrDefault(email => email.Type == ClaimTypes.Email)?.Value;

            return StatusCode(201, _repository.Post(request, Email));
        }
        catch (Exception ex)
        {
            return StatusCode(400, new { message = ex.Message });
        }
    }

    [HttpGet("{BetId}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Policy = "Client")]
    public IActionResult Get(int BetId)
    {
        try {
             var Token = HttpContext.User.Identity as ClaimsIdentity;

            var Email = Token?.Claims.FirstOrDefault(email => email.Type == ClaimTypes.Email)?.Value;

            return StatusCode(200, _repository.Get(BetId, Email));
        }
        catch (Exception ex)
        {
            return StatusCode(400, new { message = ex.Message });
        }
    }
}