using Agazaty.Data.DTOs.AccountDTOs;
using Agazaty.Data.Email.DTOs;
using Agazaty.Models;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;

namespace Agazaty.Data.Services.Interfaces
{
    public interface IAccountService
    {
        Task<IdentityResult> ChangePassword(ApplicationUser user, ChangePasswordDTO model);
        Task<ApplicationUser> FindById(string id);
        Task<ApplicationUser> FindByNationalId(string NationalId);
        Task<ApplicationUser> FindByName(string UserName);
        Task<ApplicationUser> FindByEmail(string email);
        Task<IEnumerable<ApplicationUser>> GetAllActiveUsers();
        Task<IEnumerable<ApplicationUser>> GetAllNonActiveUsers();
        Task<IEnumerable<ApplicationUser>> GetAllActiveAvailableCoworkers(ApplicationUser user);
        Task<IEnumerable<ApplicationUser>> GetAllUsersInRole(string RoleName);
        Task<IEnumerable<ApplicationUser>> GetAllUsersByDepartmentId(Guid DepartmentId);
        Task<IEnumerable<string>> GetAllRolesOfUser(ApplicationUser user);
        Task<string> GetFirstRole(ApplicationUser user);
        Task<string> GetDeanORSupervisor(string RoleName);
        Task TransferingUserNormalLeaveCountToNewSection(ApplicationUser user);
        Task InitalizeLeavesCountOfUser();
        //Task InitalizeLeavesCountOfUser(ApplicationUser user);
        Task<bool> IsInRoleAsync(ApplicationUser user, string RoleName);
        Task<IdentityResult> RemoveUserFromRole(ApplicationUser user, string RoleName);
        Task<IdentityResult> AddUserToRole(ApplicationUser user, string role);
        Task<bool> CheckPassword(ApplicationUser user, string passsword);
        Task<IdentityResult> Create(string RoleName, CreateUserDTO model);
        Task<AuthModel> GetTokenAsync(ApplicationUser user);
        Task<IdentityResult> Update(ApplicationUser user);
        Task<IdentityResult> Delete(ApplicationUser user);
        Task<ForgetPassResponse> ForgetPassword(SendOTPDTO DTO);
        Task<ForgetPassResponse> ResetPassword(ResetPasswordDTO DTO);
        Task<ForgetPassResponse> SendOTP(string email);
        Task<ForgetPassResponse> VerifyOtpAsync(string Email, string enteredOtp);
    }
}