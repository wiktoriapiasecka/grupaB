using System.Data;
using Microsoft.Data.SqlClient;
using grupaB.DTOs;

namespace grupaB.Repositories;

public class MakerRepository : IMakerRepository
{
    private readonly string _connectionString;

    public MakerRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")!;
    }

    public async Task<List<MakerDto>> GetMakersAsync(string? name)
{
    var query = @"
    SELECT 
        m.Id AS MakerId,
        m.Name AS MakerName,

        p.Id AS ProductId,
        p.Name AS ProductName,
        p.Description,
        p.StickerPrice AS StrickerPrice,

        pt.Id AS ProductTypeId,
        pt.Name AS ProductTypeName,

        v.Code AS VendorCode,
        v.Name AS VendorName,

        vp.Amount,
        vp.PricePerUnit

    FROM Makers m
    LEFT JOIN Products p ON p.MakerId = m.Id
    LEFT JOIN ProductTypes pt ON pt.Id = p.ProductTypeId
    LEFT JOIN VendorProducts vp ON vp.ProductId = p.Id
    LEFT JOIN Vendors v ON v.Code = vp.VendorCode

    WHERE (@name IS NULL OR m.Name LIKE '%' + @name + '%')
    ORDER BY m.Id, p.Id;
    ";

    using var con = new SqlConnection(_connectionString);
    await con.OpenAsync();

    using var cmd = new SqlCommand(query, con);
    cmd.Parameters.AddWithValue("@name", (object?)name ?? DBNull.Value);

    using var reader = await cmd.ExecuteReaderAsync();

    var makersDict = new Dictionary<int, MakerDto>();

    while (await reader.ReadAsync())
    {
        var makerId = reader.GetInt32(reader.GetOrdinal("MakerId"));

        if (!makersDict.TryGetValue(makerId, out var maker))
        {
            maker = new MakerDto
            {
                Id = makerId,
                Name = reader.GetString(reader.GetOrdinal("MakerName")),
                Products = []
            };

            makersDict.Add(makerId, maker);
        }

        if (reader.IsDBNull(reader.GetOrdinal("ProductId")))
            continue;

        var productId = reader.GetInt32(reader.GetOrdinal("ProductId"));

        var product = maker.Products.FirstOrDefault(p => p.Id == productId);

        if (product == null)
        {
            product = new ProductDto
            {
                Id = productId,
                Name = reader.GetString(reader.GetOrdinal("ProductName")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Description")),
                StrickerPrice = reader.GetDecimal(reader.GetOrdinal("StrickerPrice")),
                ProductType = new ProductTypeDto
                {
                    Id = reader.GetInt32(reader.GetOrdinal("ProductTypeId")),
                    Name = reader.GetString(reader.GetOrdinal("ProductTypeName"))
                },
                Vendors = []
            };

            maker.Products.Add(product);
        }

        if (reader.IsDBNull(reader.GetOrdinal("VendorCode")))
            continue;

        var vendor = new VendorDto
        {
            Code = reader.GetString(reader.GetOrdinal("VendorCode")),
            Name = reader.GetString(reader.GetOrdinal("VendorName")),
            Amount = reader.GetInt32(reader.GetOrdinal("Amount")),
            PricePerUnit = reader.GetDecimal(reader.GetOrdinal("PricePerUnit"))
        };

        product.Vendors.Add(vendor);
    }

    return makersDict.Values.ToList();
}

    public async Task<int> CreateMakerAsync(CreateMakerDto dto)
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        throw new ArgumentException("Maker name is required");

    using var con = new SqlConnection(_connectionString);
    await con.OpenAsync();

    using var transaction = con.BeginTransaction();

    try
    {
        var insertMakerQuery = @"
            INSERT INTO Makers (Name)
            OUTPUT INSERTED.Id
            VALUES (@Name);
        ";

        using var makerCmd = new SqlCommand(insertMakerQuery, con, transaction);
        makerCmd.Parameters.AddWithValue("@Name", dto.Name);

        var makerId = (int)await makerCmd.ExecuteScalarAsync();

        if (dto.Products != null)
        {
            foreach (var product in dto.Products)
            {
                var getProductTypeQuery = @"
                    SELECT Id 
                    FROM ProductTypes 
                    WHERE Name = @Type;
                ";

                using var typeCmd = new SqlCommand(getProductTypeQuery, con, transaction);
                typeCmd.Parameters.AddWithValue("@Type", product.Type);

                var typeIdObj = await typeCmd.ExecuteScalarAsync();

                if (typeIdObj == null)
                    throw new ArgumentException($"Product type '{product.Type}' does not exist");

                var productTypeId = (int)typeIdObj;

                var insertProductQuery = @"
                    INSERT INTO Products 
                    (Name, Description, StickerPrice, ProductTypeId, MakerId)
                    VALUES 
                    (@Name, @Description, @StickerPrice, @ProductTypeId, @MakerId);
                ";

                using var productCmd = new SqlCommand(insertProductQuery, con, transaction);
                productCmd.Parameters.AddWithValue("@Name", product.Name);
                productCmd.Parameters.AddWithValue("@Description", 
                    (object?)product.Description ?? DBNull.Value);
                productCmd.Parameters.AddWithValue("@StickerPrice", product.StrickerPrice);
                productCmd.Parameters.AddWithValue("@ProductTypeId", productTypeId);
                productCmd.Parameters.AddWithValue("@MakerId", makerId);

                await productCmd.ExecuteNonQueryAsync();
            }
        }

        transaction.Commit();

        return makerId;
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}
}