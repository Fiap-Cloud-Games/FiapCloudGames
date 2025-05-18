using System.Text;
using FCG.API.Models;
using FCG.Domain.Account;
using FCG.Domain.Interfaces;
using System.Security.Claims;
using FCG.Domain.Notifications;
using FCG.Application.Interfaces;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;

namespace FCG.Application.Services
{
    public class AuthenticateService : IAuthenticate
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ILogger<AuthenticateService> _logger;
        private readonly IUsuarioService _usuarioService;
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;

        public AuthenticateService(ILogger<AuthenticateService> logger, IConfiguration configuration, 
            IUsuarioService usuarioService, IUsuarioRepository usuarioRepository, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _configuration = configuration;
            _usuarioService = usuarioService;
            _usuarioRepository = usuarioRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<DomainNotificationsResult<UserTokenViewModel>> Login(LoginDTO loginDTO)
        {
            var resultNotifications = new DomainNotificationsResult<UserTokenViewModel>();

            try
            {
                _logger.LogInformation("Iniciando login.");

                var usuarioRecuperado = await _usuarioRepository.SelecionarPorEmail(loginDTO.EmailUsuario);

                if (usuarioRecuperado == null)
                {
                    resultNotifications.Notifications.Add("Usuário não localizado no banco de dados!");
                    return resultNotifications;
                }

                var usuarioAutenticado = await Autenticar(loginDTO.EmailUsuario, loginDTO.Password);

                if (!usuarioAutenticado)
                {
                    resultNotifications.Notifications.Add("Usuário não autenticado, credenciais inválidas!");
                    return resultNotifications;
                }

                var usuario = await _usuarioService.SelecionarPorEmail(loginDTO.EmailUsuario);

                var token = await GerarToken(usuario.Id, usuario.Email);

                resultNotifications.Result = new UserTokenViewModel
                {
                    Token = token
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer login.");
                resultNotifications.Notifications.Add($"Erro ao fazer login");
            }

            _logger.LogInformation("Login realizado com sucesso.");

            return resultNotifications;
        }

        public async Task<bool> Autenticar(string email, string senha)
        {
            var usuario = await _usuarioRepository.SelecionarPorEmail(email);

            if (usuario == null)
                return false;

            using var hmac = new HMACSHA512(usuario.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(senha));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != usuario.PasswordHash[i])
                    return false;
            }

            return true;
        }

        public async Task<string> GerarToken(int id, string email)
        {
            var usuario = await _usuarioRepository.SelecionarPorEmail(email);

            var claims = new[]
            {
                new Claim("id", id.ToString()),
                new Claim("email", email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, usuario.IsAdmin ? "Admin" : "Usuario")
            };

            var privateKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["jwt:secretKey"]));
            var credentials = new SigningCredentials(privateKey, SecurityAlgorithms.HmacSha256);
            var expiration = DateTime.UtcNow.AddDays(1);

            var token = new JwtSecurityToken(
                issuer: _configuration["jwt:issuer"],
                audience: _configuration["jwt:audience"],
                claims: claims,
                expires: expiration,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<DomainNotificationsResult<string>> RecuperarSenha(string email)
        {
            var resultNotifications = new DomainNotificationsResult<string>();

            try
            {
                _logger.LogInformation("Iniciando recuperação de senha.");

                var usuario = await _usuarioRepository.SelecionarPorEmail(email);

                if (usuario == null)
                {
                    resultNotifications.Notifications.Add("Usuário não localizado no banco de dados!");
                    return resultNotifications;
                }

                string novaSenha = usuario.GerarSenhaSeguraTemporaria();

                using var hmac = new HMACSHA512();
                byte[] passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(novaSenha));
                byte[] passwordSalt = hmac.Key;

                usuario.AlterarSenha(passwordHash, passwordSalt);

                _usuarioRepository.Alterar(usuario);
                await _unitOfWork.Commit();

                resultNotifications.Result = novaSenha.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao recuperar senha.");
                resultNotifications.Notifications.Add("Erro ao recuperar senha.");
            }

            _logger.LogInformation("Recuperação de senha finalizada.");

            return resultNotifications;
        }

    }
}
