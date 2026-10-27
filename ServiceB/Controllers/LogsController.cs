using Microsoft.AspNetCore.Mvc;

namespace ServiceB.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly ILogger<LogsController> _logger;

        public LogsController(ILogger<LogsController> logger)
        {
            _logger = logger;
        }

        [HttpGet("information")]
        public IActionResult WriteInformationLog()
        {
            string requestId = HttpContext.TraceIdentifier;

            _logger.LogInformation(
                "Service B bilgi logu oluşturdu. RequestId: {RequestId}",
                requestId);

            return Ok(new
            {
                Message = "Service B Information logu oluşturuldu.",
                RequestId = requestId
            });
        }

        [HttpGet("warning")]
        public IActionResult WriteWarningLog()
        {
            string requestId = HttpContext.TraceIdentifier;

            _logger.LogWarning(
                "Service B uyarı logu oluşturdu. RequestId: {RequestId}",
                requestId);

            return Ok(new
            {
                Message = "Service B Warning logu oluşturuldu.",
                RequestId = requestId
            });
        }

        [HttpGet("error")]
        public IActionResult WriteErrorLog()
        {
            string requestId = HttpContext.TraceIdentifier;

            try
            {
                throw new InvalidOperationException(
                    "Service B için örnek hata oluşturuldu.");
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Service B işlemi başarısız oldu. RequestId: {RequestId}",
                    requestId);

                return StatusCode(500, new
                {
                    Message = "Service B için örnek bir hata oluşturuldu.",
                    RequestId = requestId
                });
            }
        }
    }
}