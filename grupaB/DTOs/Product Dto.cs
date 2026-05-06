namespace grupaB.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal StrickerPrice { get; set; }
    public ProductTypeDto ProductType { get; set; } = null!;
    public List<VendorDto> Vendors { get; set; } = [];
}