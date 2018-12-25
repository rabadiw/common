// Copyright 2018 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.Annotations;
using static Nuke.Platform.IO.PathConstruction;

namespace Nuke.Platform.IO
{
    [PublicAPI]
    public class WinRelativePath : RelativePath
    {
        protected WinRelativePath(string path, char? separator)
            : base(path, separator)
        {
        }

        public static explicit operator WinRelativePath([CanBeNull] string path)
        {
            return new WinRelativePath(NormalizePath(path, WinSeparator), WinSeparator);
        }
    }
}
