using Agazaty.Data.Base;
using Agazaty.Data.DTOs.AccountDTOs;
using Agazaty.Data.DTOs.CasualLeaveDTOs;
using Agazaty.Data.DTOs.DepartmentDTOs;
using Agazaty.Data.Services.Interfaces;
using Agazaty.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace Agazaty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentController : ControllerBase
    {
        private readonly IEntityBaseRepository<Department> _base;
        private readonly IAccountService _accountService;
        private readonly IMapper _mapper;
        public DepartmentController(IMapper mapper, IEntityBaseRepository<Department> Ebase, IAccountService accountService)
        {
            _mapper = mapper;
            _base = Ebase;
            _accountService = accountService;
        }
        [Authorize(Roles = "مدير الموارد البشرية,عميد الكلية")]
        [HttpGet("GetAllDepartments")]
        public async Task<IActionResult> GetAllDepartments()
        {
            try
            {
                var departments = await _base.GetAll();

                if (!departments.Any())
                {
                    return NotFound("لم يتم العثور على أقسام.");
                }
                var depts = _mapper.Map<IEnumerable<DepartmentDTO>>(departments);
                foreach(var dept in depts)
                {
                    var manager = await _accountService.FindById(dept.ManagerId);
                    dept.ManagerName = $"{manager.FirstName} {manager.SecondName} {manager.ThirdName} {manager.ForthName}";
                }
                return Ok(depts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpGet("GetDepartmentById/{departmentID:guid}")]
        public async Task<IActionResult> GetDepartmentById(Guid departmentID)
        {
            if (departmentID == Guid.Empty)
            {
                return BadRequest(new { Message = "معرّف القسم غير صالح." });
            }
            try
            {
                var department = await _base.Get(d => d.Id == departmentID);

                if (department == null)
                {
                    return NotFound($"لا يوجد قسم بالمعرّف {departmentID}.");
                }

                var dept = _mapper.Map<DepartmentDTO>(department);
                var manager = await _accountService.FindById(dept.ManagerId);
                dept.ManagerName = $"{manager.FirstName} {manager.SecondName} {manager.ThirdName} {manager.ForthName}";
                return Ok(dept);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpGet("GetAllAvailableForDeptCreation/{roleName}")]
        public async Task<IActionResult> GetAllAvailableForDeptCreation(string roleName)
        {
            try
            {
                var users = await _accountService.GetAllUsersInRole(roleName);
                if (roleName == "هيئة تدريس")
                {
                    var supervisor = (await _accountService.GetAllUsersInRole("عميد الكلية"))
                               .Where(u => u.Active == true)
                               .FirstOrDefault();
                    var allUsers = users.Append(supervisor); // supervisor حتى لو null هيتضاف
                    var ReturnedUsers = _mapper.Map<IEnumerable<UserDTO>>(allUsers);
                    foreach (var ReturnedUser in ReturnedUsers)
                    {
                        var user = await _accountService.FindById(ReturnedUser.Id);
                        ReturnedUser.FullName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                        ReturnedUser.RoleName = await _accountService.GetFirstRole(user);
                    }
                    return Ok(ReturnedUsers);
                }
                else if (roleName == "موظف")
                {
                    var supervisor = (await _accountService.GetAllUsersInRole("أمين الكلية"))
                                .Where(u => u.Active == true )
                                .FirstOrDefault();
                    var HR = (await _accountService.GetAllUsersInRole("مدير الموارد البشرية"))
                                .Where(u => u.Active == true )
                                .FirstOrDefault();
                    var allUsers = users.Append(supervisor); // supervisor حتى لو null هيتضاف
                    allUsers = users.Append(HR); // supervisor حتى لو null هيتضاف
                    var ReturnedUsers = _mapper.Map<IEnumerable<UserDTO>>(allUsers);
                    foreach (var ReturnedUser in ReturnedUsers)
                    {
                        var user = await _accountService.FindById(ReturnedUser.Id);
                        ReturnedUser.FullName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                        ReturnedUser.RoleName = await _accountService.GetFirstRole(user);
                    }

                    return Ok(ReturnedUsers);
                }
                var ReturnedUserss = _mapper.Map<IEnumerable<UserDTO>>(users);
                foreach (var ReturnedUser in ReturnedUserss)
                {
                    var user = await _accountService.FindById(ReturnedUser.Id);
                    ReturnedUser.FullName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    ReturnedUser.RoleName = await _accountService.GetFirstRole(user);
                }
                return Ok(ReturnedUserss);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }

        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpPost("CreateDepartment")]
        public async Task<IActionResult> CreateDepartment([FromBody]CreateDepartmentDTO model)
        {

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                if (model == null)
                {
                    return BadRequest("بيانات القسم غير صالحة.");
                }
                var res = await _accountService.FindById(model.ManagerId);
                if (res == null)
                {
                    return NotFound(new { Message = "معرّف المستخدم غير موجود." });
                }
                var UserRole = await _accountService.GetFirstRole(res);
                if (UserRole == "عميد الكلية" || UserRole == "أمين الكلية")
                {
                    return BadRequest(new { Message = "لا يمكن ان يكون عميد الكلية او أمين الكلية رؤساء لاقسام" });
                }
                // to check : is user whose id is equal model.managerid exists in another department ?
                var IsExistsInAnotherDepartment = await _base.Get(d => d.Id == res.Departement_ID);
                if (IsExistsInAnotherDepartment != null) // this means that user exists in another department
                {
                    return BadRequest(new { Message = $"المستخدم موجود في قسم {IsExistsInAnotherDepartment.Name}، يرجى جعله بدون قسم حتى تتمكن من تعيينه مديرًا للقسم الذي تنشئه الآن." });
                }
                //var IsAlreadyManager = await _base.Get(d => d.ManagerId == model.ManagerId);
                //if (IsAlreadyManager != null)
                //{
                //    return BadRequest(new { Message = "This Manager already in a head of a department." });
                //}
                var department = _mapper.Map<Department>(model);
                await _base.Add(department);
                var manager = await _accountService.FindById(department.ManagerId);
                manager.Departement_ID = department.Id;
                await _accountService.Update(manager);

                var dept = _mapper.Map<DepartmentDTO>(department);
                dept.ManagerName = $"{manager.FirstName} {manager.SecondName} {manager.ThirdName} {manager.ForthName}";
                return CreatedAtAction(nameof(GetDepartmentById), new { departmentID = department.Id }, dept);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpPut("UpdateDepartment/{departmentID:guid}")]
        public async Task<IActionResult> UpdateDepartment([FromRoute]Guid departmentID, [FromBody]UpdateDepartmentDTO model)
        {
            if (departmentID == Guid.Empty)
            {
                return BadRequest("بيانات القسم غير صالحة.");
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var res = await _accountService.FindById(model.ManagerId);
                if (res == null)
                {
                    return NotFound(new { Message = "معرّف المستخدم غير موجود." });
                }
                var UserRole = await _accountService.GetFirstRole(res);
                if (UserRole == "عميد الكلية" || UserRole == "أمين الكلية")
                {
                    return BadRequest(new { Message = "لا يمكن ان يكون عميد الكلية او أمين الكلية رؤساء لاقسام" });
                }
                var department = await _base.Get(d => d.Id == departmentID);
                if (department == null)
                {
                    return NotFound(new { Message = "لم يتم العثور على القسم." });
                }
                //var IsAlreadyManager = await _base.Get(d => d.ManagerId == model.ManagerId);
                //if(IsAlreadyManager != null)
                //{
                //    if (department.Id != IsAlreadyManager.Id)
                //    {
                //        return BadRequest(new { Message = "This Manager already in a head of a another department." });
                //    }
                //}
                // to check is user exists in another department
                var IsExistsInAnotherDepartment = await _base.Get(d => d.Id == res.Departement_ID);
                if (IsExistsInAnotherDepartment != null)
                {
                    if (IsExistsInAnotherDepartment.Id != department.Id)
                    {
                        return BadRequest(new { Message = $"المستخدم موجود في قسم {IsExistsInAnotherDepartment.Name}. قم بإضافة المستخدم كعضو في قسم {department.Name} أو جعله بدون قسم، حتى تتمكن من تعيينه رئيسًا لقسم {department.Name}." });
                    }
                }
                _mapper.Map(model, department);
                await _base.Update(department);

                var dept = _mapper.Map<DepartmentDTO>(department);
                var manager = await _accountService.FindById(dept.ManagerId);
                dept.ManagerName = $"{manager.FirstName} {manager.SecondName} {manager.ThirdName} {manager.ForthName}";
                return Ok(new { Message = "تم تحديث القسم بنجاح.", Department = dept });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpDelete("DeleteDepartment/{departmentID:guid}")]
        public async Task<IActionResult> DeleteDepartment(Guid departmentID)
        {
            if (departmentID == Guid.Empty)
            {
                return BadRequest(new { Message = "معرّف القسم غير صالح." });
            }

            try
            {
                var department = await _base.Get(d => d.Id == departmentID);

                if (department == null)
                {
                    return NotFound("لم يتم العثور على قسم.");
                }
                var users = await _accountService.GetAllUsersByDepartmentId(departmentID);
                if (users.Any())
                {
                    return BadRequest(new { Message = "هذا القسم يحتوي على أعضاء، قم بتعديل قسمهم إلى قسم آخر حتى تتمكن من حذف هذا القسم." });
                }
                await _base.Remove(department);

                return Ok("تم حذف القسم بنجاح.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
    }
}