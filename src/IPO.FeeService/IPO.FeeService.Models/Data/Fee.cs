using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPO.FeeService.Models.Data
{
    public class Fee
    {
        public int Id { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceExVat { get; set; }

        [MaxLength(256)]
        [Required]
        public string Rule { get; set; } = Constants.Settings.NoFeeRuleValue;

        public DateTime EffectiveFrom { get; set; }
        
        public DateTime? EffectiveTo { get; set; }

        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }
    }
}
