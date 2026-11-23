using Microsoft.AspNetCore.Http;

namespace acaigalatico.Web.Services
{
    /// <summary>
    /// Centraliza o upload de imagens do site (fotos de perfil, produtos e conteúdo).
    /// Aceita só JPG e PNG (confere a extensão, o tipo informado e os primeiros bytes
    /// do arquivo), limita o tamanho a 5 MB e gera o nome do arquivo no servidor
    /// (GUID + extensão), ignorando o nome enviado pelo navegador. Isso evita path
    /// traversal e o upload de arquivos .html/.js disfarçados de imagem.
    /// </summary>
    public static class UploadImagem
    {
        public const long TamanhoMaximoBytes = 5 * 1024 * 1024; // 5 MB

        private static readonly string[] ExtensoesPermitidas = { ".jpg", ".jpeg", ".png" };
        private static readonly string[] TiposPermitidos = { "image/jpeg", "image/png" };

        // Assinatura (primeiros bytes) de cada formato
        private static readonly byte[] AssinaturaJpg = { 0xFF, 0xD8, 0xFF };
        private static readonly byte[] AssinaturaPng = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        /// <summary>Devolve a mensagem de erro, ou null se a imagem for válida.</summary>
        public static string? Validar(IFormFile? arquivo)
        {
            if (arquivo == null || arquivo.Length == 0)
                return "Nenhuma imagem foi enviada.";

            if (arquivo.Length > TamanhoMaximoBytes)
                return "A imagem deve ter no máximo 5 MB.";

            var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
            if (!ExtensoesPermitidas.Contains(extensao) || !TiposPermitidos.Contains(arquivo.ContentType?.ToLowerInvariant()))
                return "Formato não permitido. Envie uma imagem JPG ou PNG.";

            if (!AssinaturaConfere(arquivo, extensao))
                return "O arquivo não é uma imagem JPG ou PNG válida.";

            return null;
        }

        /// <summary>Confere se os primeiros bytes do arquivo batem com o formato da extensão.</summary>
        private static bool AssinaturaConfere(IFormFile arquivo, string extensao)
        {
            var esperado = extensao == ".png" ? AssinaturaPng : AssinaturaJpg;
            var inicio = new byte[esperado.Length];

            using var stream = arquivo.OpenReadStream();
            var lidos = stream.Read(inicio, 0, inicio.Length);

            return lidos == esperado.Length && inicio.SequenceEqual(esperado);
        }

        /// <summary>
        /// Salva a imagem em wwwroot/uploads/{subpasta} e devolve a URL pública.
        /// Lança InvalidOperationException se a imagem for inválida.
        /// </summary>
        public static async Task<string> SalvarAsync(IFormFile arquivo, string webRootPath, string subpasta)
        {
            var erro = Validar(arquivo);
            if (erro != null)
                throw new InvalidOperationException(erro);

            var pasta = Path.Combine(webRootPath, "uploads", subpasta);
            Directory.CreateDirectory(pasta);

            var nomeArquivo = $"{Guid.NewGuid()}{Path.GetExtension(arquivo.FileName).ToLowerInvariant()}";
            var caminho = Path.Combine(pasta, nomeArquivo);

            using (var stream = new FileStream(caminho, FileMode.Create))
            {
                await arquivo.CopyToAsync(stream);
            }

            return $"/uploads/{subpasta}/{nomeArquivo}";
        }
    }
}
