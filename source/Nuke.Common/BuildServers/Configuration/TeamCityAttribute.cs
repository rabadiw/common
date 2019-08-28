// Copyright 2019 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.PathConstruction;

namespace Nuke.Common.BuildServers.Configuration
{
    [PublicAPI]
    public class TeamCityAttribute : Attribute, IPreLogoBuildExtension, IBuildFinishedExtension
    {
        public const string ConfigurationParameterName = "configure-build-server";

        public TeamCityAttribute(TeamCityAgentPlatform platform)
        {
            Platform = platform;
        }

        public bool AutoGenerate { get; set; } = true;

        public TeamCityAgentPlatform Platform { get; }
        public string Description { get; set; }
        public string DefaultBranch { get; set; } = "develop";

        public string VcsTriggerBranchFilter { get; set; } = "";
        public string VcsTriggerRules { get; set; } = "+:**";
        public string[] VcsTriggeredTargets { get; set; } = new string[0];

        public string NightlyTriggerBranchFilter { get; set; } = "";
        public string NightlyTriggerRules { get; set; } = "+:**";
        public string[] NightlyTriggeredTargets { get; set; } = new string[0];

        public string[] ManuallyTriggeredTargets { get; set; } = new string[0];
        public string[] NonEntryTargets { get; set; } = new string[0];
        public string[] ExcludedTargets { get; set; } = new string[0];

        public void PreLogo(
            NukeBuild build,
            IReadOnlyCollection<ExecutableTarget> executableTargets)
        {
            if (!ParameterService.Instance.GetParameter<bool>(ConfigurationParameterName))
            {
                if (AutoGenerate)
                {
                    ProcessTasks.StartProcess(
                        Assembly.GetEntryAssembly().Location,
                        $"--{ConfigurationParameterName} --host teamcity",
                        logInvocation: false,
                        logOutput: false);
                }

                return;
            }

            Generate(build, executableTargets);

            Environment.Exit(0);
        }

        public void OnBuildFinished(NukeBuild build)
        {
            if (TeamCity.Instance == null)
                return;

            // serialize difference to start object

            var stateFile = NukeBuild.TemporaryDirectory / $"{TeamCity.Instance.BuildTypeId}.xml";
            FileSystemTasks.Touch(stateFile);
            // FileSystemTasks.CopyFile(NukeBuild.TemporaryDirectory / "state.xml", stateFile, FileExistsPolicy.Overwrite);
            TeamCity.Instance.PublishArtifacts($"+:{stateFile} => .teamcity/states");
        }

        private void Generate(NukeBuild build, IReadOnlyCollection<ExecutableTarget> executableTargets)
        {
            ControlFlow.Assert(NukeBuild.RootDirectory != null, "NukeBuild.RootDirectory != null");
            var teamcityDirectory = NukeBuild.RootDirectory / ".teamcity";

            TextTasks.WriteAllLines(
                teamcityDirectory / "pom.xml",
                ResourceUtility.GetResourceAllLines<TeamCityAttribute>("teamcity.pom.xml"));

            using (var fileStream = File.Open(teamcityDirectory / "settings.kts", FileMode.Create))
            using (var streamWriter = new StreamWriter(fileStream))
            {
                var writer = new KotlinScriptWriter(streamWriter);

                GetHeader().ForEach(writer.WriteLine);

                var project = GetProject(build, executableTargets);
                project.Write(writer);

                project.VcsRoot.Write(writer);
                project.BuildTypes.ForEach(x => x.Write(writer));
            }
        }

        protected virtual IEnumerable<string> GetHeader()
        {
            return new[]
                   {
                       "// THIS FILE IS AUTO-GENERATED",
                       "// ITS CONTENT IS OVERWRITTEN WITH EXCEPTION OF MARKED USER BLOCKS",
                       "",
                       "import jetbrains.buildServer.configs.kotlin.v2018_1.*",
                       "import jetbrains.buildServer.configs.kotlin.v2018_1.buildFeatures.*",
                       "import jetbrains.buildServer.configs.kotlin.v2018_1.buildSteps.*",
                       "import jetbrains.buildServer.configs.kotlin.v2018_1.triggers.*",
                       "import jetbrains.buildServer.configs.kotlin.v2018_1.vcs.*",
                       "",
                       "version = \"2019.1\"",
                       ""
                   };
        }

