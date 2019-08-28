// Copyright 2019 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Linq;
using Nuke.Common.Utilities;

namespace Nuke.Common.BuildServers.Configuration
{
    public class TeamCityProject : TeamCityConfigurationEntity
    {
        public string Description { get; set; }
        public TeamCityParameter[] Parameters { get; set; }
        public TeamCityVcsRoot VcsRoot { get; set; }
        public TeamCityBuildType[] BuildTypes { get; set; }

        public override void Write(KotlinScriptWriter writer)
        {
            using (writer.WriteBlock("project"))
            {
                if (Description != null)
                    writer.WriteLine($"description = {Description}");

                writer.WriteLine($"vcsRoot({VcsRoot.Id})");
                writer.WriteLine();

                foreach (var buildType in BuildTypes)
                    writer.WriteLine($"buildType({buildType.Id})");
                writer.WriteLine();

                writer.WriteLine($"buildTypesOrder = arrayListOf({BuildTypes.Select(x => x.Id).JoinComma()})");
                writer.WriteLine();

                if (Parameters.Any() )
                {
                    using (writer.WriteBlock("params"))
                    {
                        foreach (var parameter in Parameters)
                            parameter.Write(writer);
                    }
                }
            }
        }
    }
}
