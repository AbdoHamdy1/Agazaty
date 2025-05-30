using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Agazaty.Data.DTOs.AccountDTOs
{
    public class UpdateUserDTO
    {
        [Required]
        [RegularExpression(@"^[\u0621-\u063A\u0641-\u064A\u0622-\u0626\u0640]+$",
        ErrorMessage = "يجب أن يحتوي الاسم الأول على حروف عربية فقط بدون أرقام أو رموز أو مسافات")]
        public string FirstName { get; set; }
        [Required]
        [RegularExpression(@"^[\u0621-\u063A\u0641-\u064A\u0622-\u0626\u0640]+$",
        ErrorMessage = "يجب أن يحتوي الاسم الثاني على حروف عربية فقط بدون أرقام أو رموز أو مسافات")]
        public string SecondName { get; set; }
        [Required]
        [RegularExpression(@"^[\u0621-\u063A\u0641-\u064A\u0622-\u0626\u0640]+$",
        ErrorMessage = "يجب أن يحتوي الاسم الثالث على حروف عربية فقط بدون أرقام أو رموز أو مسافات")]
        public string ThirdName { get; set; }
        [Required]
        [RegularExpression(@"^[\u0621-\u063A\u0641-\u064A\u0622-\u0626\u0640]+$",
        ErrorMessage = "يجب أن يحتوي الاسم الرابع على حروف عربية فقط بدون أرقام أو رموز أو مسافات")]
        public string ForthName { get; set; }
        [Required]
        public string UserName { get; set; }
        [Required]
        [RegularExpression(@"^\d{14}$", ErrorMessage = "يجب أن يحتوي حقل الرقم القومي على 14 رقمًا بالضبط، دون مسافات أو أحرف أخرى.")]
        public string NationalID { get; set; }
        [Required]
        public DateTime HireDate { get; set; }
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب.")]
        [EmailAddress(ErrorMessage = "تنسيق البريد الإلكتروني غير صحيح.")]
        public string Email { get; set; }
        [Required]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "يجب أن يحتوي حقل رقم الهاتف على 11 رقماً بالضبط دون مسافات أو رموز أخرى.")]
        public string PhoneNumber { get; set; }
        [Required]
        public string Gender { get; set; }
        [Required]
        public DateTime DateOfBirth { get; set; }
        [Required]
        public int position { get; set; }
        public int NormalLeavesCount { get; set; }
        public int CasualLeavesCount { get; set; }
        public int NonChronicSickLeavesCount { get; set; }
        //public int HistoryNormalLeavesCount { get; set; }
        public Guid? Departement_ID { get; set; }
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
        // [DefaultValue(0)]
        [Required]
        public string RoleName { get; set; }
        public string? Street { get; set; }
        public string? governorate { get; set; }
        public string? State { get; set; }
        public bool Disability { get; set; }

    }
}