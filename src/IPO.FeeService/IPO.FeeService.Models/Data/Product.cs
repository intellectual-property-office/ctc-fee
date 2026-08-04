using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IPO.FeeService.Models.Data
{
    public class Product
    {
        public int Id { get; set; }

        [MaxLength(2)]
        [Required]
        public string? Channel { get; set; }

        [MaxLength(20)]
        [Required]
        public string? Code { get; set; }

        [MaxLength(128)]
        public string? Description { get; set; }

        public List<TaxCode> TaxCodes { get; } = new();

        public List<QuantityRule> QuantityRules { get; } = new();

        public List<ProductServiceRequestType> ProductServiceRequestTypes { get; } = new();

        public virtual ICollection<Fee>? Fees { get; set; }
        public int? ProductNumber { get; set; }
	}
}
