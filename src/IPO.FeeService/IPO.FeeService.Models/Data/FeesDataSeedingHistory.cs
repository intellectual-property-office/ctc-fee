using System;
using System.ComponentModel.DataAnnotations;

namespace IPO.FeeService.Models.Data
{
    public class FeesDataSeedingHistory
    {
        public int Id { get; set; }
        
        [Required]
        public int Version { get; set; }
        
        [Required]
        public DateTime CreatedOn { get; set; }
    }
}