        protected virtual TeamCityProject GetProject(
            NukeBuild build,
            IReadOnlyCollection<ExecutableTarget> executableTargets)
        {
            var relevantTargets = VcsTriggeredTargets.Concat(ManuallyTriggeredTargets)
                .SelectMany(x => ExecutionPlanner.GetExecutionPlan(executableTargets, new[] { x }))
                .Distinct()
                .Where(x => !ExcludedTargets.Contains(x.Name) && !NonEntryTargets.Contains(x.Name)).ToList();

            var vcsRoot = GetVcsRoot(build);
            var lookupTable = new LookupTable<ExecutableTarget, TeamCityBuildType>();
            var buildTypes = relevantTargets
                .SelectMany(x => GetBuildTypes(build, x, vcsRoot, lookupTable), (x, y) => (ExecutableTarget: x, BuildType: y))
                .ForEachLazy(x => lookupTable.Add(x.ExecutableTarget, x.BuildType))
                .Select(x => x.BuildType).ToArray();

            var parameters = GetGlobalParameters(build);
            if (Platform == TeamCityAgentPlatform.Windows)
            {
                parameters = parameters
                    .Concat(new TeamCityKeyValueParameter
                            {
                                Key = "teamcity.runner.commandline.stdstreams.encoding",
                                Value = "IBM-437"
                            });
            }

            return new TeamCityProject
                   {
                       VcsRoot = vcsRoot,
                       BuildTypes = buildTypes,
                       Parameters = parameters.ToArray()
                   };
        }

        protected virtual IEnumerable<TeamCityBuildType> GetBuildTypes(
            NukeBuild build,
            ExecutableTarget executableTarget,
            TeamCityVcsRoot vcsRoot,
            LookupTable<ExecutableTarget, TeamCityBuildType> buildTypes)
        {
            var isPartitioned = ArtifactExtensions.Partitions.ContainsKey(executableTarget.Definition);
            var artifactRules = ArtifactExtensions.ArtifactProducts[executableTarget.Definition].Select(GetArtifactRule).ToArray();
            var artifactDependencies = (
                from artifactDependency in ArtifactExtensions.ArtifactDependencies[executableTarget.Definition]
                let dependency = executableTarget.ExecutionDependencies.Single(x => x.Factory == artifactDependency.Item1)
                let rules = (artifactDependency.Item2.Any()
                        ? artifactDependency.Item2
                        : ArtifactExtensions.ArtifactProducts[dependency.Definition])
                    .Select(GetArtifactRule).ToArray()
                select new TeamCityArtifactDependency
                       {
                           BuildType = buildTypes[dependency].Single(x => x.Partition == null),
                           ArtifactRules = rules
                       }).ToArray<TeamCityDependency>();

            var invokedTargets = executableTarget
                .DescendantsAndSelf(x => x.ExecutionDependencies, x => NonEntryTargets.Contains(x.Name))
                .Where(x => x == executableTarget || NonEntryTargets.Contains(x.Name))
                .Reverse()
                .Select(x => x.Name).ToArray();
            var snapshotDependencies = executableTarget.ExecutionDependencies
                .Where(x => !ExcludedTargets.Contains(x.Name) && !NonEntryTargets.Contains(x.Name))
                .SelectMany(x => buildTypes[x])
                .Where(x => x.Partition == null)
                .Select(x => new TeamCitySnapshotDependency
                             {
                                 BuildType = x,
                                 FailureAction = TeamCityDependencyFailureAction.FailToStart,
                                 CancelAction = TeamCityDependencyFailureAction.Cancel
                             }).ToArray<TeamCityDependency>();

            if (isPartitioned)
            {
                var partitions = ArtifactExtensions.Partitions[executableTarget.Definition];
                for (var i = 0; i < partitions; i++)
                {
                    var partition = new Partition { Part = i + 1, Total = partitions };
                    yield return new TeamCityBuildType
                                 {
                                     Id = $"{executableTarget.Name}_P{partition.Part}T{partition.Total}",
                                     Name = $"{executableTarget.Name} {partition}",
                                     Description = executableTarget.Description,
                                     Platform = Platform,
                                     ArtifactRules = artifactRules,
                                     Partition = partition,
                                     PartitionTarget = executableTarget.Name,
                                     InvokedTargets = invokedTargets,
                                     VcsRoot = new TeamCityBuildTypeVcsRoot { Root = vcsRoot },
                                     Dependencies = snapshotDependencies.Concat(artifactDependencies).ToArray()
                                 };
                }

                snapshotDependencies = buildTypes[executableTarget]
                    .Select(x => new TeamCitySnapshotDependency
                                 {
                                     BuildType = x,
                                     FailureAction = TeamCityDependencyFailureAction.FailToStart,
                                     CancelAction = TeamCityDependencyFailureAction.Cancel
                                 }).ToArray<TeamCityDependency>();
                artifactDependencies = new TeamCityDependency[0];
            }

            var parameters = executableTarget.Requirements
                .Where(x => !(x is Expression<Func<bool>>))
                .Select(x => GetParameter(x.GetMemberInfo(), build, required: true)).ToArray();
            var triggers = GetTriggers(executableTarget, buildTypes).ToArray();

            yield return new TeamCityBuildType
                         {
                             Id = executableTarget.Name,
                             Name = executableTarget.Name,
                             Description = executableTarget.Description,
                             Platform = Platform,
                             VcsRoot = new TeamCityBuildTypeVcsRoot
                                       {
                                           Root = vcsRoot,
                                           ShowDependenciesChanges = isPartitioned
                                       },
                             IsComposite = isPartitioned,
                             InvokedTargets = invokedTargets,
                             ArtifactRules = artifactRules,
                             Dependencies = snapshotDependencies.Concat(artifactDependencies).ToArray(),
                             Parameters = parameters,
                             Triggers = triggers
                         };
        }

