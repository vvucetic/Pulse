using Pulse.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pulse.Core.Storage;

namespace Pulse.Core.Common
{
    public class JobConverter : Newtonsoft.Json.Converters.CustomCreationConverter<Job>
    {
        public override Job Create(Type objectType)
        {
            throw new NotImplementedException();
        }


        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Load JObject from stream
            JObject jObject = JObject.Load(reader);

            // Create target object based on JObject
            var target = new InvocationData(
                type: (string)jObject.Property("Type"),
                method: (string)jObject.Property("Method"),
                arguments: (string)jObject.Property("Arguments"),
                parameterTypes: (string)jObject.Property("ParameterTypes"));

            // Populate the object properties
            serializer.Populate(jObject.CreateReader(), target);

            return target;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is Job job)
            {                
                base.WriteJson(writer, InvocationData.Serialize(job), serializer);
            }
            else
            {
                base.WriteJson(writer, value, serializer);
            }
        }
    }
}
