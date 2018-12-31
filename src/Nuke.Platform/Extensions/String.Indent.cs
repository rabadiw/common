// Copyright 2018 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.Annotations;

namespace Nuke.Platform.Extensions
{
    public static partial class StringExtensions
    {
        [Pure]
        public static string Indent(this string text, int count)
        {
            return new string(c: ' ', count) + text;
        }
    }
}
