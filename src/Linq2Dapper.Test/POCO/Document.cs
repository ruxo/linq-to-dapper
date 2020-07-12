using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dapper.Contrib.Linq2Dapper.Test.POCO
{
    [Table("Document")]
    public class Document
    {
        [Key]
        public int DocumentId { get; set; }
        public int FieldId { get; set; }
        public string Name { get; set; }
        public DateTime? Created { get; set; }
    }
}