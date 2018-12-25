// Copyright 2018 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;

namespace Nuke.Platform.Extensions
{
    public static partial class StringExtensions
    {
        [Pure]
        public static string Capitalize(this string text)
        {
            return text.Substring(startIndex: 0, length: 1).ToUpper(CultureInfo.InvariantCulture) +
                   text.Substring(startIndex: 1);
        }
    }
}
