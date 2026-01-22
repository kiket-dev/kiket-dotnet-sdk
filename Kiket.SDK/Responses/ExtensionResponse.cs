namespace Kiket.SDK.Responses;

/// <summary>
/// Standard response format for extension handlers.
///
/// <para>Use the static factory methods to build properly formatted responses:</para>
/// <list type="bullet">
///   <item><see cref="Allow()"/> - Build an allow response</item>
///   <item><see cref="Deny(string)"/> - Build a deny response</item>
///   <item><see cref="Pending(string)"/> - Build a pending response</item>
/// </list>
///
/// <para>Example usage:</para>
/// <code>
/// // Simple allow
/// return ExtensionResponse.Allow()
///     .WithMessage("Successfully configured")
///     .Build();
///
/// // Allow with output fields
/// return ExtensionResponse.Allow()
///     .WithMessage("Successfully configured Mailjet")
///     .WithData("routeId", 123)
///     .WithOutputField("inbound_email", "abc@parse.example.com")
///     .Build();
///
/// // Deny with error details
/// return ExtensionResponse.Deny("Invalid credentials")
///     .WithData("errorCode", "AUTH_FAILED")
///     .Build();
/// </code>
/// </summary>
public class ExtensionResponse
{
    public string Status { get; }
    public string? Message { get; }
    public IReadOnlyDictionary<string, object?> Metadata { get; }

    private ExtensionResponse(string status, string? message, Dictionary<string, object?> metadata)
    {
        Status = status;
        Message = message;
        Metadata = metadata.AsReadOnly();
    }

    /// <summary>
    /// Convert to a Dictionary for JSON serialization.
    /// </summary>
    public Dictionary<string, object?> ToDictionary()
    {
        var result = new Dictionary<string, object?>
        {
            ["status"] = Status,
            ["metadata"] = Metadata
        };

        if (Message != null)
        {
            result["message"] = Message;
        }

        return result;
    }

    /// <summary>
    /// Start building an allow response.
    /// </summary>
    public static AllowBuilder Allow() => new();

    /// <summary>
    /// Start building a deny response.
    /// </summary>
    /// <param name="message">Reason for denial (required)</param>
    public static ResponseBuilder Deny(string message)
    {
        if (string.IsNullOrEmpty(message))
            throw new ArgumentException("Deny response requires a message", nameof(message));
        return new ResponseBuilder("deny", message);
    }

    /// <summary>
    /// Start building a pending response.
    /// </summary>
    /// <param name="message">Status message (required)</param>
    public static ResponseBuilder Pending(string message)
    {
        if (string.IsNullOrEmpty(message))
            throw new ArgumentException("Pending response requires a message", nameof(message));
        return new ResponseBuilder("pending", message);
    }

    /// <summary>
    /// Builder for allow responses with output fields support.
    /// </summary>
    public class AllowBuilder
    {
        private string? _message;
        private readonly Dictionary<string, object?> _data = new();
        private readonly Dictionary<string, string> _outputFields = new();

        internal AllowBuilder() { }

        /// <summary>
        /// Set an optional success message.
        /// </summary>
        public AllowBuilder WithMessage(string message)
        {
            _message = message;
            return this;
        }

        /// <summary>
        /// Add data to the response metadata.
        /// </summary>
        public AllowBuilder WithData(string key, object? value)
        {
            _data[key] = value;
            return this;
        }

        /// <summary>
        /// Add multiple data entries to the response metadata.
        /// </summary>
        public AllowBuilder WithData(IDictionary<string, object?> data)
        {
            if (data != null)
            {
                foreach (var kv in data)
                {
                    _data[kv.Key] = kv.Value;
                }
            }
            return this;
        }

        /// <summary>
        /// Add an output field to be displayed in the configuration UI.
        /// </summary>
        /// <param name="key">Field key (must match manifest output_fields schema)</param>
        /// <param name="value">Field value</param>
        public AllowBuilder WithOutputField(string key, string value)
        {
            _outputFields[key] = value;
            return this;
        }

        /// <summary>
        /// Add multiple output fields to be displayed in the configuration UI.
        /// </summary>
        public AllowBuilder WithOutputFields(IDictionary<string, string> fields)
        {
            if (fields != null)
            {
                foreach (var kv in fields)
                {
                    _outputFields[kv.Key] = kv.Value;
                }
            }
            return this;
        }

        /// <summary>
        /// Build the response.
        /// </summary>
        public ExtensionResponse Build()
        {
            var metadata = new Dictionary<string, object?>(_data);
            if (_outputFields.Count > 0)
            {
                metadata["output_fields"] = new Dictionary<string, string>(_outputFields);
            }
            return new ExtensionResponse("allow", _message, metadata);
        }
    }

    /// <summary>
    /// Builder for deny and pending responses.
    /// </summary>
    public class ResponseBuilder
    {
        private readonly string _status;
        private readonly string _message;
        private readonly Dictionary<string, object?> _data = new();

        internal ResponseBuilder(string status, string message)
        {
            _status = status;
            _message = message;
        }

        /// <summary>
        /// Add data to the response metadata.
        /// </summary>
        public ResponseBuilder WithData(string key, object? value)
        {
            _data[key] = value;
            return this;
        }

        /// <summary>
        /// Add multiple data entries to the response metadata.
        /// </summary>
        public ResponseBuilder WithData(IDictionary<string, object?> data)
        {
            if (data != null)
            {
                foreach (var kv in data)
                {
                    _data[kv.Key] = kv.Value;
                }
            }
            return this;
        }

        /// <summary>
        /// Build the response.
        /// </summary>
        public ExtensionResponse Build()
        {
            return new ExtensionResponse(_status, _message, _data);
        }
    }
}
