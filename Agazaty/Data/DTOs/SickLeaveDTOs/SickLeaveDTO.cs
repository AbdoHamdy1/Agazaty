using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Agazaty.Data.DTOs.SickLeaveDTOs
{
    public class SickLeaveDTO
    {
        public Guid Id { get; set; } // Changed from int to Guid
        public string Disease { get; set; }
        public DateTime RequestDate { get; set; }
        public string? MedicalCommitteAddress { get; set; }
        public bool RespononseDoneForMedicalCommitte { get; set; }
        public bool ResponseDoneFinal { get; set; }
        public int Year { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Street { get; set; }
        public string? governorate { get; set; }
        public string? State { get; set; }
        public int? Days { get; set; }
        public bool Chronic { get; set; }
        public bool Certified { get; set; }
        public string PhoneNumber { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public string UserID { get; set; }
        public string General_ManagerID { get; set; }
        public bool GeneralManagerDecision { get; set; }
        public string GeneralManagerName { get; set; }
        public string DepartmentName { get; set; }

        public string LeaveType { get; set; } = "مرضي";

    }
}
