using LearnBackend.Api.Content;
using Microsoft.AspNetCore.Mvc;

namespace LearnBackend.Api.Controllers;

/// <summary>
/// Controller style: a class whose public methods are "actions".
/// [ApiController] turns on API conveniences: automatic 400 responses for invalid models,
/// binding complex types from the body, ProblemDetails for errors.
/// [Route("api/[controller]")] → "api/glossary" ([controller] = class name minus "Controller").
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GlossaryController(ContentStore content) : ControllerBase
{
    // GET api/glossary?q=inject
    [HttpGet]
    public ActionResult<IEnumerable<GlossaryTerm>> GetAll([FromQuery] string? q, [FromQuery] string? category)
    {
        IEnumerable<GlossaryTerm> terms = content.Current.Glossary;

        if (!string.IsNullOrWhiteSpace(q))
        {
            var query = q.Trim();
            terms = terms.Where(t =>
                t.En.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Ar.Contains(query, StringComparison.Ordinal) ||
                t.Id.Contains(query, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(category))
            terms = terms.Where(t => string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase));

        return Ok(terms);
    }

    // GET api/glossary/dependency-injection
    [HttpGet("{id}")]
    public ActionResult<GlossaryTerm> GetById(string id) =>
        content.Current.GlossaryById.TryGetValue(id, out var term) ? Ok(term) : NotFound();
}
