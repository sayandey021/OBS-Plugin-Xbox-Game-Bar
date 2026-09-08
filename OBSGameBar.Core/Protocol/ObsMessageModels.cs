using System.Text.Json;
using System.Text.Json.Serialization;

namespace OBSGameBar.Core.Protocol
{
    public class ObsRawMessage
    {
        [JsonPropertyName("op")]
        public int Op { get; set; }

        [JsonPropertyName("d")]
        public JsonElement Data { get; set; }
    }

    public class ObsHelloData
    {
        [JsonPropertyName("obsWebSocketVersion")]
        public string ObsWebSocketVersion { get; set; }

        [JsonPropertyName("rpcVersion")]
        public int RpcVersion { get; set; }

        [JsonPropertyName("authentication")]
        public ObsAuthChallenge Authentication { get; set; }
    }

    public class ObsAuthChallenge
    {
        [JsonPropertyName("challenge")]
        public string Challenge { get; set; }

        [JsonPropertyName("salt")]
        public string Salt { get; set; }
    }

    public class ObsIdentifyData
    {
        [JsonPropertyName("rpcVersion")]
        public int RpcVersion { get; set; } = 1;

        [JsonPropertyName("authentication")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Authentication { get; set; }

        [JsonPropertyName("eventSubscriptions")]
        public uint EventSubscriptions { get; set; }
    }

    public class ObsRequestEnvelope
    {
        [JsonPropertyName("op")]
        public int Op { get; set; } = (int)ObsOpCode.Request;

        [JsonPropertyName("d")]
        public ObsRequestPayload Payload { get; set; }
    }

    public class ObsRequestPayload
    {
        [JsonPropertyName("requestType")]
        public string RequestType { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("requestData")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object RequestData { get; set; }
    }

    public class ObsRequestStatus
    {
        [JsonPropertyName("result")]
        public bool Result { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("comment")]
        public string Comment { get; set; }
    }

    public class ObsEventData
    {
        [JsonPropertyName("eventType")]
        public string EventType { get; set; }

        [JsonPropertyName("eventIntent")]
        public uint EventIntent { get; set; }

        [JsonPropertyName("eventData")]
        public JsonElement EventPayload { get; set; }
    }
}