        protected virtual IEnumerable<TeamCityTrigger> GetTriggers(
            ExecutableTarget executableTarget,
            LookupTable<ExecutableTarget, TeamCityBuildType> buildTypes)
        {
            if (VcsTriggeredTargets.Contains(executableTarget.Name))
            {
                yield return new TeamCityVcsTrigger
                             {
                                 BranchFilter = VcsTriggerBranchFilter,
                                 TriggerRules = VcsTriggerRules
                             };
            }

            if (NightlyTriggeredTargets.Contains(executableTarget.Name))
            {
                yield return new TeamCityScheduledTrigger
                             {
                                 BranchFilter = NightlyTriggerBranchFilter,
                                 TriggerRules = NightlyTriggerRules,
                                 EnableQueueOptimization = true,
                                 WithPendingChangesOnly = false
                             };
            }

            foreach (var triggerDependency in executableTarget.TriggerDependencies)
            {
                yield return new TeamCityFinishBuildTrigger
                             {
                                 BuildType = buildTypes[triggerDependency].Single(x => x.Partition == null)
                             };
            }
        }

        protected virtual TeamCityVcsRoot GetVcsRoot(NukeBuild build)
        {
            var repository = GitRepository.FromLocalDirectory(NukeBuild.RootDirectory);
            return new TeamCityVcsRoot
                   {
                       Name = $"{repository.HttpsUrl}#refs/heads/{DefaultBranch}",
                       Url = repository.HttpsUrl,
                       Branch = $"refs/heads/{DefaultBranch}",
                       PollInterval = 60,
                       BranchSpec = new[] { "+:refs/heads/*" }
                   };
        }

        protected virtual IEnumerable<TeamCityParameter> GetGlobalParameters(NukeBuild build)
        {
            return InjectionUtility.GetParameterMembers(build.GetType(), includeUnlisted: false)
                .Except(build.ExecutableTargets.SelectMany(x => x.Requirements
                    .Where(y => !(y is Expression<Func<bool>>))
                    .Select(y => y.GetMemberInfo())))
                .Where(x => x.DeclaringType != typeof(NukeBuild) || x.Name == nameof(NukeBuild.Verbosity))
                .Select(x => GetParameter(x, build, required: false));
        }

        protected virtual TeamCityConfigurationParameter GetParameter(MemberInfo member, NukeBuild build, bool required)
        {
            var attribute = member.GetCustomAttribute<ParameterAttribute>();
            var valueSet = ParameterService.Instance.GetParameterValueSet(member, build);

            var defaultValue = member.GetValue(build);
            if (member.GetMemberType() == typeof(AbsolutePath) ||
                member.GetMemberType() == typeof(Solution) ||
                member.GetMemberType() == typeof(Project))
                defaultValue = (UnixRelativePath) GetRelativePath(NukeBuild.RootDirectory, defaultValue.ToString());

            return new TeamCityConfigurationParameter
                   {
                       Name = member.Name,
                       Description = attribute.Description,
                       Options = valueSet?.ToDictionary(x => x.Item1, x => x.Item2),
                       Type = valueSet != null ? TeamCityParameterType.Select : TeamCityParameterType.Text,
                       DefaultValue = defaultValue?.ToString(),
                       Display = required ? TeamCityParameterDisplay.Prompt : TeamCityParameterDisplay.Normal,
                       AllowMultiple = member.GetMemberType().IsArray,
                       ValueSeparator = attribute.Separator ?? " "
                   };
        }

        protected virtual string GetArtifactRule(string rule)
        {
            if (IsDescendantPath(NukeBuild.RootDirectory, rule))
                rule = GetRelativePath(NukeBuild.RootDirectory, rule);

            return HasPathRoot(rule)
                ? rule
                : (UnixRelativePath) rule;
        }
    }
}
