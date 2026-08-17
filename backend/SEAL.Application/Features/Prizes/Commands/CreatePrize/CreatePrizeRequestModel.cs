using System.ComponentModel.DataAnnotations;

namespace SEAL_Application.Features.Prizes.Commands.CreatePrize
{
    public class CreatePrizeRequestModel
    {
        /// <summary>Hạng mục áp dụng. Để trống = giải chung toàn sự kiện.</summary>
        public string? TrackId { get; set; }

        [Required]
        public string PrizeName { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;
    }
}
