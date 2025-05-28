using Agazaty.Data.Base;
using Agazaty.Data.DTOs.CasualLeaveDTOs;
using Agazaty.Data.DTOs.NormalLeaveDTOs;
using Agazaty.Data.Services;
using Agazaty.Data.Services.Interfaces;
using Agazaty.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Net;
using static Agazaty.Data.Enums.LeaveTypes;

namespace Agazaty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CasualLeaveController : ControllerBase
    {
        private readonly IEntityBaseRepository<CasualLeave> _base;
        private readonly IAccountService _accountService;
        private readonly IMapper _mapper;
        private readonly ILeaveValidationService _leaveValidationService;
        private readonly IEntityBaseRepository<Department> _departmentBase;

        public CasualLeaveController(IMapper mapper, IEntityBaseRepository<CasualLeave> Ebase, IAccountService accountService, ILeaveValidationService leaveValidationService, IEntityBaseRepository<Department> basedepartment)
        {
            _mapper = mapper;
            _base = Ebase;
            _accountService = accountService;
            _leaveValidationService = leaveValidationService;
            _departmentBase = basedepartment;
        }
        [Authorize(Roles = "عميد الكلية,أمين الكلية,مدير الموارد البشرية")]
        [HttpGet("GetCasualLeaveById/{leaveID:int}", Name = "GetCasualLeave")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CasualLeaveDTO>> GetCasualLeaveById(int leaveID)
        {
            if (leaveID <= 0)
                return BadRequest(new { message = "معرّف الإجازة العارضة غير صالح." });
            try
            {
                var casualLeave = await _base.Get(c => c.Id == leaveID);
                if (casualLeave == null)
                {
                    return NotFound(new { Message = "لم يتم العثور على أي إجازة عارضة." });
                }
                var leave = _mapper.Map<CasualLeaveDTO>(casualLeave);
                var user = await _accountService.FindById(leave.UserID);
                leave.PhoneNumber = user.PhoneNumber;
                var generalManager = await _accountService.FindById(casualLeave.General_ManagerID);
                leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                var department = await _departmentBase.Get(d => d.Id == user.Departement_ID);
                if (department != null)
                {
                    leave.DepartmentName = department.Name;
                }
                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                return Ok(leave);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
        [Authorize(Roles = "عميد الكلية,أمين الكلية,مدير الموارد البشرية")]
        [HttpGet("GetAllCasualLeaves", Name = "GetAllCasualLeaves")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<CasualLeaveDTO>>> GetAllCasualLeaves()
        {
            try
            {
                var casualLeaves = await _base.GetAll();
                if (!casualLeaves.Any())
                {
                    return NotFound(new { Message = "لم يتم العثور على أي إجازات عارضة." });
                }
                var leaves = _mapper.Map<IEnumerable<CasualLeaveDTO>>(casualLeaves);
                foreach(var leave in leaves)
                {
                    var user = await _accountService.FindById(leave.UserID);
                    var generalManager = await _accountService.FindById(leave.General_ManagerID);
                    leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    var department = await _departmentBase.Get(d => d.Id == user.Departement_ID);
                    if (department != null)
                    {
                        leave.DepartmentName = department.Name;
                    }
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                }
                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpGet("GetAllCasualLeavesByUserID/{userID}", Name = "GetAllCasualLeavesByUserID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<CasualLeaveDTO>>> GetAllCasualLeavesByUserID(string userID)
        {
            if (string.IsNullOrWhiteSpace(userID))
                return BadRequest(new { message = "معرف المستخدم غير صالح." });
            try
            {
                var casualLeaves = await _base.GetAll(c => c.UserId == userID);
                if(!casualLeaves.Any())
                {
                    return NotFound(new { Message = "لم يتم العثور على أي إجازات عارضة." });
                }
                var leaves = _mapper.Map<IEnumerable<CasualLeaveDTO>>(casualLeaves);
                foreach (var leave in leaves)
                {
                    var user = await _accountService.FindById(leave.UserID);
                    var generalManager = await _accountService.FindById(leave.General_ManagerID);
                    leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    var department = await _departmentBase.Get(d => d.Id == user.Departement_ID);
                    if (department != null)
                    {
                        leave.DepartmentName = department.Name;
                    }
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                }
                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpGet("GetAllCasualLeavesByUserIDAndYear/{userID}/{year:int}", Name = "GetAllCasualLeavesByUserIDAndYear")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<CasualLeaveDTO>>> GetAllCasualLeavesByUserIDAndYear(string userID, int year)
        {
            if (string.IsNullOrWhiteSpace(userID) || year < 1900)
                return BadRequest(new { message = "معرف المستخدم أو السنة غير صالح." });
            try
            {
                var casualLeaves = await _base.GetAll(c => c.UserId == userID
                                  && c.Year == year);
                if (!casualLeaves.Any())
                {
                    return NotFound(new { Message = "لم يتم العثور على أي إجازات عارضة." });
                }
                var leaves = _mapper.Map<IEnumerable<CasualLeaveDTO>>(casualLeaves);
                foreach (var leave in leaves)
                {
                    var user = await _accountService.FindById(leave.UserID);
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                }
                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpPost("CreateCasualLeave", Name = "CreateCasualLeave")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CasualLeaveDTO>> CreateCasualLeave([FromBody] CreateCasualLeaveDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                if (model == null)
                {
                    return BadRequest();
                }
                if (await _leaveValidationService.IsSameLeaveOverlapping(model.UserId, model.StartDate, model.EndDate, "CasualLeave"))
                {
                    return BadRequest(new { Message = "لديك بالفعل إجازة عارضة في هذه الفترة!" });
                }

                if (await _leaveValidationService.IsLeaveOverlapping(model.UserId, model.StartDate, model.EndDate, "CasualLeave"))
                {
                    return BadRequest(new { Message = "لديك بالفعل إجازة من نوع آخر في هذه الفترة!" });
                }
                ApplicationUser user = await _accountService.FindById(model.UserId);
                //var allCasualLeave = await _base.GetAll(u => u.UserId == user.Id);
                //if (allCasualLeave != null && allCasualLeave.Count() !=0)
                //{
                //    var lastCasualLeave = allCasualLeave.OrderByDescending(c => c.EndDate).FirstOrDefault();
                   
                //    if ((model.StartDate <= lastCasualLeave.EndDate && model.EndDate>=lastCasualLeave.StartDate)||(model.EndDate>=lastCasualLeave.StartDate&&model.StartDate<=lastCasualLeave.StartDate))
                //    {
                //        return BadRequest(new { Message = "you already have a casual leave in this date." });
                //    }
                //}
                if (((model.EndDate-model.StartDate).TotalDays + 1) > user.CasualLeavesCount)
                {
                    return BadRequest(new { Message = $"لا يمكن إتمام الطلب، عدد الأيام المتاحة لك هي {user.CasualLeavesCount}." });
                }
                if (model.EndDate >= DateTime.Today || model.StartDate >= DateTime.Today)
                {
                    return BadRequest(new { Message = "يجب أن تكون فترة الإجازة في الماضي." });
                }
                if ((model.EndDate - model.StartDate).TotalDays+1 > 2)
                {
                    return BadRequest(new { Message = "لقد تجاوزت العدد المسموح به من الأيام، يمكنك اختيار يوم أو يومين فقط." });
                }
                if ((model.EndDate - model.StartDate).TotalDays < 0)
                {
                    return BadRequest(new { Message = "يجب أن يكون تاريخ البدء قبل تاريخ الانتهاء." });
                }

                var casualLeave = _mapper.Map<CasualLeave>(model);
                if (await _accountService.IsInRoleAsync(user, "هيئة تدريس"))
                {
                    var res = await _accountService.GetAllUsersInRole("عميد الكلية");
                    var Dean = res.FirstOrDefault();
                    if (Dean == null) { return BadRequest(new { Message = "لا يوجد مستخدم لديه دور العميد." }); }
                    casualLeave.General_ManagerID = Dean.Id;
                }
                else if (await _accountService.IsInRoleAsync(user, "موظف"))
                {
                    var res = await _accountService.GetAllUsersInRole("أمين الكلية");
                    var Supervisor = res.FirstOrDefault();
                    if (Supervisor == null) { return BadRequest(new { Message = "لا يوجد مستخدم لديه دور أمين كلية." }); }
                    casualLeave.General_ManagerID = Supervisor.Id; 
                }
                else if (await _accountService.IsInRoleAsync(user, "أمين الكلية"))
                {
                    // if أمين الكلية made a leave request
                    var res = await _accountService.GetAllUsersInRole("عميد الكلية");
                    var Dean = res.FirstOrDefault();
                    if (Dean == null) { return BadRequest(new { Message = "لا يوجد مستخدم لديه دور العميد." }); }
                    casualLeave.General_ManagerID = Dean.Id;
                }
                else if (await _accountService.IsInRoleAsync(user, "مدير الموارد البشرية"))
                {
                    var res = await _accountService.GetAllUsersInRole("أمين الكلية");
                    var Supervisor = res.FirstOrDefault();
                    if (Supervisor == null) { return BadRequest(new { Message = "لا يوجد مستخدم لديه دور أمين كلية." }); }
                    casualLeave.General_ManagerID = Supervisor.Id;
                } 

                casualLeave.LeaveStatus=false;
                casualLeave.Year = model.StartDate.Year;
                casualLeave.RequestDate = DateTime.UtcNow.Date;
                casualLeave.Days = (model.EndDate - model.StartDate).Days + 1;
                user.CasualLeavesCount -= (int)((casualLeave.EndDate - casualLeave.StartDate).TotalDays + 1) ;
                await _accountService.Update(user);
                await _base.Add(casualLeave);

                var leave = _mapper.Map<CasualLeaveDTO>(casualLeave);
                var generalManager = await _accountService.FindById(casualLeave.General_ManagerID);
                leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                var department = await _departmentBase.Get(d => d.Id == user.Departement_ID);
                if (department != null)
                {
                    leave.DepartmentName = department.Name;
                }
                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                return CreatedAtAction(nameof(GetCasualLeaveById), new { leaveID = casualLeave.Id }, leave);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
        [Authorize(Roles = "عميد الكلية,أمين الكلية")]
        [HttpPut("UpdateGeneralManagerDecicion/{leaveID:int}", Name = "UpdateGeneralManagerDecicion")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateGeneralManagerDecicion(int leaveID)
        {
            if (leaveID <= 0)
            {
                return BadRequest(new { Message = "معرف الإجازة غير صالح." });
            }
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var casualLeave = await _base.Get(c => c.Id == leaveID);
                if (casualLeave == null)
                {
                    return NotFound(new { Message = "لم يتم العثور على إجازة عارضة بهذا المعرف." });
                }
                ApplicationUser user = await _accountService.FindById(casualLeave.UserId);
                casualLeave.LeaveStatus = true;
                await _base.Update(casualLeave);
                var leave = _mapper.Map<CasualLeaveDTO>(casualLeave);
                var generalManager = await _accountService.FindById(casualLeave.General_ManagerID);
                leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                var department = await _departmentBase.Get(d => d.Id == user.Departement_ID);
                if (department != null)
                {
                    leave.DepartmentName = department.Name;
                }
                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                return Ok(new { Message = "تم التحديث بنجاح.", CasualLeaveDetails = leave });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
        //[Authorize(Roles = "عميد الكلية,أمين الكلية")]
        [HttpGet("GetAllWaitingCasualLeavesByGeneral_ManagerID/{general_managerID}", Name = "GetAllWaitingCasualLeavesByGeneral_ManagerID")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAllWaitingCasualLeavesByGeneral_ManagerID(string general_managerID)
        {
            if (string.IsNullOrWhiteSpace(general_managerID))
                return BadRequest(new { message = "معرف المدير المختص غير صالح." });
            try
            {

                var CasualLeaves = await _base.GetAll(n =>
                    n.General_ManagerID == general_managerID &&
                    n.LeaveStatus == false);
                if (!CasualLeaves.Any())
                {
                    return NotFound(new { message = "لا يوجد أي إجازات عارضة في الانتظار." });
                }

                var leaves = new List<CasualLeaveDTO>();
                foreach (var casualleave in CasualLeaves)
                {
                    var leave = _mapper.Map<CasualLeaveDTO>(casualleave);
                    var user = await _accountService.FindById(casualleave.UserId);
                    leave.PhoneNumber = user.PhoneNumber;
                    var generalManager = await _accountService.FindById(casualleave.General_ManagerID);
                    leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    var department = await _departmentBase.Get(d => d.Id == user.Departement_ID);
                    if (department != null)
                    {
                        leave.DepartmentName = department.Name;
                    }
                    leaves.Add(leave);
                }
                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
        [Authorize(Roles = "عميد الكلية,أمين الكلية,مدير الموارد البشرية")]
        [HttpPut("UpdateCasualLeave/{leaveID:int}", Name = "UpdateCasualLeave")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCasualLeave(int leaveID, [FromBody] UpdateCasualLeaveDTO model)
        {
            if (leaveID <= 0)
            {
                return BadRequest(new { Message = "معرف الإجازة غير صالح." });
            }
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var casualLeave = await _base.Get(c => c.Id == leaveID);
                if (casualLeave == null)
                {
                    return NotFound(new { Message = "لم يتم العثور على إجازة عارضة بهذا المعرف." });
                }

                var user = await _accountService.FindById(model.UserId);
                user.CasualLeavesCount += (int)((casualLeave.EndDate - casualLeave.StartDate).TotalDays + 1);

                if (((model.EndDate - model.StartDate).TotalDays + 1) > user.CasualLeavesCount)
                {
                    return BadRequest(new { Message = $"عدد الأيام المتاحة لك هو {user.CasualLeavesCount}." });
                }
                if (model.EndDate >= DateTime.Today || model.StartDate >= DateTime.Today)
                {
                    return BadRequest(new { Message = "يجب أن تكون فترة الإجازة في الماضي." });
                }
                if ((model.EndDate - model.StartDate).TotalDays +1 > 2)
                {
                    return BadRequest(new { Message = "لقد تجاوزت العدد المسموح به من الأيام، يمكنك اختيار يوم أو يومين فقط." });
                }
                if ((model.EndDate - model.StartDate).TotalDays < 0)
                {
                    return BadRequest(new { Message = "يجب أن يكون تاريخ البدء قبل تاريخ الانتهاء." });
                }


                _mapper.Map(model, casualLeave);
                casualLeave.Year = model.StartDate.Year;
                user.CasualLeavesCount -= (int)((casualLeave.EndDate - casualLeave.StartDate).TotalDays + 1);
                await _accountService.Update(user);
                await _base.Update(casualLeave);

                var leave = _mapper.Map<CasualLeaveDTO>(casualLeave);
                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                return Ok(new { Message = "تم التحديث بنجاح.", CasualLeaveDetails = leave });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
        [Authorize(Roles = "عميد الكلية,أمين الكلية,مدير الموارد البشرية")]
        [HttpDelete("DeleteCasualLeave/{leaveID:int}", Name = "DeleteCasualLeave")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCasualLeave(int leaveID)
        {
            if (leaveID<=0)
            {
                return BadRequest(new { Message = "معرف الإجازة غير صالح." });
            }
            try
            {
                var casualLeave = await _base.Get(c => c.Id == leaveID);
                if (casualLeave == null)
                {
                    return NotFound(new { Message = "لم يتم العثور على إجازة عارضة بهذا المعرف." });
                }
                await _base.Remove(casualLeave);
                return Ok(new { Message = "تم الحذف بنجاح." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
    }
}