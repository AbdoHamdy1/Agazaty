using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Agazaty.Models
{
    public class SickLeave
    {
        public int Id { get; set; }
        public string Disease { get; set; }
        public DateTime RequestDate { get; set; }
        public string? MedicalCommitteAddress { get; set; }
        [DefaultValue(false)]
        public bool RespononseDoneForMedicalCommitte { get; set; }
        [DefaultValue(false)]
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
        public bool GeneralManagerDecision { get; set; } = false;
        [Required]
        public string General_ManagerID { get; set; }
        [Required]
        [ForeignKey("User")]
        public string UserID { get; set; }
        public ApplicationUser User { get; set; }
        //public string LeaveType { get; set; } = "مرضية";
    }
}
