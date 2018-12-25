// Copyright 2018 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Nuke.Common
{
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class ExcludeAssemblyFromCodeCoverageAttribute : Attribute
    {
    }
}
