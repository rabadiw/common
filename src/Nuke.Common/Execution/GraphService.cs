// Copyright 2018 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Nuke.Platform;
using Nuke.Platform.Utilities;

namespace Nuke.Common.Execution
{
    internal class GraphService
    {
        public static void ShowGraph<T>(T build)
            where T : NukeBuild
        {
            var graph = new StringBuilder();
            foreach (var target in build.TargetDefinitions)
            {
                var dependentBy = build.TargetDefinitions.Where(x => x.TargetDefinitionDependencies.Contains(target)).ToList();
                if (dependentBy.Count == 0)
                    graph.AppendLine(GetDeclaration(target));
                else
                    dependentBy.ForEach(x => graph.AppendLine($"{GetDeclaration(target)} --> {GetDeclaration(x)}"));
            }

            var resourceText = ResourceUtility.GetResourceText<GraphService>("graph.html");
            var contents = resourceText.Replace("__GRAPH__", graph.ToString());
            var path = Path.Combine(NukeBuild.TemporaryDirectory, "graph.html");
            File.WriteAllText(path, contents);

            // Workaround for https://github.com/dotnet/corefx/issues/10361
            Process.Start(new ProcessStartInfo
                          {
                              FileName = path,
                              UseShellExecute = true
                          });
        }

        private static string GetDeclaration(TargetDefinition targetDefinition)
        {
            return targetDefinition.IsDefault
                ? $"defaultTarget[{targetDefinition.Name}]"
                : targetDefinition.Name;
        }
    }
}
