﻿using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace NgSwaggerServiceConvert.Model
{
    public class CliOptions
    {
        [Option('m', "module", Required = false, HelpText = "Angular module name.", Default = "Api")]
        public string ModuleName { get; set; }

        [Option('s', "source", Required = true, HelpText = "Swagger source.")]
        public string URL { get; set; }

        [Option('o', "output", Required = false, HelpText = "Angular module output directory path.", Default = "./output")]
        public string OutputDirectory { get; set; }
    }
}
