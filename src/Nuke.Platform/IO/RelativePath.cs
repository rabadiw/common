// Copyright 2018 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using static Nuke.Platform.IO.PathConstruction;

namespace Nuke.Platform.IO
{
    [PublicAPI]
    [DebuggerDisplay("{" + nameof(_path) + "}")]
    public class RelativePath
    {
        private readonly string _path;
        private readonly char? _separator;

        protected RelativePath(string path, char? separator = null)
        {
            _path = path;
            _separator = separator;
        }

        public static explicit operator RelativePath([CanBeNull] string path)
        {
            if (path is null)
                return null;
                
            return new RelativePath(NormalizePath(path));
        }

        public static implicit operator string([CanBeNull] RelativePath path)
        {
            return path?._path;
        }

        public static RelativePath operator /(RelativePath path1, [CanBeNull] string path2)
        {
            var separator = path1.NotNull("path1 != null")._separator;
            return new RelativePath(NormalizePath(Combine(path1, (RelativePath) path2, separator), separator), separator);
        }

        public override string ToString()
        {
            return _path;
        }
    }
}
