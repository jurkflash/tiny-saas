using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaaS.Application;
using SaaS.Infrastructure.Persistence;

namespace SaaS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TodoItemsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public TodoItemsController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var items = await _db.TodoItems
            .Select(i => new TodoItemDto(i.Id, i.Title, i.IsDone))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _db.TodoItems.FindAsync([id], cancellationToken);
        if (item is null)
            return NotFound();

        return Ok(new TodoItemDto(item.Id, item.Title, item.IsDone));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTodoItemRequest request, CancellationToken cancellationToken)
    {
        var item = new SaaS.Domain.TodoItem(_tenantContext.TenantId, request.Title);
        _db.TodoItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = new TodoItemDto(item.Id, item.Title, item.IsDone);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, dto);
    }

    public sealed record CreateTodoItemRequest(string Title);
    public sealed record TodoItemDto(Guid Id, string Title, bool IsDone);
}
