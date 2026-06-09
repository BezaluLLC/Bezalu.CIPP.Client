using Microsoft.Kiota.Abstractions;

namespace Bezalu.CIPP.Client
{
    /// <summary>
    /// Represents an error returned by the CIPP API.
    /// </summary>
    /// <remarks>
    /// Extends the Kiota <see cref="ApiException"/> so it integrates with the request pipeline's
    /// error mapping while exposing CIPP-specific diagnostic context (HTTP status, server-provided
    /// error code, and the correlation/request id) for actionable logging and support.
    /// </remarks>
    public class CippApiException : ApiException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CippApiException"/> class.
        /// </summary>
        public CippApiException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CippApiException"/> class with a message.
        /// </summary>
        /// <param name="message">A description of the failure.</param>
        public CippApiException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CippApiException"/> class with a message and inner exception.
        /// </summary>
        /// <param name="message">A description of the failure.</param>
        /// <param name="innerException">The exception that caused this failure.</param>
        public CippApiException(string message, Exception? innerException) : base(message, innerException)
        {
        }

        /// <summary>The HTTP status code returned by the CIPP API.</summary>
        public int StatusCode { get; init; }

        /// <summary>The server-provided error code or message, when present in the response body.</summary>
        public string? ErrorCode { get; init; }

        /// <summary>The correlation/request id used to trace the request in CIPP logs.</summary>
        public string? RequestId { get; init; }
    }
}
