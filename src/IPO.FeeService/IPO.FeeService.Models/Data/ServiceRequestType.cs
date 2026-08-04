using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IPO.FeeService.Models.Data
{
    public class ServiceRequestType
    {
        public int Id { get; set; }

        [MaxLength(128)]
        [Required]
        public string? Name { get; set; }

        [MaxLength(8)]
        [Required]
        public string? E5AccountNumber { get; set; }

		public List<ProductServiceRequestType> ProductServiceRequestTypes { get; } = new();
	}
}
