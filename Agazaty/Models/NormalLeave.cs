using NormalLeaveTask.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static NuGet.Packaging.PackagingConstants;

namespace Agazaty.Models
{
    public class NormalLeave
    {
        [Required]
        public int ID { get; set; }
        [ForeignKey("User")]
        [Required]
        public string UserID { get; set; }
        [Required]
        public string Direct_ManagerID { get; set; }
        [Required]
        public string General_ManagerID { get; set; }
        [Required]
        public string Coworker_ID { get; set; }
        [Required]
        public DateTime RequestDate { get; set; }
        [Required]
        public DateTime StartDate { get; set; }
        [Required]
        public DateTime EndDate { get; set; }
        public int Days { get; set; }
        public string? NotesFromEmployee { get; set; }
        public int Year { get; set; }
        public bool Accepted { get; set; } = false;
        public bool DirectManager_Decision { get; set; } = false;
        public bool GeneralManager_Decision { get; set; } = false;
        public bool CoWorker_Decision { get; set; } = false;
        public bool ResponseDone { get; set; } = false;
        public string? DisapproveReasonOfGeneral_Manager { get; set; }
        public string? DisapproveReasonOfDirect_Manager { get; set; }
        public ApplicationUser User { get; set; }
        public LeaveStatus LeaveStatus { get; set; }
        public Holder Holder { get; set; }
        public RejectedBy RejectedBy { get; set; }
        //public string LeaveType { get; set; } = "اعتيادية";

    }
}
