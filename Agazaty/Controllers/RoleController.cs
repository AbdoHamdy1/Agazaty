using Agazaty.Data.Base;
using Agazaty.Data.DTOs.AccountDTOs;
using Agazaty.Data.DTOs.DepartmentDTOs;
using Agazaty.Data.DTOs.PermitLeavesDTOs;
using Agazaty.Data.DTOs.RoleDTOs;
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
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;
        private readonly IAccountService _accountService;
        private readonly IMapper _mapper;
        private readonly IEntityBaseRepository<Department> _deptBase;

        public RoleController(IRoleService roleService, IMapper mapper, IAccountService accountService, IEntityBaseRepository<Department> deptBase)
        {
            _roleService = roleService;
            _mapper = mapper;
            _accountService = accountService;
            _deptBase = deptBase;
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpGet("GetRoleByID/{roleId}")]
        public async Task<IActionResult> GetRoleByID([FromRoute]string roleId)
        { 
            if (string.IsNullOrWhiteSpace(roleId))
                return BadRequest(new { message = "معرّف المنصب غير صالح." });
            try
            {
                var role = await _roleService.FindById(roleId);
                if (role == null) return NotFound(new { Message = "لم يتم العثور على منصب بهذا المعرف." });
                return Ok(_mapper.Map<RoleDTO>(role));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpGet("GetAllUsersInRole/{RoleName}")]
        public async Task<IActionResult> GetAllUsersInRoles(string RoleName)
        {
            if (!await _roleService.IsRoleExisted(RoleName))
                return BadRequest(new { Message = "Invalid Role!" });
            try
            {
                var users = await _accountService.GetAllUsersInRole(RoleName);
                if (users.Count() == 0) return NotFound(new { Message = "لم يتم العثور على مستخدمين في هذا المنصب." });
                var ReturnedUsers = _mapper.Map<IEnumerable<UserDTO>>(users);
                foreach (var ReturnedUser in ReturnedUsers)
                {
                    var user = await _accountService.FindById(ReturnedUser.Id);
                    ReturnedUser.FullName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    ReturnedUser.RoleName= RoleName;
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
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpGet("GetAllRoles")]
        public async Task<IActionResult> GetAllRoles()
        {
            try
            {
                var roles = await _roleService.GetAllRoles();
                if (roles.Count() == 0) return NotFound(new { Message = "لم يتم العثور على مناصب." });
                return Ok(_mapper.Map<IEnumerable<RoleDTO>>(roles)); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpPost("CreateRole")]
        public async Task<IActionResult> CreateRole([FromBody]CreateRoleDTO CR)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if(await _roleService.IsRoleExisted(CR.Name) == true)
                    {
                        return BadRequest(new { Message = "تم إنشاء هذا المنصب بالفعل." });
                    }
                    var res = await _roleService.CreateRole(CR);
                    if (res.Succeeded)
                    {
                        var role = await _roleService.FindByName(CR.Name);
                        return CreatedAtAction(nameof(GetRoleByID), new { roleId = role.Id }, _mapper.Map<RoleDTO>(role));
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
        [HttpPut("UpdateRole/{roleid}")]
        public async Task<IActionResult> UpdateRole([FromRoute]string roleid, [FromBody]UpdateRoleDTO UR)
        {
            if (string.IsNullOrWhiteSpace(roleid))
                return BadRequest(new { message = "معرّف المنصب غير صالح." });
            try
            {
                if (ModelState.IsValid)
                {
                    if (await _roleService.IsRoleExisted(UR.Name) == true)
                    {
                        return BadRequest(new { Message = "تم إنشاء هذا المنصب بالفعل." });
                    }
                    var role = await _roleService.FindById(roleid);
                    if (role == null) return NotFound(new { Message = "لا يوجد منصب بهذا المعرف." });

                    _mapper.Map(UR,role);

                    var res = await _roleService.UpdateRole(role);
                    if (res.Succeeded)
                    {
                        return Ok(new { Message = "تم التحديث بنجاح.", Role = _mapper.Map<RoleDTO>(role) });
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
        [HttpDelete("DeleteRole/{roleid}")]
        public async Task<IActionResult> DeleteRole(string roleid)
        {
            if (string.IsNullOrWhiteSpace(roleid))
                return BadRequest(new { message = "معرّف المنصب غير صالح." });
            try
            {
                var role = await _roleService.FindById(roleid);
                if (role == null) return NotFound(new { Message = "لا يوجد منصب بهذا المعرف." });

                var users = await _accountService.GetAllUsersInRole(roleid);
                if (users.Any()) return BadRequest(new { Message = "هناك مستخدمون بهذا المنصب، قم بتعديل منصبهم إلى منصب آخر حتى تتمكن من حذف هذا المنصب." });

                var res = await _roleService.DeleteRole(role);
                if (res.Succeeded)
                {
                    return Ok(new { Message = "تم الحذف بنجاح." });
                }
                return BadRequest(res.Errors);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
    }
}