using System.ComponentModel.DataAnnotations;

namespace IPO.FeeService.Models.API
{
    public interface ICalculateFeeRequest 
    {
        [Required]
        public string CustomerChannel { get; set; }
    }
}
