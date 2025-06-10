using Agazaty.Data.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Agazaty.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [RegularExpression(@"^[\u0621-\u063A\u0641-\u064A\u0622-\u0626\u0640\s]+$",
        ErrorMessage = "يجب أن يحتوي الاسم الأول على حروف عربية ومسافات فقط بدون أرقام أو رموز")]
        public string FirstName { get; set; }
        [Required]
        [RegularExpression(@"^[\u0621-\u063A\u0641-\u064A\u0622-\u0626\u0640\s]+$",
        ErrorMessage = "يجب أن يحتوي الاسم الثاني على حروف عربية ومسافات فقط بدون أرقام أو رموز")]
        public string SecondName { get; set; }
        [Required]
        [RegularExpression(@"^[\u0621-\u063A\u0641-\u064A\u0622-\u0626\u0640\s]+$",
        ErrorMessage = "يجب أن يحتوي الاسم الثالث على حروف عربية ومسافات فقط بدون أرقام أو رموز")]
        public string ThirdName { get; set; }
        [Required]
        [RegularExpression(@"^[\u0621-\u063A\u0641-\u064A\u0622-\u0626\u0640\s]+$",
        ErrorMessage = "يجب أن يحتوي الاسم الرابع على حروف عربية ومسافات فقط بدون أرقام أو رموز")]
        public string ForthName { get; set; }
        [Required]
        public DateTime DateOfBirth { get; set; }
        [Required]
        public string Gender { get; set; }
        [Required]
        public DateTime HireDate { get; set; }
        [Required]
        [RegularExpression(@"^\d{14}$", ErrorMessage = "يجب أن يحتوي حقل الرقم القومي على 14 رقمًا بالضبط، دون مسافات أو أحرف أخرى.")]
        public string NationalID { get; set; }
        public int position { get; set; }
        [Required]
        public int NormalLeavesCount { get; set; }
        public int CasualLeavesCount { get; set; }
        public int NonChronicSickLeavesCount { get; set; }
        [ForeignKey("Department")]
        public Guid? Departement_ID { get; set; }
        public Department? Department { get; set; }
        public string? OTP { get; set; }
        public DateTime? OTPExpiry { get; set; }
        [DefaultValue(true)]
        public bool Active { get; set; }
        [DefaultValue(0)]
        public int NormalLeavesCount_47 { get; set; }
        [DefaultValue(0)]
        public int NormalLeavesCount_81Before3Years { get; set; }
        [DefaultValue(0)]
        public int NormalLeavesCount_81Before2Years { get; set; }
        [DefaultValue(0)]
        public int NormalLeavesCount_81Before1Years { get; set; }
        [DefaultValue(0)]
        public int HowManyDaysFrom81And47 { get; set; }
        [DefaultValue(0)]
        public int YearsOfWork { get; set; }
        public NormalLeaveSection? LeaveSection { get; set; }
        public CountsFromNormalLeaveTypes? Counts { get; set; }
        [DefaultValue(0)]
        public int TakenNormalLeavesCount_47 { get; set; }
        [DefaultValue(0)]
        public int TakenNormalLeavesCount_81Before3Years { get; set; }
        [DefaultValue(0)]
        public int TakenNormalLeavesCount_81Before2Years { get; set; }
        [DefaultValue(0)]
        public int TakenNormalLeavesCount_81Before1Years { get; set; }
        [DefaultValue(0)]
        public int TakenNormalLeavesCount { get; set; }
        public string? Street {  get; set; }
        public string? Governorate { get; set; }
        public string? State { get; set; }
        public bool Disability { get; set; }

        //[DefaultValue(true)]
        //public bool IntializationCheck { get; set; }
    }
}