using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace NgSwaggerGenerator.Model
{
    public class NgType
    {
        public List<string> ImportTypes {
            get {
                var allTypes = Properties.Select(x => x.Type).Concat(Extends);

                return
                    string.Join(" ", allTypes)
                    .Split(new char[] { ',', '|', ' ', '(', ')', '[', ']', '&' }, StringSplitOptions.RemoveEmptyEntries)
                    .Distinct()
                    .Intersect(Types.Select(x => x.Name))
                    .Where(x => x != Name)
                    .ToList();
            }
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public List<string> Extends { get; set; } = new List<string>();

        public List<NgProperty> Properties { get; set; } = new List<NgProperty>();

        internal List<NgType> Types { get; set; }
        public NgType(List<NgType> types)
        {
            Types = types;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            if (ImportTypes.Count != 0)
            {
                foreach (var type in ImportTypes)
                {
                    builder.AppendLine($"import {{{type}}} from './{type}';");
                }
                //builder.AppendLine($"import {{\r\n {string.Join(",\r\n", ImportTypes.Select(x => "\t" + x))}\r\n }} from './index';\r\n");
            }

            if (Description != null)
            {
                builder.AppendLine($"/**\r\n{string.Join("\r\n", Description.Split("\r\n").Select(x => " * " + x))}\r\n */");
            }

            builder.Append($"export interface {Name}");
            if (Extends.Count != 0)
            {
                builder.Append(" extends " + string.Join(", ", ImportTypes));
            }
            builder.Append(" {\r\n\r\n");

            foreach (var property in Properties)
            {
                builder.Append(string.Join("\r\n", property.ToString().Split("\r\n").Select(x => "\t" + x)) + "\r\n");
            }

            builder.AppendLine("}");

            return builder.ToString();
        }
    }
}
