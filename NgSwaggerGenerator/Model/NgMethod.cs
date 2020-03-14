using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NgSwaggerGenerator.Model
{
    public class NgMethod
    {
        internal string Tag { get; set; }
        public string Method { get; set; }
        public string Url { get; set; }
        public bool IsFormData { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ReturnType { get; set; }
        public List<NgParameter> Parameters { get; set; } = new List<NgParameter>();

        public override string ToString()
        {
            Parameters = Parameters.OrderBy(x => x.DefaultValue != null).ThenBy(x => x.IsOption).ToList();

            var builder = new StringBuilder();

            if (Description != null || Parameters.Count(x => x.Description != null) > 0)
            {
                builder.AppendLine("/**");

                if (Description != null)
                {
                    foreach (var line in Description.Split("\r\n"))
                    {
                        builder.AppendLine(" * " + line);
                    }
                    foreach (var parameter in Parameters)
                    {
                        builder.AppendLine($" * @param {parameter.Name} {parameter.Description}");
                    }
                }

                builder.AppendLine(" */");
            }

            builder.Append($"{Program.FirstCharToLower(Name)}(");
            if (Parameters.Count != 0)
            {
                builder.Append("\r\n");
                builder.Append(string.Join(",\r\n", Parameters.Select(x => "\t" + x.ToString())));
                builder.Append("\r\n");
            }
            builder.AppendLine($"): Observable<{ReturnType}> {{");

            builder.AppendLine($"\tlet url = '{Url}';");

            #region PATH
            if (Parameters.Any(x => x.Kind == NSwag.OpenApiParameterKind.Path))
            {
                builder.AppendLine();
                foreach (var pathParameter in Parameters.Where(x => x.Kind == NSwag.OpenApiParameterKind.Path))
                {
                    builder.AppendLine($"\t// #region Path Parameter Name: {pathParameter.Name}");
                    builder.AppendLine($"\turl = url.replace('{{{pathParameter.Name}}}', {pathParameter.Name}.toString());");
                    builder.AppendLine($"\t// #endregion");
                }
            }
            #endregion

            #region QUERY
            if (Parameters.Any(x => x.Kind == NSwag.OpenApiParameterKind.Query))
            {
                builder.AppendLine();
                builder.AppendLine("\tconst queryList = [];");
                builder.AppendLine();

                foreach (var queryParameter in Parameters.Where(x => x.Kind == NSwag.OpenApiParameterKind.Query))
                {
                    builder.AppendLine($"\t// #region Query Parameter Name: {queryParameter.Name}");
                    builder.AppendLine($"\tif ( {queryParameter.Name} !== null && {queryParameter.Name} !== undefined ) {{");

                    // Is Array
                    if (queryParameter.Type.EndsWith("[]"))
                    {
                        builder.AppendLine($"\t\tfor ( const item of {queryParameter.Name} ) {{");
                        builder.AppendLine($"\t\t\tif ( item !== null && item !== undefined ) {{");
                        builder.AppendLine($"\t\t\t\tqueryList.push('{queryParameter.Name}=' + encodeURIComponent(item.toString()));");
                        builder.AppendLine("\t\t\t}");
                        builder.AppendLine("\t\t}");
                    }
                    else
                    {
                        builder.AppendLine($"\t\tqueryList.push('{queryParameter.Name}=' + encodeURIComponent({queryParameter.Name}.toString()));");
                    }

                    builder.AppendLine("\t}");
                    builder.AppendLine($"\t// #endregion\r\n");
                }

                builder.AppendLine("\t// Append URL");
                builder.AppendLine("\tif( queryList.length > 0 ) {");
                builder.AppendLine("\t\turl += '?'+ queryList.join('&');");
                builder.AppendLine("\t}");
            }
            #endregion

            #region FormData
            if (Parameters.Any(x => x.Kind == NSwag.OpenApiParameterKind.FormData))
            {
                builder.AppendLine();
                builder.AppendLine("\tconst formData = new FormData();\r\n");

                foreach (var queryParameter in Parameters.Where(x => x.Kind == NSwag.OpenApiParameterKind.FormData))
                {
                    builder.AppendLine($"\t// #region FormData Parameter Name: {queryParameter.Name}");
                    if (queryParameter.Type.EndsWith("[]"))
                    {
                        builder.AppendLine($"\tfor ( const item of {queryParameter.Name} ) {{");
                        builder.AppendLine($"\t\tformData.append('{queryParameter.Name}', item);");
                        builder.AppendLine("\t}");
                    }
                    else
                    {
                        builder.AppendLine($"\tformData.append('{queryParameter.Name}', {queryParameter.Name});");
                    }
                    builder.AppendLine($"\t// #endregion");
                }
            }
            #endregion

            #region Request
            builder.AppendLine();
            builder.AppendLine($"\t// #region Send Request");
            builder.AppendLine("\treturn this.http." + this.Method + $"<{this.ReturnType}>(");
            builder.Append("\t\turl");
            if (Parameters.Any(x => x.Kind == NSwag.OpenApiParameterKind.Body) || Parameters.Any(x => x.Kind == NSwag.OpenApiParameterKind.FormData))
            {
                builder.AppendLine(",");
                if (IsFormData)
                {
                    builder.AppendLine("\t\tformData");
                    builder.AppendLine("\t);");
                }
                else
                {
                    var bodyParameterName = Parameters.FirstOrDefault(x => x.Kind == NSwag.OpenApiParameterKind.Body).Name;
                    builder.AppendLine($"\t\t{bodyParameterName}");
                    builder.AppendLine("\t);");
                }
            }
            else
            {
                builder.Append("\r\n\t);\r\n");
            }
            builder.AppendLine($"\t// #endregion");
            #endregion

            builder.AppendLine("}");
            return builder.ToString();
        }

    }
}