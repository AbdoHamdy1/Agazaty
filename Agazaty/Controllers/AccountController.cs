﻿using Agazaty.Data.Base;
using Agazaty.Data.DTOs.AccountDTOs;
using Agazaty.Data.Email.DTOs;
using Agazaty.Data.Enums;
using Agazaty.Data.Services.Interfaces;
using Agazaty.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
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
        private readonly IEntityBaseRepository<Department> _deptBase;
        private readonly IMapper _mapper;
        public AccountController(IMapper mapper, IAccountService accountService, SignInManager<ApplicationUser> signInManager, IRoleService roleService, IEntityBaseRepository<Department> deptBase)
        {
            _accountService = accountService;
            _signInManager = signInManager;
            _roleService = roleService;
            _deptBase = deptBase;
            _mapper = mapper;
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
                return NotFound(".لم يتم العثور على المستخدم");

            var result = await _accountService.ChangePassword(user, model);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(new { Errors = errors });
            }
            return Ok(".تم تغيير كلمة المرور بنجاح");
        }
        //[Authorize]
        [HttpGet("GetUserById/{userID}")]
        public async Task<IActionResult> GetUserById([FromRoute]string userID)
        {
            if (string.IsNullOrWhiteSpace(userID))
                return BadRequest(new { message = ".معرف المستخدم غير صالح" });
            try
            {
                var user = await _accountService.FindById(userID);
                if (user == null)
                {
                    return NotFound(new { Message = ".لم يتم العثور على المستخدم" });
                }

                var ReturnedUser = _mapper.Map<UserDTO>(user);
                ReturnedUser.FullName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                ReturnedUser.RoleName = await _accountService.GetFirstRole(user);
                if (user.Departement_ID != null)
                {
                    var dept = await _deptBase.Get(d => d.Id == user.Departement_ID);
                    ReturnedUser.DepartmentName = dept.Name;
                }
                return Ok(ReturnedUser);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ".حدث خطأ أثناء معالجة طلبك", error = ex.Message });
            }
        }
        //[Authorize]
        [HttpGet("GetRoleOfUser/{userID}")]
        public async Task<IActionResult> GetRoleOfUser([FromRoute] string userID)
        {
            if (string.IsNullOrWhiteSpace(userID))
                return BadRequest(new { message = ".معرف المستخدم غير صالح" });
            try
            {
                var user = await _accountService.FindById(userID);
                if (user == null)
                {
                    return NotFound(new { Message = ".لم يتم العثور على المستخدم" });
                }
                var role =await _accountService.GetFirstRole(user);
                return Ok(new{role});
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ".حدث خطأ أثناء معالجة طلبك", error = ex.Message });
            }
        }
        //[Authorize(Roles = "عميد الكلية,أمين الكلية,مدير الموارد البشرية")]
        [HttpGet("GetUserByNationalId/{NationalId}")]
        public async Task<IActionResult> GetUserByNationalId(string NationalId)
        {
            if (string.IsNullOrWhiteSpace(NationalId))
                return BadRequest(new { message = ".معرف المستخدم غير صالح" });
            try
            {
                var user = await _accountService.FindByNationalId(NationalId);
                if (user == null) return NotFound(new { Message = ".لم يتم العثور على المستخدم" });

                var ReturnedUser = _mapper.Map<UserDTO>(user);
                ReturnedUser.FullName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                ReturnedUser.RoleName = await _accountService.GetFirstRole(user);
                if (user.Departement_ID != null)
                {
                    var dpt = await _deptBase.Get(d => d.Id == user.Departement_ID);
                    ReturnedUser.DepartmentName = dpt.Name;
                }
                return Ok(ReturnedUser);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ".حدث خطأ أثناء معالجة طلبك", error = ex.Message });
            }
        }
        //[Authorize(Roles = "عميد الكلية,أمين الكلية,مدير الموارد البشرية")]
        [HttpGet("GetAllActiveUsers")]
        public async Task<IActionResult> GetAllActiveUsers()
        {
            try
            {
                var users = await _accountService.GetAllActiveUsers();
                if (!users.Any()) { return NotFound(new { Message = ".لم يتم العثور على أي مستخدمين" }); }

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
                }
                return Ok(ReturnedUsers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ".حدث خطأ أثناء معالجة طلبك", error = ex.Message });
            }
        }
        //[Authorize(Roles = "عميد الكلية,أمين الكلية,مدير الموارد البشرية")]
        [HttpGet("GetAllNonActiveUsers")]
        public async Task<IActionResult> GetAllNonActiveUsers()
        {
            try
            {
                var users = await _accountService.GetAllNonActiveUsers();
                if (!users.Any()) { return NotFound(new { Message = ".لم يتم العثور على أي مستخدمين" }); }

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
                }
                return Ok(ReturnedUsers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ".حدث خطأ أثناء معالجة طلبك", error = ex.Message });
            }
        }
        //[Authorize]
        [HttpGet("GetAllAvailabelCoworkers/{userId}")]
        public async Task<IActionResult> GetAllAvailableCoworkers(string userId)
        {
            try
            {
                var u = await _accountService.FindById(userId);
                if (u == null) return NotFound(new { Message = ".لم يتم العثور على المستخدم" });

                var users = await _accountService.GetAllActiveAvailableCoworkers(u);
                if (!users.Any()) { return NotFound(new { Message = ".لم يتم العثور على أي مستخدمين" }); }

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
                return StatusCode(500, new { message = ".حدث خطأ أثناء معالجة طلبك", error = ex.Message });
            }
        }
        //[Authorize(Roles = "عميد الكلية,أمين الكلية,مدير الموارد البشرية")]
        [HttpGet("GetAllUsersByDepartmentId/{DepartmentId}")]
        public async Task<IActionResult> GetAllUsersByDepartmentId(int DepartmentId)
        {
            if (DepartmentId <= 0)
                return BadRequest(new {Message = ".معرف القسم غير صالح" });
            try
            {
                var dept = await _deptBase.Get(d=>d.Id==DepartmentId);
                if (dept == null) return NotFound(new { Message = ".لم يتم العثور على قسم بهذا المعرف" });

                var users = await _accountService.GetAllUsersByDepartmentId(DepartmentId);
                if(!users.Any()) return NotFound(new { Message = ".لم يتم العثور على مستخدمين في هذا القسم" });

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
                }
                return Ok(ReturnedUsers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ".حدث خطأ أثناء معالجة طلبك", error = ex.Message });
            }
        }        
        //[Authorize(Roles = "مدير الموارد البشرية")]
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
                        if (list.Any()) return BadRequest(new { Message = $".يوجد بالفعل مستخدم لديه دور {RoleName} ، يجب أن يُخصص هذا الدور لمستخدم واحد فقط" });
                    }
                    if (await _accountService.FindByEmail(model.Email) is not null)
                        return BadRequest(new { Message = "!هذا البريد الإلكتروني مسجل بالفعل" });

                    if (await _accountService.FindByNationalId(model.NationalID) is not null)
                        return BadRequest(new { Message = "!هذا الرقم القومي مسجل بالفعل" });

                    if (await _accountService.FindByName(model.UserName) is not null)
                        return BadRequest(new { Message = "!هذا اسم المستخدم مسجل بالفعل" });

                    if (!await _roleService.IsRoleExisted(RoleName))
                        return BadRequest(new { Message = "!معرف المستخدم أو المنصب غير صالح" });

                    if (model.Departement_ID != null)
                    {
                        if (await _deptBase.Get(d => d.Id == model.Departement_ID) is null)
                            return BadRequest(new { Message = "!القسم غير صالح" });
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
                return StatusCode(500, new { message = ".حدث خطأ أثناء معالجة طلبك", error = ex.Message });
            }
        }
        //[AllowAnonymous]
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
                        return Unauthorized(new { Message = "!اسم المستخدم أو كلمة المرور غير صحيحة" });
                    }

                    var res = await _accountService.GetTokenAsync(user);
                    if (res.IsAuthenticated)
                    {
                        await _accountService.TransferingUserNormalLeaveCountToNewSection(user);
                        return Ok(res);
                    }
                    return Unauthorized(new {Message = res.Message});
                }
                return BadRequest(ModelState);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ".حدث خطأ أثناء معالجة طلبك", error = ex.Message });
            }
        }
        //[Authorize(Roles = "مدير الموارد البشرية")]
        [HttpPut("UpdateUser/{userid}/{RoleName}")]
        public async Task<IActionResult> UdpateUser(string userid, string RoleName, [FromBody]UpdateUserDTO model)
        {
            if (string.IsNullOrWhiteSpace(userid) || string.IsNullOrWhiteSpace(RoleName))
                return BadRequest(new { message = ".معرف المستخدم أو المنصب غير صحيح" });


            try
            {
                if (ModelState.IsValid)
                {
                    var user = await _accountService.FindById(userid);
                    if (user == null) return NotFound(new { Message = ".لم يتم العثور على المستخدم" });

                    if (RoleName == "عميد الكلية" || RoleName == "أمين الكلية" || RoleName == "مدير الموارد البشرية") // ??????????
                    {
                        var list = await _accountService.GetAllUsersInRole(RoleName);
                        if (list.Any())
                        {
                            var manager = list.FirstOrDefault();
                            if (manager != null)
                            {
                                if (manager.Id != userid)
                                    return BadRequest(new { Message = $".يوجد بالفعل مستخدم لديه دور {RoleName}، يجب أن يكون هذا الدور مخصصًا لمستخدم واحد فقط" });                            }
                        }
                    }
                    var u = await _accountService.FindByEmail(model.Email);
                    if (u != null)
                    {
                        if (u.Id != userid)
                            return BadRequest(new { Message = "!هذا البريد الإلكتروني مسجل بالفعل" });
                    }

                    u = await _accountService.FindByNationalId(model.NationalID);
                    if (u != null)
                    {
                        if (u.Id != userid)
                            return BadRequest(new { Message = "!هذا الرقم القومي مسجل بالفعل" });
                    }

                    u = await _accountService.FindByName(model.UserName);
                    if (u != null)
                    {
                        if (u.Id != userid)
                            return BadRequest(new { Message = "!هذا اسم المستخدم مسجل بالفعل" });
                    }

                    if (!await _roleService.IsRoleExisted(RoleName))
                        return BadRequest(new { Message = "!منصب غير صالح" });

                    if (model.Departement_ID != null)
                    {
                        if (await _deptBase.Get(d => d.Id == model.Departement_ID) is null)
                            return BadRequest(new { Message = "!القسم غير صالح" });
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
                        await _accountService.AddUserToRole(user, RoleName);

                        var ReturnedUser = _mapper.Map<UserDTO>(user);
                        ReturnedUser.FullName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                        ReturnedUser.RoleName = await _accountService.GetFirstRole(user);
                        if (user.Departement_ID != null)
                        {
                            var dpt = await _deptBase.Get(d => d.Id == user.Departement_ID);
                            if(dpt!=null) ReturnedUser.DepartmentName = dpt.Name;
                        }
                        return Ok(new { Message = ".تم التحديث بنجاح", User = ReturnedUser });
                    }
                    return BadRequest(res.Errors);
                }
                return BadRequest(ModelState);

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ".حدث خطأ أثناء معالجة طلبك", error = ex.Message });
            }
        }
        //[Authorize]
        [HttpPut("UdpateUserForUser/{userid}")]
        public async Task<IActionResult> UdpateUserForUser(string userid, [FromBody] UpdateUserDTOforuser model)
        {
            if (string.IsNullOrWhiteSpace(userid))
                return BadRequest(new { message = "Invalid user ID." });

            try
            {
                if (ModelState.IsValid)
                {
                    //var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var user = await _accountService.FindById(userid);
                    if (user == null) return NotFound(new { Message = "User is not found" });

                    var u = await _accountService.FindByEmail(model.Email);
                    if (u != null)
                    {
                        if (u.Id != userid)
                            return BadRequest(new { Message = "Email is already registered!" });
                    }

                    user.Email = model.Email;
                    user.PhoneNumber = model.PhoneNumber;
                    user.Street = model.Street;
                    user.Governorate= model.Governorate;
                    user.State= model.State;

                    var res = await _accountService.Update(user);
                    if (res.Succeeded)
                    {
                        return Ok(new { Message = "Update is succeeded"});
                    }
                    return BadRequest(res.Errors);
                }
                return BadRequest(ModelState);

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ".حدث خطأ أثناء معالجة طلبك", error = ex.Message });
            }
        }
        //[Authorize(Roles = "مدير الموارد البشرية")]
        [HttpPut("SoftDeleteUser/{userid}")]
        public async Task<IActionResult> SoftDeleteUser(string userid)
        {
            if (string.IsNullOrWhiteSpace(userid))
                return BadRequest(new { message = ".معرف المستخدم غير صالح" });
            try
            {
                var user = await _accountService.FindById(userid);
                if (user != null)
                {
                    var roleName = await _accountService.GetFirstRole(user);
                    if (roleName == "عميد الكلية" || roleName == "أمين الكلية" || roleName == "مدير الموارد البشرية") // ??????????
                    {
                        return BadRequest(new { Message = $".هذا المستخدم لديه منصب {roleName}. قبل حذف هذا المستخدم، يجب تعيين منصب {roleName} لمستخدم جديد." });
                        //return BadRequest(new { Message = $"This user has {roleName} role. Before deleting this user, you should assign {roleName} role to new user." });
                    }
                    var IsDeptHead = await _deptBase.Get(d => d.ManagerId == user.Id);
                    if (IsDeptHead != null)
                    {
                        return BadRequest(new { Message = $".هذا المستخدم هو مدير قسم {IsDeptHead.Name}، يجب تعيين مدير جديد لهذا القسم قبل حذف هذا المستخدم." });
                       // return BadRequest(new { Message = $"This user is {IsDeptHead.Name} department manager, you should assign a new manager to this department before deleting this user." });
                    }
                    if (user.Active)
                    {
                        user.Active = false;
                        await _accountService.Update(user);
                        return Ok(new { Message = ".تم الحذف بنجاح" });
                    }
                    else
                    {
                        return BadRequest(new { Message = ".المستخدم غير نشط بالفعل" });
                    }


                    //var res = await _accountService.Delete(user);
                    //if (res.Succeeded)
                    //{
                    //    return Ok(new { Message = "Deletion is succeeded" });
                    //}
                    //return BadRequest(res.Errors);
                }
                return NotFound(new { Message = ".المستخدم غير موجود" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ".حدث خطأ أثناء معالجة طلبك", error = ex.Message });
            }
        }
        //[Authorize(Roles = "مدير الموارد البشرية")]
        [HttpPut("ReActiveUser/{userid}")]
        public async Task<IActionResult> ReActiveUser(string userid)
        {
            if (string.IsNullOrWhiteSpace(userid))
                return BadRequest(new { message =".معرف المستخدم غير صالح" });
            try
            {
                var user = await _accountService.FindById(userid);
                if (user != null)
                {
                    if (!user.Active)   
                    {
                        user.Active = true;
                        await _accountService.Update(user);
                        return Ok(new { Message = "تم التنشيط بنجاح" });
                    }
                    else
                    {
                        return BadRequest(new { Message = "المستخدم نشط بالفعل" });
                    }

                    //var res = await _accountService.Delete(user);
                    //if (res.Succeeded)
                    //{
                    //    return Ok(new { Message = "Deletion is succeeded" });
                    //}
                    //return BadRequest(res.Errors);
                }
                return NotFound(new { Message = "المستخدم غير موجود" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ".حدث خطأ أثناء معالجة طلبك", error = ex.Message });
            }
        }
    }
}

