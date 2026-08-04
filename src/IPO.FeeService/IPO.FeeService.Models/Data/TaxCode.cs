using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPO.FeeService.Models.Data
{
    public class TaxCode
    {
        public int Id { get; set; }

        [MaxLength(10)]
        [Required]
        public string? Code { get; set; }

        [MaxLength(60)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Rate { get; set; }

        public DateTime EffectiveFrom { get; set; }
        
        public DateTime? EffectiveTo { get; set; }

        public List<Product> Products { get; } = new();
    }
}
