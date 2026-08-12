using System.ComponentModel.DataAnnotations;

namespace SEAL_Application.Features.Prizes.Commands.UpdatePrize
{
    public class UpdatePrizeRequestModel
    {
        [Required]
        public string PrizeName { get; set; } = string.Empty;
        
        [Required]
        public string Value { get; set; } = string.Empty;
        
        public int Quantity { get; set; }
    }
}
