using FCG.API.Models;
using FCG.Domain.Account;
using Microsoft.AspNetCore.Mvc;

namespace FCG.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AutenticacaoController : BaseController
    {
        private readonly IAuthenticate _authenticateService;

        public AutenticacaoController(IAuthenticate authenticateService)
        {
            _authenticateService = authenticateService;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginDTO loginDTO)
        {
            var result = await _authenticateService.Login(loginDTO);
            return CreateIActionResult(result);
        }

        [HttpPost("RecuperarSenha")]
        public async Task<IActionResult> RecuperarSenha([FromQuery] string email)
        {
            var result = await _authenticateService.RecuperarSenha(email);
            return CreateIActionResult(result);
        }
    }
}
