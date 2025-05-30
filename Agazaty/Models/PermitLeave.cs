using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Agazaty.Models
{
    public class PermitLeave
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public double Hours { get; set; }
        [Required]
        public DateTime Date { get; set; }
        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; }
        public PermitLeaveImage? PermitLeaveImage { get; set; }
        public ApplicationUser User { get; set; }

        //public string LeaveType { get; set; } = "تصريح";

    }
}
