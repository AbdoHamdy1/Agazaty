using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Agazaty.Models
{
    public class PermitLeaveImage
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string ImageUrl { get; set; }
        [ForeignKey("PermitLeave")]
        public Guid LeaveId { get; set; }
        public PermitLeave PermitLeave { get; set; }
    }
}
