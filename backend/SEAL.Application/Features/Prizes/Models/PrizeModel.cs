using System;

namespace SEAL_Application.Features.Prizes.Models
{
    public class PrizeModel
    {
        public string Id { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string? TrackId { get; set; }
        public string PrizeName { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
