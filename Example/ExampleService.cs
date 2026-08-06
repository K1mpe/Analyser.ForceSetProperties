using Analyser.ForceSetProperties;
using Analyser.ForceSetProperties.TestModels;
using Example.TestModels;
using System.Runtime.CompilerServices;

namespace Example;

public static class ExampleService
{
    [ForceSetProperties]
    public static DatabaseModel CreateClone(DatabaseModel model)
    {
        var clone = new DatabaseModel()
        {
            Id = model.Id,
            Name = model.Name,
            Description = model.Description,
            //IsActived = model.IsActived,
            //CreatedDate = model.CreatedDate,
            //UpdatedDate = model.UpdatedDate,
            IsDeleted = model.IsDeleted
        };
        return clone;
    }

    [ForceSetProperties]
    public static Dto MapToDto(DatabaseModel model)
    {
        return new Dto
        {
            Id = model.Id,
            Name = model.Name,
            IsActived = model.IsActived,
            Created = model.CreatedDate,
            Updated = model.UpdatedDate
        };
    }
    
}
