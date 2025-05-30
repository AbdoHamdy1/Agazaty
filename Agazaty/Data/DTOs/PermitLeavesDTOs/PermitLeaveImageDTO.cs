using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Agazaty.Data.DTOs.PermitLeavesDTOs
{
    public class PermitLeaveImageDTO
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; }
        public Guid LeaveId { get; set; }
    }
}
