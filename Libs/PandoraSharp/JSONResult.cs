namespace PandoraSharp
{
    public struct JsonFault
    {
        public Util.ErrorCodes Error;
        public string FaultString;
    }

    public class JsonResult : Newtonsoft.Json.Linq.JObject
    {
        public JsonResult(string data)
        {
            Newtonsoft.Json.Linq.JObject r =
                (Newtonsoft.Json.Linq.JObject) Newtonsoft.Json.JsonConvert.DeserializeObject(data);
            foreach (System.Collections.Generic.KeyValuePair<string, Newtonsoft.Json.Linq.JToken> kvp in r)
            {
                Add(kvp.Key, kvp.Value);
            }
        }

        private const string UPDATE_REQUIRED = "Your client requires an update to continue listening to Pandora.";

        public Newtonsoft.Json.Linq.JToken Result => this["result"];

        public bool Fault
        {
            get
            {
                Newtonsoft.Json.Linq.JToken v;
                if (!TryGetValue("stat", out v)) return false;
                return v.ToString() == "fail";
            }
        }

        public Util.ErrorCodes FaultCode
        {
            get
            {
                if (!Fault)
                    return Util.ErrorCodes.Success;
                int c = this["code"].ToObject<int>();

                try
                {
                    return (Util.ErrorCodes) c;
                }
                catch
                {
                    Util.Log.O("Unknown error code: " + c);
                    return Util.ErrorCodes.UnknownError;
                }
            }
        }

        public string FaultString
        {
            get
            {
                if (!Fault) return "Operation completed successfully.";

                int c = this["code"].ToObject<int>();

                string err = "[ERROR CODE " + c + "]";

                try
                {
                    err = System.Enum.GetName(typeof (Util.Errors), c);
                }
                catch
                {
                    //todo
                }

                return err + " - " + Util.Errors.GetErrorMessage(FaultCode, this["message"].ToString());
            }
        }

        public JsonFault FaultObject => new JsonFault {Error = FaultCode, FaultString = FaultString};
    }
}