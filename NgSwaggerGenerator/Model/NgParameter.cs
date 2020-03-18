using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSwag;

namespace NgSwaggerGenerator.Model
{
    public class NgParameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public bool IsOption { get; set; }
        public object DefaultValue { get; set; }
        public OpenApiParameterKind Kind { get; set; }
        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.Append(Name);

            if (IsOption && DefaultValue == null)
            {
                builder.Append("?");
            }
            builder.Append(": ");

            builder.Append(Type);

            if (DefaultValue != null)
            {
                builder.Append(" = " + JsonConvert.SerializeObject(DefaultValue));
            }

            return builder.ToString();
        }
    }
}
