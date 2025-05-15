using System.ComponentModel.DataAnnotations;

namespace Agazaty.Data.DTOs.CasualLeaveDTOs
{
    public class CasualLeaveDTO
    {
        public int Id { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        [Required]
        public string General_ManagerID { get; set; }
        public string GeneralManagerName { get; set; }
        public string DepartmentName {  get; set; }
        public bool LeaveStatus { get; set; }
        public int Year { get; set; }
        public string? Notes { get; set; }
        public string? Days { get; set; }
        public string UserName { get; set; }
        [Required]
        public string UserId { get; set; }
    }
}
