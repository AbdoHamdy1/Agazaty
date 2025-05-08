using System.ComponentModel.DataAnnotations;

namespace Agazaty.Models
{
    public class AuthModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string DepartmentName { get; set; }
        public int  SickLeavesCount { get; set; }
        public int NormalLeavesCount { get; set; }
        public int CasualLeavesCount { get; set; }
        public int PermitLeavesCount { get; set; }
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public int Position {  get; set; }
        public string Message { get; set; }
        public bool IsAuthenticated { get; set; }
        public string Token { get; set; }
        public bool IsDirectManager { get; set; }

        //public DateTime ExpiresOn { get; set; }
    }
}
