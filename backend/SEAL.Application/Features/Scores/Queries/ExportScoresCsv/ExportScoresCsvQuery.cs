using MediatR;
using SEAL_Domain.Base;

namespace SEAL_Application.Features.Scores.Queries.ExportScoresCsv
{
    public class ExportScoresCsvQuery : IRequest<Result<byte[]>>
    {
        public string EventId { get; set; } = string.Empty;
        public bool Anonymize { get; set; } = true;
    }
}
