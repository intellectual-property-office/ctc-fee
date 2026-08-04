using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace IPO.FeeService.Models.Data
{
	public class ProductServiceRequestType
	{
		[Key, Column(Order = 0)]
		public int ProductsId { get; set; }

		[Key, Column(Order = 1)]
		public int ServiceRequestTypesId { get; set; }

		[Range(1, int.MaxValue)]
		[Required]
		[DefaultValue(1)]
		public int ProductSequence { get; set; } = 1;

		// Navigation properties
		public virtual Product? Product { get; set; }
		public virtual ServiceRequestType? ServiceRequestType { get; set; }
	}
}
