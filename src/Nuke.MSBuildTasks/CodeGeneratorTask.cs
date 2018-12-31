// Copyright 2018 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Build.Framework;
using Nuke.CodeGeneration;
using Nuke.Platform.IO;

namespace Nuke.MSBuildTasks
{
    [PublicAPI]
    public class CodeGeneratorTask : ITask
    {
        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }

        [Required]
        public ITaskItem[] SpecificationFiles { get; set; }

        [Required]
        public string BaseDirectory { get; set; }

        public bool UseNestedNamespaces { get; set; }

        [CanBeNull]
        public string BaseNamespace { get; set; }

        public bool UpdateReferences { get; set; }

        public bool Execute()
        {
            var specificationFiles = SpecificationFiles.Select(x => x.GetMetadata("Fullpath")).ToList();

            CodeGenerator.GenerateCode(
                specificationFiles,
                outputFileProvider: x =>
                    (AbsolutePath) BaseDirectory
                    / (UseNestedNamespaces ? x.Name : ".")
                    / x.DefaultOutputFileName,
                namespaceProvider: x =>
                    !UseNestedNamespaces
                        ? BaseNamespace
                        : string.IsNullOrEmpty(BaseNamespace)
                            ? x.Name
                            : $"{BaseNamespace}.{x.Name}");

            if (UpdateReferences)
                ReferenceUpdater.UpdateReferences(specificationFiles);

            return true;
        }
    }
}
