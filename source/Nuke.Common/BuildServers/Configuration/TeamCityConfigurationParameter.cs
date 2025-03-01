// Copyright 2019 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common.Utilities;

namespace Nuke.Common.BuildServers.Configuration
{
    public class TeamCityConfigurationParameter : TeamCityParameter
    {
        public TeamCityParameterType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public TeamCityParameterDisplay Display { get; set; }
        public string DefaultValue { get; set; }
        public IReadOnlyDictionary<string, object> Options { get; set; }
        public bool AllowMultiple { get; set; }
        public string ValueSeparator { get; set; }

        public override void Write(KotlinScriptWriter writer)
        {
            writer.WriteLine($"{Type.ToString().ToLowerInvariant()} (");
            using (writer.Indent())
            {
                writer.WriteLine($"{("env." + Name).DoubleQuote()},");
                writer.WriteLine($"label = {Name.DoubleQuote()},");

                if (Description != null)
                    writer.WriteLine($"description = {Description.DoubleQuote()},");

                writer.WriteLine($"value = {DefaultValue.DoubleQuote()},");

                if (AllowMultiple)
                {
                    writer.WriteLine("allowMultiple = true,");
                    writer.WriteLine($"valueSeparator = {ValueSeparator.DoubleQuote()},");
                }

                if (Options != null)
                {
                    var mappings = Options.Select(x => $"{x.Key.DoubleQuote()} to {x.Value.ToString().DoubleQuote()}");
                    writer.WriteLine($"options = listOf({mappings.JoinComma()}),");
                }

                if (Options == null)
                    writer.WriteLine($"allowEmpty = {(Display != TeamCityParameterDisplay.Prompt).ToString().ToLowerInvariant()},");

                writer.WriteLine($"display = ParameterDisplay.{Display.ToString().ToUpperInvariant()})");
            }
        }
    }
}
