using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WadnereJwellors.Domain.Exceptions;
using WadnereJwellors.Models;

namespace WadnereJwellors.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var errorResponse = new ErrorResponse();

            switch (exception)
            {
                case NotFoundException notFoundEx:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.Message = notFoundEx.Message;
                    break;

                case KeyNotFoundException keyNotFoundEx:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.Message = keyNotFoundEx.Message;
                    break;

                case ArgumentException argEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Message = argEx.Message;
                    break;

                case Microsoft.Data.SqlClient.SqlException sqlEx:
                    context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                    errorResponse.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                    errorResponse.Message = $"Database Connection Error: {sqlEx.Message}";
                    errorResponse.Details = _env.IsDevelopment() ? sqlEx.StackTrace : null;
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Message = "An unexpected server error occurred.";
                    errorResponse.Details = _env.IsDevelopment() ? exception.StackTrace : null;
                    break;
            }

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonResult = JsonSerializer.Serialize(errorResponse, jsonOptions);
            return context.Response.WriteAsync(jsonResult);
        }
    }
}
