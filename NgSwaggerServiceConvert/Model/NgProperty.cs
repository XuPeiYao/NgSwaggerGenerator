using System.Linq;
using System.Text;

namespace NgSwaggerServiceConvert.Model
{
    public class NgProperty
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public bool Required { get; set; }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            if (Description != null)
            {
                builder.AppendLine($"/**\r\n{string.Join("\r\n", Description.Split("\r\n").Select(x => " * " + x))}\r\n */");
            }
            builder.Append(Name);
            if (!Required)
            {
                builder.Append("?");
            }

            builder.Append($" : {Type};\r\n");

            return builder.ToString();
        }
    }
}