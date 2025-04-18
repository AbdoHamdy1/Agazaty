﻿using Agazaty.Data.Base;
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
        //[Authorize(Roles = "مدير الموارد البشرية")]
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
        //[Authorize(Roles = "مدير الموارد البشرية")]
        [HttpGet("GetDepartmentById/{departmentID:int}")]
        public async Task<IActionResult> GetDepartmentById(int departmentID)
        {
            if (departmentID <= 0)
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
        //[Authorize(Roles = "مدير الموارد البشرية")]
        [HttpPost("CreateDepartment")]
        public async Task<IActionResult> CreateDepartment([FromBody]CreateDepartmentDTO model)
        {

            try
            {
                if (model == null)
                {
                    return BadRequest("بيانات القسم غير صالحة.");
                }
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var res = await _accountService.FindById(model.ManagerId);
                if (res == null)
                {
                    return NotFound(new { Message = "معرّف المستخدم غير موجود." });
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
        //[Authorize(Roles = "مدير الموارد البشرية")]
        [HttpPut("UpdateDepartment/{departmentID:int}")]
        public async Task<IActionResult> UpdateDepartment([FromRoute]int departmentID, [FromBody]UpdateDepartmentDTO model)
        {
            if (departmentID<=0)
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
        //[Authorize(Roles = "مدير الموارد البشرية")]
        [HttpDelete("DeleteDepartment/{departmentID:int}")]
        public async Task<IActionResult> DeleteDepartment(int departmentID)
        {
            if (departmentID <= 0)
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