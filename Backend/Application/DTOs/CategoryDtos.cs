namespace Application.DTOs;

public record CategoryDto(Guid Id, Guid UserId, string Name, string Color);

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#3b82f6";
}

public class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#3b82f6";
}
