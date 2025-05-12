using Agazaty.Data.Base;
using Agazaty.Data.DTOs.AccountDTOs;
using Agazaty.Data.Email.DTOs;
using Agazaty.Data.Enums;
using Agazaty.Data.Services.Interfaces;
using Agazaty.Models;
using AutoMapper;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Drawing;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Agazaty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly IRoleService _roleService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEntityBaseRepository<Department> _deptBase;
        private readonly IMapper _mapper;
        public AccountController(IMapper mapper, IAccountService accountService, SignInManager<ApplicationUser> signInManager, IRoleService roleService, IEntityBaseRepository<Department> deptBase, UserManager<ApplicationUser> userManager)
        {
            _accountService = accountService;
            _signInManager = signInManager;
            _roleService = roleService;
            _deptBase = deptBase;
            _mapper = mapper;
            _userManager = userManager;
        }
        
        //[Authorize]
        [HttpPut("Reset-Password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDTO DTO)
        {
            var result = await _accountService.ResetPassword(DTO);
            if (!result.IsAuthenticated) return BadRequest(result);
            return Ok(result);
        }
        //[Authorize]
        [HttpPost("Forget-Password")]
        public async Task<IActionResult> ForgetPassword(SendOTPDTO DTO)
        {
            var result = await _accountService.ForgetPassword(DTO);
            if (!result.IsAuthenticated) return BadRequest(result);
            return Ok(result);
        }
        //[Authorize]
        [HttpPost("Send-OTP")]
        public async Task<IActionResult> SendOtp(SendOTPDTO DTO)
        {
            var result = await _accountService.SendOTP(DTO.Email);
            if (!result.IsAuthenticated) return BadRequest(result);
            return Ok(result);
        }
        [HttpPost("Verify-OTP")]
        public async Task<IActionResult> VerifyOtp(VerifyOTPDTO verifyOtpDTO)
        {
            var result = await _accountService.VerifyOtpAsync(verifyOtpDTO.Email, verifyOtpDTO.EnteredOtp);
            if (!result.IsAuthenticated) return BadRequest(result);
            return Ok(result);
        }
        //[Authorize]
        [HttpPost("Change-Password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            //var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _accountService.FindById(model.UseId);
            if (user == null)
                return NotFound("لم يتم العثور على المستخدم.");

            var result = await _accountService.ChangePassword(user, model);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(new { Errors = errors });
            }
            return Ok("تم تغيير كلمة المرور بنجاح.");
        }
        [Authorize]
        [HttpGet("GetUserById/{userID}")]
        public async Task<IActionResult> GetUserById([FromRoute]string userID)
        {
            if (string.IsNullOrWhiteSpace(userID))
                return BadRequest(new { message = "معرف المستخدم غير صالح." });
            try
            {
                var user = await _accountService.FindById(userID);
                if (user == null)
                {
                    return NotFound(new { Message = "لم يتم العثور على المستخدم." });
                }

                var ReturnedUser = _mapper.Map<UserDTO>(user);

                ReturnedUser.FullName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                ReturnedUser.RoleName = await _accountService.GetFirstRole(user);
                if (user.Departement_ID != null)
                {
                    var dept = await _deptBase.Get(d => d.Id == user.Departement_ID);
                    ReturnedUser.DepartmentName = dept.Name;
                }
                var DepartmentManager = await _deptBase.Get(d => d.ManagerId == ReturnedUser.Id);
                if (DepartmentManager != null) ReturnedUser.IsDirectManager = true;
                else ReturnedUser.IsDirectManager = false;
                return Ok(ReturnedUser);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpGet("GetRoleOfUser/{userID}")]
        public async Task<IActionResult> GetRoleOfUser([FromRoute] string userID)
        {
            if (string.IsNullOrWhiteSpace(userID))
                return BadRequest(new { message = "معرف المستخدم غير صالح." });
            try
            {
                var user = await _accountService.FindById(userID);
                if (user == null)
                {
                    return NotFound(new { Message = "لم يتم العثور على المستخدم." });
                }
                var role =await _accountService.GetFirstRole(user);
                return Ok(new{role});
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "عميد الكلية,أمين الكلية,مدير الموارد البشرية")]
        [HttpGet("GetUserByNationalId/{NationalId}")]
        public async Task<IActionResult> GetUserByNationalId(string NationalId)
        {
            if (string.IsNullOrWhiteSpace(NationalId))
                return BadRequest(new { message = "معرف المستخدم غير صالح." });
            try
            {
                var user = await _accountService.FindByNationalId(NationalId);
                if (user == null) return NotFound(new { Message = "لم يتم العثور على المستخدم." });

                var ReturnedUser = _mapper.Map<UserDTO>(user);
                ReturnedUser.FullName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                ReturnedUser.RoleName = await _accountService.GetFirstRole(user);
                if (user.Departement_ID != null)
                {
                    var dpt = await _deptBase.Get(d => d.Id == user.Departement_ID);
                    ReturnedUser.DepartmentName = dpt.Name;
                }
                var DepartmentManager = await _deptBase.Get(d => d.ManagerId == ReturnedUser.Id);
                if (DepartmentManager != null) ReturnedUser.IsDirectManager = true;
                else ReturnedUser.IsDirectManager = false;
                return Ok(ReturnedUser);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "عميد الكلية,أمين الكلية,مدير الموارد البشرية")]
        [HttpGet("GetAllActiveUsers")]
        public async Task<IActionResult> GetAllActiveUsers()
        {
            try
            {
                var users = await _accountService.GetAllActiveUsers();
                if (!users.Any()) { return NotFound(new { Message = "لم يتم العثور على أي مستخدمين." }); }

                var ReturnedUsers = _mapper.Map<IEnumerable<UserDTO>>(users);
                foreach(var ReturnedUser in ReturnedUsers)
                {
                    var user = await _accountService.FindById(ReturnedUser.Id);
                    ReturnedUser.FullName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    ReturnedUser.RoleName = await _accountService.GetFirstRole(user);
                    if (user.Departement_ID != null)
                    {
                        var dpt = await _deptBase.Get(d => d.Id == user.Departement_ID);
                        ReturnedUser.DepartmentName = dpt.Name;
                    }
                    var DepartmentManager = await _deptBase.Get(d => d.ManagerId == ReturnedUser.Id);
                    if (DepartmentManager != null) ReturnedUser.IsDirectManager = true;
                    else ReturnedUser.IsDirectManager = false;
                }
                return Ok(ReturnedUsers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "عميد الكلية,أمين الكلية,مدير الموارد البشرية")]
        [HttpGet("GetAllNonActiveUsers")]
        public async Task<IActionResult> GetAllNonActiveUsers()
        {
            try
            {
                var users = await _accountService.GetAllNonActiveUsers();
                if (!users.Any()) { return NotFound(new { Message = "لم يتم العثور على أي مستخدمين." }); }

                var ReturnedUsers = _mapper.Map<IEnumerable<UserDTO>>(users);
                foreach (var ReturnedUser in ReturnedUsers)
                {
                    var user = await _accountService.FindById(ReturnedUser.Id);
                    ReturnedUser.FullName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    ReturnedUser.RoleName = await _accountService.GetFirstRole(user);
                    if (user.Departement_ID != null)
                    {
                        var dpt = await _deptBase.Get(d => d.Id == user.Departement_ID);
                        ReturnedUser.DepartmentName = dpt.Name;
                    }
                    var DepartmentManager = await _deptBase.Get(d => d.ManagerId == ReturnedUser.Id);
                    if (DepartmentManager != null) ReturnedUser.IsDirectManager = true;
                    else ReturnedUser.IsDirectManager = false;
                }
                return Ok(ReturnedUsers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpGet("GetAllAvailabelCoworkers/{userId}")]
        public async Task<IActionResult> GetAllAvailableCoworkers(string userId)
        {
            try
            {
                var u = await _accountService.FindById(userId);
                if (u == null) return NotFound(new { Message = "لم يتم العثور على المستخدم." });

                var users = await _accountService.GetAllActiveAvailableCoworkers(u);
                if (!users.Any()) { return NotFound(new { Message = "لم يتم العثور على أي مستخدمين." }); }

                var ReturnedUsers = _mapper.Map<IEnumerable<CoworkerDTO>>(users);
                foreach (var ReturnedUser in ReturnedUsers)
                {
                    var user = await _accountService.FindById(ReturnedUser.Id);
                    ReturnedUser.FullName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    ReturnedUser.RoleName = await _accountService.GetFirstRole(user);
                    if (user.Departement_ID != null)
                    {
                        var dpt = await _deptBase.Get(d => d.Id == user.Departement_ID);
                        ReturnedUser.DepartmentName = dpt.Name;
                    }
                }
                return Ok(ReturnedUsers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpGet("GetAllUsersByDepartmentId/{DepartmentId}")]
        public async Task<IActionResult> GetAllUsersByDepartmentId(int DepartmentId)
        {
            if (DepartmentId <= 0)
                return BadRequest(new { Message = "معرف القسم غير صالح." });
            try
            {
                var dept = await _deptBase.Get(d=>d.Id==DepartmentId);
                if (dept == null) return NotFound(new { Message = "لم يتم العثور على قسم بهذا المعرف." });

                var users = await _accountService.GetAllUsersByDepartmentId(DepartmentId);
                if (!users.Any()) return NotFound(new { Message = "لم يتم العثور على مستخدمين في هذا القسم." });

                var ReturnedUsers = _mapper.Map<IEnumerable<UserDTO>>(users);
                foreach (var ReturnedUser in ReturnedUsers)
                {
                    var user = await _accountService.FindById(ReturnedUser.Id);
                    ReturnedUser.FullName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    ReturnedUser.RoleName = await _accountService.GetFirstRole(user);
                    if (user.Departement_ID != null)
                    {
                        var dpt = await _deptBase.Get(d => d.Id == user.Departement_ID);
                        ReturnedUser.DepartmentName = dpt.Name;
                    }
                    var DepartmentManager = await _deptBase.Get(d => d.ManagerId == ReturnedUser.Id);
                    if (DepartmentManager != null) ReturnedUser.IsDirectManager = true;
                    else ReturnedUser.IsDirectManager = false;
                }
                return Ok(ReturnedUsers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        //[HttpPost("upload-users-excel")]
        //public async Task<IActionResult> UploadUsersExcel(IFormFile file)
        //{
        //    // 1. Validate File
        //    if (file == null || file.Length == 0)
        //        return BadRequest("No file uploaded.");

        //    if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        //        return BadRequest("Only .xlsx files are allowed.");

        //    var results = new List<string>();
        //    var validationErrors = new List<string>();

        //    using var stream = new MemoryStream();
        //    await file.CopyToAsync(stream);

        //    // 2. Set EPPlus LicenseContext (critical for EPPlus 5+)
        //    ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // or Commercial

        //    using var package = new ExcelPackage(stream);
        //    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
        //    int rowCount = worksheet.Dimension?.Rows ?? 0;

        //    if (rowCount < 2)
        //        return BadRequest("Excel file has no data rows.");

        //    // 3. Process Each Row
        //    for (int row = 2; row <= rowCount; row++)
        //    {
        //        try
        //        {
        //            // 4. Parse and Validate Data
        //            if (!DateTime.TryParse(worksheet.Cells[row, 9].Text, out var dateOfBirth))
        //            {
        //                results.Add($"Row {row}: تاريخ الميلاد غير صالح.");
        //                continue;
        //            }

        //            if (!DateTime.TryParse(worksheet.Cells[row, 11].Text, out var hireDate))
        //            {
        //                results.Add($"Row {row}: تاريخ التعيين غير صالح.");
        //                continue;
        //            }

        //            var dto = new CreateUserDTO
        //            {
        //                UserName = worksheet.Cells[row, 1].Text?.Trim(),
        //                PhoneNumber = worksheet.Cells[row, 2].Text?.Trim(),
        //                Email = worksheet.Cells[row, 3].Text?.Trim(),
        //                Password = worksheet.Cells[row, 4].Text?.Trim(),
        //                FirstName = worksheet.Cells[row, 5].Text?.Trim(),
        //                SecondName = worksheet.Cells[row, 6].Text?.Trim(),
        //                ThirdName = worksheet.Cells[row, 7].Text?.Trim(),
        //                ForthName = worksheet.Cells[row, 8].Text?.Trim(),
        //                DateOfBirth = dateOfBirth,
        //                Gender = worksheet.Cells[row, 10].Text?.Trim(),
        //                HireDate = hireDate,
        //                NationalID = worksheet.Cells[row, 12].Text?.Trim(),
        //                position = int.TryParse(worksheet.Cells[row, 13].Text, out var pos) ? pos : 0,
        //                Departement_ID = string.IsNullOrWhiteSpace(worksheet.Cells[row, 14].Text)
        //                    ? null
        //                    : int.TryParse(worksheet.Cells[row, 14].Text, out var dept) ? dept : null,
        //                Disability = worksheet.Cells[row, 15].Text?.Trim() == "نعم",
        //                Street = worksheet.Cells[row, 16].Text?.Trim(),
        //                governorate = worksheet.Cells[row, 17].Text?.Trim(),
        //                State = worksheet.Cells[row, 18].Text?.Trim(),
        //                NormalLeavesCount = int.TryParse(worksheet.Cells[row, 19].Text, out var normal) ? normal : 0,
        //                CasualLeavesCount = int.TryParse(worksheet.Cells[row, 20].Text, out var casual) ? casual : 0,
        //                NonChronicSickLeavesCount = int.TryParse(worksheet.Cells[row, 21].Text, out var sick) ? sick : 0,
        //                NormalLeavesCount_47 = int.TryParse(worksheet.Cells[row, 22].Text, out var n47) ? n47 : 0,
        //                NormalLeavesCount_81Before3Years = int.TryParse(worksheet.Cells[row, 23].Text, out var n81_3) ? n81_3 : 0,
        //                NormalLeavesCount_81Before2Years = int.TryParse(worksheet.Cells[row, 24].Text, out var n81_2) ? n81_2 : 0,
        //                NormalLeavesCount_81Before1Years = int.TryParse(worksheet.Cells[row, 25].Text, out var n81_1) ? n81_1 : 0,
        //                HowManyDaysFrom81And47 = int.TryParse(worksheet.Cells[row, 26].Text, out var totalDays) ? totalDays : 0
        //            };

        //            // 5. Validate DTO (using DataAnnotations)
        //            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(dto);
        //            var validationResults = new List<ValidationResult>();
        //            bool isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

        //            if (!isValid)
        //            {
        //                var errorMessages = validationResults.Select(v => v.ErrorMessage);
        //                results.Add($"Row {row}: خطأ في التحقق - {string.Join(" | ", errorMessages)}");
        //                continue;
        //            }
        //            // 6. Map DTO to ApplicationUser (manual mapping)
        //            var user = new ApplicationUser
        //            {
        //                UserName = dto.UserName,
        //                Email = dto.Email,
        //                PhoneNumber = dto.PhoneNumber,
        //                FirstName = dto.FirstName,
        //                SecondName = dto.SecondName,
        //                ThirdName = dto.ThirdName,
        //                ForthName = dto.ForthName,
        //                DateOfBirth = dto.DateOfBirth,
        //                Gender = dto.Gender,
        //                HireDate = dto.HireDate,
        //                NationalID = dto.NationalID,
        //                position = dto.position,
        //                Departement_ID = dto.Departement_ID,
        //                Disability = dto.Disability,
        //                Street = dto.Street,
        //                Governorate = dto.governorate,
        //                State = dto.State,
        //                NormalLeavesCount = dto.NormalLeavesCount,
        //                CasualLeavesCount = dto.CasualLeavesCount,
        //                NonChronicSickLeavesCount = dto.NonChronicSickLeavesCount,
        //                NormalLeavesCount_47 = dto.NormalLeavesCount_47,
        //                NormalLeavesCount_81Before3Years = dto.NormalLeavesCount_81Before3Years,
        //                NormalLeavesCount_81Before2Years = dto.NormalLeavesCount_81Before2Years,
        //                NormalLeavesCount_81Before1Years = dto.NormalLeavesCount_81Before1Years,
        //                HowManyDaysFrom81And47 = dto.HowManyDaysFrom81And47,
        //            };

        //            // 7. Create User
        //            var createResult = await _userManager.CreateAsync(user, dto.Password);

        //            if (!createResult.Succeeded)
        //            {
        //                var errors = createResult.Errors.Select(e => e.Description);
        //                results.Add($"Row {row}: فشل إنشاء المستخدم - {string.Join(" | ", errors)}");
        //                continue;
        //            }

        //            results.Add($"Row {row}: تم إنشاء المستخدم بنجاح.");
        //        }
        //        catch (Exception ex)
        //        {
        //            results.Add($"Row {row}: خطأ غير متوقع - {ex.Message}");
        //        }
        //    }

        //    // 8. Return Structured Response
        //    var successCount = results.Count(r => r.Contains("نجاح"));
        //    var errorCount = results.Count - successCount;

        //    return Ok(new
        //    {
        //        TotalRowsProcessed = rowCount - 1,
        //        SuccessCount = successCount,
        //        ErrorCount = errorCount,
        //        Results = results
        //    });
        //}
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpGet("export-active-users-excel")]
        public async Task<IActionResult> ExportActiveUsersExcel()
        {
            try
            {
                // 1. Get all active users with their roles
                List<ApplicationUser> users;
                try
                {
                    users = await _userManager.Users
                        .Where(u => u.Active)  // Only include active users
                        .Include(u => u.Department)
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                    $"حدث خطأ أثناء استرجاع بيانات المستخدمين: {ex.Message}");
                }

                // 2. Create Excel package
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Users");

                // 3. Add headers
                try
                {
                    worksheet.Cells[1, 1].Value = "UserName";
                    worksheet.Cells[1, 2].Value = "PhoneNumber";
                    worksheet.Cells[1, 3].Value = "Email";
                    worksheet.Cells[1, 4].Value = "FirstName";
                    worksheet.Cells[1, 5].Value = "SecondName";
                    worksheet.Cells[1, 6].Value = "ThirdName";
                    worksheet.Cells[1, 7].Value = "ForthName";
                    worksheet.Cells[1, 8].Value = "DateOfBirth";
                    worksheet.Cells[1, 9].Value = "Gender";
                    worksheet.Cells[1, 10].Value = "HireDate";
                    worksheet.Cells[1, 11].Value = "NationalID";
                    worksheet.Cells[1, 12].Value = "Position";
                    worksheet.Cells[1, 13].Value = "Department";
                    worksheet.Cells[1, 14].Value = "Disability";
                    worksheet.Cells[1, 15].Value = "Street";
                    worksheet.Cells[1, 16].Value = "Governorate";
                    worksheet.Cells[1, 17].Value = "State";
                    worksheet.Cells[1, 18].Value = "NormalLeavesCount";
                    worksheet.Cells[1, 19].Value = "CasualLeavesCount";
                    worksheet.Cells[1, 20].Value = "NonChronicSickLeavesCount";
                    worksheet.Cells[1, 21].Value = "NormalLeavesCount_47";
                    worksheet.Cells[1, 22].Value = "NormalLeavesCount_81Before3Years";
                    worksheet.Cells[1, 23].Value = "NormalLeavesCount_81Before2Years";
                    worksheet.Cells[1, 24].Value = "NormalLeavesCount_81Before1Years";
                    worksheet.Cells[1, 25].Value = "HowManyDaysFrom81And47";
                    worksheet.Cells[1, 26].Value = "Role";

                    // Format headers
                    using (var range = worksheet.Cells[1, 1, 1, 26])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        $"Failed to create Excel headers: {ex.Message}");
                }

                // 4. Add user data
                int row = 2;
                int successCount = 0;
                var errorMessages = new List<string>();

                foreach (var user in users)
                {
                    try
                    {
                        // Get user's role
                        IList<string> roles;
                        try
                        {
                            roles = await _userManager.GetRolesAsync(user);
                        }
                        catch (Exception ex)
                        {
                            errorMessages.Add($"Failed to get roles for user {user.UserName}: {ex.Message}");
                            continue;
                        }

                        var role = roles.FirstOrDefault();

                        worksheet.Cells[row, 1].Value = user.UserName;
                        worksheet.Cells[row, 2].Value = user.PhoneNumber;
                        worksheet.Cells[row, 3].Value = user.Email;
                        worksheet.Cells[row, 4].Value = user.FirstName;
                        worksheet.Cells[row, 5].Value = user.SecondName;
                        worksheet.Cells[row, 6].Value = user.ThirdName;
                        worksheet.Cells[row, 7].Value = user.ForthName;
                        worksheet.Cells[row, 8].Value = user.DateOfBirth.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 9].Value = user.Gender;
                        worksheet.Cells[row, 10].Value = user.HireDate.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 11].Value = user.NationalID;
                        worksheet.Cells[row, 12].Value = user.position;
                        worksheet.Cells[row, 13].Value = user.Department?.Name;
                        worksheet.Cells[row, 14].Value = user.Disability ? "1" : "0";
                        worksheet.Cells[row, 15].Value = user.Street;
                        worksheet.Cells[row, 16].Value = user.Governorate;
                        worksheet.Cells[row, 17].Value = user.State;
                        worksheet.Cells[row, 18].Value = user.NormalLeavesCount;
                        worksheet.Cells[row, 19].Value = user.CasualLeavesCount;
                        worksheet.Cells[row, 20].Value = user.NonChronicSickLeavesCount;
                        worksheet.Cells[row, 21].Value = user.NormalLeavesCount_47;
                        worksheet.Cells[row, 22].Value = user.NormalLeavesCount_81Before3Years;
                        worksheet.Cells[row, 23].Value = user.NormalLeavesCount_81Before2Years;
                        worksheet.Cells[row, 24].Value = user.NormalLeavesCount_81Before1Years;
                        worksheet.Cells[row, 25].Value = user.HowManyDaysFrom81And47;
                        worksheet.Cells[row, 26].Value = role;

                        row++;
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errorMessages.Add($"Failed to process user {user.UserName}: {ex.Message}");
                        continue;
                    }
                }

                // Skip if no data was added
                if (successCount == 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        $"No active users found to export. Errors: {string.Join("; ", errorMessages)}");
                }

                // Auto-fit columns
                try
                {
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                }
                catch
                {
                    // Continue even if auto-fit fails
                }

                // 5. Return the file
                try
                {
                    var fileName = $"قائمة_الموظفين_النشطاء_{DateTime.Now:yyyy-MM-dd}.xlsx";
                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    // Add warning if there were errors
                    if (errorMessages.Any())
                    {
                        Response.Headers.Add("X-Export-Warnings",
                            $"{errorMessages.Count} users failed: {string.Join("; ", errorMessages.Take(3))}...");
                    }

                    return File(stream, contentType, fileName);
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        $"Failed to generate Excel file: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"An unexpected error occurred: {ex.Message}");
            }
        }
        [HttpGet("export-nonactive-users-excel")]
        public async Task<IActionResult> ExportNonActiveUsersExcel()
        {
            try
            {
                // 1. Get all active users with their roles
                List<ApplicationUser> users;
                try
                {
                    users = await _userManager.Users
                        .Where(u => u.Active==false)  // Only include active users
                        .Include(u => u.Department)
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        $"Failed to retrieve users: {ex.Message}");
                }

                // 2. Create Excel package
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Users");

                // 3. Add headers
                try
                {
                    worksheet.Cells[1, 1].Value = "UserName";
                    worksheet.Cells[1, 2].Value = "PhoneNumber";
                    worksheet.Cells[1, 3].Value = "Email";
                    worksheet.Cells[1, 4].Value = "FirstName";
                    worksheet.Cells[1, 5].Value = "SecondName";
                    worksheet.Cells[1, 6].Value = "ThirdName";
                    worksheet.Cells[1, 7].Value = "ForthName";
                    worksheet.Cells[1, 8].Value = "DateOfBirth";
                    worksheet.Cells[1, 9].Value = "Gender";
                    worksheet.Cells[1, 10].Value = "HireDate";
                    worksheet.Cells[1, 11].Value = "NationalID";
                    worksheet.Cells[1, 12].Value = "Position";
                    worksheet.Cells[1, 13].Value = "Department";
                    worksheet.Cells[1, 14].Value = "Disability";
                    worksheet.Cells[1, 15].Value = "Street";
                    worksheet.Cells[1, 16].Value = "Governorate";
                    worksheet.Cells[1, 17].Value = "State";
                    worksheet.Cells[1, 18].Value = "NormalLeavesCount";
                    worksheet.Cells[1, 19].Value = "CasualLeavesCount";
                    worksheet.Cells[1, 20].Value = "NonChronicSickLeavesCount";
                    worksheet.Cells[1, 21].Value = "NormalLeavesCount_47";
                    worksheet.Cells[1, 22].Value = "NormalLeavesCount_81Before3Years";
                    worksheet.Cells[1, 23].Value = "NormalLeavesCount_81Before2Years";
                    worksheet.Cells[1, 24].Value = "NormalLeavesCount_81Before1Years";
                    worksheet.Cells[1, 25].Value = "HowManyDaysFrom81And47";
                    worksheet.Cells[1, 26].Value = "Role";

                    // Format headers
                    using (var range = worksheet.Cells[1, 1, 1, 26])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        $"Failed to create Excel headers: {ex.Message}");
                }

                // 4. Add user data
                int row = 2;
                int successCount = 0;
                var errorMessages = new List<string>();

                foreach (var user in users)
                {
                    try
                    {
                        // Get user's role
                        IList<string> roles;
                        try
                        {
                            roles = await _userManager.GetRolesAsync(user);
                        }
                        catch (Exception ex)
                        {
                            errorMessages.Add($"Failed to get roles for user {user.UserName}: {ex.Message}");
                            continue;
                        }

                        var role = roles.FirstOrDefault();

                        worksheet.Cells[row, 1].Value = user.UserName;
                        worksheet.Cells[row, 2].Value = user.PhoneNumber;
                        worksheet.Cells[row, 3].Value = user.Email;
                        worksheet.Cells[row, 4].Value = user.FirstName;
                        worksheet.Cells[row, 5].Value = user.SecondName;
                        worksheet.Cells[row, 6].Value = user.ThirdName;
                        worksheet.Cells[row, 7].Value = user.ForthName;
                        worksheet.Cells[row, 8].Value = user.DateOfBirth.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 9].Value = user.Gender;
                        worksheet.Cells[row, 10].Value = user.HireDate.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 11].Value = user.NationalID;
                        worksheet.Cells[row, 12].Value = user.position;
                        worksheet.Cells[row, 13].Value = user.Department?.Name;
                        worksheet.Cells[row, 14].Value = user.Disability ? "1" : "0";
                        worksheet.Cells[row, 15].Value = user.Street;
                        worksheet.Cells[row, 16].Value = user.Governorate;
                        worksheet.Cells[row, 17].Value = user.State;
                        worksheet.Cells[row, 18].Value = user.NormalLeavesCount;
                        worksheet.Cells[row, 19].Value = user.CasualLeavesCount;
                        worksheet.Cells[row, 20].Value = user.NonChronicSickLeavesCount;
                        worksheet.Cells[row, 21].Value = user.NormalLeavesCount_47;
                        worksheet.Cells[row, 22].Value = user.NormalLeavesCount_81Before3Years;
                        worksheet.Cells[row, 23].Value = user.NormalLeavesCount_81Before2Years;
                        worksheet.Cells[row, 24].Value = user.NormalLeavesCount_81Before1Years;
                        worksheet.Cells[row, 25].Value = user.HowManyDaysFrom81And47;
                        worksheet.Cells[row, 26].Value = role;

                        row++;
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errorMessages.Add($"Failed to process user {user.UserName}: {ex.Message}");
                        continue;
                    }
                }

                // Skip if no data was added
                if (successCount == 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        $"No active users found to export. Errors: {string.Join("; ", errorMessages)}");
                }

                // Auto-fit columns
                try
                {
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                }
                catch
                {
                    // Continue even if auto-fit fails
                }

                // 5. Return the file
                try
                {
                    var fileName = $"قائمة_الموظفين_الغير_نشطاء_{DateTime.Now:yyyy-MM-dd}.xlsx";
                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    // Add warning if there were errors
                    if (errorMessages.Any())
                    {
                        Response.Headers.Add("X-Export-Warnings",
                            $"{errorMessages.Count} users failed: {string.Join("; ", errorMessages.Take(3))}...");
                    }

                    return File(stream, contentType, fileName);
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        $"Failed to generate Excel file: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"An unexpected error occurred: {ex.Message}");
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpGet("export-user-excel/{nationalId}")]
        public async Task<IActionResult> ExportUserExcel(string nationalId)
        {
            // 1. Validate input
            if (string.IsNullOrWhiteSpace(nationalId))
            {
                return BadRequest("National ID is required");
            }

            // 2. Find user by national ID
            var user = await _userManager.Users
                .Include(u => u.Department) // Include department if needed
                .FirstOrDefaultAsync(u => u.NationalID == nationalId);

            if (user == null)
            {
                return NotFound($"User with National ID {nationalId} not found");
            }

            // 3. Get user's role
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            // 4. Create Excel package
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("User");

            // 5. Add headers
            var headers = new string[]
            {
        "UserName", "PhoneNumber", "Email", "FirstName", "SecondName", "ThirdName", "ForthName",
        "DateOfBirth", "Gender", "HireDate", "NationalID", "Position", "Department", "Disability",
        "Street", "Governorate", "State", "NormalLeavesCount", "CasualLeavesCount",
        "NonChronicSickLeavesCount", "NormalLeavesCount_47", "NormalLeavesCount_81Before3Years",
        "NormalLeavesCount_81Before2Years", "NormalLeavesCount_81Before1Years",
        "HowManyDaysFrom81And47", "Role"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }

            // Format headers
            using (var range = worksheet.Cells[1, 1, 1, headers.Length])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }

            // 6. Add user data
            worksheet.Cells[2, 1].Value = user.UserName;
            worksheet.Cells[2, 2].Value = user.PhoneNumber;
            worksheet.Cells[2, 3].Value = user.Email;
            worksheet.Cells[2, 4].Value = user.FirstName;
            worksheet.Cells[2, 5].Value = user.SecondName;
            worksheet.Cells[2, 6].Value = user.ThirdName;
            worksheet.Cells[2, 7].Value = user.ForthName;
            worksheet.Cells[2, 8].Value = user.DateOfBirth.ToString("yyyy-MM-dd");
            worksheet.Cells[2, 9].Value = user.Gender;
            worksheet.Cells[2, 10].Value = user.HireDate.ToString("yyyy-MM-dd");
            worksheet.Cells[2, 11].Value = user.NationalID;
            worksheet.Cells[2, 12].Value = user.position;
            worksheet.Cells[2, 13].Value = user.Department?.Name;
            worksheet.Cells[2, 14].Value = user.Disability ? "1" : "0";
            worksheet.Cells[2, 15].Value = user.Street;
            worksheet.Cells[2, 16].Value = user.Governorate;
            worksheet.Cells[2, 17].Value = user.State;
            worksheet.Cells[2, 18].Value = user.NormalLeavesCount;
            worksheet.Cells[2, 19].Value = user.CasualLeavesCount;
            worksheet.Cells[2, 20].Value = user.NonChronicSickLeavesCount;
            worksheet.Cells[2, 21].Value = user.NormalLeavesCount_47;
            worksheet.Cells[2, 22].Value = user.NormalLeavesCount_81Before3Years;
            worksheet.Cells[2, 23].Value = user.NormalLeavesCount_81Before2Years;
            worksheet.Cells[2, 24].Value = user.NormalLeavesCount_81Before1Years;
            worksheet.Cells[2, 25].Value = user.HowManyDaysFrom81And47;
            worksheet.Cells[2, 26].Value = role;

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // 7. Return the file
            var fileName = $"المستخدم_{user.FirstName + ' ' + user.SecondName + ' ' + user.ThirdName + ' ' + user.ForthName}_{DateTime.Now:yyyy-MM-dd}.xlsx";
            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return File(stream, contentType, fileName);
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpPost("export-selected-users-excel")]
        public async Task<IActionResult> ExportSelectedUsersExcel([FromBody] List<string> nationalIds)
        {
            try
            {
                // 1. Validate input
                if (nationalIds == null || !nationalIds.Any())
                {
                    return BadRequest("At least one National ID is required");
                }

                // Clean and deduplicate input
                nationalIds = nationalIds.Select(id => id?.Trim())
                                       .Where(id => !string.IsNullOrEmpty(id))
                                       .Distinct()
                                       .ToList();

                // 2. Verify ALL national IDs exist
                var existingIds = await _userManager.Users
                    .Where(u => nationalIds.Contains(u.NationalID))
                    .Select(u => u.NationalID)
                    .ToListAsync();

                var missingIds = nationalIds.Except(existingIds).ToList();
                if (missingIds.Any())
                {
                    return NotFound(new
                    {
                        Message = "Some national IDs were not found",
                        TotalRequested = nationalIds.Count,
                        MissingCount = missingIds.Count,
                        SampleMissing = missingIds.Take(5),
                        Suggestion = "Verify all national IDs exist in the system"
                    });
                }

                // 3. Get all users with required data
                var users = await _userManager.Users
                    .Include(u => u.Department)
                    .Where(u => nationalIds.Contains(u.NationalID))
                    .ToListAsync();

                // 4. Create Excel package
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Users");

                // Define all column headers exactly as requested
                string[] headers = {
            "UserName", "PhoneNumber", "Email", "FirstName", "SecondName",
            "ThirdName", "ForthName", "DateOfBirth", "Gender", "HireDate",
            "NationalID", "Position", "Department", "Disability", "Street",
            "Governorate", "State", "NormalLeavesCount", "CasualLeavesCount",
            "NonChronicSickLeavesCount", "NormalLeavesCount_47",
            "NormalLeavesCount_81Before3Years", "NormalLeavesCount_81Before2Years",
            "NormalLeavesCount_81Before1Years", "HowManyDaysFrom81And47", "Role"
        };

                // Write headers with styling
                for (int col = 0; col < headers.Length; col++)
                {
                    worksheet.Cells[1, col + 1].Value = headers[col];
                    worksheet.Cells[1, col + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, col + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                }

                // 5. Add user data
                int row = 2;
                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var role = roles.FirstOrDefault() ?? "No Role";

                    worksheet.Cells[row, 1].Value = user.UserName;
                    worksheet.Cells[row, 2].Value = user.PhoneNumber;
                    worksheet.Cells[row, 3].Value = user.Email;
                    worksheet.Cells[row, 4].Value = user.FirstName;
                    worksheet.Cells[row, 5].Value = user.SecondName;
                    worksheet.Cells[row, 6].Value = user.ThirdName;
                    worksheet.Cells[row, 7].Value = user.ForthName;
                    worksheet.Cells[row, 8].Value = user.DateOfBirth.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 9].Value = user.Gender;
                    worksheet.Cells[row, 10].Value = user.HireDate.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 11].Value = user.NationalID;
                    worksheet.Cells[row, 12].Value = user.position;
                    worksheet.Cells[row, 13].Value = user.Department?.Name;
                    worksheet.Cells[row, 14].Value = user.Disability ? "Yes" : "No";
                    worksheet.Cells[row, 15].Value = user.Street;
                    worksheet.Cells[row, 16].Value = user.Governorate;
                    worksheet.Cells[row, 17].Value = user.State;
                    worksheet.Cells[row, 18].Value = user.NormalLeavesCount;
                    worksheet.Cells[row, 19].Value = user.CasualLeavesCount;
                    worksheet.Cells[row, 20].Value = user.NonChronicSickLeavesCount;
                    worksheet.Cells[row, 21].Value = user.NormalLeavesCount_47;
                    worksheet.Cells[row, 22].Value = user.NormalLeavesCount_81Before3Years;
                    worksheet.Cells[row, 23].Value = user.NormalLeavesCount_81Before2Years;
                    worksheet.Cells[row, 24].Value = user.NormalLeavesCount_81Before1Years;
                    worksheet.Cells[row, 25].Value = user.HowManyDaysFrom81And47;
                    worksheet.Cells[row, 26].Value = role;

                    row++;
                }

                // 6. Format Excel file
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                worksheet.View.FreezePanes(2, 1); // Freeze header row

                // 7. Generate and return file
                var fileName = $"User_Export_{DateTime.Now:yyyy-MM-dd}.xlsx";
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "An unexpected error occurred",
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }
        //[HttpPost("upload-users-excel")]
        //public async Task<IActionResult> UploadUsersExcel(IFormFile file)
        //{
        //    // 1. Validate File
        //    if (file == null || file.Length == 0)
        //        return BadRequest("No file uploaded.");

        //    if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        //        return BadRequest("Only .xlsx files are allowed.");

        //    var results = new List<string>();
        //    var validationErrors = new List<string>();

        //    using var stream = new MemoryStream();
        //    await file.CopyToAsync(stream);

        //    // 2. Set EPPlus LicenseContext
        //    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        //    using var package = new ExcelPackage(stream);
        //    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
        //    int rowCount = worksheet.Dimension?.Rows ?? 0;

        //    if (rowCount < 2)
        //        return BadRequest("Excel file has no data rows.");

        //    // Define column indexes (adjust these numbers based on your actual Excel structure)
        //    const int UserNameCol = 1;
        //    const int PhoneNumberCol = 2;
        //    const int EmailCol = 3;
        //    const int PasswordCol = 4;
        //    const int FirstNameCol = 5;
        //    const int SecondNameCol = 6;
        //    const int ThirdNameCol = 7;
        //    const int ForthNameCol = 8;
        //    const int DateOfBirthCol = 9;
        //    const int GenderCol = 10;
        //    const int HireDateCol = 11;
        //    const int NationalIDCol = 12;
        //    const int PositionCol = 13;
        //    const int DepartmentCol = 14;
        //    const int DisabilityCol = 15;
        //    const int StreetCol = 16;
        //    const int GovernorateCol = 17;
        //    const int StateCol = 18;
        //    const int NormalLeavesCol = 19;
        //    const int CasualLeavesCol = 20;
        //    const int SickLeavesCol = 21;
        //    const int Leaves47Col = 22;
        //    const int Leaves81_3Col = 23;
        //    const int Leaves81_2Col = 24;
        //    const int Leaves81_1Col = 25;
        //    const int TotalDaysCol = 26;
        //    const int RoleNameCol = 27; // Column for role name (not part of DTO)

        //    // 3. Process Each Row
        //    for (int row = 2; row <= rowCount; row++)
        //    {
        //        try
        //        {
        //            // 4. Parse and Validate Data
        //            if (!DateTime.TryParse(worksheet.Cells[row, DateOfBirthCol].Text, out var dateOfBirth))
        //            {
        //                results.Add($"Row {row}: تاريخ الميلاد غير صالح.");
        //                continue;
        //            }

        //            if (!DateTime.TryParse(worksheet.Cells[row, HireDateCol].Text, out var hireDate))
        //            {
        //                results.Add($"Row {row}: تاريخ التعيين غير صالح.");
        //                continue;
        //            }
        //            bool Disability = false;
        //            var disabilityValue = worksheet.Cells[row, DisabilityCol].Value;

        //            if (disabilityValue != null)
        //            {
        //                if (disabilityValue is string strValue)
        //                {
        //                    Disability = strValue.Trim() == "1";
        //                }
        //                else if (disabilityValue is int intValue)
        //                {
        //                    Disability = intValue == 1;
        //                }
        //                else if (double.TryParse(disabilityValue.ToString(), out double doubleValue))
        //                {
        //                    Disability = doubleValue == 1;
        //                }
        //            }

        //            // Get role name from Excel (not part of DTO validation)
        //            var roleName = worksheet.Cells[row, RoleNameCol].Text?.Trim();

        //            var dto = new CreateUserDTO
        //            {
        //                UserName = worksheet.Cells[row, UserNameCol].Text?.Trim(),
        //                PhoneNumber = worksheet.Cells[row, PhoneNumberCol].Text?.Trim(),
        //                Email = worksheet.Cells[row, EmailCol].Text?.Trim(),
        //                Password = worksheet.Cells[row, PasswordCol].Text?.Trim(),
        //                FirstName = worksheet.Cells[row, FirstNameCol].Text?.Trim(),
        //                SecondName = worksheet.Cells[row, SecondNameCol].Text?.Trim(),
        //                ThirdName = worksheet.Cells[row, ThirdNameCol].Text?.Trim(),
        //                ForthName = worksheet.Cells[row, ForthNameCol].Text?.Trim(),
        //                DateOfBirth = dateOfBirth,
        //                Gender = worksheet.Cells[row, GenderCol].Text?.Trim(),
        //                HireDate = hireDate,
        //                NationalID = worksheet.Cells[row, NationalIDCol].Text?.Trim(),
        //                position = int.TryParse(worksheet.Cells[row, PositionCol].Text, out var pos) ? pos : 0,
        //                Departement_ID = string.IsNullOrWhiteSpace(worksheet.Cells[row, DepartmentCol].Text)
        //                    ? null
        //                    : int.TryParse(worksheet.Cells[row, DepartmentCol].Text, out var dept) ? dept : null,
        //                Disability = Disability,
        //                //Disability = worksheet.Cells[row, DisabilityCol].Text?.Trim() == "نعم",
        //                Street = worksheet.Cells[row, StreetCol].Text?.Trim(),
        //                governorate = worksheet.Cells[row, GovernorateCol].Text?.Trim(),
        //                State = worksheet.Cells[row, StateCol].Text?.Trim(),
        //                NormalLeavesCount = int.TryParse(worksheet.Cells[row, NormalLeavesCol].Text, out var normal) ? normal : 0,
        //                CasualLeavesCount = int.TryParse(worksheet.Cells[row, CasualLeavesCol].Text, out var casual) ? casual : 0,
        //                NonChronicSickLeavesCount = int.TryParse(worksheet.Cells[row, SickLeavesCol].Text, out var sick) ? sick : 0,
        //                NormalLeavesCount_47 = int.TryParse(worksheet.Cells[row, Leaves47Col].Text, out var n47) ? n47 : 0,
        //                NormalLeavesCount_81Before3Years = int.TryParse(worksheet.Cells[row, Leaves81_3Col].Text, out var n81_3) ? n81_3 : 0,
        //                NormalLeavesCount_81Before2Years = int.TryParse(worksheet.Cells[row, Leaves81_2Col].Text, out var n81_2) ? n81_2 : 0,
        //                NormalLeavesCount_81Before1Years = int.TryParse(worksheet.Cells[row, Leaves81_1Col].Text, out var n81_1) ? n81_1 : 0,
        //                HowManyDaysFrom81And47 = int.TryParse(worksheet.Cells[row, TotalDaysCol].Text, out var totalDays) ? totalDays : 0
        //            };

        //            // 5. Validate DTO (without role name)
        //            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(dto);
        //            var validationResults = new List<ValidationResult>();
        //            bool isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

        //            if (!isValid)
        //            {
        //                var errorMessages = validationResults.Select(v => v.ErrorMessage);
        //                results.Add($"Row {row}: خطأ في التحقق - {string.Join(" | ", errorMessages)}");
        //                continue;
        //            }

        //            // 6. Map DTO to ApplicationUser
        //            var user = new ApplicationUser
        //            {
        //                UserName = dto.UserName,
        //                Email = dto.Email,
        //                PhoneNumber = dto.PhoneNumber,
        //                FirstName = dto.FirstName,
        //                SecondName = dto.SecondName,
        //                ThirdName = dto.ThirdName,
        //                ForthName = dto.ForthName,
        //                DateOfBirth = dto.DateOfBirth,
        //                Gender = dto.Gender,
        //                HireDate = dto.HireDate,
        //                NationalID = dto.NationalID,
        //                position = dto.position,
        //                Departement_ID = dto.Departement_ID,
        //                Disability = dto.Disability,
        //                Street = dto.Street,
        //                Governorate = dto.governorate,
        //                State = dto.State,
        //                NormalLeavesCount = dto.NormalLeavesCount,
        //                CasualLeavesCount = dto.CasualLeavesCount,
        //                NonChronicSickLeavesCount = dto.NonChronicSickLeavesCount,
        //                NormalLeavesCount_47 = dto.NormalLeavesCount_47,
        //                NormalLeavesCount_81Before3Years = dto.NormalLeavesCount_81Before3Years,
        //                NormalLeavesCount_81Before2Years = dto.NormalLeavesCount_81Before2Years,
        //                NormalLeavesCount_81Before1Years = dto.NormalLeavesCount_81Before1Years,
        //                HowManyDaysFrom81And47 = dto.HowManyDaysFrom81And47,
        //            };

        //            // 7. Create User
        //            user.Active = true;
        //            var init = user.HireDate;
        //            var today = DateTime.UtcNow;
        //            user.YearsOfWork = today.Year - init.Year;
        //            if (init.Month < 7)
        //            {
        //                user.YearsOfWork++;
        //            }
        //            if (today.Month < 7)
        //            {
        //                user.YearsOfWork--;
        //            }
        //            if (user.Disability == true)
        //            {
        //                user.LeaveSection = NormalLeaveSection.DisabilityEmployee;
        //            }
        //            else
        //            {
        //                //var hireDuration = (DateTime.UtcNow.Date - user.HireDate).TotalDays;
        //                // validation on leaves count from front
        //                var ageInYears = DateTime.UtcNow.Year - user.DateOfBirth.Year;
        //                if (DateTime.UtcNow.Month < user.DateOfBirth.Month ||
        //                   (DateTime.UtcNow.Month == user.DateOfBirth.Month && DateTime.UtcNow.Day < user.DateOfBirth.Day))
        //                {
        //                    ageInYears--;
        //                }
        //                if (ageInYears >= 50) user.LeaveSection = NormalLeaveSection.FiftyAge;
        //                else
        //                {
        //                    if (user.YearsOfWork == 0) user.LeaveSection = NormalLeaveSection.NoSection;
        //                    if (user.YearsOfWork >= 1) user.LeaveSection = NormalLeaveSection.OneYear;
        //                    if (user.YearsOfWork >= 10) user.LeaveSection = NormalLeaveSection.TenYears;
        //                }
        //            }
        //            var createResult = await _userManager.CreateAsync(user, dto.Password);


        //            if (!createResult.Succeeded)
        //            {
        //                var errors = createResult.Errors.Select(e => e.Description);
        //                results.Add($"Row {row}: فشل إنشاء المستخدم - {string.Join(" | ", errors)}");
        //                continue;
        //            }

        //            // 8. Assign Role if specified (handled separately from DTO)
        //            if (!string.IsNullOrWhiteSpace(roleName))
        //            {
        //                try
        //                {
        //                    var roleExists = await _roleService.IsRoleExisted(roleName);
        //                    if (!roleExists)
        //                    {
        //                        results.Add($"Row {row}: تم إنشاء المستخدم بنجاح ولكن الدور '{roleName}' غير موجود.");
        //                        continue;
        //                    }

        //                    var addToRoleResult = await _userManager.AddToRoleAsync(user, roleName);
        //                    if (!addToRoleResult.Succeeded)
        //                    {
        //                        var roleErrors = addToRoleResult.Errors.Select(e => e.Description);
        //                        results.Add($"Row {row}: تم إنشاء المستخدم بنجاح ولكن فشل تعيين الدور - {string.Join(" | ", roleErrors)}");
        //                        continue;
        //                    }

        //                    results.Add($"Row {row}: تم إنشاء المستخدم وتعيين الدور '{roleName}' بنجاح.");
        //                }
        //                catch (Exception roleEx)
        //                {
        //                    results.Add($"Row {row}: تم إنشاء المستخدم بنجاح ولكن حدث خطأ أثناء تعيين الدور - {roleEx.Message}");
        //                }
        //            }
        //            else
        //            {
        //                results.Add($"Row {row}: تم إنشاء المستخدم بنجاح (بدون تعيين دور).");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            results.Add($"Row {row}: خطأ غير متوقع - {ex.Message}");
        //        }
        //    }

        //    // 9. Return Structured Response
        //    var successCount = results.Count(r => r.Contains("تم إنشاء المستخدم بنجاح"));
        //    var errorCount = results.Count - successCount;

        //    return Ok(new
        //    {
        //        TotalRowsProcessed = rowCount - 1,
        //        SuccessCount = successCount,
        //        ErrorCount = errorCount,
        //        Results = results
        //    });
        //}
        //[HttpPost("upload-users-excel")]
        //public async Task<IActionResult> UploadUsersExcel(IFormFile file)
        //{
        //    // 1. Validate File
        //    if (file == null || file.Length == 0)
        //        return BadRequest("No file uploaded.");

        //    if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        //        return BadRequest("Only .xlsx files are allowed.");

        //    var results = new List<string>();
        //    var validationErrors = new List<string>();

        //    using var stream = new MemoryStream();
        //    await file.CopyToAsync(stream);

        //    // 2. Set EPPlus LicenseContext
        //    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        //    using var package = new ExcelPackage(stream);
        //    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
        //    int rowCount = worksheet.Dimension?.Rows ?? 0;

        //    if (rowCount < 2)
        //        return BadRequest("Excel file has no data rows.");

        //    // Define column indexes
        //    const int UserNameCol = 1;
        //    const int PhoneNumberCol = 2;
        //    const int EmailCol = 3;
        //    const int PasswordCol = 4;
        //    const int FirstNameCol = 5;
        //    const int SecondNameCol = 6;
        //    const int ThirdNameCol = 7;
        //    const int ForthNameCol = 8;
        //    const int DateOfBirthCol = 9;
        //    const int GenderCol = 10;
        //    const int HireDateCol = 11;
        //    const int NationalIDCol = 12;
        //    const int PositionCol = 13;
        //    const int DepartmentCol = 14;
        //    const int DisabilityCol = 15;
        //    const int StreetCol = 16;
        //    const int GovernorateCol = 17;
        //    const int StateCol = 18;
        //    const int NormalLeavesCol = 19;
        //    const int CasualLeavesCol = 20;
        //    const int SickLeavesCol = 21;
        //    const int Leaves47Col = 22;
        //    const int Leaves81_3Col = 23;
        //    const int Leaves81_2Col = 24;
        //    const int Leaves81_1Col = 25;
        //    const int TotalDaysCol = 26;
        //    const int RoleNameCol = 27;

        //    // Get all existing emails and national IDs for duplicate checking
        //    var existingUserNames=await _userManager.Users.Select(u=>u.UserName).ToListAsync();
        //    var existingEmails = await _userManager.Users.Select(u => u.Email).ToListAsync();
        //    var existingNationalIds = await _userManager.Users.Select(u => u.NationalID).ToListAsync();

        //    // 3. Process Each Row
        //    for (int row = 2; row <= rowCount; row++)
        //    {
        //        try
        //        {
        //            // Get basic identifiers first
        //            var username= worksheet.Cells[row, UserNameCol].Text?.Trim(); ;
        //            var email = worksheet.Cells[row, EmailCol].Text?.Trim();
        //            var nationalId = worksheet.Cells[row, NationalIDCol].Text?.Trim();

        //            // Validate required fields
        //            if (string.IsNullOrWhiteSpace(email))
        //            {
        //                results.Add($"Row {row}: Email is required");
        //                continue;
        //            }

        //            if (string.IsNullOrWhiteSpace(nationalId))
        //            {
        //                results.Add($"Row {row}: National ID is required");
        //                continue;
        //            }

        //            // Check for duplicates
        //            if (existingUserNames.Contains(username))
        //            {
        //                results.Add($"Row {row}: User Name '{username}' already exists in system");
        //                continue;
        //            }
        //            if (existingEmails.Contains(email))
        //            {
        //                results.Add($"Row {row}: Email '{email}' already exists in system");
        //                continue;
        //            }

        //            if (existingNationalIds.Contains(nationalId))
        //            {
        //                results.Add($"Row {row}: National ID '{nationalId}' already exists in system");
        //                continue;
        //            }

        //            // Parse dates
        //            if (!DateTime.TryParse(worksheet.Cells[row, DateOfBirthCol].Text, out var dateOfBirth))
        //            {
        //                results.Add($"Row {row}: Invalid birth date format");
        //                continue;
        //            }

        //            if (!DateTime.TryParse(worksheet.Cells[row, HireDateCol].Text, out var hireDate))
        //            {
        //                results.Add($"Row {row}: Invalid hire date format");
        //                continue;
        //            }

        //            // Parse disability status
        //            bool disability = false;
        //            var disabilityValue = worksheet.Cells[row, DisabilityCol].Value;
        //            if (disabilityValue != null)
        //            {
        //                if (disabilityValue is string strValue)
        //                    disability = strValue.Trim() == "1" || strValue.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase);
        //                else if (disabilityValue is int intValue)
        //                    disability = intValue == 1;
        //                else if (double.TryParse(disabilityValue.ToString(), out double doubleValue))
        //                    disability = doubleValue == 1;
        //            }

        //            // Get role name
        //            var roleName = worksheet.Cells[row, RoleNameCol].Text?.Trim();

        //            // Create DTO
        //            var dto = new CreateUserDTO
        //            {
        //                UserName = worksheet.Cells[row, UserNameCol].Text?.Trim(),
        //                PhoneNumber = worksheet.Cells[row, PhoneNumberCol].Text?.Trim(),
        //                Email = email,
        //                Password = worksheet.Cells[row, PasswordCol].Text?.Trim(),
        //                FirstName = worksheet.Cells[row, FirstNameCol].Text?.Trim(),
        //                SecondName = worksheet.Cells[row, SecondNameCol].Text?.Trim(),
        //                ThirdName = worksheet.Cells[row, ThirdNameCol].Text?.Trim(),
        //                ForthName = worksheet.Cells[row, ForthNameCol].Text?.Trim(),
        //                DateOfBirth = dateOfBirth,
        //                Gender = worksheet.Cells[row, GenderCol].Text?.Trim(),
        //                HireDate = hireDate,
        //                NationalID = nationalId,
        //                position = int.TryParse(worksheet.Cells[row, PositionCol].Text, out var pos) ? pos : 0,
        //                Departement_ID = int.TryParse(worksheet.Cells[row, DepartmentCol].Text, out var dept) ? dept : (int?)null,
        //                Disability = disability,
        //                Street = worksheet.Cells[row, StreetCol].Text?.Trim(),
        //                governorate = worksheet.Cells[row, GovernorateCol].Text?.Trim(),
        //                State = worksheet.Cells[row, StateCol].Text?.Trim(),
        //                NormalLeavesCount = int.TryParse(worksheet.Cells[row, NormalLeavesCol].Text, out var normal) ? normal : 0,
        //                CasualLeavesCount = int.TryParse(worksheet.Cells[row, CasualLeavesCol].Text, out var casual) ? casual : 0,
        //                NonChronicSickLeavesCount = int.TryParse(worksheet.Cells[row, SickLeavesCol].Text, out var sick) ? sick : 0,
        //                NormalLeavesCount_47 = int.TryParse(worksheet.Cells[row, Leaves47Col].Text, out var n47) ? n47 : 0,
        //                NormalLeavesCount_81Before3Years = int.TryParse(worksheet.Cells[row, Leaves81_3Col].Text, out var n81_3) ? n81_3 : 0,
        //                NormalLeavesCount_81Before2Years = int.TryParse(worksheet.Cells[row, Leaves81_2Col].Text, out var n81_2) ? n81_2 : 0,
        //                NormalLeavesCount_81Before1Years = int.TryParse(worksheet.Cells[row, Leaves81_1Col].Text, out var n81_1) ? n81_1 : 0,
        //                HowManyDaysFrom81And47 = int.TryParse(worksheet.Cells[row, TotalDaysCol].Text, out var totalDays) ? totalDays : 0
        //            };

        //            // Validate DTO
        //            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(dto);
        //            var validationResults = new List<ValidationResult>();
        //            bool isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

        //            if (!isValid)
        //            {
        //                results.Add($"Row {row}: Validation failed - {string.Join(" | ", validationResults.Select(v => v.ErrorMessage))}");
        //                continue;
        //            }

        //            // Map to ApplicationUser
        //            var user = new ApplicationUser
        //            {
        //                UserName = dto.UserName,
        //                Email = dto.Email,
        //                PhoneNumber = dto.PhoneNumber,
        //                FirstName = dto.FirstName,
        //                SecondName = dto.SecondName,
        //                ThirdName = dto.ThirdName,
        //                ForthName = dto.ForthName,
        //                DateOfBirth = dto.DateOfBirth,
        //                Gender = dto.Gender,
        //                HireDate = dto.HireDate,
        //                NationalID = dto.NationalID,
        //                position = dto.position,
        //                Departement_ID = dto.Departement_ID,
        //                Disability = dto.Disability,
        //                Street = dto.Street,
        //                Governorate = dto.governorate,
        //                State = dto.State,
        //                NormalLeavesCount = dto.NormalLeavesCount,
        //                CasualLeavesCount = dto.CasualLeavesCount,
        //                NonChronicSickLeavesCount = dto.NonChronicSickLeavesCount,
        //                NormalLeavesCount_47 = dto.NormalLeavesCount_47,
        //                NormalLeavesCount_81Before3Years = dto.NormalLeavesCount_81Before3Years,
        //                NormalLeavesCount_81Before2Years = dto.NormalLeavesCount_81Before2Years,
        //                NormalLeavesCount_81Before1Years = dto.NormalLeavesCount_81Before1Years,
        //                HowManyDaysFrom81And47 = dto.HowManyDaysFrom81And47,
        //                Active = true
        //            };

        //            // Calculate years of service
        //            var today = DateTime.UtcNow;
        //            user.YearsOfWork = today.Year - user.HireDate.Year;
        //            if (user.HireDate.Month < 7) user.YearsOfWork++;
        //            if (today.Month < 7) user.YearsOfWork--;

        //            // Set leave section
        //            if (user.Disability)
        //            {
        //                user.LeaveSection = NormalLeaveSection.DisabilityEmployee;
        //            }
        //            else
        //            {
        //                var ageInYears = today.Year - user.DateOfBirth.Year;
        //                if (today.Month < user.DateOfBirth.Month ||
        //                   (today.Month == user.DateOfBirth.Month && today.Day < user.DateOfBirth.Day))
        //                {
        //                    ageInYears--;
        //                }

        //                if (ageInYears >= 50)
        //                    user.LeaveSection = NormalLeaveSection.FiftyAge;
        //                else if (user.YearsOfWork >= 10)
        //                    user.LeaveSection = NormalLeaveSection.TenYears;
        //                else if (user.YearsOfWork >= 1)
        //                    user.LeaveSection = NormalLeaveSection.OneYear;
        //                else
        //                    user.LeaveSection = NormalLeaveSection.NoSection;
        //            }

        //            // Create user
        //            var createResult = await _userManager.CreateAsync(user, dto.Password);

        //            if (!createResult.Succeeded)
        //            {
        //                results.Add($"Row {row}: User creation failed - {string.Join(" | ", createResult.Errors.Select(e => e.Description))}");
        //                continue;
        //            }

        //            // Assign role if specified
        //            if (!string.IsNullOrWhiteSpace(roleName))
        //            {
        //                var roleExists = await _roleService.IsRoleExisted(roleName);
        //                if (!roleExists)
        //                {
        //                    results.Add($"Row {row}: User created but role '{roleName}' doesn't exist");
        //                    continue;
        //                }

        //                var addToRoleResult = await _userManager.AddToRoleAsync(user, roleName);
        //                if (!addToRoleResult.Succeeded)
        //                {
        //                    results.Add($"Row {row}: User created but role assignment failed - {string.Join(" | ", addToRoleResult.Errors.Select(e => e.Description))}");
        //                    continue;
        //                }

        //                results.Add($"Row {row}: Successfully created user with role '{roleName}'");
        //            }
        //            else
        //            {
        //                results.Add($"Row {row}: Successfully created user (no role assigned)");
        //            }

        //            // Add to existing lists to prevent duplicates in same file
        //            existingEmails.Add(email);
        //            existingNationalIds.Add(nationalId);
        //        }
        //        catch (Exception ex)
        //        {
        //            results.Add($"Row {row}: Unexpected error - {ex.Message}");
        //        }
        //    }

        //    // Return results
        //    var successCount = results.Count(r => r.Contains("Successfully created"));
        //    var errorCount = results.Count - successCount;

        //    return Ok(new
        //    {
        //        TotalRowsProcessed = rowCount - 1,
        //        SuccessCount = successCount,
        //        ErrorCount = errorCount,
        //        Results = results
        //    });
        //}
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpPost("upload-users-excel")]
        public async Task<IActionResult> UploadUsersExcel(IFormFile file)
        {
            // 1. Validate File
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "No file uploaded." });

            if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { Message = "يُسمح فقط بملفات .xlsx." });


            var results = new List<(int Row, string Message, bool IsSuccess)>();
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            // 2. Set EPPlus LicenseContext
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage(stream);
            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
            int rowCount = worksheet.Dimension?.Rows ?? 0;

            if (rowCount < 2)
              return BadRequest(new { Message = "ملف الإكسيل لا يحتوي على بيانات." });


            // Define column indexes
            const int UserNameCol = 1;
            const int PhoneNumberCol = 2;
            const int EmailCol = 3;
            const int PasswordCol = 4;
            const int FirstNameCol = 5;
            const int SecondNameCol = 6;
            const int ThirdNameCol = 7;
            const int ForthNameCol = 8;
            const int DateOfBirthCol = 9;
            const int GenderCol = 10;
            const int HireDateCol = 11;
            const int NationalIDCol = 12;
            const int PositionCol = 13;
            const int DepartmentCol = 14;
            const int DisabilityCol = 15;
            const int StreetCol = 16;
            const int GovernorateCol = 17;
            const int StateCol = 18;
            const int NormalLeavesCol = 19;
            const int CasualLeavesCol = 20;
            const int SickLeavesCol = 21;
            const int Leaves47Col = 22;
            const int Leaves81_3Col = 23;
            const int Leaves81_2Col = 24;
            const int Leaves81_1Col = 25;
            const int TotalDaysCol = 26;
            const int RoleNameCol = 27;

            // Get all existing data for validation
            var existingUserNames = await _userManager.Users.Select(u => u.UserName).ToListAsync();
            var existingEmails = await _userManager.Users.Select(u => u.Email).ToListAsync();
            var existingNationalIds = await _userManager.Users.Select(u => u.NationalID).ToListAsync();
            var existingRoles = (await _roleService.GetAllRoles()).Select(r => r.Name).ToList();

            // 3. Process Each Row
            for (int row = 2; row <= rowCount; row++)
            {
                string username = null;
                string email = null;
                string nationalId = null;
                string roleName = null;

                try
                {
                    // Get basic identifiers first
                    username = worksheet.Cells[row, UserNameCol].Text?.Trim();
                    email = worksheet.Cells[row, EmailCol].Text?.Trim();
                    nationalId = worksheet.Cells[row, NationalIDCol].Text?.Trim();
                    roleName = worksheet.Cells[row, RoleNameCol].Text?.Trim();

                    // Validate required fields
                    if (string.IsNullOrWhiteSpace(username))
                    {
                        results.Add((row, "Username is required", false));
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(email))
                    {
                        results.Add((row, "Email is required", false));
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(nationalId))
                    {
                        results.Add((row, "National ID is required", false));
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(roleName))
                    {
                        results.Add((row, "Role is required", false));
                        continue;
                    }

                    // Check for duplicates
                    if (existingUserNames.Contains(username))
                    {
                        results.Add((row, $"Username '{username}' already exists", false));
                        continue;
                    }

                    if (existingEmails.Contains(email))
                    {
                        results.Add((row, $"Email '{email}' already exists", false));
                        continue;
                    }

                    if (existingNationalIds.Contains(nationalId))
                    {
                        results.Add((row, $"National ID '{nationalId}' already exists", false));
                        continue;
                    }

                    // Validate role exists BEFORE creating user
                    if (!existingRoles.Any(r => r.Equals(roleName, StringComparison.OrdinalIgnoreCase)))
                    {
                        results.Add((row, $"Role '{roleName}' does not exist - user not created", false));
                        continue; // Skip this row entirely
                    }

                    // Parse dates
                    if (!DateTime.TryParse(worksheet.Cells[row, DateOfBirthCol].Text, out var dateOfBirth))
                    {
                        results.Add((row, "Invalid birth date format", false));
                        continue;
                    }

                    if (!DateTime.TryParse(worksheet.Cells[row, HireDateCol].Text, out var hireDate))
                    {
                        results.Add((row, "Invalid hire date format", false));
                        continue;
                    }

                    // Parse disability status
                    bool disability = worksheet.Cells[row, DisabilityCol].GetValue<string>()?.Trim() == "1";

                    // Create DTO
                    var dto = new CreateUserDTO
                    {
                        UserName = username,
                        PhoneNumber = worksheet.Cells[row, PhoneNumberCol].Text?.Trim(),
                        Email = email,
                        Password = worksheet.Cells[row, PasswordCol].Text?.Trim(),
                        FirstName = worksheet.Cells[row, FirstNameCol].Text?.Trim(),
                        SecondName = worksheet.Cells[row, SecondNameCol].Text?.Trim(),
                        ThirdName = worksheet.Cells[row, ThirdNameCol].Text?.Trim(),
                        ForthName = worksheet.Cells[row, ForthNameCol].Text?.Trim(),
                        DateOfBirth = dateOfBirth,
                        Gender = worksheet.Cells[row, GenderCol].Text?.Trim(),
                        HireDate = hireDate,
                        NationalID = nationalId,
                        position = int.TryParse(worksheet.Cells[row, PositionCol].Text, out var pos) ? pos : 0,
                        Departement_ID = int.TryParse(worksheet.Cells[row, DepartmentCol].Text, out var dept) ? dept : (int?)null,
                        Disability = disability,
                        Street = worksheet.Cells[row, StreetCol].Text?.Trim(),
                        governorate = worksheet.Cells[row, GovernorateCol].Text?.Trim(),
                        State = worksheet.Cells[row, StateCol].Text?.Trim(),
                        NormalLeavesCount = int.TryParse(worksheet.Cells[row, NormalLeavesCol].Text, out var normal) ? normal : 0,
                        CasualLeavesCount = int.TryParse(worksheet.Cells[row, CasualLeavesCol].Text, out var casual) ? casual : 0,
                        NonChronicSickLeavesCount = int.TryParse(worksheet.Cells[row, SickLeavesCol].Text, out var sick) ? sick : 0,
                        NormalLeavesCount_47 = int.TryParse(worksheet.Cells[row, Leaves47Col].Text, out var n47) ? n47 : 0,
                        NormalLeavesCount_81Before3Years = int.TryParse(worksheet.Cells[row, Leaves81_3Col].Text, out var n81_3) ? n81_3 : 0,
                        NormalLeavesCount_81Before2Years = int.TryParse(worksheet.Cells[row, Leaves81_2Col].Text, out var n81_2) ? n81_2 : 0,
                        NormalLeavesCount_81Before1Years = int.TryParse(worksheet.Cells[row, Leaves81_1Col].Text, out var n81_1) ? n81_1 : 0,
                        HowManyDaysFrom81And47 = int.TryParse(worksheet.Cells[row, TotalDaysCol].Text, out var totalDays) ? totalDays : 0
                    };

                    // Validate DTO
                    var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(dto);
                    var validationResults = new List<ValidationResult>();
                    bool isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

                    if (!isValid)
                    {
                        results.Add((row, $"Validation failed: {string.Join(", ", validationResults.Select(v => v.ErrorMessage))}", false));
                        continue;
                    }

                    // Map to ApplicationUser
                    var user = new ApplicationUser
                    {
                        UserName = dto.UserName,
                        Email = dto.Email,
                        PhoneNumber = dto.PhoneNumber,
                        FirstName = dto.FirstName,
                        SecondName = dto.SecondName,
                        ThirdName = dto.ThirdName,
                        ForthName = dto.ForthName,
                        DateOfBirth = dto.DateOfBirth,
                        Gender = dto.Gender,
                        HireDate = dto.HireDate,
                        NationalID = dto.NationalID,
                        position = dto.position,
                        Departement_ID = dto.Departement_ID,
                        Disability = dto.Disability,
                        Street = dto.Street,
                        Governorate = dto.governorate,
                        State = dto.State,
                        NormalLeavesCount = dto.NormalLeavesCount,
                        CasualLeavesCount = dto.CasualLeavesCount,
                        NonChronicSickLeavesCount = dto.NonChronicSickLeavesCount,
                        NormalLeavesCount_47 = dto.NormalLeavesCount_47,
                        NormalLeavesCount_81Before3Years = dto.NormalLeavesCount_81Before3Years,
                        NormalLeavesCount_81Before2Years = dto.NormalLeavesCount_81Before2Years,
                        NormalLeavesCount_81Before1Years = dto.NormalLeavesCount_81Before1Years,
                        HowManyDaysFrom81And47 = dto.HowManyDaysFrom81And47,
                        Active = true
                    };

                    // Calculate years of service
                    user.YearsOfWork = CalculateYearsOfWork(user.HireDate);

                    // Set leave section
                    user.LeaveSection = DetermineLeaveSection(user);

                    // Create user (only reaches here if role exists)
                    var createResult = await _userManager.CreateAsync(user, dto.Password);

                    if (!createResult.Succeeded)
                    {
                        results.Add((row, $"User creation failed: {string.Join(", ", createResult.Errors.Select(e => e.Description))}", false));
                        continue;
                    }

                    // Assign role (already verified to exist)
                    var addToRoleResult = await _userManager.AddToRoleAsync(user, roleName);
                    if (!addToRoleResult.Succeeded)
                    {
                        // Rollback user creation if role assignment fails
                        await _userManager.DeleteAsync(user);
                        results.Add((row, $"Role assignment failed: {string.Join(", ", addToRoleResult.Errors.Select(e => e.Description))}", false));
                        continue;
                    }

                    results.Add((row, $"Successfully created user with role '{roleName}'", true));

                    // Add to existing lists to prevent duplicates in same file
                    existingUserNames.Add(username);
                    existingEmails.Add(email);
                    existingNationalIds.Add(nationalId);
                }
                catch (Exception ex)
                {
                    results.Add((row, $"Unexpected error: {ex.Message}", false));
                }
            }

            // Return structured results
            return Ok(new
            {
                TotalRows = rowCount - 1,
                SuccessfulRows = results.Count(r => r.IsSuccess),
                FailedRows = results.Count(r => !r.IsSuccess),
                Results = results.Select(r => new {
                    Row = r.Row,
                    Message = r.Message,
                    Status = r.IsSuccess ? "Success" : "Failed"
                })
            });
        }

        // Helper methods
        private int CalculateYearsOfWork(DateTime hireDate)
        {
            var today = DateTime.UtcNow;
            var years = today.Year - hireDate.Year;
            if (hireDate.Month < 7) years++;
            if (today.Month < 7) years--;
            return years;
        }

        private NormalLeaveSection DetermineLeaveSection(ApplicationUser user)
        {
            if (user.Disability)
                return NormalLeaveSection.DisabilityEmployee;

            var ageInYears = DateTime.UtcNow.Year - user.DateOfBirth.Year;
            if (DateTime.UtcNow.Month < user.DateOfBirth.Month ||
               (DateTime.UtcNow.Month == user.DateOfBirth.Month && DateTime.UtcNow.Day < user.DateOfBirth.Day))
            {
                ageInYears--;
            }

            return ageInYears >= 50 ? NormalLeaveSection.FiftyAge :
                   user.YearsOfWork >= 10 ? NormalLeaveSection.TenYears :
                   user.YearsOfWork >= 1 ? NormalLeaveSection.OneYear :
                   NormalLeaveSection.NoSection;
        }
        //[Authorize(Roles = "Admin")]
        //[HttpPost("update-users-from-excel")]
        //public async Task<IActionResult> UpdateUsersFromExcel(IFormFile file)
        //{
        //    try
        //    {
        //        // 1. Validate File
        //        if (file == null || file.Length == 0)
        //            return BadRequest(new { Message = "No file uploaded" });

        //        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        //            return BadRequest(new { Message = "Only .xlsx files are allowed" });

        //        var results = new List<string>();
        //        var validationErrors = new List<string>();

        //        using var stream = new MemoryStream();
        //        await file.CopyToAsync(stream);

        //        // 2. Set EPPlus LicenseContext
        //        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        //        using var package = new ExcelPackage(stream);
        //        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
        //        int rowCount = worksheet.Dimension?.Rows ?? 0;

        //        if (rowCount < 2)
        //            return BadRequest(new { Message = "Excel file has no data rows" });

        //        // Define column indexes (should match your Excel template)
        //        const int NationalIDCol = 1;
        //        const int UserNameCol = 2;
        //        const int PhoneNumberCol = 3;
        //        const int EmailCol = 4;
        //        const int FirstNameCol = 5;
        //        const int SecondNameCol = 6;
        //        const int ThirdNameCol = 7;
        //        const int ForthNameCol = 8;
        //        const int DateOfBirthCol = 9;
        //        const int GenderCol = 10;
        //        const int HireDateCol = 11;
        //        const int PositionCol = 12;
        //        const int DepartmentCol = 13;
        //        const int DisabilityCol = 14;
        //        const int StreetCol = 15;
        //        const int GovernorateCol = 16;
        //        const int StateCol = 17;
        //        const int NormalLeavesCol = 18;
        //        const int CasualLeavesCol = 19;
        //        const int SickLeavesCol = 20;
        //        const int Leaves47Col = 21;
        //        const int Leaves81_3Col = 22;
        //        const int Leaves81_2Col = 23;
        //        const int Leaves81_1Col = 24;
        //        const int TotalDaysCol = 25;
        //        const int RoleCol = 26;

        //        // 3. Process Each Row
        //        for (int row = 2; row <= rowCount; row++)
        //        {
        //            try
        //            {
        //                string nationalId = worksheet.Cells[row, NationalIDCol].Text?.Trim();
        //                if (string.IsNullOrEmpty(nationalId))
        //                {
        //                    results.Add($"Row {row}: National ID is required");
        //                    continue;
        //                }

        //                // Find user by National ID
        //                var user = await _userManager.Users
        //                    .FirstOrDefaultAsync(u => u.NationalID == nationalId);

        //                if (user == null)
        //                {
        //                    results.Add($"Row {row}: User with National ID {nationalId} not found");
        //                    continue;
        //                }

        //                // Parse and validate data
        //                if (!DateTime.TryParse(worksheet.Cells[row, DateOfBirthCol].Text, out var dateOfBirth))
        //                {
        //                    results.Add($"Row {row}: Invalid Date of Birth format");
        //                    continue;
        //                }

        //                if (!DateTime.TryParse(worksheet.Cells[row, HireDateCol].Text, out var hireDate))
        //                {
        //                    results.Add($"Row {row}: Invalid Hire Date format");
        //                    continue;
        //                }

        //                bool disability = worksheet.Cells[row, DisabilityCol].Text?.Trim() == "1";

        //                // Update user properties
        //                user.UserName = worksheet.Cells[row, UserNameCol].Text?.Trim() ?? user.UserName;
        //                user.PhoneNumber = worksheet.Cells[row, PhoneNumberCol].Text?.Trim() ?? user.PhoneNumber;
        //                user.Email = worksheet.Cells[row, EmailCol].Text?.Trim() ?? user.Email;
        //                user.FirstName = worksheet.Cells[row, FirstNameCol].Text?.Trim() ?? user.FirstName;
        //                user.SecondName = worksheet.Cells[row, SecondNameCol].Text?.Trim() ?? user.SecondName;
        //                user.ThirdName = worksheet.Cells[row, ThirdNameCol].Text?.Trim() ?? user.ThirdName;
        //                user.ForthName = worksheet.Cells[row, ForthNameCol].Text?.Trim() ?? user.ForthName;
        //                user.DateOfBirth = dateOfBirth;
        //                user.Gender = worksheet.Cells[row, GenderCol].Text?.Trim() ?? user.Gender;
        //                user.HireDate = hireDate;
        //                user.position = int.TryParse(worksheet.Cells[row, PositionCol].Text, out var pos) ? pos : user.position;
        //                user.Departement_ID = int.TryParse(worksheet.Cells[row, DepartmentCol].Text, out var dept) ? dept : user.Departement_ID;
        //                user.Disability = disability;
        //                user.Street = worksheet.Cells[row, StreetCol].Text?.Trim() ?? user.Street;
        //                user.Governorate = worksheet.Cells[row, GovernorateCol].Text?.Trim() ?? user.Governorate;
        //                user.State = worksheet.Cells[row, StateCol].Text?.Trim() ?? user.State;
        //                user.NormalLeavesCount = int.TryParse(worksheet.Cells[row, NormalLeavesCol].Text, out var normal) ? normal : user.NormalLeavesCount;
        //                user.CasualLeavesCount = int.TryParse(worksheet.Cells[row, CasualLeavesCol].Text, out var casual) ? casual : user.CasualLeavesCount;
        //                user.NonChronicSickLeavesCount = int.TryParse(worksheet.Cells[row, SickLeavesCol].Text, out var sick) ? sick : user.NonChronicSickLeavesCount;
        //                user.NormalLeavesCount_47 = int.TryParse(worksheet.Cells[row, Leaves47Col].Text, out var n47) ? n47 : user.NormalLeavesCount_47;
        //                user.NormalLeavesCount_81Before3Years = int.TryParse(worksheet.Cells[row, Leaves81_3Col].Text, out var n81_3) ? n81_3 : user.NormalLeavesCount_81Before3Years;
        //                user.NormalLeavesCount_81Before2Years = int.TryParse(worksheet.Cells[row, Leaves81_2Col].Text, out var n81_2) ? n81_2 : user.NormalLeavesCount_81Before2Years;
        //                user.NormalLeavesCount_81Before1Years = int.TryParse(worksheet.Cells[row, Leaves81_1Col].Text, out var n81_1) ? n81_1 : user.NormalLeavesCount_81Before1Years;
        //                user.HowManyDaysFrom81And47 = int.TryParse(worksheet.Cells[row, TotalDaysCol].Text, out var totalDays) ? totalDays : user.HowManyDaysFrom81And47;

        //                // Update years of work calculation
        //                var init = user.HireDate;
        //                var today = DateTime.UtcNow;
        //                user.YearsOfWork = today.Year - init.Year;
        //                if (init.Month < 7) user.YearsOfWork++;
        //                if (today.Month < 7) user.YearsOfWork--;

        //                // Update leave section
        //                if (user.Disability)
        //                {
        //                    user.LeaveSection = NormalLeaveSection.DisabilityEmployee;
        //                }
        //                else
        //                {
        //                    var ageInYears = DateTime.UtcNow.Year - user.DateOfBirth.Year;
        //                    if (DateTime.UtcNow.Month < user.DateOfBirth.Month ||
        //                       (DateTime.UtcNow.Month == user.DateOfBirth.Month && DateTime.UtcNow.Day < user.DateOfBirth.Day))
        //                    {
        //                        ageInYears--;
        //                    }

        //                    if (ageInYears >= 50)
        //                        user.LeaveSection = NormalLeaveSection.FiftyAge;
        //                    else if (user.YearsOfWork >= 10)
        //                        user.LeaveSection = NormalLeaveSection.TenYears;
        //                    else if (user.YearsOfWork >= 1)
        //                        user.LeaveSection = NormalLeaveSection.OneYear;
        //                    else
        //                        user.LeaveSection = NormalLeaveSection.NoSection;
        //                }

        //                // Handle role update
        //                string newRole = worksheet.Cells[row, RoleCol].Text?.Trim();
        //                if (!string.IsNullOrEmpty(newRole))
        //                {
        //                    // Get current roles
        //                    var currentRoles = await _userManager.GetRolesAsync(user);

        //                    // Remove all existing roles
        //                    var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        //                    if (!removeResult.Succeeded)
        //                    {
        //                        results.Add($"Row {row}: Failed to remove existing roles for user {nationalId}");
        //                        continue;
        //                    }

        //                    // Add new role
        //                    var roleExists = await _roleService.IsRoleExisted(newRole);
        //                    if (roleExists)
        //                    {
        //                        var addResult = await _userManager.AddToRoleAsync(user, newRole);
        //                        if (!addResult.Succeeded)
        //                        {
        //                            results.Add($"Row {row}: Failed to add role {newRole} to user {nationalId}");
        //                        }
        //                        else
        //                        {
        //                            results.Add($"Row {row}: Updated role to {newRole}");
        //                        }
        //                    }
        //                    else
        //                    {
        //                        results.Add($"Row {row}: Role {newRole} does not exist - role not updated");
        //                    }
        //                }

        //                // Save user changes
        //                var updateResult = await _userManager.UpdateAsync(user);
        //                if (!updateResult.Succeeded)
        //                {
        //                    results.Add($"Row {row}: Failed to update user {nationalId}. Errors: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
        //                }
        //                else
        //                {
        //                    results.Add($"Row {row}: Successfully updated user {nationalId}");
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                results.Add($"Row {row}: Error processing user - {ex.Message}");
        //            }
        //        }

        //        // 4. Return results
        //        var successCount = results.Count(r => r.Contains("Successfully updated"));
        //        var errorCount = results.Count - successCount;

        //        return Ok(new
        //        {
        //            TotalRowsProcessed = rowCount - 1,
        //            SuccessCount = successCount,
        //            ErrorCount = errorCount,
        //            Results = results
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError,
        //            new { Message = "An unexpected error occurred", Error = ex.Message });
        //    }
        //}
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpPost("CreateUser/{RoleName}")]
        public async Task<IActionResult> CreateUser([FromRoute]string RoleName, [FromBody] CreateUserDTO model) 
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (RoleName == "عميد الكلية" || RoleName == "أمين الكلية" || RoleName == "مدير الموارد البشرية") // ??????????
                    {
                        var list = await _accountService.GetAllUsersInRole(RoleName);
                        if (list.Any()) return BadRequest(new { Message = $"يوجد بالفعل مستخدم لديه دور {RoleName}، يجب أن يُخصص هذا الدور لمستخدم واحد فقط." });
                    }
                    if (await _accountService.FindByEmail(model.Email) is not null)
                        return BadRequest(new { Message = "هذا البريد الإلكتروني مسجل بالفعل!" });

                    if (await _accountService.FindByNationalId(model.NationalID) is not null)
                        return BadRequest(new { Message = "هذا الرقم القومي مسجل بالفعل!" });

                    if (await _accountService.FindByName(model.UserName) is not null)
                        return BadRequest(new { Message = "هذا اسم المستخدم مسجل بالفعل!" });

                    if (!await _roleService.IsRoleExisted(RoleName))
                        return BadRequest(new { Message = "معرف المستخدم أو المنصب غير صالح!" });

                    if (model.Departement_ID != null)
                    {
                        if (await _deptBase.Get(d => d.Id == model.Departement_ID) is null)
                            return BadRequest(new { Message = "القسم غير صالح!" });
                    }
                    var res = await _accountService.Create(RoleName,model);
                    if (res.Succeeded)
                    {
                        var user = await _accountService.FindByNationalId(model.NationalID);
                        var ress = await _accountService.AddUserToRole(user, RoleName);
                        if (ress.Succeeded)
                        {
                            var ReturnedUser = _mapper.Map<UserDTO>(user);
                            ReturnedUser.FullName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                            if(user.Departement_ID != null)
                            {
                                var dpt = await _deptBase.Get(d => d.Id == user.Departement_ID);
                                ReturnedUser.DepartmentName = dpt.Name;
                            }
                            var DepartmentManager = await _deptBase.Get(d => d.ManagerId == ReturnedUser.Id);
                            if (DepartmentManager != null) ReturnedUser.IsDirectManager = true;
                            else ReturnedUser.IsDirectManager = false;
                            return CreatedAtAction(nameof(GetUserById), new { userID = user.Id }, ReturnedUser);
                        }
                        return BadRequest(ress.Errors);
                    }
                    return BadRequest(res.Errors);
                }
                return BadRequest(ModelState);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [AllowAnonymous]
        [HttpPost("UserLogin")]
        public async Task<IActionResult> Login([FromBody] LogInUserDTO model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = await _accountService.FindByName(model.UserName);
                    bool checkPassword = await _accountService.CheckPassword(user, model.Password);
                    if (user is null || !checkPassword)
                    {
                        return Unauthorized(new { Message = "اسم المستخدم أو كلمة المرور غير صحيحة!" });
                    }

                    var res = await _accountService.GetTokenAsync(user);
                    if (res.IsAuthenticated)
                    {
                        var DepartmentManager = await _deptBase.Get(d => d.ManagerId == res.Id);
                        if (DepartmentManager != null) res.IsDirectManager = true;
                        else res.IsDirectManager = false;
                        await _accountService.TransferingUserNormalLeaveCountToNewSection(user);
                        return Ok(res);
                    }
                    return Unauthorized(new {Message = res.Message});
                }
                return BadRequest(ModelState);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpPut("UpdateUser/{userid}")]
        public async Task<IActionResult> UdpateUser(string userid,[FromBody]UpdateUserDTO model)
        {
            if (string.IsNullOrWhiteSpace(userid) || string.IsNullOrWhiteSpace(model.RoleName))
                return BadRequest(new { message = "معرف المستخدم أو المنصب غير صحيح." });


            try
            {
                if (ModelState.IsValid)
                {
                    var user = await _accountService.FindById(userid);
                    if (user == null) return NotFound(new { Message = "لم يتم العثور على المستخدم." });

                    if (model.RoleName == "عميد الكلية" || model.RoleName == "أمين الكلية" || model.RoleName == "مدير الموارد البشرية") // ??????????
                    {
                        var list = await _accountService.GetAllUsersInRole(model.RoleName);
                        if (list.Any())
                        {
                            var manager = list.FirstOrDefault();
                            if (manager != null)
                            {
                                if (manager.Id != userid)
                                    return BadRequest(new { Message = $"يوجد بالفعل مستخدم لديه دور {model.RoleName}، يجب أن يكون هذا الدور مخصصًا لمستخدم واحد فقط." });
                            }
                        }
                    }
                    var u = await _accountService.FindByEmail(model.Email);
                    if (u != null)
                    {
                        if (u.Id != userid)
                            return BadRequest(new { Message = "هذا البريد الإلكتروني مسجل بالفعل!" });
                    }

                    u = await _accountService.FindByNationalId(model.NationalID);
                    if (u != null)
                    {
                        if (u.Id != userid)
                            return BadRequest(new { Message = "هذا الرقم القومي مسجل بالفعل!" });
                    }

                    u = await _accountService.FindByName(model.UserName);
                    if (u != null)
                    {
                        if (u.Id != userid)
                            return BadRequest(new { Message = "هذا اسم المستخدم مسجل بالفعل!" });
                    }

                    if (!await _roleService.IsRoleExisted(model.RoleName))
                        return BadRequest(new { Message = "منصب غير صالح!" });

                    if (model.Departement_ID != null)
                    {
                        if (await _deptBase.Get(d => d.Id == model.Departement_ID) is null)
                            return BadRequest(new { Message = "القسم غير صالح!" });
                    }

                    _mapper.Map(model, user);
                    if (model.Disability == true)
                    {
                        user.LeaveSection = NormalLeaveSection.DisabilityEmployee;
                    }
                    else
                    {
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

                    var res = await _accountService.Update(user);
                    if (res.Succeeded)
                    {
                        var roles = await _accountService.GetAllRolesOfUser(user);
                        foreach (var role in roles)
                        {
                            await _accountService.RemoveUserFromRole(user, role);
                        }
                        await _accountService.AddUserToRole(user, model.RoleName);

                        var ReturnedUser = _mapper.Map<UserDTO>(user);
                        ReturnedUser.FullName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                        ReturnedUser.RoleName = await _accountService.GetFirstRole(user);
                        if (user.Departement_ID != null)
                        {
                            var dpt = await _deptBase.Get(d => d.Id == user.Departement_ID);
                            if(dpt!=null) ReturnedUser.DepartmentName = dpt.Name;
                        }
                        var DepartmentManager = await _deptBase.Get(d => d.ManagerId == ReturnedUser.Id);
                        if (DepartmentManager != null) ReturnedUser.IsDirectManager = true;
                        else ReturnedUser.IsDirectManager = false;
                        return Ok(new { Message = "تم التحديث بنجاح.", User = ReturnedUser });
                    }
                    return BadRequest(res.Errors);
                }
                return BadRequest(ModelState);

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpPut("UdpateUserForUser/{userid}")]
        public async Task<IActionResult> UdpateUserForUser(string userid, [FromBody] UpdateUserDTOforuser model)
        {
            if (string.IsNullOrWhiteSpace(userid))
                return BadRequest(new { message = "معرف المستخدم غير صالح." });

            try
            {
                if (ModelState.IsValid)
                {
                    //var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var user = await _accountService.FindById(userid);
                    if (user == null) return NotFound(new { Message = "لم يتم العثور على المستخدم." });

                    var u = await _accountService.FindByEmail(model.Email);
                    if (u != null)
                    {
                        if (u.Id != userid)
                            return BadRequest(new { Message = "هذا البريد الإلكتروني مسجل بالفعل!" });
                    }

                    user.Email = model.Email;
                    user.PhoneNumber = model.PhoneNumber;
                    user.Street = model.Street;
                    user.Governorate= model.Governorate;
                    user.State= model.State;

                    var res = await _accountService.Update(user);
                    if (res.Succeeded)
                    {
                        return Ok(new { Message = "تم التحديث بنجاح." });
                    }
                    return BadRequest(res.Errors);
                }
                return BadRequest(ModelState);

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpPut("SoftDeleteUser/{userid}")]
        public async Task<IActionResult> SoftDeleteUser(string userid)
        {
            if (string.IsNullOrWhiteSpace(userid))
                return BadRequest(new { message = "معرف المستخدم غير صالح." });
            try
            {
                var user = await _accountService.FindById(userid);
                if (user != null)
                {
                    var roleName = await _accountService.GetFirstRole(user);
                    if (roleName == "عميد الكلية" || roleName == "أمين الكلية" || roleName == "مدير الموارد البشرية") // ??????????
                    {
                        return BadRequest(new { Message = $"هذا المستخدم لديه منصب {roleName}. قبل حذف هذا المستخدم، يجب تعيين منصب {roleName} لمستخدم جديد." });
                        //return BadRequest(new { Message = $"This user has {roleName} role. Before deleting this user, you should assign {roleName} role to new user." });
                    }
                    var IsDeptHead = await _deptBase.Get(d => d.ManagerId == user.Id);
                    if (IsDeptHead != null)
                    {
                        return BadRequest(new { Message = $"هذا المستخدم هو مدير قسم {IsDeptHead.Name}، يجب تعيين مدير جديد لهذا القسم قبل حذف هذا المستخدم." });
                        // return BadRequest(new { Message = $"This user is {IsDeptHead.Name} department manager, you should assign a new manager to this department before deleting this user." });
                    }
                    if (user.Active)
                    {
                        user.Active = false;
                        await _accountService.Update(user);
                        return Ok(new { Message = "تم الحذف بنجاح." });
                    }
                    else
                    {
                        return BadRequest(new { Message = "المستخدم غير نشط بالفعل." });
                    }


                    //var res = await _accountService.Delete(user);
                    //if (res.Succeeded)
                    //{
                    //    return Ok(new { Message = "Deletion is succeeded" });
                    //}
                    //return BadRequest(res.Errors);
                }
                return NotFound(new { Message = "المستخدم غير موجود." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpPut("ReActiveUser/{userid}")]
        public async Task<IActionResult> ReActiveUser(string userid)
        {
            if (string.IsNullOrWhiteSpace(userid))
                return BadRequest(new { message = "معرف المستخدم غير صالح." });
            try
            {
                var user = await _accountService.FindById(userid);
                if (user != null)
                {
                    if (!user.Active)   
                    {
                        user.Active = true;
                        await _accountService.Update(user);
                        return Ok(new { Message = "تم التنشيط بنجاح." });
                    }
                    else
                    {
                        return BadRequest(new { Message = "المستخدم نشط بالفعل." });
                    }

                    //var res = await _accountService.Delete(user);
                    //if (res.Succeeded)
                    //{
                    //    return Ok(new { Message = "Deletion is succeeded" });
                    //}
                    //return BadRequest(res.Errors);
                }
                return NotFound(new { Message = "المستخدم غير موجود." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
    }
}