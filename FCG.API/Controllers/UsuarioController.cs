using FCG.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using FCG.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace FCG.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : BaseController
    {
        private readonly IUsuarioService _usuarioService;

        public UsuarioController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpPost("Incluir")]
        public async Task<IActionResult> Incluir(UsuarioDTO usuarioDTO)
        {
            var result = await _usuarioService.Incluir(usuarioDTO);
            return CreateIActionResult(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("Alterar")]
        public async Task<IActionResult> Alterar(UsuarioDTO usuarioDTO)
        {
            #region Validação Adicional Usuário Admin
            var idUsuarioLogado = int.Parse(User.Claims.FirstOrDefault(c => c.Type == "id")?.Value);
            var usuarioLogado = await _usuarioService.Selecionar(idUsuarioLogado);

            if (!usuarioLogado.Result.IsAdmin)
                return Unauthorized("Acesso negado! Usuário não autorizado a realizar exclusão.");
            #endregion

            var result = await _usuarioService.Alterar(usuarioDTO);
            return CreateIActionResult(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("Excluir")]
        public async Task<IActionResult> Excluir(int id)
        {
            var result = await _usuarioService.Excluir(id);
            return CreateIActionResult(result);
        }

        [HttpGet("SelecionarPorId")]
        public async Task<IActionResult> Selecionar(int id)
        {
            var result = await _usuarioService.Selecionar(id);
            return CreateIActionResult(result);
        }

        [HttpGet("SelecionarPorNomeEmail")]
        public async Task<IActionResult> SelecionarPorNomeEmail([FromQuery] string email, [FromQuery] string nome)
        {
            var result = await _usuarioService.SelecionarPorNomeEmail(email, nome);
            return CreateIActionResult(result);
        }
    }
}
