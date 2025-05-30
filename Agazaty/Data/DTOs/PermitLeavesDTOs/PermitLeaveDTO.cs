using Agazaty.Models;
using System.ComponentModel.DataAnnotations;

namespace Agazaty.Data.DTOs.PermitLeavesDTOs
{
    public class PermitLeaveDTO
    {
        public Guid Id { get; set; }
        public double Hours { get; set; }
        [Required]
        public DateTime Date { get; set; }
        public string UserName { get; set; }
        public string UserID { get; set; }
        public string LeaveType { get; set; } = "تصريح";


    }
}
