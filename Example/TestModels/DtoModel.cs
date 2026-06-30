using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Analyser.ForceSetProperties.TestModels
{
    [ForceSetProperties] //Placing it on a class, forces every constructor, method and expression to have all properties set.
    public class DtoModel
    {
        public DtoModel()
        {
            
        }

        [ForceSetProperties] //Placing the attribute on a class, forces this constructor to have all properties set.
        public DtoModel(DbModel db)
        {
            Name = db.Name;
            CreatedAt = db.CreatedAt;
            UpdatedAt = db.UpdatedAt;
        }

        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForceSetProperties] //Placing the attribute on a method, forces this method to have all properties set.
        public DtoModel FromFunction(string name, DateTime createdAt, DateTime updatedAt)
        {
            return new DtoModel
            {
                Name = name,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };
        }

         [ForceSetProperties<DtoModel>] //Placing the attrute while providing the type, this is in case we do not check the return type
        public DbModel SomeFunction(DbModel source, out DtoModel dto)
        {
            dto = new DtoModel
            {
                Name = source.Name,
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt
            };
            return source;
        }

        [ForceSetProperties] // can also be placed on expressions or lambda functions
        public static Expression<Func<DbModel, DtoModel>> FromExpression => db => new DtoModel
        {
            Name = db.Name,
            CreatedAt = db.CreatedAt,
            UpdatedAt = db.UpdatedAt
        };



    }
}
