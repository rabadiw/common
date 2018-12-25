// Copyright 2018 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Nuke.Platform.IO;

namespace Nuke.Platform
{
    public class Constants
    {
        public const string ConfigurationFileName = ".nuke";
        
        public const string TargetsSeparator = "+";
        public const string InvokedTargetsParameterName = "Target";
        public const string SkippedTargetsParameterName = "Skip";
        
        public const string CompletionParameterName = "shell-completion";

        [CanBeNull]
        public static AbsolutePath TryGetRootDirectoryFrom(string startDirectory)
        {
            return (AbsolutePath) FileSystemTasks.FindParentDirectory(
                startDirectory,
                predicate: x => x.GetFiles(ConfigurationFileName).Any());
        }

        public static AbsolutePath GetTemporaryDirectory(AbsolutePath rootDirectory)
        {
            return rootDirectory / ".tmp";
        }

        public static AbsolutePath GetCompletionFile(AbsolutePath rootDirectory)
        {
            var completionFileName = CompletionParameterName + ".yml";
            return File.Exists(rootDirectory / completionFileName)
                ? rootDirectory / completionFileName
                : GetTemporaryDirectory(rootDirectory) / completionFileName;
        }
        
        public static AbsolutePath GetBuildAttemptFile(AbsolutePath rootDirectory)
        {
            return GetTemporaryDirectory(rootDirectory) / "build-attempt.log";
        }
    }
}
