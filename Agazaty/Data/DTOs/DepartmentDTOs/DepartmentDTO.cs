namespace Agazaty.Data.DTOs.DepartmentDTOs
{
    public class DepartmentDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public DateTime CreateDate { get; set; }
        public string ManagerId { get; set; }
        public string ManagerName { get; set; }
        public bool DepartmentType { get; set; }
    }
}
