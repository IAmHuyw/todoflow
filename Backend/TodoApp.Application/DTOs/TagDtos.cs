namespace TodoApp.Application.DTOs;

public record TagDto(Guid Id, Guid UserId, string Name);

public class CreateTagRequest
{
    public string Name { get; set; } = string.Empty;
}
