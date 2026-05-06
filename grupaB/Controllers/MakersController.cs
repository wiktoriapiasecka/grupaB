using Microsoft.AspNetCore.Mvc;
using grupaB.DTOs;
using grupaB.Repositories;

namespace grupaB.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MakersController : ControllerBase
{
    private readonly IMakerRepository _repository;

    public MakersController(IMakerRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetMakers([FromQuery] string? name)
    {
        var result = await _repository.GetMakersAsync(name);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateMaker([FromBody] CreateMakerDto dto)
    {
        var id = await _repository.CreateMakerAsync(dto);
        return Created($"/api/makers/{id}", new { id });
    }
}