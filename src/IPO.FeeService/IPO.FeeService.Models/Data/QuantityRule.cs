using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IPO.FeeService.Models.Data
{
    public class QuantityRule
    {
        public int Id { get; set; }

        [MaxLength(256)]
        [Required] 
        public string? Rule { get; set; }

        public DateTime EffectiveFrom { get; set; }
        
        public DateTime? EffectiveTo { get; set; }

        public List<Product> Products { get; } = new();
    }
}
