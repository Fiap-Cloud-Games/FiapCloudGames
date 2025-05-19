using FCG.Domain.Entities;
using FCG.Domain.Validation;
using FCG.Domain.ValueObjects;

namespace FCG.Tests
{
    #region Arrange, Action, Assert - Preparação, Ação, Verificação
    //ARRANGE(Preparação)
    //Aqui você configura tudo o que é necessário para executar o teste
    //Cria os objetos, mocks, dados de entrada.
    //Prepara o ambiente para o teste.
    //Ex:
    //var usuario = new Usuario();
    //    var senhaValida = "Senha123!";

    //#################################################################################

    //    ACT(Ação)
    //Aqui você executa o comportamento que deseja testar.
    //É o momento em que você chama o método que está sendo testado.
    //Ex:
    //var excecao = Record.Exception(() => usuario.ValidarSenhaSegura(senhaValida));

    //#################################################################################

    //    ASSERT(Verificação)
    //Aqui você verifica se o resultado foi o esperado.
    //Usa Assert.True, Assert.Equal, Assert.Null, Assert.Throws, etc.
    //Ex:
    //Assert.Null(excecão);

    //#################################################################################

    //EXEMPLO COMPLETO:

    //[Fact]
    //    public void Senha_DeveSerValida_QuandoContemRequisitos()
    //    {
    //        // Arrange
    //        var usuario = new Usuario();
    //        var senhaValida = "Senha123!";

    //        // Act
    //        var excecao = Record.Exception(() => usuario.ValidarSenhaSegura(senhaValida));

    //        // Assert
    //        Assert.Null(excecao);
    //    }
    #endregion

    public class UsuarioDomainTests
    {
        [Fact]
        public void SenhaSeguraDevePassar()
        {
            var usuario = new Usuario();
            var senhaValida = "Senha123!";

            Exception ex = Record.Exception(() => usuario.ValidarSenhaSegura(senhaValida));

            Assert.Null(ex);
        }

        [Theory]
        [InlineData("1234567")]
        [InlineData("senhasemletraMaiuscula!")]
        [InlineData("SENHASEMNÚMEROS")]
        
        public void SenhaNaoSeguraDeveFalhar(string senhaInvalida)
        {
            var usuario = new Usuario();

            Assert.Throws<DomainExceptionValidation>(() => usuario.ValidarSenhaSegura(senhaInvalida));
        }

        [Fact]
        public void UsuarioPadraoNaoPodeSerAdmin()
        {
            var email = new Email("teste@fiap.com");
            var usuario = new Usuario(1, "Vinícius", email);

            Assert.False(usuario.IsAdmin);
        }

        [Fact]
        public void UsuarioDevePermitirPromoverAdmin()
        {
            var email = new Email("admin@fiap.com");
            var usuario = new Usuario(1, "Administrador", email);

            usuario.PromoverAdmin();

            Assert.True(usuario.IsAdmin);
        }
    }
}