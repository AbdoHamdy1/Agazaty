using Agazaty.Data.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Agazaty.Data.DTOs.AccountDTOs
{
    public class UserDTO
    {
        public string Id { get; set; }
        public string? FirstName { get; set; }
        public string? SecondName { get; set; }
        public string? ThirdName { get; set; }
        public string? ForthName { get; set; }
        public string? FullName { get; set; }
        public string? RoleName { get; set; }
        public string? DepartmentName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string UserName { get; set; }
        public int NonChronicSickLeavesCount { get; set; }
        public int NormalLeavesCount { get; set; }
        public int CasualLeavesCount { get; set; }
        public int NormalLeavesCount_47 { get; set; }
        public int NormalLeavesCount_81Before3Years { get; set; }
        public int NormalLeavesCount_81Before2Years { get; set; }
        public int NormalLeavesCount_81Before1Years { get; set; }
        public int HowManyDaysFrom81And47 { get; set; }
        public int YearsOfWork { get; set; }
        public NormalLeaveSection? LeaveSection { get; set; }
        public string? NationalID { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public DateTime HireDate { get; set; }
        public string? Street { get; set; }
        public string? governorate { get; set; }
        public string? State { get; set; }
        public bool Disability { get; set; }
        public bool IsDirectManager { get; set; }
        public string Gender { get; set; }
        public int position { get; set; }
    }
}