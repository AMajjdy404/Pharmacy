using Pharmacy.API.Dtos;
using Serilog.Context;
using System.Diagnostics;
using System.Text.Json;

namespace Pharmacy.API.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, IWebHostEnvironment env, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _env = env;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew(); // بدء قياس الوقت

            try
            {
                await _next(context);

                // التحقق مما إذا كانت الاستجابة قد بدأت قبل تعديل الرؤوس
                if (!context.Response.HasStarted)
                {
                    if (context.Response.StatusCode == 403)
                    {
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync("{\"Message\": \"Access Denied: You do not have the required permissions to access this resource.\"}");
                    }
                    else if (context.Response.StatusCode == 401)
                    {
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync("{\"Message\": \"Unauthorized: Please log in to access this resource.\"}");
                    }
                }
                else
                {
                    _logger.LogWarning("Cannot modify response for StatusCode {StatusCode}: Response has already started.", context.Response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                // التحقق مما إذا كانت الاستجابة قد بدأت قبل تعديل الرؤوس
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";
                    var response = _env.IsDevelopment()
                        ? new ErrorResponse { Message = "Internal Server Error", Details = ex.ToString() }
                        : new ErrorResponse { Message = "An unexpected error occurred. Please try again later.", Details = null };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                }
                else
                {
                    _logger.LogWarning("Cannot handle exception: Response has already started.");
                }

                LogError(context, ex, stopwatch.Elapsed.TotalMilliseconds);
            }
            finally
            {
                stopwatch.Stop(); // إيقاف قياس الوقت
            }
        }

        private void LogError(HttpContext context, Exception ex, double elapsedMilliseconds)
        {
            using (LogContext.PushProperty("RequestPath", context.Request.Path))
            using (LogContext.PushProperty("RequestMethod", context.Request.Method))
            using (LogContext.PushProperty("StatusCode", context.Response.StatusCode))
            using (LogContext.PushProperty("Elapsed", elapsedMilliseconds))
            using (LogContext.PushProperty("User", context.User?.Identity?.Name ?? "Anonymous"))
            using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
            using (LogContext.PushProperty("SpanId", Activity.Current?.SpanId.ToString() ?? "N/A"))
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
            }
        }
    }
}
