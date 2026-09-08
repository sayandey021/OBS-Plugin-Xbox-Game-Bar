using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OBSGameBar.Core.Protocol;

namespace OBSGameBar.Tests
{
    [TestClass]
    public class ObsProtocolTests
    {
        [TestMethod]
        public void TestIdentifySerialization_IncludesCorrectOpCodeAndSubscriptions()
        {
            var identifyData = new ObsIdentifyData
            {
                RpcVersion = 1,
                Authentication = "auth123",
                EventSubscriptions = (uint)ObsEventSubscriptions.AllStandard
            };

            var envelope = new
            {
                op = (int)ObsOpCode.Identify,
                d = identifyData
            };

            string json = JsonSerializer.Serialize(envelope);
            using (var doc = JsonDocument.Parse(json))
            {
                Assert.AreEqual(1, doc.RootElement.GetProperty("op").GetInt32());
                var d = doc.RootElement.GetProperty("d");
                Assert.AreEqual(1, d.GetProperty("rpcVersion").GetInt32());
                Assert.AreEqual("auth123", d.GetProperty("authentication").GetString());
                Assert.AreEqual((uint)ObsEventSubscriptions.AllStandard, d.GetProperty("eventSubscriptions").GetUInt32());
            }
        }

        [TestMethod]
        public void TestRequestEnvelopeSerialization_IncludesRequestIdAndData()
        {
            var req = new ObsRequestEnvelope
            {
                Op = (int)ObsOpCode.Request,
                Payload = new ObsRequestPayload
                {
                    RequestType = "SetCurrentProgramScene",
                    RequestId = "req_100",
                    RequestData = new { sceneName = "Gaming" }
                }
            };

            string json = JsonSerializer.Serialize(req);
            using (var doc = JsonDocument.Parse(json))
            {
                Assert.AreEqual(6, doc.RootElement.GetProperty("op").GetInt32());
                var d = doc.RootElement.GetProperty("d");
                Assert.AreEqual("SetCurrentProgramScene", d.GetProperty("requestType").GetString());
                Assert.AreEqual("req_100", d.GetProperty("requestId").GetString());
                Assert.AreEqual("Gaming", d.GetProperty("requestData").GetProperty("sceneName").GetString());
            }
        }

        [TestMethod]
        public void TestRequestStatus_SuccessfulResponse_ParsedCorrectly()
        {
            string json = "{\"op\":7,\"d\":{\"requestType\":\"GetSceneList\",\"requestId\":\"req_1\",\"requestStatus\":{\"result\":true,\"code\":100,\"comment\":\"Success\"},\"responseData\":{\"currentProgramSceneName\":\"Gaming\"}}}";

            using (var doc = JsonDocument.Parse(json))
            {
                var d = doc.RootElement.GetProperty("d");
                var status = JsonSerializer.Deserialize<ObsRequestStatus>(d.GetProperty("requestStatus").GetRawText());

                Assert.IsTrue(status.Result);
                Assert.AreEqual(100, status.Code);
                Assert.AreEqual("Success", status.Comment);
                Assert.AreEqual("Gaming", d.GetProperty("responseData").GetProperty("currentProgramSceneName").GetString());
            }
        }
    }
}
