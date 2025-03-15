﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Agazaty.Data.DTOs.SickLeaveDTOs
{
    public class SickLeaveDTO
    {
        public int Id { get; set; }
        public string Disease { get; set; }
        public DateTime RequestDate { get; set; }
        public string? MedicalCommitteAddress { get; set; }
        public bool RespononseDone { get; set; }
        public int Year { get; set; }
        public string? Street { get; set; }
        public string? governorate { get; set; }
        public string? State { get; set; }
        public int? Days { get; set; }
        public string UserName { get; set; }
        public string UserID { get; set; }
    }
}
