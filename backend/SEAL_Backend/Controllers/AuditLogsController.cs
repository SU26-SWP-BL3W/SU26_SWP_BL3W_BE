using MediatR;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Commons;
using SEAL_Application.Features.AuditLogs.Queries.GetAuditLogs;
using SEAL_Application.Features.AuditLogs.Queries.GetAuditLogs.Models;
using SEAL_Backend.Filters;
using SEAL_Backend.Helpers;
using SEAL_Domain.Base;
using SEAL_Domain.Entity.Enums;
using System.Threading.Tasks;

namespace SEAL_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Http.Tags("6. System & Utility")]
    public class AuditLogsController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public AuditLogsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Nhật ký thao tác nhạy cảm trong sự kiện (loại đội, tính/công bố/hủy kết quả, chốt điểm).
        /// </summary>
        [HttpGet]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<PagedResult<AuditLogItemModel>>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Get([FromQuery] GetAuditLogsQuery query)
        {
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }
    }
}
