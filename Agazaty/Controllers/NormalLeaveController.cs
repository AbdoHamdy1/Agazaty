using Agazaty.Data.Base;
using Agazaty.Data.DTOs.CasualLeaveDTOs;
using Agazaty.Data.DTOs.NormalLeaveDTOs;
using Agazaty.Data.Email;
using Agazaty.Data.Enums;
using Agazaty.Data.Services.Interfaces;
using Agazaty.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NormalLeaveTask.Models;
using System.Data;
using static System.Net.WebRequestMethods;
using System.Security.Principal;
using System.Runtime.InteropServices;
using Agazaty.Data.DTOs.AccountDTOs;

namespace Agazaty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NormalLeaveController : ControllerBase
    {
        private readonly IEntityBaseRepository<NormalLeave> _base;
        private readonly IAccountService _accountService;
        private readonly IEntityBaseRepository<Department> _departmentBase;
        private readonly IMapper _mapper;
        private readonly AppDbContext _appDbContext;
        private readonly IEmailService _EmailService;
        private readonly ILeaveValidationService _leaveValidationService;
        public NormalLeaveController(AppDbContext appDbContext, IEntityBaseRepository<NormalLeave> Ebase, IAccountService accountService, IMapper mapper, IEntityBaseRepository<Department> departmentBase, IEmailService EmailService, ILeaveValidationService leaveValidationService)
        {
            _base = Ebase;
            _accountService = accountService;
            _mapper = mapper;
            _appDbContext = appDbContext;
            _departmentBase = departmentBase;
            _EmailService = EmailService;
            _leaveValidationService = leaveValidationService;
        }
        //private static Dictionary<int, List<DateTime>> officialHolidaysByYear = new Dictionary<int, List<DateTime>>();
        // //✅ 1. إدخال الإجازات الرسمية لسنة معينة من الـ HR
        //[HttpPost("add-holidays/{year}")]
        //public IActionResult AddOfficialHolidays(int year, [FromBody] List<DateTime> holidays)
        //{
        //    if (!officialHolidaysByYear.ContainsKey(year))
        //    {
        //        officialHolidaysByYear[year] = new List<DateTime>();
        //    }

        //    officialHolidaysByYear[year].AddRange(holidays);
        //    return Ok(new { message = $"تمت إضافة الإجازات الرسمية لسنة {year} بنجاح!", holidays });
        //}
        //// ✅ 2. عرض الإجازات الرسمية لسنة معينة
        //[HttpGet("holidays/{year}")]
        //public IActionResult GetOfficialHolidays(int year)
        //{
        //    if (!officialHolidaysByYear.ContainsKey(year))
        //    {
        //        return NotFound(new { message = $"لا توجد إجازات مسجلة لسنة {year}." });
        //    }

        //    return Ok(new { year, holidays = officialHolidaysByYear[year] });
        //}
        [Authorize]
        [HttpGet("GetNormalLeaveById/{leaveID:guid}")]
        public async Task<IActionResult> GetNormalLeaveById(Guid leaveID)
        {
            if (leaveID == Guid.Empty)
                return BadRequest(new { message = "معرف الإجازة غير صالح." });
            try
            {

                var NormalLeave = await _base.Get(n => n.ID == leaveID);
                if (NormalLeave == null)
                {
                    return NotFound(new { message = "لم يتم العثور على إجازة اعتيادية." });
                }

                var leave = _mapper.Map<NormalLeaveDTO>(NormalLeave);
                var user = await _accountService.FindById(NormalLeave.UserID);
                var coworker = await _accountService.FindById(NormalLeave.Coworker_ID);
                var generalManager = await _accountService.FindById(NormalLeave.General_ManagerID); ;
                var directManager = await _accountService.FindById(NormalLeave.Direct_ManagerID);
                leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                leave.DirectManagerName = $"{directManager.FirstName} {directManager.SecondName} {directManager.ThirdName} {directManager.ForthName}";
                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                leave.PhoneNumber = user.PhoneNumber;
                var department = await _departmentBase.Get(d => d.Id == user.Departement_ID);
                if (department != null)
                {
                    leave.DepartmentName = department.Name;
                }
                leave.CoworkerName = $"{coworker.FirstName} {coworker.SecondName} {coworker.ThirdName} {coworker.ForthName}";
                leave.FirstName = user.FirstName;
                leave.SecondName=user.SecondName;
                return Ok(leave);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية,عميد الكلية")]
        [HttpGet("GetAllNormalLeaves")]
        public async Task<IActionResult> GetAllNormalLeaves()
        {
            try
            {

                var NormalLeaves = await _base.GetAll();
                if (!NormalLeaves.Any())
                {
                    return NotFound(new { message = "لم يتم العثور على أي إجازات اعتيادية." });
                }
                var leaves = new List<NormalLeaveDTO>();
                foreach (var normalleave in NormalLeaves)
                {
                    var leave = _mapper.Map<NormalLeaveDTO>(normalleave);
                    var user = await _accountService.FindById(normalleave.UserID);
                    var coworker = await _accountService.FindById(normalleave.Coworker_ID);
                    var generalManager = await _accountService.FindById(normalleave.General_ManagerID); ;
                    var directManager = await _accountService.FindById(normalleave.Direct_ManagerID);
                    leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                    leave.DirectManagerName = $"{directManager.FirstName} {directManager.SecondName} {directManager.ThirdName} {directManager.ForthName}";
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    leave.CoworkerName = $"{coworker.FirstName} {coworker.SecondName} {coworker.ThirdName} {coworker.ForthName}";

                    leaves.Add(leave);
                }
                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpGet("GetAllAcceptedNormalLeaves")]
        public async Task<IActionResult> GetAllAcceptedNormalLeaves()
        {
            try
            {

                var NormalLeaves = await _base.GetAll(n => n.Accepted == true && n.ResponseDone == true);
                if (!NormalLeaves.Any())
                {
                    return NotFound(new { message = "لم يتم العثور على أي إجازات اعتيادية مقبولة." });
                }
                var leaves = new List<NormalLeaveDTO>();
                foreach (var normalleave in NormalLeaves)
                {
                    var leave = _mapper.Map<NormalLeaveDTO>(normalleave);
                    var user = await _accountService.FindById(normalleave.UserID);
                    var coworker = await _accountService.FindById(normalleave.Coworker_ID);
                    var generalManager = await _accountService.FindById(normalleave.General_ManagerID); ;
                    var directManager = await _accountService.FindById(normalleave.Direct_ManagerID);
                    leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                    leave.DirectManagerName = $"{directManager.FirstName} {directManager.SecondName} {directManager.ThirdName} {directManager.ForthName}";
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    leave.CoworkerName = $"{coworker.FirstName} {coworker.SecondName} {coworker.ThirdName} {coworker.ForthName}";

                    leaves.Add(leave);
                }
                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpGet("GetAllRejectedNormalLeaves")]
        public async Task<IActionResult> GetAllRejectedNormalLeaves()
        {
            try
            {
                var NormalLeaves = await _base.GetAll(n => n.Accepted == false && n.ResponseDone == true);
                if (!NormalLeaves.Any())
                {
                    return NotFound(new { message = "لم يتم العثور على أي إجازات اعتيادية مرفوضة." });
                }
                var leaves = new List<NormalLeaveDTO>();
                foreach (var normalleave in NormalLeaves)
                {
                    var leave = _mapper.Map<NormalLeaveDTO>(normalleave);
                    var user = await _accountService.FindById(normalleave.UserID);
                    var coworker = await _accountService.FindById(normalleave.Coworker_ID);
                    var generalManager = await _accountService.FindById(normalleave.General_ManagerID); ;
                    var directManager = await _accountService.FindById(normalleave.Direct_ManagerID);
                    leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                    leave.DirectManagerName = $"{directManager.FirstName} {directManager.SecondName} {directManager.ThirdName} {directManager.ForthName}";
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    leave.CoworkerName = $"{coworker.FirstName} {coworker.SecondName} {coworker.ThirdName} {coworker.ForthName}";

                    leaves.Add(leave);
                }
                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpGet("GetAllWaitingNormalLeaves")]
        public async Task<IActionResult> GetAllWaitingNormalLeaves()
        {
            try
            {
                var NormalLeaves = await _base.GetAll(n => n.LeaveStatus == LeaveStatus.Waiting);
                if (!NormalLeaves.Any())
                {
                    return NotFound(new { message = "لم يتم العثور على أي إجازات اعتيادية في انتظار الموافقة." });
                }
                var leaves = new List<NormalLeaveDTO>();
                foreach (var normalleave in NormalLeaves)
                {
                    var leave = _mapper.Map<NormalLeaveDTO>(normalleave);
                    var user = await _accountService.FindById(normalleave.UserID);
                    var coworker = await _accountService.FindById(normalleave.Coworker_ID);
                    var generalManager = await _accountService.FindById(normalleave.General_ManagerID); ;
                    var directManager = await _accountService.FindById(normalleave.Direct_ManagerID);
                    leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                    leave.DirectManagerName = $"{directManager.FirstName} {directManager.SecondName} {directManager.ThirdName} {directManager.ForthName}";
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    leave.CoworkerName = $"{coworker.FirstName} {coworker.SecondName} {coworker.ThirdName} {coworker.ForthName}";

                    leaves.Add(leave);
                }
                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpGet("AllNormalLeavesByUserId/{userID}")]
        public async Task<IActionResult> GetAllNormalLeavesByUserID(string userID)
        {
            if (string.IsNullOrWhiteSpace(userID))
                return BadRequest(new { message = "معرف المستخدم غير صالح." });
            try
            {
                var NormalLeaves = await _base.GetAll(n => n.UserID == userID);
                if (!NormalLeaves.Any())
                {
                    return NotFound(new { message = "لم يتم العثور على أي إجازات اعتيادية." });
                }
                var leaves = new List<NormalLeaveDTO>();
                foreach (var normalleave in NormalLeaves)
                {
                    var leave = _mapper.Map<NormalLeaveDTO>(normalleave);
                    var user = await _accountService.FindById(normalleave.UserID);
                    var coworker = await _accountService.FindById(normalleave.Coworker_ID);
                    var generalManager = await _accountService.FindById(normalleave.General_ManagerID); ;
                    var directManager = await _accountService.FindById(normalleave.Direct_ManagerID);
                    leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                    leave.DirectManagerName = $"{directManager.FirstName} {directManager.SecondName} {directManager.ThirdName} {directManager.ForthName}";
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    leave.CoworkerName = $"{coworker.FirstName} {coworker.SecondName} {coworker.ThirdName} {coworker.ForthName}";

                    leaves.Add(leave);
                }
                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpGet("AcceptedByUserId/{userID}")]
        public async Task<IActionResult> GetAllAcceptedNormalLeavesByUserID(string userID)
        {
            if (string.IsNullOrWhiteSpace(userID))
                return BadRequest(new { message = "معرف المستخدم غير صالح." });
            try
            {

                var NormalLeaves = await _base.GetAll(n => n.UserID == userID &&
                    n.Accepted == true &&
                    n.ResponseDone == true);
                if (!NormalLeaves.Any())
                {
                    return NotFound(new { message = "لم يتم العثور على أي إجازات اعتيادية مقبولة." });
                }
                var leaves = new List<NormalLeaveDTO>();
                foreach (var normalleave in NormalLeaves)
                {
                    var leave = _mapper.Map<NormalLeaveDTO>(normalleave);
                    var user = await _accountService.FindById(normalleave.UserID);
                    var coworker = await _accountService.FindById(normalleave.Coworker_ID);
                    var generalManager = await _accountService.FindById(normalleave.General_ManagerID); ;
                    var directManager = await _accountService.FindById(normalleave.Direct_ManagerID);
                    leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                    leave.DirectManagerName = $"{directManager.FirstName} {directManager.SecondName} {directManager.ThirdName} {directManager.ForthName}";
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    leave.CoworkerName = $"{coworker.FirstName} {coworker.SecondName} {coworker.ThirdName} {coworker.ForthName}";

                    leaves.Add(leave);
                }
                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpGet("AcceptedByUserIdAndYear/{userID}/{year:int}")]
        public async Task<IActionResult> GetAllAcceptedNormalLeavesByUserIDAndYear(string userID, int year)
        {
            try
            {
                var errors = new List<string>();
                int currentYear = DateTime.Now.Year;
                if (string.IsNullOrWhiteSpace(userID))
                    errors.Add("معرف المستخدم غير صالح.");
                if (year < 1900)
                    errors.Add("السنة غير صالحة.");
                else if (year > currentYear)
                    errors.Add($"لا يمكن أن تكون السنة أحدث من السنة الحالية ({currentYear}).");
                if (errors.Any())
                    return BadRequest(new { messages = errors });

                var NormalLeaves = await _base.GetAll(n =>
                    n.UserID == userID &&
                    n.Year == year &&
                    n.Accepted == true &&
                    n.ResponseDone == true);
                if (!NormalLeaves.Any())
                {
                    return NotFound(new { message = $"لم يتم العثور على أي إجازات اعتيادية مقبولة في سنة {year}." });
                }
                var leaves = new List<NormalLeaveDTO>();
                foreach (var normalleave in NormalLeaves)
                {
                    var leave = _mapper.Map<NormalLeaveDTO>(normalleave);
                    var user = await _accountService.FindById(normalleave.UserID);
                    var coworker = await _accountService.FindById(normalleave.Coworker_ID);
                    var generalManager = await _accountService.FindById(normalleave.General_ManagerID); ;
                    var directManager = await _accountService.FindById(normalleave.Direct_ManagerID);
                    leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                    leave.DirectManagerName = $"{directManager.FirstName} {directManager.SecondName} {directManager.ThirdName} {directManager.ForthName}";
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    leave.CoworkerName = $"{coworker.FirstName} {coworker.SecondName} {coworker.ThirdName} {coworker.ForthName}";

                    leaves.Add(leave);
                }
                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpGet("RejectedByUserId/{userID}")]
        public async Task<IActionResult> GetAllRejectedNormalLeavesByUserID(string userID)
        {
            if (string.IsNullOrWhiteSpace(userID))
                return BadRequest(new { message = "معرف المستخدم غير صالح." });
            try
            {

                var NormalLeaves = await _base.GetAll(n =>
                    n.UserID == userID &&
                    n.Accepted == false &&
                    n.ResponseDone == true);
                if (!NormalLeaves.Any())
                {
                    return NotFound(new { message = "لم يتم العثور على أي إجازات اعتيادية مرفوضة." });
                }
                var leaves = new List<NormalLeaveDTO>();
                foreach (var normalleave in NormalLeaves)
                {
                    var leave = _mapper.Map<NormalLeaveDTO>(normalleave);
                    var user = await _accountService.FindById(normalleave.UserID);
                    var coworker = await _accountService.FindById(normalleave.Coworker_ID);
                    var generalManager = await _accountService.FindById(normalleave.General_ManagerID); ;
                    var directManager = await _accountService.FindById(normalleave.Direct_ManagerID);
                    leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                    leave.DirectManagerName = $"{directManager.FirstName} {directManager.SecondName} {directManager.ThirdName} {directManager.ForthName}";
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    leave.CoworkerName = $"{coworker.FirstName} {coworker.SecondName} {coworker.ThirdName} {coworker.ForthName}";

                    leaves.Add(leave);
                }
                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpGet("WaitingByUserID/{userID}")]
        public async Task<IActionResult> GetAllWaitingNormalLeavesByUserID(string userID)
        {
            if (string.IsNullOrWhiteSpace(userID))
                return BadRequest(new { message = "معرف المستخدم غير صالح." });
            try
            {

                var NormalLeaves = await _base.GetAll(n =>
                    n.UserID == userID &&
                    n.ResponseDone == false);
                if (!NormalLeaves.Any())
                {
                    return NotFound(new { message = "لم يتم العثور على أي إجازات اعتيادية في انتظار الموافقة." });
                }

                var leaves = new List<NormalLeaveDTO>();
                foreach (var normalleave in NormalLeaves)
                {
                    var leave = _mapper.Map<NormalLeaveDTO>(normalleave);
                    var user = await _accountService.FindById(normalleave.UserID);
                    var coworker = await _accountService.FindById(normalleave.Coworker_ID);
                    var generalManager = await _accountService.FindById(normalleave.General_ManagerID); ;
                    var directManager = await _accountService.FindById(normalleave.Direct_ManagerID);
                    leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                    leave.DirectManagerName = $"{directManager.FirstName} {directManager.SecondName} {directManager.ThirdName} {directManager.ForthName}";
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    leave.CoworkerName = $"{coworker.FirstName} {coworker.SecondName} {coworker.ThirdName} {coworker.ForthName}";

                    leaves.Add(leave);
                }
                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
        [Authorize(Roles = "عميد الكلية,أمين الكلية")]
        [HttpGet("WaitingByGeneral_ManagerID/{general_managerID}")]
        public async Task<IActionResult> GetAllWaitingNormalLeavesByGeneral_ManagerID(string general_managerID)
        {
            if (string.IsNullOrWhiteSpace(general_managerID))
                return BadRequest(new { message = "معرف المدير المختص غير صالح." });
            try
            {

                var NormalLeaves = await _base.GetAll(n =>
                    n.General_ManagerID == general_managerID &&
                    n.GeneralManager_Decision == false &&
                    n.DirectManager_Decision == true &&
                    n.CoWorker_Decision == true &&
                    n.ResponseDone == false);
                if (!NormalLeaves.Any())
                {
                    return NotFound(new { message = "لا يوجد أي إجازات اعتيادية في الانتظار." });
                }

                var leaves = new List<NormalLeaveDTO>();
                foreach (var normalleave in NormalLeaves)
                {
                    var leave = _mapper.Map<NormalLeaveDTO>(normalleave);
                    var user = await _accountService.FindById(normalleave.UserID);
                    var coworker = await _accountService.FindById(normalleave.Coworker_ID);
                    var generalManager = await _accountService.FindById(normalleave.General_ManagerID); ;
                    var directManager = await _accountService.FindById(normalleave.Direct_ManagerID);
                    leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                    leave.DirectManagerName = $"{directManager.FirstName} {directManager.SecondName} {directManager.ThirdName} {directManager.ForthName}";
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    leave.CoworkerName = $"{coworker.FirstName} {coworker.SecondName} {coworker.ThirdName} {coworker.ForthName}";
                    leave.PhoneNumber = user.PhoneNumber;
                    var department = await _departmentBase.Get(d => d.Id == user.Departement_ID);
                    leave.DepartmentName = department.Name;
                    leaves.Add(leave);
                }
                return Ok(leaves);
                //return Ok(_mapper.Map<IEnumerable<NormalLeaveDTO>>(NormalLeaves));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpGet("WaitingByDirect_ManagerID/{direct_managerID}")]
        public async Task<IActionResult> GetAllWaitingNormalLeavesByDirect_ManagerID(string direct_managerID)
        {
            if (string.IsNullOrWhiteSpace(direct_managerID))
                return BadRequest(new { message = "معرف المدير المباشر غير صالح." });
            try
            {

                var NormalLeaves = await _base.GetAll(n =>
                    n.Direct_ManagerID == direct_managerID &&
                    n.DirectManager_Decision == false &&
                    n.CoWorker_Decision == true &&
                    n.ResponseDone == false);
                if (!NormalLeaves.Any())
                {
                    return NotFound(new { message = "لا يوجد أي إجازات اعتيادية في الانتظار." });
                }

                var leaves = new List<NormalLeaveDTO>();
                foreach (var normalleave in NormalLeaves)
                {
                    var leave = _mapper.Map<NormalLeaveDTO>(normalleave);
                    var user = await _accountService.FindById(normalleave.UserID);
                    var coworker = await _accountService.FindById(normalleave.Coworker_ID);
                    var generalManager = await _accountService.FindById(normalleave.General_ManagerID); ;
                    var directManager = await _accountService.FindById(normalleave.Direct_ManagerID);
                    leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                    leave.DirectManagerName = $"{directManager.FirstName} {directManager.SecondName} {directManager.ThirdName} {directManager.ForthName}";
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    leave.CoworkerName = $"{coworker.FirstName} {coworker.SecondName} {coworker.ThirdName} {coworker.ForthName}";
                    leave.PhoneNumber = user.PhoneNumber;
                    var department = await _departmentBase.Get(d => d.Id == user.Departement_ID);
                    leave.DepartmentName = department.Name;
                    leaves.Add(leave);
                }
                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpGet("WaitingByCoWorkerID/{coworkerID}")]
        public async Task<IActionResult> GetAllWaitingNormalLeavesByCoWorkerID(string coworkerID)
        {
            if (string.IsNullOrWhiteSpace(coworkerID))
                return BadRequest(new { message = "معرف القائم بالعمل غير صالح." });
            try
            {

                var NormalLeaves = await _base.GetAll(n =>
                    n.Coworker_ID == coworkerID &&
                    n.ResponseDone == false && n.CoWorker_Decision == false);
                if (!NormalLeaves.Any())
                {
                    return NotFound(new { message = "لا يوجد أي إجازات اعتيادية في الانتظار." });
                }

                var leaves = new List<NormalLeaveDTO>();
                foreach (var normalleave in NormalLeaves)
                {
                    var leave = _mapper.Map<NormalLeaveDTO>(normalleave);
                    var user = await _accountService.FindById(normalleave.UserID);
                    var coworker = await _accountService.FindById(normalleave.Coworker_ID);
                    var generalManager = await _accountService.FindById(normalleave.General_ManagerID); ;
                    var directManager = await _accountService.FindById(normalleave.Direct_ManagerID);
                    leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                    leave.DirectManagerName = $"{directManager.FirstName} {directManager.SecondName} {directManager.ThirdName} {directManager.ForthName}";
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                    leave.CoworkerName = $"{coworker.FirstName} {coworker.SecondName} {coworker.ThirdName} {coworker.ForthName}";

                    leaves.Add(leave);
                }
                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpGet("GetLeaveTypes")]
        public async Task<IActionResult> GetLeaveTypes()
        {
            return Ok(LeaveTypes.res);
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpPost("MinusOrAddNormalLeavesToUser/{UserID}")]
        public async Task<IActionResult> MinusOrAddNormalLeavesToUser(string UserID, [FromBody] MinusOrAddNormalLeavesToUser model)
        {

            if (string.IsNullOrWhiteSpace(UserID))
                return BadRequest(new { message = "معرف المستخدم غير صالح." });
            try
            {
                var user = await _accountService.FindById(UserID);
                if (user == null)
                {
                    return NotFound(new { message = "لا يوجد مستخدم بهذا المعرف." });
                }
                if (model.Decision)
                {
                    user.NormalLeavesCount += model.Days;
                }
                else
                {
                    user.NormalLeavesCount -= model.Days;
                }
                await _accountService.Update(user);


                return Ok(new { message = "تم تنفيذ طلبك بنجاح." });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }

        }
        [Authorize]
        [HttpPost("CreateNormalLeave")]
        public async Task<IActionResult> CreateNormalLeave([FromBody] CreateNormalLeaveDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                if (model == null)
                {
                    return BadRequest(new { message = "بيانات الإجازة الاعتيادية غير صالحة." });
                }
                // Check if the user already has a pending leave request
                bool hasPendingLeave = _appDbContext.NormalLeaves
                .Any(l => l.UserID == model.UserID && l.ResponseDone == false);
                if (hasPendingLeave)
                {
                    return BadRequest(new { message = "لديك بالفعل طلب إجازة قيد الانتظار لم يتم الرد عليه بعد." });
                }
                if (await _leaveValidationService.IsSameLeaveOverlapping(model.UserID, model.StartDate, model.EndDate, "NormalLeave"))
                {
                    return BadRequest("لديك بالفعل إجازة اعتيادية في هذه الفترة.");
                }
                if (await _leaveValidationService.IsLeaveOverlapping(model.UserID, model.StartDate, model.EndDate, "NormalLeave"))
                {
                    return BadRequest("لديك بالفعل إجازة من نوع آخر في هذه الفترة.");
                }
                var cowrker = await _accountService.FindById(model.Coworker_ID);
                var user = await _accountService.FindById(model.UserID);
                if (model.Coworker_ID == model.UserID || user == null || cowrker == null)
                {
                    return BadRequest(new { Message = "معرف المستخدم أو معرف القائم بالعمل غير صالح." });
                }
                int LeaveDays = await _leaveValidationService.CalculateLeaveDays(model.StartDate, model.EndDate);
                if (LeaveDays > user.NormalLeavesCount + user.NormalLeavesCount_81Before1Years + user.NormalLeavesCount_81Before2Years + user.NormalLeavesCount_81Before3Years + user.NormalLeavesCount_47)
                {
                    return BadRequest(new { Message = "لا يوجد أيام كافية." });
                }

                int DifferenceDays = LeaveDays- user.NormalLeavesCount;
                if (user.HowManyDaysFrom81And47 + DifferenceDays > 60)
                {
                    return BadRequest(new { Message = "لا يمكنك طلب إجازة لأنك قد تجاوزت الحد الأقصى البالغ 60 يومًا وفقًا لقانوني الخدمة المدنية رقم 81 و47." });
                }

                var errors = new List<string>();
                DateTime today = DateTime.Today;
                int year = DateTime.Now.Year;
                

                // Check if the new leave period overlaps with any existing approved leave
                bool hasOverlappingLeave = _appDbContext.NormalLeaves
                    .Any(l => l.UserID == model.UserID && l.Accepted == true &&
                              !(model.EndDate < l.StartDate || model.StartDate > l.EndDate));
                // This ensures that the new leave period does NOT completely fall outside an existing leave period
                if (hasOverlappingLeave)
                {
                    return BadRequest(new { message = "فترة الإجازة التي طلبتها تتداخل مع فترة إجازة معتمدة موجودة." });
                }
                user.TakenNormalLeavesCount = 0;
                user.TakenNormalLeavesCount_47 = 0;
                user.TakenNormalLeavesCount_81Before1Years = 0;
                user.TakenNormalLeavesCount_81Before2Years = 0;
                user.TakenNormalLeavesCount_81Before3Years = 0;

                //validation on year
                if (model.StartDate.Date == DateTime.UtcNow.Date) { 
                DateTime currentTime = DateTime.UtcNow; // Or use request timestamp
                var UserRole = await _accountService.GetFirstRole(user);

                    if (UserRole == "موظف")
                    {
                        if (user.position == 1)
                        {

                            // Define cutoff time (8:30 AM)
                            TimeSpan cutoffTime = new TimeSpan(8, 30, 0); // 8:30 AM

                            if (currentTime.TimeOfDay > cutoffTime)
                            {
                                // Reject if current time is 8:30 AM or later
                                return BadRequest(new { message = "Administrative staff can only submit requests before or equal 8:30 AM." });
                            }
                        }
                        else
                        {
                            // Define cutoff time (7:30 AM)
                            TimeSpan cutoffTime = new TimeSpan(7, 30, 0); // 7:30 AM

                            if (currentTime.TimeOfDay > cutoffTime)
                            {
                                // Reject if current time is 7:30 AM or later
                                return BadRequest(new { message = "Nonadministrative staff can only submit requests before or equal 7:30 AM." });
                            }
                        }
                    }
                }
                if (model.EndDate < today)
                    errors.Add("تاريخ النهاية لا يمكن أن يكون في الماضي، الرجاء اختيار تاريخ مستقبلي.");

                if (model.StartDate < today)
                    errors.Add("تاريخ البدء لا يمكن أن يكون في الماضي، الرجاء اختيار تاريخ مستقبلي.");

                if (model.StartDate > model.EndDate)
                    errors.Add("تاريخ البدء لا يمكن أن يكون بعد تاريخ النهاية.");

                if (DateTime.UtcNow.Date > model.StartDate)
                    errors.Add("تاريخ الطلب لا يمكن أن يكون بعد تاريخ البدء.");

                if (errors.Any())
                    return BadRequest(new { messages = errors });

                var normalLeave = _mapper.Map<NormalLeave>(model);
                normalLeave.RequestDate = DateTime.UtcNow.Date;
                normalLeave.Year = normalLeave.RequestDate.Year;
                normalLeave.LeaveStatus = LeaveStatus.Waiting;
                normalLeave.Holder = Holder.CoWorker;
                normalLeave.RejectedBy = RejectedBy.NotRejected;
                normalLeave.Days = LeaveDays;
                if (await _accountService.IsInRoleAsync(user, "هيئة تدريس"))
                {
                    var res = await _accountService.GetAllUsersInRole("عميد الكلية");
                    var Dean = res.FirstOrDefault();
                    if (Dean == null) { return BadRequest(new { Message = "لا يوجد مستخدم لديه دور العميد." }); }
                    normalLeave.General_ManagerID = Dean.Id;
                    // >>>>>>>>>>>>>>>>>>>>>>>>>>>  add this
                    var IsManagerOfDepartment = await _departmentBase.Get(d => d.ManagerId == user.Id);
                    if (IsManagerOfDepartment != null)
                    {
                        normalLeave.Direct_ManagerID = Dean.Id;
                    }
                    else
                    {
                        //if (DepartmentofUser == null) { return BadRequest(new { Message = "This user doesn't have a department, so user doesn't have a direct manager." }); }
                        if (user.Departement_ID == null) normalLeave.Direct_ManagerID = Dean.Id;
                        else
                        {
                            var DepartmentofUser = await _departmentBase.Get(dm => dm.Id == user.Departement_ID);
                            if (DepartmentofUser == null)
                            {
                                normalLeave.Direct_ManagerID = Dean.Id;
                            }
                            else if (DepartmentofUser.ManagerId == null)  // القسم ليس له رئيس
                            {
                                normalLeave.Direct_ManagerID = Dean.Id;
                            }
                            else
                            {
                                var manager = await _accountService.FindById(DepartmentofUser.ManagerId);
                                if (manager == null) normalLeave.Direct_ManagerID = Dean.Id;
                                else normalLeave.Direct_ManagerID = DepartmentofUser.ManagerId;
                            }
                        }
                    }
                    // >>>>>>>>>>>>>>>>>>>>>>>>>>>

                    //var DepartmentofUser = await _departmentBase.Get(dm => dm.Id == user.Departement_ID);
                    //if (DepartmentofUser == null) { return BadRequest(new { Message = "هذا المستخدم ليس لديه قسم، لذلك ليس لديه مدير مباشر." }); }
                    //normalLeave.Direct_ManagerID = DepartmentofUser.ManagerId;
                }
                else if (await _accountService.IsInRoleAsync(user, "موظف"))
                {
                    var res = await _accountService.GetAllUsersInRole("أمين الكلية");
                    var Supervisor = res.FirstOrDefault();
                    if (Supervisor == null) { return BadRequest(new { Message = "لا يوجد مستخدم لديه دور أمين كلية." }); }
                    normalLeave.General_ManagerID = Supervisor.Id;
                    // >>>>>>>>>>>>>>>>>>>>>>>>>>>  add this
                    var IsManagerOfDepartment = await _departmentBase.Get(d => d.ManagerId == user.Id);
                    if (IsManagerOfDepartment != null)
                    {
                        normalLeave.Direct_ManagerID = Supervisor.Id;
                    }
                    else
                    {
                        //if (DepartmentofUser == null) { return BadRequest(new { Message = "This user doesn't have a department, so user doesn't have a direct manager." }); }
                        if (user.Departement_ID == null) normalLeave.Direct_ManagerID = Supervisor.Id;
                        else
                        {
                            var DepartmentofUser = await _departmentBase.Get(dm => dm.Id == user.Departement_ID);
                            if (DepartmentofUser == null)
                            {
                                normalLeave.Direct_ManagerID = Supervisor.Id;
                            }
                            else if (DepartmentofUser.ManagerId == null)  // القسم ليس له رئيس
                            {
                                normalLeave.Direct_ManagerID = Supervisor.Id;
                            }
                            else
                            {
                                var manager = await _accountService.FindById(DepartmentofUser.ManagerId);
                                if (manager == null) normalLeave.Direct_ManagerID = Supervisor.Id;
                                else normalLeave.Direct_ManagerID = DepartmentofUser.ManagerId;
                            }
                        }
                    }
                    // >>>>>>>>>>>>>>>>>>>>>>>>>>>

                    //var DepartmentofUser = await _departmentBase.Get(dm => dm.Id == user.Departement_ID);
                    //if (DepartmentofUser == null) { return BadRequest(new { Message = "هذا المستخدم ليس لديه قسم، لذلك ليس لديه مدير مباشر." }); }
                    //normalLeave.Direct_ManagerID = DepartmentofUser.ManagerId;
                }
                else if (await _accountService.IsInRoleAsync(user, "أمين الكلية"))
                {
                    // if أمين الكلية made a leave request
                    var res = await _accountService.GetAllUsersInRole("عميد الكلية");
                    var Dean = res.FirstOrDefault();
                    if (Dean == null) { return BadRequest(new { Message = "لا يوجد مستخدم لديه دور العميد." }); }
                    normalLeave.General_ManagerID = Dean.Id;
                    normalLeave.Direct_ManagerID = Dean.Id;
                }
                else if (await _accountService.IsInRoleAsync(user, "مدير الموارد البشرية"))
                {
                    var res = await _accountService.GetAllUsersInRole("أمين الكلية");
                    var Supervisor = res.FirstOrDefault();
                    if (Supervisor == null) { return BadRequest(new { Message = "لا يوجد مستخدم لديه دور أمين كلية." }); }
                    normalLeave.General_ManagerID = Supervisor.Id;
                    normalLeave.Direct_ManagerID = Supervisor.Id;
                }
                await _base.Add(normalLeave);


                var leave = _mapper.Map<NormalLeaveDTO>(normalLeave);
                var coworker = await _accountService.FindById(normalLeave.Coworker_ID);
                var generalManager = await _accountService.FindById(normalLeave.General_ManagerID); ;
                var directManager = await _accountService.FindById(normalLeave.Direct_ManagerID);
                leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                leave.DirectManagerName = $"{directManager.FirstName} {directManager.SecondName} {directManager.ThirdName} {directManager.ForthName}";
                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                leave.CoworkerName = $"{coworker.FirstName} {coworker.SecondName} {coworker.ThirdName} {coworker.ForthName}";
                return CreatedAtAction(nameof(GetNormalLeaveById), new { leaveID = normalLeave.ID }, leave);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب.", error = ex.Message });
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpPut("UpdateNormalLeave/{leaveID:guid}")]
        public async Task<IActionResult> UpdateNormalLeave(Guid leaveID, [FromBody] UpdateNormalLeaveDTO model) // قطع الاجازة
        {
            if (leaveID == Guid.Empty)
                return BadRequest(new { message = "معرّف الإجازة غير صالح." });

            try
            {
                if (model == null)
                {
                    return BadRequest(new { message = "بيانات الطلب غير صالحة." });
                }
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var NormalLeave = await _base.Get(n =>
                    n.ID == leaveID &&
                    n.ResponseDone == true &&
                    n.Accepted == true
                    );

                if (NormalLeave == null)
                {
                    return NotFound(new { message = "لم يتم العثور على الإجازة الإعتيادية أو أنها غير قابلة للتحديث." });
                }
                DateTime today = DateTime.Today;
                var errors = new List<string>();

                if (model.EndDate < today)
                    errors.Add("تاريخ النهاية لا يمكن أن يكون في الماضي.");

                if (NormalLeave.StartDate > model.EndDate)
                    errors.Add("تاريخ البدء لا يمكن أن يكون بعد تاريخ النهاية الجديد.");

                if (errors.Any())
                    return BadRequest(new { messages = errors });
                // Update properties
                var user = await _accountService.FindById(NormalLeave.UserID);

                NormalLeave.NotesFromEmployee = model.NotesFromEmployee;
                //user.NormalLeavesCount += (int)((NormalLeave.EndDate - model.EndDate).TotalDays + 1);

                int returnedDays = await _leaveValidationService.CalculateLeaveDays(NormalLeave.EndDate,model.EndDate);
                NormalLeave.EndDate = model.EndDate;
                NormalLeave.Days = await _leaveValidationService.CalculateLeaveDays(model.EndDate,NormalLeave.StartDate);
                if (user.Counts == CountsFromNormalLeaveTypes.FromNormalLeave)
                {
                    user.NormalLeavesCount += returnedDays;
                    returnedDays = 0;
                }

                else if (user.Counts == CountsFromNormalLeaveTypes.From81Before3Years)
                {
                    if (returnedDays >= user.TakenNormalLeavesCount_81Before3Years)
                    {
                        user.NormalLeavesCount_81Before3Years += user.TakenNormalLeavesCount_81Before3Years;
                        returnedDays -= user.TakenNormalLeavesCount_81Before3Years;
                    }
                    else
                    {
                        user.NormalLeavesCount_81Before3Years += returnedDays;
                        returnedDays = 0;
                    }
                    user.NormalLeavesCount += returnedDays;
                    returnedDays = 0;
                }
                else if (user.Counts == CountsFromNormalLeaveTypes.From81Before2Years)
                {
                    if (returnedDays >= user.TakenNormalLeavesCount_81Before2Years)
                    {
                        user.NormalLeavesCount_81Before2Years += user.TakenNormalLeavesCount_81Before2Years;
                        returnedDays -= user.TakenNormalLeavesCount_81Before2Years;
                    }
                    else
                    {
                        user.NormalLeavesCount_81Before2Years += returnedDays;
                        returnedDays = 0;
                    }
                    if (returnedDays >= user.TakenNormalLeavesCount_81Before3Years)
                    {
                        user.NormalLeavesCount_81Before3Years += user.TakenNormalLeavesCount_81Before3Years;
                        returnedDays -= user.TakenNormalLeavesCount_81Before3Years;
                    }
                    else
                    {
                        user.NormalLeavesCount_81Before3Years += returnedDays;
                        returnedDays = 0;
                    }
                    user.NormalLeavesCount += returnedDays;
                    returnedDays = 0;
                }

                else if (user.Counts == CountsFromNormalLeaveTypes.From81Before1Years)
                {
                    if (returnedDays >= user.TakenNormalLeavesCount_81Before1Years)
                    {
                        user.NormalLeavesCount_81Before1Years += user.TakenNormalLeavesCount_81Before1Years;
                        returnedDays -= user.TakenNormalLeavesCount_81Before1Years;
                    }
                    else
                    {
                        user.NormalLeavesCount_81Before1Years += returnedDays;
                        returnedDays = 0;
                    }
                    if (returnedDays >= user.TakenNormalLeavesCount_81Before2Years)
                    {
                        user.NormalLeavesCount_81Before2Years += user.TakenNormalLeavesCount_81Before2Years;
                        returnedDays -= user.TakenNormalLeavesCount_81Before2Years;
                    }
                    else
                    {
                        user.NormalLeavesCount_81Before2Years += returnedDays;
                        returnedDays = 0;
                    }
                    if (returnedDays >= user.TakenNormalLeavesCount_81Before3Years)
                    {
                        user.NormalLeavesCount_81Before3Years += user.TakenNormalLeavesCount_81Before3Years;
                        returnedDays -= user.TakenNormalLeavesCount_81Before3Years;
                    }
                    else
                    {
                        user.NormalLeavesCount_81Before3Years += returnedDays;
                        returnedDays = 0;
                    }
                    user.NormalLeavesCount += returnedDays;
                    returnedDays = 0;
                }

                else if (user.Counts == CountsFromNormalLeaveTypes.From47)
                {
                    if (returnedDays >= user.TakenNormalLeavesCount_47)
                    {
                        user.NormalLeavesCount_47 += user.TakenNormalLeavesCount_47;
                        returnedDays -= user.TakenNormalLeavesCount_47;
                    }
                    else
                    {
                        user.NormalLeavesCount_47 += returnedDays;
                        returnedDays = 0;
                    }
                    if (returnedDays >= user.TakenNormalLeavesCount_81Before1Years)
                    {
                        user.NormalLeavesCount_81Before1Years += user.TakenNormalLeavesCount_81Before1Years;
                        returnedDays -= user.TakenNormalLeavesCount_81Before1Years;
                    }
                    else
                    {
                        user.NormalLeavesCount_81Before1Years += returnedDays;
                        returnedDays = 0;
                    }
                    if (returnedDays >= user.TakenNormalLeavesCount_81Before2Years)
                    {
                        user.NormalLeavesCount_81Before2Years += user.TakenNormalLeavesCount_81Before2Years;
                        returnedDays -= user.TakenNormalLeavesCount_81Before2Years;
                    }
                    else
                    {
                        user.NormalLeavesCount_81Before2Years += returnedDays;
                        returnedDays = 0;
                    }
                    if (returnedDays >= user.TakenNormalLeavesCount_81Before3Years)
                    {
                        user.NormalLeavesCount_81Before3Years += user.TakenNormalLeavesCount_81Before3Years;
                        returnedDays -= user.TakenNormalLeavesCount_81Before3Years;
                    }
                    else
                    {
                        user.NormalLeavesCount_81Before3Years += returnedDays;
                        returnedDays = 0;
                    }
                    user.NormalLeavesCount += returnedDays;
                    returnedDays = 0;
                }

                await _base.Update(NormalLeave);

                var leave = _mapper.Map<NormalLeaveDTO>(NormalLeave);
                var coworker = await _accountService.FindById(NormalLeave.Coworker_ID);
                var generalManager = await _accountService.FindById(NormalLeave.General_ManagerID); ;
                var directManager = await _accountService.FindById(NormalLeave.Direct_ManagerID);
                leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                leave.DirectManagerName = $"{directManager.FirstName} {directManager.SecondName} {directManager.ThirdName} {directManager.ForthName}";
                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                leave.CoworkerName = $"{coworker.FirstName} {coworker.SecondName} {coworker.ThirdName} {coworker.ForthName}";

                return Ok(new
                {
                    message = "تم تحديث الإجازة الإعتيادية بنجاح.",
                    Leave = leave
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء التحديث.", error = ex.Message });
            }

        }
        [Authorize(Roles = "عميد الكلية,أمين الكلية")]
        [HttpPut("UpdateGeneralManagerDecision/{leaveID:guid}")]
        public async Task<IActionResult> UpdateGeneralManagerDecision(Guid leaveID, [FromBody] GeneralManagerDecisionDTO model)
        {
            if (leaveID == Guid.Empty)
                return BadRequest(new { message = "معرّف الإجازة غير صالح." });

            try
            {
                if (model == null)
                {
                    return BadRequest(new { message = "بيانات الطلب غير صالحة." });
                }
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var NormalLeave = await _base.Get(n =>
                    n.ID == leaveID &&
                    n.CoWorker_Decision == true &&
                    n.DirectManager_Decision == true &&
                    n.Accepted == false &&
                    n.ResponseDone == false
                    );

                if (NormalLeave == null)
                {
                    return NotFound(new { message = "لم يتم العثور على إجازة إعتيادية أو أنها غير قابلة للتحديث." });
                }
                var user = await _accountService.FindById(NormalLeave.UserID);
                // Update properties
                NormalLeave.GeneralManager_Decision = model.GeneralManagerDecision;
                NormalLeave.ResponseDone = true;

                if (model.GeneralManagerDecision == true)
                {
                    //int LeaveDays=await _leaveValidationService.CalculateLeaveDays(NormalLeave.StartDate, NormalLeave.EndDate);
                    int LeaveDays = NormalLeave.Days;
                    NormalLeave.Accepted = true;
                    if (LeaveDays > user.NormalLeavesCount)
                    {                       
                        user.TakenNormalLeavesCount += user.NormalLeavesCount;
                        int DifferenceDays = LeaveDays- user.NormalLeavesCount;
                        user.NormalLeavesCount = 0;
                        user.HowManyDaysFrom81And47 += DifferenceDays;
                        if (DifferenceDays > user.NormalLeavesCount_81Before3Years)
                        {
                            user.TakenNormalLeavesCount_81Before3Years += user.NormalLeavesCount_81Before3Years;
                            DifferenceDays -= user.NormalLeavesCount_81Before3Years;
                            user.NormalLeavesCount_81Before3Years = 0;

                            if (DifferenceDays > user.NormalLeavesCount_81Before2Years)
                            {
                                user.TakenNormalLeavesCount_81Before2Years += user.NormalLeavesCount_81Before2Years;
                                DifferenceDays -= user.NormalLeavesCount_81Before2Years;
                                user.NormalLeavesCount_81Before2Years = 0;

                                if (DifferenceDays > user.NormalLeavesCount_81Before1Years)
                                {
                                    user.TakenNormalLeavesCount_81Before1Years += user.NormalLeavesCount_81Before1Years;
                                    DifferenceDays -= user.NormalLeavesCount_81Before1Years;
                                    user.NormalLeavesCount_81Before1Years = 0;
                                }
                                else
                                {
                                    user.TakenNormalLeavesCount_81Before1Years += DifferenceDays;
                                    user.Counts = CountsFromNormalLeaveTypes.From81Before1Years;
                                    user.NormalLeavesCount_81Before1Years -= DifferenceDays;
                                    DifferenceDays = 0;
                                }
                            }
                            else
                            {
                                user.TakenNormalLeavesCount_81Before2Years += DifferenceDays;
                                user.Counts = CountsFromNormalLeaveTypes.From81Before2Years;
                                user.NormalLeavesCount_81Before2Years -= DifferenceDays;
                                DifferenceDays = 0;
                            }
                        }
                        else
                        {
                            user.TakenNormalLeavesCount_81Before3Years += DifferenceDays;
                            user.Counts = CountsFromNormalLeaveTypes.From81Before3Years;
                            user.NormalLeavesCount_81Before3Years -= DifferenceDays;
                            DifferenceDays = 0;
                        }

                        if (DifferenceDays > 0) 
                        {
                            user.TakenNormalLeavesCount_47 += DifferenceDays;
                            user.NormalLeavesCount_47 -= DifferenceDays;
                            user.Counts = CountsFromNormalLeaveTypes.From47;
                        }
                    }
                    else
                    {
                        user.TakenNormalLeavesCount +=LeaveDays;
                        user.NormalLeavesCount -= user.TakenNormalLeavesCount;
                        user.Counts = CountsFromNormalLeaveTypes.FromNormalLeave;
                    }


                    NormalLeave.LeaveStatus = LeaveStatus.Accepted;
                    NormalLeave.Holder = Holder.NotWaiting;


                    var emailrequest = new EmailRequest
                    {
                        Email = user.Email,
                        Subject = "تم قبول إجازتك الإعتيادية المطلوبة."
                    };
                    await _EmailService.SendEmail(emailrequest);
                }
                else
                {
                    NormalLeave.DisapproveReasonOfGeneral_Manager = model.DisapproveReason;
                    NormalLeave.LeaveStatus = LeaveStatus.Rejected;
                    NormalLeave.RejectedBy = RejectedBy.GeneralManager;
                    NormalLeave.Holder = Holder.NotWaiting;



                    var emailrequest = new EmailRequest
                    {
                        Email = user.Email,
                        Subject = "تم رفض إجازتك الإعتيادية المطلوبة."
                    };
                    await _EmailService.SendEmail(emailrequest);
                }

                await _base.Update(NormalLeave);

                var leave = _mapper.Map<NormalLeaveDTO>(NormalLeave);
                var coworker = await _accountService.FindById(NormalLeave.Coworker_ID);
                var generalManager = await _accountService.FindById(NormalLeave.General_ManagerID); ;
                var directManager = await _accountService.FindById(NormalLeave.Direct_ManagerID);
                leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                leave.DirectManagerName = $"{directManager.FirstName} {directManager.SecondName} {directManager.ThirdName} {directManager.ForthName}";
                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                leave.CoworkerName = $"{coworker.FirstName} {coworker.SecondName} {coworker.ThirdName} {coworker.ForthName}";

                return Ok(new
                {
                    message = "تم تحديث قرار المدير المختص بنجاح.",
                    Leave = leave
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء التحديث.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpPut(("UpdateDirectManagerDecision/{leaveID:guid}"))]
        public async Task<IActionResult> UpdateDirectManagerDecision(Guid leaveID, [FromBody] DirectManagerDecisionDTO model)
        {
            if (leaveID == Guid.Empty)
                return BadRequest(new { message = "معرّف الإجازة غير صالح." });

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var NormalLeave = await _base.Get(n =>
                    n.ID == leaveID &&
                    n.ResponseDone == false &&
                    n.Accepted == false &&
                    n.DirectManager_Decision == false &&
                    n.GeneralManager_Decision == false &&
                    n.CoWorker_Decision == true);

                if (NormalLeave == null)
                {
                    return NotFound(new { message = "لم يتم العثور على إجازة إعتيادية أو أنها غير قابلة للتحديث." });
                }

                var user = await _accountService.FindById(NormalLeave.UserID);
                // Update properties
                NormalLeave.DirectManager_Decision = model.DirectManagerDecision;
                if (model.DirectManagerDecision == false)
                {
                    NormalLeave.DisapproveReasonOfDirect_Manager = model.DisapproveReason;
                    NormalLeave.ResponseDone = true;
                    NormalLeave.LeaveStatus = LeaveStatus.Rejected;
                    NormalLeave.Holder = Holder.NotWaiting;
                    NormalLeave.RejectedBy = RejectedBy.DirectManager;

                    var emailrequest = new EmailRequest
                    {
                        Email = user.Email,
                        Subject = "تم رفض إجازتك الإعتيادية المطلوبة."
                    };
                    await _EmailService.SendEmail(emailrequest);
                }
                else
                {
                    NormalLeave.ResponseDone = false;
                    NormalLeave.Holder = Holder.GeneralManager;
                }

                await _base.Update(NormalLeave);

                var leave = _mapper.Map<NormalLeaveDTO>(NormalLeave);
                var coworker = await _accountService.FindById(NormalLeave.Coworker_ID);
                var generalManager = await _accountService.FindById(NormalLeave.General_ManagerID); ;
                var directManager = await _accountService.FindById(NormalLeave.Direct_ManagerID);
                leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                leave.DirectManagerName = $"{directManager.FirstName} {directManager.SecondName} {directManager.ThirdName} {directManager.ForthName}";
                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                leave.CoworkerName = $"{coworker.FirstName} {coworker.SecondName} {coworker.ThirdName} {coworker.ForthName}";

                return Ok(new
                {
                    message = "تم تحديث قرار المدير المباشر بنجاح.",
                    Leave = leave
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء التحديث.", error = ex.Message });
            }
        }
        [Authorize]
        [HttpPut("UpdateCoworkerDecision/{leaveID:guid}")]
        public async Task<IActionResult> UpdateCoworkerDecision([FromRoute] Guid leaveID, [FromQuery] bool CoworkerDecision)
        {
            if (leaveID == Guid.Empty)
                return BadRequest(new { message = "معرّف الإجازة غير صالح." });

            try
            {
                var NormalLeave = await _base.Get(n =>
                    n.ID == leaveID &&
                    n.ResponseDone == false &&
                    n.DirectManager_Decision == false &&
                    n.GeneralManager_Decision == false &&
                    n.Accepted == false);

                if (NormalLeave == null)
                {
                    return NotFound(new { message = "لم يتم العثور على إجازة إعتيادية أو أنها غير قابلة للتحديث." });
                }

                var user = await _accountService.FindById(NormalLeave.UserID);
                // Update properties
                NormalLeave.CoWorker_Decision = CoworkerDecision;
                if (CoworkerDecision == false)
                {
                    NormalLeave.ResponseDone = true;
                    NormalLeave.LeaveStatus = LeaveStatus.Rejected;
                    NormalLeave.Holder = Holder.NotWaiting;
                    NormalLeave.RejectedBy = RejectedBy.CoWorker;


                    var emailrequest = new EmailRequest
                    {
                        Email = user.Email,
                        Subject = "تم رفض إجازتك الإعتيادية المطلوبة."
                    };
                    await _EmailService.SendEmail(emailrequest);
                }
                else
                {
                    NormalLeave.Holder = Holder.DirectManager;

                    // >>>>>>>>>>>>>>>>>>>>>>>>>>>  add this
                    if (NormalLeave.Direct_ManagerID == NormalLeave.General_ManagerID)
                    {
                        NormalLeave.DirectManager_Decision = true;
                        NormalLeave.Holder = Holder.GeneralManager;
                    }
                    // >>>>>>>>>>>>>>>>>>>>>>>>>>>
                    // if Head of Departement made a leave request
                    // if أمين الكلية made a leave request
                    //var userr = await _accountService.FindById(NormalLeave.UserID);
                    //var IsdeptManager = await _departmentBase.Get(d => d.ManagerId == userr.Id);
                    //bool cheackRole = await _accountService.IsInRoleAsync(userr, "أمين الكلية");
                    //bool cheackRoleHr = await _accountService.IsInRoleAsync(userr, "مدير الموارد البشرية");
                    //if (cheackRole || IsdeptManager != null || cheackRoleHr)
                    //    NormalLeave.DirectManager_Decision = true;
                }
                await _base.Update(NormalLeave);

                var leave = _mapper.Map<NormalLeaveDTO>(NormalLeave);
                var coworker = await _accountService.FindById(NormalLeave.Coworker_ID);
                var generalManager = await _accountService.FindById(NormalLeave.General_ManagerID); ;
                var directManager = await _accountService.FindById(NormalLeave.Direct_ManagerID);
                leave.GeneralManagerName = $"{generalManager.FirstName} {generalManager.SecondName} {generalManager.ThirdName} {generalManager.ForthName}";
                leave.DirectManagerName = $"{directManager.FirstName} {directManager.SecondName} {directManager.ThirdName} {directManager.ForthName}";
                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                leave.CoworkerName = $"{coworker.FirstName} {coworker.SecondName} {coworker.ThirdName} {coworker.ForthName}";

                return Ok(new
                {
                    message = "تم تحديث قرار القائم بالعمل بنجاح.",
                    Leave = leave
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء التحديث.", error = ex.Message });
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpDelete("DeleteNormalLeave/{leaveID:guid}")]
        public async Task<IActionResult> DeleteNormalLeave([FromRoute] Guid leaveID)
        {
            if (leaveID == Guid.Empty)
            {
                return BadRequest("معرّف الإجازة غير صحيح.");
            }
            try
            {
                var NormalLeave = await _base.Get(n => n.ID == leaveID);
                if (NormalLeave == null)
                {
                    return NotFound(new { message = "لم يتم العثور على إجازة إعتيادية." });
                }
                await _base.Remove(NormalLeave);

                return Ok(new
                {
                    message = "تم حذف الإجازة الإعتيادية بنجاح.",
                    leaveID = NormalLeave.ID,
                    userID = NormalLeave.UserID
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء الحذف.", error = ex.Message });
            }
        }
    }
}