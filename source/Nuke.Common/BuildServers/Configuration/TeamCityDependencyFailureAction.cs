// Copyright 2019 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Linq;

namespace Nuke.Common.BuildServers.Configuration
{
    public enum TeamCityDependencyFailureAction
    {
        // TODO: add description from web UI
        AddProblem,
        FailToStart,
        Ignore,
        Cancel
    }
}
