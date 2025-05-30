using System.ComponentModel.DataAnnotations;

namespace Agazaty.Models
{
    public class Holiday
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public DateTime Date { get; set; }

    }
}
