using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIS3DEngine.Application.Dtos.Common
{
    public record CommandResultDto
    {
        public bool Success { get; init; }
        public string Message { get; init; } = "";
        public object? Data { get; init; }
        public int StatusCode { get; init; } = 200;

        // Factory methods
        public static CommandResultDto Ok(string message, object? data = null) => new()
        {
            Success = true,
            Message = message,
            Data = data,
            StatusCode = 200
        };

        public static CommandResultDto NotFound(string message) => new()
        {
            Success = false,
            Message = message,
            StatusCode = 404
        };

        public static CommandResultDto BadRequest(string message) => new()
        {
            Success = false,
            Message = message,
            StatusCode = 400
        };


        public static CommandResultDto Unauthorized(string message) => new()
        {
            Success = false,
            Message = message,
            StatusCode = 401
        };
    }
}
