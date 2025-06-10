using Agazaty.Data.Base;
using Agazaty.Data.DTOs.CasualLeaveDTOs;
using Agazaty.Data.DTOs.RoleDTOs;
using Agazaty.Data.DTOs.SickLeaveDTOs;
using Agazaty.Data.Services;
using Agazaty.Data.Services.Implementation;
using Agazaty.Data.Services.Interfaces;
using Agazaty.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Data;
using System.Net;
using static Agazaty.Data.Enums.LeaveTypes;

namespace Agazaty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SickLeaveController : ControllerBase
    {
        private readonly IEntityBaseRepository<SickLeave> _base;
        private readonly IAccountService _accoutnService;
        private readonly IMapper _mapper;
        private readonly ILeaveValidationService _leaveValidationService;
        private readonly AppDbContext _appDbContext;
        public SickLeaveController(IAccountService accoutnService, IMapper mapper, IEntityBaseRepository<SickLeave> Ebase, ILeaveValidationService leaveValidationService, AppDbContext appDbContext)
        {
            _mapper = mapper;
            _base = Ebase;
            _accoutnService = accoutnService;
            _leaveValidationService = leaveValidationService;
            _appDbContext = appDbContext;
        }
        [Authorize]
        [HttpGet("GetSickLeaveById/{leaveID:guid}")]
        public async Task<IActionResult> GetSickLeaveById(Guid leaveID)
        {
            if (leaveID == Guid.Empty)
                return BadRequest(new { message = "معرّف إجازة مرضية غير صالح." });
            try
            {

                var sickLeave = await _base.Get(s => s.Id == leaveID);
                if (sickLeave == null)
                {
                    return NotFound(new { message = $"لم يتم العثور على إجازة مرضية لهذا المعرف {leaveID}." });
                }

                var leave = _mapper.Map<SickLeaveDTO>(sickLeave);
                var user = await _accoutnService.FindById(leave.UserID);
                leave.PhoneNumber = user.PhoneNumber;
                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                leave.FirstName = user.FirstName;
                leave.SecondName = user.SecondName;
                return Ok(leave);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpGet("GetAllSickLeavesByUserID/{userID}")]
        public async Task<IActionResult> GetAllSickLeavesByUserID(string userID)
        {
            if (string.IsNullOrWhiteSpace(userID))
                return BadRequest(new { message = "معرّف المستخدم غير صالح." });
            try
            {

                var sickleaves = await _base.GetAll(s => s.UserID == userID);
                if (!sickleaves.Any())
                {
                    return NotFound(new { message = $"لم يتم العثور على إجازات مرضية لهذا معرف المستخدم {userID}." });
                }

                var leaves = _mapper.Map<IEnumerable<SickLeaveDTO>>(sickleaves);
                foreach(var leave in leaves)
                {
                    var user = await _accoutnService.FindById(leave.UserID);
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                }
                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "عميد الكلية,أمين الكلية,مدير الموارد البشرية")]
        [HttpGet("GetAllSickLeave")]
        public async Task<IActionResult> GetAllSickLeave()
        {
            try
            {
                var sickLeaves = await _base.GetAll();
                if (!sickLeaves.Any()) return NotFound(new { Message = "لم يتم العثور على إجازات مرضية." });

                var leaves = _mapper.Map<IEnumerable<SickLeaveDTO>>(sickLeaves);
                foreach (var leave in leaves)
                {
                    var user = await _accoutnService.FindById(leave.UserID);
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                }
                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpGet("GetAllSickLeavesByUserIDAndYear/{userID}/{year:int}")]
        public async Task<IActionResult> GetAllSickLeavesByUserIDAndYear(string userID, int year)
        {
            if (string.IsNullOrWhiteSpace(userID) || year < 1900)
            {
                return BadRequest("معرّف المستخدم أو السنة غير صالحين.");
            }

            try
            {
                var sickLeaves = await _base.GetAll(s => s.UserID == userID && s.Year == year);

                if (sickLeaves.Any())
                {
                    var leaves = _mapper.Map<IEnumerable<SickLeaveDTO>>(sickLeaves);
                    foreach (var leave in leaves)
                    {
                        var user = await _accoutnService.FindById(leave.UserID);
                        leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    }
                    return Ok(leaves);
                }

                return NotFound("لم يتم العثور على إجازات مرضية لهذا معرف المستخدم والسنة المحددة.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        //[Authorize(Roles = "مدير الموارد البشرية")]
        //[HttpGet("GetAllAcceptedSickLeaves")]
        //public async Task<IActionResult> GetAllAcceptedSickLeaves()
        //{
        //    try
        //    {
        //        var waitingSickLeaves = await _base.GetAll(s => s.Re == true);

        //        if (waitingSickLeaves.Any())
        //        {
        //            var leaves = _mapper.Map<IEnumerable<SickLeaveDTO>>(waitingSickLeaves);
        //            foreach (var leave in leaves)
        //            {
        //                var user = await _accoutnService.FindById(leave.UserID);
        //                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
        //            }
        //            return Ok(leaves);
        //        }

        //        return NotFound("No accepted sick leaves found.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "An error occurred while processing your request.", error = ex.Message });
        //    }
        //}
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpGet("GetAllWaitingCertifiedSickLeaves")]
        public async Task<IActionResult> GetAllCertifiedWaitingSickLeaves()
        {
            try
            {
                var waitingSickLeaves = await _base.GetAll(s => s.RespononseDoneForMedicalCommitte == true &&s.ResponseDoneFinal==false);

                if (waitingSickLeaves.Any())
                {
                    var leaves = _mapper.Map<IEnumerable<SickLeaveDTO>>(waitingSickLeaves);
                    foreach (var leave in leaves)
                    {
                        var user = await _accoutnService.FindById(leave.UserID);
                        leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    }
                    return Ok(leaves);
                }

                return NotFound("لم يتم العثور على إجازات مرضية في الانتظار.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "عميد الكلية,أمين الكلية")]
        [HttpGet("GetAllWaitingSickLeavesForGeneralManager/{general_managerid}")]
        public async Task<IActionResult> GetAllWaitingSickLeavesForGeneralManager(string general_managerid)
        {
            try
            {
                var waitingSickLeaves = await _base.GetAll(s => s.RespononseDoneForMedicalCommitte == false && s.GeneralManagerDecision == false &&s.General_ManagerID==general_managerid);

                if (waitingSickLeaves.Any())
                {
                    var leaves = _mapper.Map<IEnumerable<SickLeaveDTO>>(waitingSickLeaves);
                    foreach (var leave in leaves)
                    {
                        var user = await _accoutnService.FindById(leave.UserID);
                        leave.PhoneNumber=user.PhoneNumber;
                        leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    }
                    return Ok(leaves);
                }

                return NotFound("لم يتم العثور على إجازات مرضية في الانتظار.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpGet("GetAllWaitingSickLeavesForHR")]
        public async Task<IActionResult> GetAllWaitingSickLeavesForHR()
        {
            try
            {
                var waitingSickLeaves = await _base.GetAll(s => s.RespononseDoneForMedicalCommitte == false &&s.GeneralManagerDecision==true);

                if (waitingSickLeaves.Any())
                {
                    var leaves = _mapper.Map<IEnumerable<SickLeaveDTO>>(waitingSickLeaves);
                    foreach (var leave in leaves)
                    {
                        var user = await _accoutnService.FindById(leave.UserID);
                        leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    }
                    return Ok(leaves);
                }

                return NotFound("لم يتم العثور على إجازات مرضية في الانتظار.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpPost("CreateSickLeave")]
        public async Task<IActionResult> CreateSickLeave([FromBody]CreateSickLeaveDTO model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest("بيانات الإجازة المرضية غير صالحة.");
                }
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                // Check if the user already has a pending leave request
                bool hasPendingLeave = _appDbContext.SickLeaves
                .Any(l => l.UserID == model.UserID && l.RespononseDoneForMedicalCommitte == false);
                if (hasPendingLeave)
                {
                    return BadRequest(new { message = "لديك طلب إجازة قيد الانتظار ولم يتم الرد عليه بعد." });
                }
                ApplicationUser user = await _accoutnService.FindById(model.UserID);
                SickLeave sickLeave = _mapper.Map<SickLeave>(model);

                if (await _accoutnService.IsInRoleAsync(user, "هيئة تدريس"))
                {
                    var res = await _accoutnService.GetAllUsersInRole("عميد الكلية");
                    var Dean = res.FirstOrDefault();
                    if (Dean == null) { return BadRequest(new { Message = "لا يوجد مستخدم لديه دور العميد." }); }
                    sickLeave.General_ManagerID = Dean.Id;
                }
                else if (await _accoutnService.IsInRoleAsync(user, "موظف"))
                {
                    var res = await _accoutnService.GetAllUsersInRole("أمين الكلية");
                    var Supervisor = res.FirstOrDefault();
                    if (Supervisor == null) { return BadRequest(new { Message = "لا يوجد مستخدم لديه دور أمين كلية." }); }
                    sickLeave.General_ManagerID = Supervisor.Id;
                }
                else if (await _accoutnService.IsInRoleAsync(user, "أمين الكلية"))
                {
                    // if أمين الكلية made a leave request
                    var res = await _accoutnService.GetAllUsersInRole("عميد الكلية");
                    var Dean = res.FirstOrDefault();
                    if (Dean == null) { return BadRequest(new { Message = "لا يوجد مستخدم لديه دور العميد." }); }
                    sickLeave.General_ManagerID = Dean.Id;
                }
                else if (await _accoutnService.IsInRoleAsync(user, "مدير الموارد البشرية"))
                {
                    var res = await _accoutnService.GetAllUsersInRole("أمين الكلية");
                    var Supervisor = res.FirstOrDefault();
                    if (Supervisor == null) { return BadRequest(new { Message = "لا يوجد مستخدم لديه دور أمين كلية." }); }
                    sickLeave.General_ManagerID = Supervisor.Id;
                }
                sickLeave.RequestDate = DateTime.UtcNow.Date;
                sickLeave.Year = sickLeave.RequestDate.Year;

                await _base.Add(sickLeave);

                var leave = _mapper.Map<SickLeaveDTO>(sickLeave);
                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                return CreatedAtAction(nameof(GetSickLeaveById), new { leaveID = sickLeave.Id }, leave);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "عميد الكلية,أمين الكلية")]
        [HttpPut("TransferToHR/{leaveID:guid}")]
        public async Task<IActionResult> TransferToHR(Guid leaveID)
        {
            if (leaveID == Guid.Empty)
            {
                return BadRequest("معرّف الإجازة غير صالح.");
            }

            try
            {
                var sickLeave = await _base.Get(s => s.Id == leaveID);

                if (sickLeave == null)
                {
                    return NotFound("لم يتم العثور على طلب إجازة مرضية.");
                }

                // Update fields
                sickLeave.GeneralManagerDecision = true;
                //var leave = _mapper.Map<SickLeaveDTO>(sickLeave);
                //var user = await _accoutnService.FindById(leave.UserID);
                //leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                await _base.Update(sickLeave);

                return Ok(new { Message = "تم الأحالة لرئيس الموارد البشرية."});
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }

        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpPut("UpdateMedicalCommiteAddressResponse/{leaveID:guid}/{address}")]
        public async Task<IActionResult> UpdateMedicalCommiteAddressResponse(Guid leaveID, string address)
        {
            if (leaveID == Guid.Empty || string.IsNullOrWhiteSpace(address))
            {
                return BadRequest("معرّف الإجازة أو العنوان غير صالح.");
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var sickLeave = await _base.Get(s => s.Id == leaveID&&s.GeneralManagerDecision==true);

                if (sickLeave == null)
                {
                    return NotFound("لم يتم العثور على طلب إجازة مرضية.");
                }

                // Update fields
                sickLeave.MedicalCommitteAddress = address;
                sickLeave.RespononseDoneForMedicalCommitte= true;

                var leave = _mapper.Map<SickLeaveDTO>(sickLeave);
                var user = await _accoutnService.FindById(leave.UserID);
                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                await _base.Update(sickLeave);

                return Ok(new { Message = "تم تحديث عنوان القومسيون الطبي وتم الرد.", Leave = leave });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpPut("UpdateSickLeave/{leaveID:guid}")]
        public async Task<IActionResult> UpdateSickLeave(Guid leaveID, [FromBody]UpdateSickLeaveDTO model)
        {
            if (leaveID == Guid.Empty)
            {
                return BadRequest("معرّف الإجازة غير صالح.");
            }
            try
            {
                if (model == null)
                {
                    return BadRequest("بيانات الإجازة المرضية غير صالحة.");
                }
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var sickleave = await _base.Get(s => s.Id == leaveID && s.RespononseDoneForMedicalCommitte == true && s.ResponseDoneFinal == false);
                if (sickleave == null)
                    return NotFound("لم يتم العثور على إجازة مرضية.");
                if (model.Certified == false)
                {
                    sickleave.ResponseDoneFinal = true;
                    await _base.Update(sickleave);
                    return Ok(new { Message = "تم التحديث بنجاح.", Leave = sickleave });
                }
                else
                {
                    sickleave.ResponseDoneFinal = true;
                    //var userleave =await _base.Get(l=>l.Id==leaveID);
                    var userid = sickleave.UserID;
                    if (await _leaveValidationService.IsSameLeaveOverlapping(userid, model.StartDate, model.EndDate, "SickLeave"))
                    {
                        return BadRequest("لديك إجازة مرضية في هذه الفترة بالفعل.");
                    }
                    if (await _leaveValidationService.IsLeaveOverlapping(userid, model.StartDate, model.EndDate, "SickLeave"))
                    {
                        return BadRequest("لديك نوع آخر من الإجازات في هذه الفترة بالفعل.");
                    }
                    if (sickleave == null)
                    {
                        return NotFound(".لم يتم العثور على إجازة مرضية");
                    }
                    var errors = new List<string>();
                    DateTime today = DateTime.Today;
                    //if (model.EndDate <= today)
                    //    errors.Add(".فترة الإجازة قد انتهت بالفعل. يرجى اختيار تواريخ مستقبلية");

                    //if (model.StartDate <= today)
                    //    errors.Add(".لا يمكن أن يكون تاريخ البداية في الماضي. يرجى اختيار تاريخ مستقبلي");

                    if (model.StartDate > model.EndDate)
                        errors.Add("لا يمكن أن يكون تاريخ البداية بعد تاريخ النهاية.");

                    if (DateTime.UtcNow.Date > model.StartDate)
                        errors.Add("لا يمكن أن يكون تاريخ الطلب بعد تاريخ البداية.");

                    if (errors.Any())
                        return BadRequest(new { messages = errors });

                    _mapper.Map(model, sickleave);
                    sickleave.Days = ((model.EndDate - model.StartDate).Days) + 1;
                    await _base.Update(sickleave);

                    var leave = _mapper.Map<SickLeaveDTO>(sickleave);
                    var user = await _accoutnService.FindById(leave.UserID);
                    if (!model.Chronic)
                    {
                        user.NonChronicSickLeavesCount += (int)sickleave.Days;
                    }
                    await _accoutnService.Update(user);
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    return Ok(new { Message = "تم التحديث بنجاح.", Leave = leave });
                }

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpDelete("DeleteSickLeave/{leaveID:guid}")]
        public async Task<IActionResult> DeleteSickLeave(Guid leaveID)
        {
            if (leaveID == Guid.Empty)
                return BadRequest(new { message = "معرّف الإجازة غير صالح." });
            try
            {
                var sickleave = await _base.Get(s => s.Id == leaveID);

                if (sickleave == null)
                {
                    return NotFound("لم يتم العثور على إجازة مرضية.");
                }

                await _base.Remove(sickleave);
                return Ok("تم حذف الإجازة المرضية بنجاح.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
    }
}