﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NgSwaggerServiceConvert.Extensions;
using NgSwaggerServiceConvert.Model;
using NJsonSchema;
using NSwag;

namespace NgSwaggerServiceConvert
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CliOptions>(args).WithParsed(options => {
                Main(options).GetAwaiter().GetResult();
            });
        }

        static async Task ClearOutput(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        static async Task Main(CliOptions options)
        {
            await ClearOutput(options.OutputDirectory);
            var swaggerDoc = await OpenApiDocument.FromUrlAsync(options.URL);

            var types = await LoadTypes(swaggerDoc);
            var services = await LoadServices(swaggerDoc, types);

            await OutputTypes(options, types);
            await OutputServices(options, services);

            await OutputModule(Path.Combine(options.OutputDirectory, FirstCharToLower(options.ModuleName) + ".module.ts"), options.ModuleName, services);
            await OutputModuleIndexTs(Path.Combine(options.OutputDirectory, "index.ts"), options.ModuleName);
        }

        static async Task OutputModuleIndexTs(string path, string moduleName)
        {
            var builder = new StringBuilder();
            builder.AppendLine("export * from './models';");
            builder.AppendLine("export * from './services';");
            builder.AppendLine($"export * from './{FirstCharToLower(moduleName)}.module';");

            System.IO.File.WriteAllText(path, builder.ToString());
        }

        static async Task OutputModule(string path, string moduleName, List<NgService> services)
        {
            var builder = new StringBuilder();
            builder.AppendLine("import { NgModule } from '@angular/core';");
            builder.AppendLine("import { CommonModule } from '@angular/common';");
            builder.AppendLine("import { HttpClientModule } from '@angular/common/http';");
            builder.AppendLine($"import {{\r\n {string.Join(",\r\n", services.Select(x => "\t" + x.Name + "Service"))}\r\n }} from './';\r\n");
            builder.AppendLine();

            builder.AppendLine("@NgModule({");
            builder.AppendLine("\tdeclarations: [],");
            builder.AppendLine("\timports: [CommonModule, HttpClientModule],");
            builder.AppendLine("\tproviders: [");
            builder.Append(string.Join(",\r\n", services.Select(x => "\t\t" + x.Name + "Service")) + "\r\n");
            builder.AppendLine("\t]");
            builder.AppendLine("})");
            builder.AppendLine($"export class {moduleName}Module {{");
            builder.AppendLine("}");

            System.IO.File.WriteAllText(path, builder.ToString());
        }

        static async Task<List<NgType>> LoadTypes(OpenApiDocument swaggerDoc)
        {
            List<NgType> result = new List<NgType>();
            foreach (var type in swaggerDoc.Definitions)
            {
                if (type.Value.IsEnumeration)
                {
                    continue;
                }

                var newType = new NgType(result);

                // 類型名稱
                newType.Name = type.Key;

                // 類型敘述
                newType.Description = type.Value.Description;

                foreach (var inherted in type.Value.AllInheritedSchemas)
                {
                    newType.Extends.Add(GetTypeString(swaggerDoc, inherted));
                }

                // 產生屬性
                foreach (var property in type.Value.Properties.Concat(type.Value.ActualProperties))
                {
                    var newProp = new NgProperty();

                    // 屬性名稱
                    newProp.Name = property.Key;

                    if (newType.Properties.Any(x => x.Name == newProp.Name))
                    {
                        continue;
                    }

                    // 屬性敘述
                    newProp.Description = property.Value.Description;

                    // 屬性必要性
                    newProp.Required = type.Value.RequiredProperties.Contains(newProp.Name);

                    // 取得屬性類型
                    newProp.Type = GetTypeString(swaggerDoc, property.Value);

                    newType.Properties.Add(newProp);
                }

                result.Add(newType);
            }
            return result;
        }

        static async Task<List<NgService>> LoadServices(OpenApiDocument swaggerDoc, List<NgType> types)
        {
            List<NgService> result = new List<NgService>();
            List<NgMethod> methods = new List<NgMethod>();
            foreach (var path in swaggerDoc.Paths)
            {
                foreach (var method in path.Value.Keys)
                {
                    var newMethod = new NgMethod();
                    newMethod.Description = path.Value[method].Description ?? path.Value[method].Summary;
                    newMethod.Method = method;
                    newMethod.Name = string.Join("_", path.Value[method].OperationId.Split("_").Skip(1));
                    newMethod.Url = path.Key;

                    if (path.Value[method].Responses["200"].Schema != null)
                    {
                        newMethod.ReturnType = GetTypeString(swaggerDoc, path.Value[method].Responses["200"].Schema);
                    }
                    else
                    {
                        newMethod.ReturnType = "void";
                    }

                    if (newMethod.ReturnType == "file")
                    {
                        newMethod.ReturnType = "void";
                    }

                    newMethod.Tag = path.Value[method].Tags.FirstOrDefault() ?? "Unknown";
                    newMethod.IsFormData = path.Value[method].Consumes?.Contains("multipart/form-data") ?? false;

                    foreach (var param in path.Value[method].Parameters)
                    {
                        var newParam = new NgParameter();
                        newParam.Name = param.Name;
                        newParam.Kind = param.Kind;
                        newParam.IsOption = !param.IsRequired || (param.IsNullableRaw.HasValue ? param.IsNullableRaw.Value : false);
                        newParam.DefaultValue = param.Default;
                        newParam.Description = param.Description;
                        newParam.Type = GetTypeString(swaggerDoc, param);

                        if (newParam.Type == "file")
                        {
                            newParam.Type = "File";
                            if (param.CollectionFormat == OpenApiParameterCollectionFormat.Multi)
                            {
                                newParam.Type += "[]";
                            }
                            else
                            {
                                newParam.Type = "any";
                            }
                        }

                        newMethod.Parameters.Add(newParam);
                    }

                    methods.Add(newMethod);
                }
            }

            foreach (var methodSet in methods.GroupBy(x => x.Tag))
            {
                result.Add(new NgService(types)
                {
                    Name = methodSet.Key,
                    Methods = methodSet.ToList()
                });
            }
            return result;
        }

        static async Task OutputTypes(CliOptions options, List<NgType> types)
        {
            var directoryPath = Path.Combine(options.OutputDirectory, "models");
            Directory.CreateDirectory(directoryPath);

            string indexTs = "";
            foreach (var type in types)
            {
                System.IO.File.WriteAllText(Path.Combine(directoryPath, type.Name + ".ts"), type.ToString());
                indexTs += $"export * from './{FirstCharToLower(type.Name)}';\r\n";
            }

            System.IO.File.WriteAllText(Path.Combine(directoryPath, "index.ts"), indexTs);
        }

        static async Task OutputServices(CliOptions options, List<NgService> services)
        {
            var directoryPath = Path.Combine(options.OutputDirectory, "services");
            Directory.CreateDirectory(directoryPath);

            string indexTs = "";
            foreach (var service in services)
            {
                System.IO.File.WriteAllText(Path.Combine(directoryPath, FirstCharToLower(service.Name) + ".service.ts"), service.ToString());
                indexTs += $"export * from './{FirstCharToLower(service.Name)}.service';\r\n";
            }

            System.IO.File.WriteAllText(Path.Combine(directoryPath, "index.ts"), indexTs);
        }

        public static string FirstCharToLower(string s)
        {
            if (s != string.Empty && char.IsUpper(s[0]))
            {
                s = char.ToLower(s[0]) + s.Substring(1);
            }
            return s;
        }
        static string GetTypeString(OpenApiDocument doc, JsonSchema schema)
        {
            try
            {
                if (schema.Type == NJsonSchema.JsonObjectType.Array)
                {
                    return GetTypeString(doc, schema.Item) + "[]";
                }
                else if (
                    schema.Reference == null &&
                    schema.Type != JsonObjectType.Array &&
                    schema.Type != JsonObjectType.None)
                {
                    switch (schema.Type)
                    {
                        case JsonObjectType.Integer:
                            return "number";
                        case JsonObjectType.None:
                            return "null";
                        case JsonObjectType.String:
                            if (schema.IsEnumeration)
                            {
                                return string.Join(" | ", schema.Enumeration.Select(x => "'" + x.ToString() + "'"));
                            }
                            return schema.Type.ToString().ToLower();
                        case JsonObjectType.Object:
                            if (doc.Definitions.Any(x => x.Value == schema))
                            {
                                var refDef = doc.Definitions.SingleOrDefault(x => x.Value == schema);
                                return refDef.Key;
                            }
                            else
                            {
                                return schema.Type.ToString().ToLower();
                            }
                            break;
                        default:
                            return schema.Type.ToString().ToLower();
                    }
                }
                else
                {
                    if (schema.Reference != null)
                    {
                        var typeName = doc.Definitions.SingleOrDefault(x => x.Value == schema.Reference);
                        return typeName.Key;
                    }
                    else if (schema is OpenApiParameter parameterSchema)
                    {
                        return GetTypeString(doc, parameterSchema.Schema);
                    }
                    else
                    {
                        string typeStr = null;
                        if (schema.HasOneOfSchemaReference)
                        {
                            typeStr = string.Join(" | ", schema.OneOf.Select(x => GetTypeString(doc, x)));
                        }
                        if (schema.HasAnyOfSchemaReference)
                        {
                            if (typeStr == null)
                            {
                                typeStr = string.Join(" | ", schema.AnyOf.Select(x => GetTypeString(doc, x)));
                            }
                            else
                            {
                                typeStr = "(" + typeStr + ") | " + string.Join(" | ", schema.AnyOf.Select(x => GetTypeString(doc, x)));
                            }
                        }
                        if (schema.HasAllOfSchemaReference)
                        {
                            if (typeStr == null)
                            {
                                typeStr = string.Join(" & ", schema.AllOf.Select(x => GetTypeString(doc, x)));
                            }
                            else
                            {
                                typeStr = "(" + typeStr + ") | " + string.Join(" & ", schema.AllOf.Select(x => GetTypeString(doc, x)));
                            }
                        }
                        return typeStr;
                    }
                }

                return "void";
            }
            catch
            {
                return "any";
            }
        }
    }
}
