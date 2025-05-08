using Agazaty.Data.Base;
using Agazaty.Data.DTOs.AccountDTOs;
using Agazaty.Data.Email;
using Agazaty.Data.Email.DTOs;
using Agazaty.Data.Enums;
using Agazaty.Data.Services.Interfaces;
using Agazaty.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;

namespace Agazaty.Data.Services.Implementation
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEntityBaseRepository<Department> _baseDepartment;
        private readonly AppDbContext _appDbContext;
        private readonly IMapper _mapper;
        private readonly JWT _jwt;
        private readonly IEmailService _EmailService;
        private readonly IServiceProvider _serviceProvider;
        public AccountService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, AppDbContext appDbContext, IMapper mapper, IOptions<JWT> jwt, IEntityBaseRepository<Department> baseDepartment, IEmailService EmailService, IServiceProvider serviceProvider)
        {
            _userManager = userManager;
            _appDbContext = appDbContext;
            _mapper = mapper;
            _jwt = jwt.Value;
            _roleManager = roleManager;
            _baseDepartment = baseDepartment;
            _EmailService = EmailService;
            _serviceProvider = serviceProvider;
        }
        public async Task<IdentityResult> ChangePassword(ApplicationUser user, ChangePasswordDTO model)
        {
            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            return result;
        }
        public async Task<ApplicationUser> FindById(string UserId)
        {
            var user = await _userManager.FindByIdAsync(UserId);
            //if (user != null)
            //{
            //    if (user.Active == true) return user;
            //}
            return user;
        }
        public async Task<ApplicationUser> FindByNationalId(string NationalId)
        {
            return await _appDbContext.Users.Where(u => u.NationalID == NationalId).FirstOrDefaultAsync();
        }
        public async Task<ApplicationUser> FindByName(string UserName)
        {
            var user = await _userManager.FindByNameAsync(UserName);
            //if (user != null)
            //{
            //    if (user.Active == true) return user;
            //    return null;
            //}
            return user;
        }
        public async Task<ApplicationUser> FindByEmail(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            //if (user != null)
            //{
            //    if (user.Active == true) return user;
            //    return null;
            //}
            return user;
        }
        public async Task<IEnumerable<ApplicationUser>> GetAllActiveUsers()
        {
            return await _appDbContext.Users.Where(u => u.Active == true).ToListAsync();/*.AsNoTracking();*/
        }
        public async Task<IEnumerable<ApplicationUser>> GetAllNonActiveUsers()
        {
            return await _appDbContext.Users.Where(u => u.Active == false).ToListAsync();/*.AsNoTracking();*/
        }
        public async Task<IEnumerable<ApplicationUser>> GetAllNotActiveUsers()
        {
            return await _appDbContext.Users.Where(u => u.Active == false).ToListAsync();/*.AsNoTracking();*/
        }
        public async Task<IEnumerable<ApplicationUser>> GetAllActiveAvailableCoworkers(ApplicationUser user)
        {
            var role = (await GetAllRolesOfUser(user)).FirstOrDefault();
            if (role == "عميد الكلية")
            {
                var coworkers = (await GetAllUsersInRole("هيئة تدريس"))
                 .Where(u => u.position == 2 && u.Active == true && u.Id!= user.Id);

                var supervisor = (await GetAllUsersInRole("أمين الكلية"))
                                .Where(u => u.Active == true && u.Id != user.Id)
                                .FirstOrDefault();
                // هنا خزنا نتيجة الـ Append
                var allUsers = coworkers.Append(supervisor); // supervisor حتى لو null هيتضاف

                return allUsers;
            }
            else if (role == "أمين الكلية")
            {
                var coworkers = (await GetAllUsersInRole("موظف"))
                .Where(u => u.position == 2 && u.Active == true && u.Id != user.Id);

                var supervisor = (await GetAllUsersInRole("عميد الكلية"))
                                .Where(u => u.Active == true && u.Id != user.Id)
                                .FirstOrDefault();

                // هنا خزنا نتيجة الـ Append
                var allUsers = coworkers.Append(supervisor); // supervisor حتى لو null هيتضاف

                return allUsers;
            }
            else if (role == "مدير الموارد البشرية")
            {
                var coworkers = (await GetAllUsersInRole("موظف"))
                .Where(u => u.position == 2 && u.Active == true && u.Id != user.Id);

                var supervisor = (await GetAllUsersInRole("أمين الكلية"))
                                .Where(u => u.Active == true && u.Id != user.Id)
                                .FirstOrDefault();

                // هنا خزنا نتيجة الـ Append
                var allUsers = coworkers.Append(supervisor); // supervisor حتى لو null هيتضاف

                return allUsers;

            }
            else
            {
                var coworkers = (await GetAllUsersInRole(role)).Where(u => u.position <= user.position && u.Active == true && u.Id != user.Id);
                if (role == "هيئة تدريس")
                {
                    var supervisor = (await GetAllUsersInRole("عميد الكلية"))
                               .Where(u => u.Active == true && u.Id != user.Id)
                               .FirstOrDefault();
                    var allUsers = coworkers.Append(supervisor); // supervisor حتى لو null هيتضاف
                    return allUsers;
                }
                else if (role == "موظف")
                {
                    var supervisor = (await GetAllUsersInRole("أمين الكلية"))
                                .Where(u => u.Active == true && u.Id != user.Id)
                                .FirstOrDefault();
                    var HR = (await GetAllUsersInRole("مدير الموارد البشرية"))
                                .Where(u => u.Active == true && u.Id != user.Id)
                                .FirstOrDefault();
                    var allUsers = coworkers.Append(supervisor); // supervisor حتى لو null هيتضاف
                    allUsers = coworkers.Append(HR); // supervisor حتى لو null هيتضاف
                    return allUsers;
                }
                return coworkers;
            }
        }
        public async Task<IEnumerable<ApplicationUser>> GetAllUsersInRole(string RoleName)
        {
            return (await _userManager.GetUsersInRoleAsync(RoleName)).Where(u => u.Active == true);
        }
        public async Task<IEnumerable<ApplicationUser>> GetAllUsersByDepartmentId(int DepartmentId)
        {
            return await _appDbContext.Users.Where(u => u.Departement_ID == DepartmentId && u.Active == true).ToListAsync();
        }
        public async Task<IEnumerable<string>> GetAllRolesOfUser(ApplicationUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }
        public async Task<string> GetFirstRole(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return roles.FirstOrDefault();
        }
        public async Task<string> GetDeanORSupervisor(string RoleName)
        {
            var res = (await _userManager.GetUsersInRoleAsync(RoleName)).Where(u => u.Active == true);
            if (res.Any())
            {
                return res.First().Id;
            }
            return null;
        }
        public async Task TransferingUserNormalLeaveCountToNewSection(ApplicationUser user)
        {
            var hireDuration = (DateTime.UtcNow.Date - user.HireDate).TotalDays;
            var ageInYears = DateTime.UtcNow.Year - user.DateOfBirth.Year;
            if (DateTime.UtcNow.Month < user.DateOfBirth.Month ||
               (DateTime.UtcNow.Month == user.DateOfBirth.Month && DateTime.UtcNow.Day < user.DateOfBirth.Day))
            {
                ageInYears--;
            }
            if (ageInYears >= 50)
            {
                if (user.LeaveSection != NormalLeaveSection.FiftyAge)
                {
                    int NumberOfDaysToBeAdded = 0;
                    if (user.LeaveSection == NormalLeaveSection.SixMonths) NumberOfDaysToBeAdded = 60 - 15;
                    else if (user.LeaveSection == NormalLeaveSection.OneYear) NumberOfDaysToBeAdded = 60 - 36;
                    else if (user.LeaveSection == NormalLeaveSection.TenYears) NumberOfDaysToBeAdded = 60 - 45;

                    DateTime today = DateTime.Today;
                    DateTime nextJuly1 = new DateTime(today.Year, 7, 1);
                    // If today is already past July 1st, use next year's July 1st
                    if (today >= nextJuly1) nextJuly1 = new DateTime(today.Year + 1, 7, 1);
                    int monthsDifference = ((nextJuly1.Year - today.Year) * 12) + nextJuly1.Month - today.Month;

                    NumberOfDaysToBeAdded = (int)Math.Ceiling((NumberOfDaysToBeAdded * 1.0) / monthsDifference);

                    user.NormalLeavesCount += NumberOfDaysToBeAdded * monthsDifference;

                    user.LeaveSection = NormalLeaveSection.FiftyAge;
                    await Update(user);
                }
            }
            else
            {
                //DateTime today = DateTime.Today;
                //int monthsDifference = ((user.HireDate.Year - today.Year) * 12) + user.HireDate.Month - today.Month;

                if (hireDuration >= 30 * 6 && user.LeaveSection == NormalLeaveSection.NoSection)
                {
                    user.CasualLeavesCount = 3;
                    user.NormalLeavesCount += 15;
                    user.LeaveSection = NormalLeaveSection.SixMonths;
                    await Update(user);
                }

            }
        }
        public async Task InitalizeLeavesCountOfUser()
        {
            using (var scope = _serviceProvider.CreateScope()) // To resolve dependencies in BackgroundService
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                var users = userManager.Users.ToList(); // Fetch all users
                foreach (var user in users)
                {
                    user.NormalLeavesCount_81Before3Years = user.NormalLeavesCount_81Before2Years;
                    user.NormalLeavesCount_81Before2Years = user.NormalLeavesCount_81Before1Years;
                    user.NormalLeavesCount_81Before1Years = 0;
                    user.CasualLeavesCount = 7;
                    user.HowManyDaysFrom81And47 = 0;
                    user.YearsOfWork++;
                    if (user.YearsOfWork >= 1) { user.NormalLeavesCount = 36; user.LeaveSection = NormalLeaveSection.OneYear; }
                    if (user.YearsOfWork >= 10) { user.NormalLeavesCount = 45; user.LeaveSection = NormalLeaveSection.TenYears; }
                    if (user.LeaveSection == NormalLeaveSection.FiftyAge) user.NormalLeavesCount = 60;
                    if (user.LeaveSection == NormalLeaveSection.DisabilityEmployee) user.NormalLeavesCount = 60;
                    //var hireDuration = (DateTime.UtcNow.Date - user.HireDate).TotalDays;
                    //var age = (user.DateOfBirth - DateTime.UtcNow.Date).TotalDays;
                    //if(age >= 365*50) user.NormalLeavesCount = 60;                       // 50 years age
                    //else
                    //{
                    //    if (hireDuration >= 30 * 6) user.NormalLeavesCount = 15;         // 6 months
                    //    else if (hireDuration >= 28 * 12) user.NormalLeavesCount = 36;   // 1 year
                    //    else if (hireDuration >= 364 * 10) user.NormalLeavesCount = 45;  // 10 years
                    //}
                    await userManager.UpdateAsync(user);
                }
            }
        }
        public async Task<bool> IsInRoleAsync(ApplicationUser user, string RoleName)
        {
            return await _userManager.IsInRoleAsync(user, RoleName);
        }
        public async Task<IdentityResult> RemoveUserFromRole(ApplicationUser user, string RoleName)
        {
            return await _userManager.RemoveFromRoleAsync(user, RoleName);
        }
        public async Task<IdentityResult> AddUserToRole(ApplicationUser user, string role)
        {
            var result = await _userManager.AddToRoleAsync(user, role);
            return result;
        }
        public async Task<bool> CheckPassword(ApplicationUser user, string passsword)
        {
            return await _userManager.CheckPasswordAsync(user, passsword);
        }
        public async Task<IdentityResult> Create(string RoleName, CreateUserDTO model)
        {
            var user = _mapper.Map<ApplicationUser>(model);
            user.Active = true;
            var init = user.HireDate;
            var today = DateTime.UtcNow;
            user.YearsOfWork = today.Year - init.Year;
            if (init.Month < 7)
            {
                user.YearsOfWork++;
            }
            if (today.Month < 7)
            {
                user.YearsOfWork--;
            }
            if (model.Disability == true)
            {
                user.LeaveSection = NormalLeaveSection.DisabilityEmployee;
            }
            else
            {   
                //var hireDuration = (DateTime.UtcNow.Date - user.HireDate).TotalDays;
                // validation on leaves count from front
                var ageInYears = DateTime.UtcNow.Year - user.DateOfBirth.Year;
                if (DateTime.UtcNow.Month < user.DateOfBirth.Month ||
                   (DateTime.UtcNow.Month == user.DateOfBirth.Month && DateTime.UtcNow.Day < user.DateOfBirth.Day))
                {
                    ageInYears--;
                }
                if (ageInYears >= 50) user.LeaveSection = NormalLeaveSection.FiftyAge;
                else
                {
                    if (user.YearsOfWork == 0) user.LeaveSection = NormalLeaveSection.NoSection;
                    if (user.YearsOfWork >= 1) user.LeaveSection = NormalLeaveSection.OneYear;
                    if (user.YearsOfWork >= 10) user.LeaveSection = NormalLeaveSection.TenYears;
                }
            }

            var result = await _userManager.CreateAsync(user, model.Password);
            return result;
        }
        private async Task<JwtSecurityToken> CreateJwtToken(ApplicationUser user)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            var roleClaims = new List<Claim>();

            foreach (var role in roles)
                roleClaims.Add(new Claim("roles", role));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
                //new Claim("UserID", user.Id)
            }
            .Union(userClaims)
            .Union(roleClaims);

            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_jwt.DurationInHours),
                signingCredentials: signingCredentials);

            return jwtSecurityToken;
        }
        public async Task<AuthModel> GetTokenAsync(ApplicationUser user)
        {
            var rolesList = await _userManager.GetRolesAsync(user);
            var roleObject = await _roleManager.FindByNameAsync(rolesList.FirstOrDefault());
            var jwtSecurityToken = await CreateJwtToken(user);
            string tokenString = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
            //user.ActiveToken = tokenString;
            await Update(user);

            var authmodell = new AuthModel()
            {
                IsAuthenticated = true,
                Token = tokenString,
                //ExpiresOn = jwtSecurityToken.ValidTo,
                RoleName = roleObject.Name,
                RoleId = roleObject.Id,
                //Roles = rolesList.ToList(),
                FullName = $"{user.ForthName} {user.ThirdName} {user.SecondName} {user.FirstName}",
                NormalLeavesCount = user.NormalLeavesCount,
                CasualLeavesCount = user.CasualLeavesCount,
                SickLeavesCount = user.NonChronicSickLeavesCount,
                Email = user.Email,
                Id = user.Id,
                Position=user.position,
            };
            if (user.Departement_ID != null)
            {
                var dept = await _baseDepartment.Get(d => d.Id == user.Departement_ID);
                authmodell.DepartmentName = dept.Name;
            }
            return authmodell;
        }
        public async Task<IdentityResult> Update(ApplicationUser user)
        {
            var init = user.HireDate;
            var today = DateTime.UtcNow;
            user.YearsOfWork = today.Year - init.Year;
            if (init.Month < 7)
            {
                user.YearsOfWork++;
            }
            if (today.Month < 7)
            {
                user.YearsOfWork--;
            }
            return await _userManager.UpdateAsync(user);
        }
        public async Task<IdentityResult> Delete(ApplicationUser user)
        {
            return await _userManager.DeleteAsync(user);
        }
        public async Task<ForgetPassResponse> ForgetPassword(SendOTPDTO DTO)
        {
            var Account = await FindByEmail(DTO.Email);
            if (Account == null)
            {
                return new ForgetPassResponse { Message = "لم يتم العثور على المستخدم." };
            }
            //if (Account.UserName != DTO.UserName)
            //{
            //    return new AuthModel { Message = "Invalid Request" };
            //}
            return await SendOTP(DTO.Email);
        }
        public async Task<ForgetPassResponse> ResetPassword(ResetPasswordDTO DTO)
        {
            var Account = await FindByEmail(DTO.email);
            if (Account == null)
            {
                return new ForgetPassResponse { Message = "لم يتم العثور على المستخدم." };
            }
            //if (/*DTO.token == null || DTO.token != Account.OTP ||*/ DateTime.UtcNow > Account.OTPExpiry)
            //{
            //    return new AuthModel { Message = "Invalid OTP" };
            //}
            string NewHashedPassword = _userManager.PasswordHasher.HashPassword(Account, DTO.newPassword);
            Account.PasswordHash = NewHashedPassword;
            Account.OTP = null;
            Account.OTPExpiry = null;
            var result = await _userManager.UpdateAsync(Account);
            if (!result.Succeeded)
            {
                var errors = string.Empty;
                foreach (var error in result.Errors)
                {
                    errors += $"{error.Description} , ";
                }
                return new ForgetPassResponse { Message = errors };
            }
            return new ForgetPassResponse { Email = Account.Email, Message = "تم تغيير كلمة المرور بنجاح.", IsAuthenticated =true };
            //var result=await _UserManager.ResetPasswordAsync(user, token, NewPassword);
        }
        private string GenerateOTP()
        {
            Random random = new Random();
            string randomNumber = random.Next(0, 1000000).ToString("D6");
            return randomNumber;
        }
        public async Task<ForgetPassResponse> SendOTP(string email)
        {
            var account = await FindByEmail(email);
            if (account == null)
            {
                return new ForgetPassResponse { Message = "الحساب غير موجود." };
            }
            if (account.OTP != null && account.OTPExpiry > DateTime.UtcNow)
            {
                return new ForgetPassResponse { Email = account.Email, Message = "تم إرسال الرقم السري بالفعل."};
            }
            string OTP = GenerateOTP();
            account.OTP = OTP;
            account.OTPExpiry = DateTime.UtcNow.AddMinutes(5);
            await _userManager.UpdateAsync(account);
            var emailrequest = new EmailRequest
            {
                Email = account.Email,
                Subject = "الرقم السري الخاص بك",
                Body = $"الرقم السري الخاص بك هو: {OTP}"
            };
            await _EmailService.SendEmail(emailrequest);

            return new ForgetPassResponse { Email = account.Email, IsAuthenticated=true, Message = "تم إرسال الرقم السري إلى بريدك الإلكتروني."};
        }
        public async Task<ForgetPassResponse> VerifyOtpAsync(string Email, string enteredOtp)
        {
            ApplicationUser? Account = await _userManager.FindByEmailAsync(Email);
            if (Account == null)
            {
                return new ForgetPassResponse { Message = "لم يتم العثور على المستخدم." };
            }
            if (enteredOtp == null || enteredOtp != Account.OTP || DateTime.UtcNow > Account.OTPExpiry)
            {
                return new ForgetPassResponse { Email = Account.Email, Message = "الرقم السري غير صحيح."};
            }

            Account.OTP = null;
            Account.OTPExpiry = null;
            await _userManager.UpdateAsync(Account);

            var JwtSecurityToken = await CreateJwtToken(Account);
            //new AuthModel { Message = "Invalid OTP" };

            return new ForgetPassResponse
            {
                //Id = Account.Id,
                //Email = Account.Email,
                ////ExpiresOn = JwtSecurityToken.ValidTo,
                IsAuthenticated = true,
                ////Roles = await _userManager.GetRolesAsync(Account),
                //Token = new JwtSecurityTokenHandler().WriteToken(JwtSecurityToken),
                Message = "الرقم السري صحيح.",
                Email = Account.Email
            };
        }
    }
}