using Analyser.ForceSetProperties;
using Analyser.ForceSetProperties.TestModels;
using Example.TestModels;
using System.Runtime.CompilerServices;

namespace Example;

public static class ExampleService
{
    [ForceSetProperties<DatabaseModel>]
    public static List<DatabaseModel> CreateClone(DatabaseModel model)
    {
        var clone = new DatabaseModel()
        {
            Id = model.Id,
            Name = model.Name,
            Description = model.Description,
            IsActived = model.IsActived,
            CreatedDate = model.CreatedDate,
            UpdatedDate = model.UpdatedDate,
            IsDeleted = model.IsDeleted
        };
        return [clone];
    }

    [ForceSetProperties]
    public static InheritedDto MapToDto(DatabaseModel model)
    {
        return new InheritedDto
        {
            Id = model.Id,
            Name = model.Name,
            IsActived = model.IsActived,
            Created = model.CreatedDate,
            Updated = model.UpdatedDate,
            IsActive = true
        };
    }

    [ForceSetProperties]
    public static TestRecord Record(string name, string description)
    {
        return new(name, 10)
        {
            Description = description
        };
    }
}

public record TestRecord(string Name, int Age)
{
    public string? Description { get; set; }
}