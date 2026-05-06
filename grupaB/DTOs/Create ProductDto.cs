namespace grupaB.DTOs;

public class CreateProductDto { public string Name { get; set; } = null!; public string? Description { get; set; } public decimal StrickerPrice { get; set; } public string Type { get; set; } = null!; }