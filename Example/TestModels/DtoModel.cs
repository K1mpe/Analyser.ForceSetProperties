using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Analyser.ForceSetProperties.TestModels
{
    public class DtoModel : ICloneable
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

        public DtoModel(DtoModel source)
        {
            Name = source.Name;
            CreatedAt = source.CreatedAt;
            UpdatedAt = source.UpdatedAt;
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
                UpdatedAt = updatedAt,
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

        [ForceSetProperties<DtoModel>]
        public object Clone()
        {
            return new DtoModel
            {
                Name = this.Name,
                CreatedAt = this.CreatedAt,
                UpdatedAt = this.UpdatedAt
            };
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

