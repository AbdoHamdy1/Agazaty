﻿using Agazaty.Data.Base;
using Agazaty.Data.DTOs.CasualLeaveDTOs;
using Agazaty.Data.DTOs.RoleDTOs;
using Agazaty.Data.DTOs.SickLeaveDTOs;
using Agazaty.Data.Services;
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
        //[Authorize]
        [HttpGet("GetSickLeaveById/{leaveID:int}")]
        public async Task<IActionResult> GetSickLeaveById(int leaveID)
        {
            if (leaveID <= 0)
                return BadRequest(new { message = "Invalid leave ID." });
            try
            {

                var sikLeave = await _base.Get(s => s.Id == leaveID);
                if (sikLeave == null)
                {
                    return NotFound(new { message = $"No sick leave found for this leave ID {leaveID}." });
                }

                var leave = _mapper.Map<SickLeaveDTO>(sikLeave);
                var user = await _accoutnService.FindById(leave.UserID);
                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                return Ok(leave);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request.", error = ex.Message });
            }
        }
        //[Authorize]
        [HttpGet("GetAllSickLeavesByUserID/{userID}")]
        public async Task<IActionResult> GetAllSickLeavesByUserID(string userID)
        {
            if (string.IsNullOrWhiteSpace(userID))
                return BadRequest(new { message = "Invalid user ID." });
            try
            {

                var sickleaves = await _base.GetAll(s => s.UserID == userID);
                if (!sickleaves.Any())
                {
                    return NotFound(new { message = $"No sick leaves found for this User ID {userID}." });
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
                return StatusCode(500, new { message = "An error occurred while processing your request.", error = ex.Message });
            }
        }
        //[Authorize(Roles = "عميد الكلية,أمين الكلية,مدير الموارد البشرية")]
        [HttpGet("GetAllSickLeave")]
        public async Task<IActionResult> GetAllSickLeave()
        {
            try
            {
                var sickLeaves = await _base.GetAll();
                if (!sickLeaves.Any()) return NotFound(new {Message = "no sick leaves found."});

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
                return StatusCode(500, new { message = "An error occurred while processing your request.", error = ex.Message });
            }
        }
        //[Authorize]
        [HttpGet("GetAllSickLeavesByUserIDAndYear/{userID}/{year:int}")]
        public async Task<IActionResult> GetAllSickLeavesByUserIDAndYear(string userID, int year)
        {
            if (string.IsNullOrWhiteSpace(userID) || year < 1900)
            {
                return BadRequest("Invalid user ID or year.");
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

                return NotFound("No sick leaves found for the given user ID and year.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request.", error = ex.Message });
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
        //[Authorize(Roles = "مدير الموارد البشرية")]
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

                return NotFound("No waiting sick leaves found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request.", error = ex.Message });
            }
        }
        //[Authorize(Roles = "مدير الموارد البشرية")]
        [HttpGet("GetAllWaitingSickLeaves")]
        public async Task<IActionResult> GetAllWaitingSickLeaves()
        {
            try
            {
                var waitingSickLeaves = await _base.GetAll(s => s.RespononseDoneForMedicalCommitte == false);

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

                return NotFound("No waiting sick leaves found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request.", error = ex.Message });
            }
        }
        //[Authorize]
        [HttpPost("CreateSickLeave")]
        public async Task<IActionResult> CreateSickLeave([FromBody]CreateSickLeaveDTO model)
        {
            try
            {
                if (model == null)
                {
                    return NotFound("Invalid sick leave data.");
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
                    return BadRequest(new { message = "You already have a pending leave request that has not been processed yet." });
                }
                SickLeave sickLeave = _mapper.Map<SickLeave>(model);
                sickLeave.RequestDate = DateTime.UtcNow.Date;
                sickLeave.Year = sickLeave.RequestDate.Year;

                await _base.Add(sickLeave);

                var leave = _mapper.Map<SickLeaveDTO>(sickLeave);
                var user = await _accoutnService.FindById(leave.UserID);
                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                return CreatedAtAction(nameof(GetSickLeaveById), new { leaveID = sickLeave.Id }, leave);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request.", error = ex.Message });
            }
        }
        //[Authorize(Roles = "مدير الموارد البشرية")]
        [HttpPut("UpdateMedicalCommiteAddressResponse/{leaveID:int}/{address}")]
        public async Task<IActionResult> UpdateMedicalCommiteAddressResponse(int leaveID, string address)
        {
            if (leaveID <= 0 || string.IsNullOrWhiteSpace(address))
            {
                return BadRequest("Invalid leave ID or address.");
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var sickLeave = await _base.Get(s => s.Id == leaveID);

                if (sickLeave == null)
                {
                    return NotFound("Sick leave request not found.");
                }

                // Update fields
                sickLeave.MedicalCommitteAddress = address;
                sickLeave.RespononseDoneForMedicalCommitte= true;

                var leave = _mapper.Map<SickLeaveDTO>(sickLeave);
                var user = await _accoutnService.FindById(leave.UserID);
                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                await _base.Update(sickLeave);

                return Ok(new { Message = "Medical committee address updated and response marked as done.", Leave = leave });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request.", error = ex.Message });
            }
        }
        //Authorize(Roles = "مدير الموارد البشرية")]
        [HttpPut("UpdateSickLeave/{leaveID}")]
        public async Task<IActionResult> UpdateSickLeave(int leaveID, [FromBody]UpdateSickLeaveDTO model)
        {
            if (leaveID <= 0)
            {
                return BadRequest("Invalid leave ID.");
            }
            try
            {
                if (model == null)
                {
                    return BadRequest("Invalid sick leave data.");
                }
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var sickleave = await _base.Get(s => s.Id == leaveID && s.RespononseDoneForMedicalCommitte == true && s.ResponseDoneFinal == false);
                if (model.Certified == false)
                {
                    sickleave.ResponseDoneFinal = true;
                    await _base.Update(sickleave);
                    return Ok(new { Message = "Update is succeeded.", Leave = sickleave });
                }
                else
                {
                    sickleave.ResponseDoneFinal = true;
                    //var userleave =await _base.Get(l=>l.Id==leaveID);
                    var userid = sickleave.UserID;
                    if (await _leaveValidationService.IsSameLeaveOverlapping(userid, model.StartDate, model.EndDate, "SickLeave"))
                    {
                        return BadRequest("You already have a Sick leave in this period.");
                    }
                    if (await _leaveValidationService.IsLeaveOverlapping(userid, model.StartDate, model.EndDate, "SickLeave"))
                    {
                        return BadRequest("You already have a different type of leave in this period.");
                    }
                    if (sickleave == null)
                    {
                        return NotFound("SickLeave not found.");
                    }
                    var errors = new List<string>();
                    DateTime today = DateTime.Today;
                    if (model.EndDate < today)
                        errors.Add("The leave period has already passed. Please select future dates.");

                    if (model.StartDate < today)
                        errors.Add("The start date cannot be in the past. Please select today or a future date.");

                    if (model.StartDate > model.EndDate)
                        errors.Add("Start date cannot be after the end date.");

                    if (DateTime.UtcNow.Date > model.StartDate)
                        errors.Add("Request date cannot be after the start date.");

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
                    return Ok(new { Message = "Update is succeeded.", Leave = leave });
                }
            
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request.", error = ex.Message });
            }
        }
        //[Authorize(Roles = "مدير الموارد البشرية")]
        [HttpDelete("DeleteSickLeave/{leaveID}")]
        public async Task<IActionResult> DeleteSickLeave(int leaveID)
        {
            if (leaveID <= 0)
                return BadRequest(new { message = "Invalid leave ID." });
            try
            {
                var sickleave = await _base.Get(s => s.Id == leaveID);

                if (sickleave == null)
                {
                    return NotFound("Sick leave not found.");
                }

                await _base.Remove(sickleave);
                return Ok($"Sick leave has been deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request.", error = ex.Message });
            }
        }
    }
}