using Alura.ListaLeitura.Modelos;
using Alura.ListaLeitura.Persistencia;
using Alura.WebAPI.Api.Modelos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Linq;

namespace Alura.ListaLeitura.Api.Controllers
{
    [Authorize]
    [ApiController]
    [ApiVersion("2.0")]
    [ApiExplorerSettings(GroupName = "v2")]
    [Route("api/v{version:apiVersion}/livros")]
    public class Livros2Controller : ControllerBase
    {
        private readonly IRepository<Livro> _repo;

        public Livros2Controller(IRepository<Livro> repository)
        {
            _repo = repository;
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Recupera uma coleção paginada de livros",
            Tags = new[] { "Livros" },
            Produces = new[] { "application/json", "application/xml" }
        )]
        [ProducesResponseType(statusCode: 200, Type = typeof(LivroPaginado))]
        [ProducesResponseType(statusCode: 500, Type = typeof(ErrorResponse))]
        [ProducesResponseType(statusCode: 404)]
        public IActionResult ListaDeLivros(
            [FromQuery][SwaggerParameter(Description = "Filtro indicado por autor, titulo, subtitulo e/ou nome da lista", Required = false)] LivroFiltro filtro,
            [FromQuery][SwaggerParameter(Description = "Parâmetro passado para ordenar a lista: autor, titulo, subtitulo e/ou lista", Required = false)] LivroOrdem ordem,
            [FromQuery][SwaggerParameter(Description = "Número de itens por página e qual a página", Required = false)] LivroPaginacao paginacao)
        {
            var livros = _repo.All
                .AplicaFiltro(filtro)
                .AplicaOrdem(ordem)
                .Select(l => l.ToApi())
                .ToLivroPaginado(paginacao);
            return Ok(livros);
        }

        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Recupera o livro identificado por seu {id}.",
            Tags = new[] {"Livros"},
            Produces = new[] {"application/json", "application/xml"}
        )]
        [ProducesResponseType(statusCode: 200, Type = typeof(LivroApi))]
        [ProducesResponseType(statusCode: 500, Type = typeof(ErrorResponse))]
        [ProducesResponseType(statusCode: 404)]
        public IActionResult Recuperar(
            [FromRoute]
            [SwaggerParameter("Id do livro a ser recuperado")] int id)
        {
            var model = _repo.Find(id);
            if (model == null)
            {
                return NotFound();
            }
            return Ok(model);
        }

        [HttpGet("capas/{id}")]
        [SwaggerOperation(
            Summary = "Recupera a capa do livro identificada por seu {id}.",
            Tags = new[] { "Livros" },
            Produces = new[] { "image/png" }
        )]
        [ProducesResponseType(statusCode: 200, Type = typeof(byte[]))]
        [ProducesResponseType(statusCode: 500, Type = typeof(ErrorResponse))]
        [ProducesResponseType(statusCode: 404)]
        public IActionResult ImagemCapa([FromRoute][SwaggerParameter("Id di livro do qual a capa deve ser recuperada")]int id)
        {
            byte[] img = _repo.All
                .Where(l => l.Id == id)
                .Select(l => l.ImagemCapa)
                .FirstOrDefault();
            if (img != null)
            {
                return File(img, "image/png");
            }
            return File("~/images/capas/capa-vazia.png", "image/png");
        }

        [HttpPost]
        [SwaggerOperation(
            Summary = "Registra novo livro na base.",
            Tags = new[] { "Livros" },
            Produces = new[] { "application/json", "application/json" }
        )]
        [ProducesResponseType(statusCode: 200, Type = typeof(Livro))]
        [ProducesResponseType(statusCode: 500, Type = typeof(ErrorResponse))]
        [ProducesResponseType(statusCode: 404)]
        public IActionResult Incluir([FromForm][SwaggerParameter("Um objeto contendo o autor, titulo, subtitulo e lista a ser adicionado")] LivroUpload model)
        {
            if (ModelState.IsValid)
            {
                var livro = model.ToLivro();
                _repo.Incluir(livro);
                
                var uri = Url.Action("Recuperar", new { id = livro.Id });
                return Created(uri, livro);
            }
            return BadRequest(ErrorResponse.FromModelState(ModelState));
        }

        [HttpPut]
        [SwaggerOperation(
            Summary = "Modifica o livro na base.",
            Tags = new[] { "Livros" }
        )]
        [ProducesResponseType(statusCode: 200)]
        [ProducesResponseType(statusCode: 500, Type = typeof(ErrorResponse))]
        [ProducesResponseType(statusCode: 404)]
        public IActionResult Alterar([FromForm][SwaggerParameter("Um objeto contendo o autor, titulo, subtitulo e lista a ser alterado")] LivroUpload model)
        {
            if (ModelState.IsValid)
            {
                var livro = model.ToLivro();
                if (model.Capa == null)
                {
                    livro.ImagemCapa = _repo.All
                        .Where(l => l.Id == livro.Id)
                        .Select(l => l.ImagemCapa)
                        .FirstOrDefault();
                }
                _repo.Alterar(livro);
                return Ok();
            }
            return BadRequest();
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Excluir o livro da base.",
            Tags = new[] { "Livros" }
        )]
        [ProducesResponseType(statusCode: 200)]
        [ProducesResponseType(statusCode: 500, Type = typeof(ErrorResponse))]
        [ProducesResponseType(statusCode: 404)]
        public IActionResult Remover([FromRoute][SwaggerParameter("Id do livro a ser excluído")]int id)
        {
            var model = _repo.Find(id);
            if (model == null)
            {
                return NotFound();
            }
            _repo.Excluir(model);
            return NoContent();
        }
    }
}
