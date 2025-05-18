using AutoMapper;
using System.Text;
using FCG.API.Models;
using FCG.Domain.Entities;
using FCG.Application.DTOs;
using FCG.Domain.Interfaces;
using FCG.Domain.Notifications;
using FCG.Application.Interfaces;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace FCG.Application.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ILogger<UsuarioService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UsuarioService(IUsuarioRepository usuarioRepository, ILogger<UsuarioService> logger, 
            IUnitOfWork unitOfWork, IMapper mapper)
        {
            _usuarioRepository = usuarioRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<DomainNotificationsResult<UsuarioViewModel>> Incluir(UsuarioDTO usuarioDTO)
        {
            var resultNotifications = new DomainNotificationsResult<UsuarioViewModel>();
            
            try
            {
                _logger.LogInformation("Iniciando inclusão de usuário.");

                var usuarioRecuperado = await _usuarioRepository.SelecionarPorEmail(usuarioDTO.Email);

                if (usuarioRecuperado != null)
                {
                    resultNotifications.Notifications.Add("O usuário já existe no banco de dados!");
                    return resultNotifications;
                }

                var usuario = _mapper.Map<Usuario>(usuarioDTO);

                if (usuarioDTO.Password != null)
                {
                    usuario.ValidarSenhaSegura(usuarioDTO.Password);

                    using var hmac = new HMACSHA512();

                    byte[] passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(usuarioDTO.Password));

                    byte[] passwordSalt = hmac.Key;

                    usuario.AlterarSenha(passwordHash, passwordSalt);
                }

                await _usuarioRepository.Incluir(usuario);
                await _unitOfWork.Commit();

                resultNotifications.Result = _mapper.Map<UsuarioViewModel>(usuario);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao incluir usuário.");
                resultNotifications.Notifications.Add(ex.Message.ToString());
            }

            _logger.LogInformation("Usuário incluído com sucesso.");

            return resultNotifications;
        }

        public async Task<DomainNotificationsResult<UsuarioViewModel>> Alterar(UsuarioDTO usuarioDTO)
        {
            var resultNotifications = new DomainNotificationsResult<UsuarioViewModel>();

            try
            {
                var usuario = await _usuarioRepository.Selecionar(usuarioDTO.Id);

                if (usuario == null)
                {
                    resultNotifications.Notifications.Add("Usuário não encontrado.");
                    return resultNotifications;
                }

                _mapper.Map(usuarioDTO, usuario);

                if (usuarioDTO.Password != null)
                {
                    usuario.ValidarSenhaSegura(usuarioDTO.Password);

                    using var hmac = new HMACSHA512();

                    byte[] passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(usuarioDTO.Password));

                    byte[] passwordSalt = hmac.Key;

                    usuario.AlterarSenha(passwordHash, passwordSalt);
                }

                _usuarioRepository.Alterar(usuario);
                await _unitOfWork.Commit();

                resultNotifications.Result = _mapper.Map<UsuarioViewModel>(usuario);
            }
            catch (Exception ex)
            {
                resultNotifications.Notifications.Add($"Erro ao alterar o usuário: {ex.Message.ToString()}");
            }

            return resultNotifications;
        }

        public async Task<DomainNotificationsResult<UsuarioViewModel>> Excluir(int id)
        {
            var resultNotifications = new DomainNotificationsResult<UsuarioViewModel>();

            try
            {
                var usuario = await _usuarioRepository.Excluir(id);
                if (usuario == null)
                {
                    resultNotifications.Notifications.Add("Usuário não encontrado.");
                    return resultNotifications;
                }

                await _unitOfWork.Commit();

                resultNotifications.Result = _mapper.Map<UsuarioViewModel>(usuario);
            }
            catch (Exception ex)
            {
                resultNotifications.Add($"Erro ao excluir o usuário: {ex.InnerException.Message}");
            }

            return resultNotifications;
        }

        public async Task<DomainNotificationsResult<UsuarioViewModel>> Selecionar(int id)
        {
            var resultNotifications = new DomainNotificationsResult<UsuarioViewModel>();

            var usuario = await _usuarioRepository.Selecionar(id);

            if (usuario == null)
            {
                resultNotifications.Notifications.Add($"Usuario com o ID {id} não encontrado.");
                return resultNotifications;
            }

            resultNotifications.Result = _mapper.Map<UsuarioViewModel>(usuario);

            return resultNotifications;
        }

        public async Task<UsuarioViewModel> SelecionarPorEmail(string email)
        {
            var usuarioSelecionado = await _usuarioRepository.SelecionarPorEmail(email);
            return _mapper.Map<UsuarioViewModel>(usuarioSelecionado);
        }

        public async Task<DomainNotificationsResult<UsuarioViewModel>> SelecionarPorNomeEmail(string email, string nome)
        {
            var resultNotifications = new DomainNotificationsResult<UsuarioViewModel>();

            if (!string.IsNullOrWhiteSpace(nome))
            {
                var usuarioPorNome = await _usuarioRepository.SelecionarPorNome(nome);
                if (usuarioPorNome != null)
                {
                    resultNotifications.Result = _mapper.Map<UsuarioViewModel>(usuarioPorNome);
                    return resultNotifications;
                }
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                var usuarioPorEmail = await _usuarioRepository.SelecionarPorEmail(email);
                if (usuarioPorEmail != null)
                {
                    resultNotifications.Result = _mapper.Map<UsuarioViewModel>(usuarioPorEmail);
                    return resultNotifications;
                }
            }

            resultNotifications.Notifications.Add("Usuário não encontrado com os dados fornecidos.");
            return resultNotifications;
        }

    }
}
