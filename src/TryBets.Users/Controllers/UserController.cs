using System;
using Microsoft.AspNetCore.Mvc;
using TryBets.Users.Repository;
using TryBets.Users.Services;
using TryBets.Users.Models;
using TryBets.Users.DTO;

namespace TryBets.Users.Controllers;

[Route("[controller]")]
public class UserController : Controller
{
    private readonly IUserRepository _repository;
    public UserController(IUserRepository repository)
    {
        _repository = repository;
    }

    [HttpPost("signup")]
    public IActionResult Post([FromBody] User user)
    {
        try
        {
            var createUser = _repository.Post(user);
            var token = new TokenManager().Generate(createUser);

            var result = new AuthDTOResponse
            {
                Token = token
            };
            return StatusCode(201, result);
        }
        catch (Exception ex)
        {
            return StatusCode(400, ex.Message);
        }
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] AuthDTORequest login)
    {
        try
        {
            var user = _repository.Login(login);
            var token = new TokenManager().Generate(user);

            var result = new AuthDTOResponse
            {
                Token = token
            };

            return StatusCode(200, result);
        }
        catch (Exception ex)
        {
            return StatusCode(400, new { message = ex.Message });
        }
    }
}