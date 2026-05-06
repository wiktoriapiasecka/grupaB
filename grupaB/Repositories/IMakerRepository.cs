using grupaB.DTOs;

namespace grupaB.Repositories;

public interface IMakerRepository
{
    Task<List<MakerDto>> GetMakersAsync(string? name);
    Task<int> CreateMakerAsync(CreateMakerDto dto);
}