using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NgSwaggerGenerator.Model
{
    public class NgService
    {
        public string Name { get; set; }
        public List<NgMethod> Methods { get; set; } = new List<NgMethod>();


        public List<string> ImportTypes {
            get {
                var allTypes = Methods.SelectMany(x => x.Parameters.Select(x => x.Type).Concat(new string[] { x.ReturnType }));

                return
                    string.Join(" ", allTypes)
                    .Split(new char[] { ',', '|', ' ', '(', ')', '[', ']', '&' }, StringSplitOptions.RemoveEmptyEntries)
                    .Distinct()
                    .Intersect(Types.Select(x => x.Name))
                    .ToList();
            }
        }

        internal List<NgType> Types { get; set; }
        public NgService(List<NgType> types)
        {
            Types = types;
        }


        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("﻿import { Injectable } from '@angular/core';");
            builder.AppendLine("import { HttpClient } from '@angular/common/http';");
            builder.AppendLine("import { Observable } from 'rxjs';");

            if (ImportTypes.Count != 0)
            {
                builder.AppendLine($"import {{\r\n{string.Join(",\r\n", ImportTypes.Select(x => "\t" + x))}\r\n }} from '../models';\r\n");
            }

            builder.AppendLine();
            builder.Append("@Injectable({\r\n\tprovidedIn: 'root'\r\n})\r\n");
            builder.Append($"export class {Name}Service {{\r\n\r\n");
            builder.Append("\tconstructor(private http: HttpClient) {}\r\n\r\n");
            foreach (var method in Methods)
            {
                builder.Append(string.Join("\r\n", method.ToString().Split("\r\n").Select(x => "\t" + x)) + "\r\n");
            }

            builder.AppendLine("}");

            var result = builder.ToString().Replace("\t", "    ");
            Regex regex = new Regex(@"[ ]+\r\n");
            result = regex.Replace(result, "\r\n");

            return result;
        }
    }
}
