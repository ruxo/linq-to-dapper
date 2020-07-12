using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dapper.Contrib.Linq2Dapper.Test.POCO
{
    [Table("Field")]
    public class Field
    {
        [Key]
        public int FieldId { get; set; }
        public int DataTypeId { get; set; }
        public string Name { get; set; }
    }
}