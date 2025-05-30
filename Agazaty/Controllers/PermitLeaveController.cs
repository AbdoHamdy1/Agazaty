using Agazaty.Data.Base;
using Agazaty.Data.DTOs.DepartmentDTOs;
using Agazaty.Data.DTOs.PermitLeavesDTOs;
using Agazaty.Data.Services.Interfaces;
using Agazaty.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Net.Http;

namespace Agazaty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PermitLeaveController : ControllerBase
    {
        private readonly IEntityBaseRepository<PermitLeave> _Permitbase;
        private readonly IEntityBaseRepository<PermitLeaveImage> _PermitImagebase;
        private readonly IAccountService _accountService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IMapper _mapper;

        public PermitLeaveController(IAccountService accountService, IMapper mapper, IEntityBaseRepository<PermitLeave> Ebase, IEntityBaseRepository<PermitLeaveImage> PermitImagebase, IWebHostEnvironment webHostEnvironment)
        {
            _mapper = mapper;
            _Permitbase = Ebase;
            _PermitImagebase = PermitImagebase;
            _webHostEnvironment = webHostEnvironment;
            _accountService = accountService;
        }
        [Authorize]
        [HttpGet("GetPermitLeaveImageByleaveId/{leaveID:guid}", Name = "GetPermitLeaveImageByleaveId")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PermitLeaveDTO>> GetPermitLeaveImageByleaveId(Guid leaveID)
        {
            if (leaveID == Guid.Empty)
                return BadRequest(new { message = "معرف التصريح غير صالح." });
            try
            {
                var permitLeaveImage = await _PermitImagebase.Get(c => c.LeaveId == leaveID);
                if (permitLeaveImage == null)
                {
                    return NotFound(new { Message = "لا توجد صورة للتصريح." });
                }
                return Ok(_mapper.Map<PermitLeaveImageDTO>(permitLeaveImage));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpGet("GetPermitLeaveById/{leaveID:guid}", Name = "GetPermitLeave")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PermitLeaveDTO>> GetPermitLeaveById(Guid leaveID)
        {
            if (leaveID == Guid.Empty)
                return BadRequest(new { message = "معرف التصريح غير صالح." });
            try
            {
                var permitLeave = await _Permitbase.Get(c => c.Id == leaveID);
                if (permitLeave == null)
                {
                    return NotFound(new { Message = "لم يتم العثور على تصريح." });
                }
                var leave = _mapper.Map<PermitLeaveDTO>(permitLeave);
                var user = await _accountService.FindById(permitLeave.UserId);
                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                leave.FirstName= user.FirstName;
                leave.SecondName= user.SecondName;
                return Ok(leave);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpGet("GetAllPermitLeaves", Name = "GetAllPermitLeaves")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<PermitLeaveDTO>>> GetAllPermitLeaves()
        {
            try
            {
                var permitLeaves = await _Permitbase.GetAll();
                if (!permitLeaves.Any())
                {
                    return NotFound(new { Message = "لا توجد تصاريح." });
                }

                var leaves = _mapper.Map<IEnumerable<PermitLeaveDTO>>(permitLeaves);
                foreach(var leave in leaves)
                {
                    var user = await _accountService.FindById(leave.UserID);
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
        [HttpGet("GetAllPermitLeavesByUserID/{userID}", Name = "GetAllPermitLeavesByUserID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<PermitLeaveDTO>>> GetAllPermitLeavesByUserID(string userID)
        {
            if (string.IsNullOrWhiteSpace(userID))
                return BadRequest(new { message = "معرّف المستخدم غير صالح." });
            try
            {
                var permitLeaves = await _Permitbase.GetAll(p => p.UserId == userID);
                if (!permitLeaves.Any())
                {
                    return NotFound(new { Message = "لا توجد تصاريح." });
                }

                var leaves = _mapper.Map<IEnumerable<PermitLeaveDTO>>(permitLeaves);
                foreach (var leave in leaves)
                {
                    var user = await _accountService.FindById(leave.UserID);
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                }
                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpGet("GetAllPermitLeavesByUserIDAndMonth/{userID}/{month:int}", Name = "GetAllPermitLeavesByUserIDAndMonth")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<PermitLeaveDTO>>> GetAllPermitLeavesByUserIDAndMonth(string userID, int month)
        {
            if (string.IsNullOrWhiteSpace(userID) || month < 1 || month > 12)
                return BadRequest(new { message = "معرف المستخدم أو الشهر غير صحيح." });
            try
            {
                var permitLeaves = await _Permitbase.GetAll(p => p.UserId == userID &&
                                   p.Date.Month == month);
                if (!permitLeaves.Any())
                {
                    return NotFound(new { Message = "لا توجد تصاريح." });
                }

                var leaves = _mapper.Map<IEnumerable<PermitLeaveDTO>>(permitLeaves);
                foreach (var leave in leaves)
                {
                    var user = await _accountService.FindById(leave.UserID);
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                }
                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpGet("GetAllPermitLeavesByUserIDAndYear/{userID}/{year:int}", Name = "GetAllPermitLeavesByUserIDAndYear")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<PermitLeaveDTO>>> GetAllPermitLeavesByUserIDAndYear(string userID, int year)
        {
            if (string.IsNullOrWhiteSpace(userID) || year < 1900)
                return BadRequest(new { message = "معرف المستخدم أو السنة غير صحيحة." });
            try
            {
                var permitLeaves = await _Permitbase.GetAll(p => p.UserId == userID &&
                                   p.Date.Year == year);
                if (!permitLeaves.Any())
                {
                    return NotFound(new { Message = "لا توجد تصاريح." });
                }

                var leaves = _mapper.Map<IEnumerable<PermitLeaveDTO>>(permitLeaves);
                foreach (var leave in leaves)
                {
                    var user = await _accountService.FindById(leave.UserID);
                    leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                }
                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [Consumes("multipart/form-data")]
        [HttpPost("CreatePermitLeave", Name = "CreatePermitLeave")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PermitLeave>> CreatePermitLeave([FromForm] CreatePermitLeaveDTO model,IFormFile? file)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var PermitleavesOfUsers = await _Permitbase.GetAll(p=>p.UserId==model.UserId);
                if (PermitleavesOfUsers.Any(p=>p.Date.Date==model.Date.Date))
                {
                    return BadRequest(new { Message = "الحد الاقصي تصريح واحد يوميا" });
                }
                if (model.Hours <= 0) return BadRequest(new { Message = "عدد الساعات يجب أن يكون أكثر من 0." });
                if (model.Hours > 3) return BadRequest(new { Message = "عدد الساعات لا يمكن أن تكون أكثر من ثلاث ساعات." });

                if (await _accountService.FindById(model.UserId) is null) return BadRequest(new { Message = "المستخدم غير موجود." });

                var permitLeave = _mapper.Map<PermitLeave>(model);

                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (file != null)
                {          
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string permitLeavePath = @"images\PermitLeaves\PermitLeaveUser-" + permitLeave.UserId;
                        string finalPath = Path.Combine(wwwRootPath, permitLeavePath);

                        if (!Directory.Exists(finalPath))
                        {
                            Directory.CreateDirectory(finalPath);
                        }
                        using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                        {
                            file.CopyTo(fileStream);
                        }
                        PermitLeaveImage permitLeaveImage = new PermitLeaveImage()
                        {
                            ImageUrl = @"\" + permitLeavePath + @"\" + fileName,
                            LeaveId = permitLeave.Id
                        };


                        if (permitLeave.PermitLeaveImage == null)
                        {
                            permitLeave.PermitLeaveImage = new PermitLeaveImage();
                        }
                        permitLeave.PermitLeaveImage=permitLeaveImage;
                    
                }
                await _Permitbase.Add(permitLeave);

                var leave = _mapper.Map<PermitLeaveDTO>(permitLeave);
                var user = await _accountService.FindById(permitLeave.UserId);
                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                return CreatedAtAction(nameof(GetPermitLeaveById), new { leaveID = permitLeave.Id }, leave);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [Consumes("multipart/form-data")]
        [HttpPut("UpdatePermitLeave/{leaveID:guid}", Name = "UpdatePermitLeave")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePermitLeave(Guid leaveID, [FromForm] UpdatePermitLeaveDTO model, IFormFile? file) 
        {
            if (leaveID == Guid.Empty)
            {
                return BadRequest(new { Message = "معرف التصريح غير صالح." });
            }
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                if (model.Hours <= 0) return BadRequest(new { Message = "عدد الساعات يجب أن يكون أكثر من 0." });
                if (await _accountService.FindById(model.UserId) is null) return BadRequest(new { Message = "المستخدم غير موجود." });


                var permitLeave = await _Permitbase.Get(c => c.Id == leaveID);
                if (permitLeave == null)
                {
                    return NotFound(new { Message = "لا توجد تصاريح." });
                }

                _mapper.Map(model, permitLeave);

                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (file != null)
                {
                    
                    
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string permitLeavePath = @"images\PermitLeaves\PermitLeaveUser-" + permitLeave.UserId;
                        string finalPath = Path.Combine(wwwRootPath, permitLeavePath);

                        if (!Directory.Exists(finalPath))
                        {
                            Directory.CreateDirectory(finalPath);
                        }
                        using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                        {
                            file.CopyTo(fileStream);
                        }

                        PermitLeaveImage permitLeaveImage = new PermitLeaveImage()
                        {
                            ImageUrl = @"\" + permitLeavePath + @"\" + fileName,
                            LeaveId = permitLeave.Id
                        };


                        if (permitLeave.PermitLeaveImage == null)
                        {
                            permitLeave.PermitLeaveImage = new PermitLeaveImage();
                        }
                        permitLeave.PermitLeaveImage=permitLeaveImage;
                    
                }
                await _Permitbase.Update(permitLeave);

                var leave = _mapper.Map<PermitLeaveDTO>(permitLeave);
                var user = await _accountService.FindById(permitLeave.UserId);
                leave.UserName = $"{user.FirstName} {user.SecondName} {user.ThirdName} {user.ForthName}";
                return Ok(new { Message = "تم التحديث بنجاح.", PermitLeave = leave });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpDelete("DeletePermitLeave/{leaveID:guid}", Name = "DeletePermitLeave")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePermitLeave(Guid leaveID)
        {
            if (leaveID == Guid.Empty)
            {
                return BadRequest(new { Message = "معرف التصريح غير صحيح." });
            }
            try
            {
                var permitLeave = await _Permitbase.Get(c => c.Id == leaveID);
                if (permitLeave == null)
                {
                    return NotFound(new { Message = "لا يوجد تصريح." });
                }
                string permitLeavePath = @"images\Permitleaves\PermitLeaveUser-" + permitLeave.UserId;
                string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, permitLeavePath);

                if (Directory.Exists(finalPath))
                {
                    string[] filePaths = Directory.GetFiles(finalPath);
                    foreach (string filePath in filePaths)
                    {
                        System.IO.File.Delete(filePath);
                    }
                    Directory.Delete(finalPath);
                }
                await _Permitbase.Remove(permitLeave);

                return Ok(new { Message = "تم الحذف بنجاح." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
        [Authorize(Roles = "مدير الموارد البشرية")]
        [HttpDelete("DeleteImage/{imageId:guid}", Name = "DeleteImage")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteImage(Guid imageId)
        {
            if (imageId == Guid.Empty)
                return BadRequest(new { Message = "معرف الصورة غير صالح." });
            try
            {
                var imageToBeDeleted = await _PermitImagebase.Get(pi => pi.Id == imageId);
                if (imageToBeDeleted != null)
                {
                    if (!string.IsNullOrEmpty(imageToBeDeleted.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageToBeDeleted.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }
                    await _PermitImagebase.Remove(imageToBeDeleted);

                    return Ok(new { Message = "تم الحذف بنجاح." });
                }
                return NotFound(new { Message = "لا توجد صورة." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة طلبك.", error = ex.Message });
            }
        }
    }
}