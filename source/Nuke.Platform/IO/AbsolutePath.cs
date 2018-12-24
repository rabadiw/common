// Copyright 2018 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using static Nuke.Common.IO.PathConstruction;

namespace Nuke.Common.IO
{
    [PublicAPI]
    [DebuggerDisplay("{" + nameof(_path) + "}")]
    [TypeConverter(typeof(TypeConverter))]
    public class AbsolutePath
    {
        public class TypeConverter : System.ComponentModel.TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string stringValue)
                {
                    return (AbsolutePath) (HasPathRoot(stringValue)
                        ? stringValue
                        : Combine(EnvironmentInfo.WorkingDirectory, stringValue));
                }

                return base.ConvertFrom(context, culture, value);
            }
        }

        private readonly string _path;

        private AbsolutePath(string path)
        {
            _path = NormalizePath(path);
        }

        public AbsolutePath Parent =>
            !IsWinRoot(_path.TrimEnd(WinSeparator)) &&
            !IsUncRoot(_path) &&
            !IsUnixRoot(_path)
                ? this / ".."
                : null;

        #region Operators

        public static AbsolutePath operator /(AbsolutePath path1, [CanBeNull] string path2)
        {
            return new AbsolutePath(Combine(path1.NotNull("path1 != null"), path2));
        }

        public static explicit operator AbsolutePath([CanBeNull] string path)
        {
            if (path is null)
                return null;

            ControlFlow.Assert(HasPathRoot(path), $"Path '{path}' must be rooted.");
            return new AbsolutePath(path);
        }

        public static implicit operator string([CanBeNull] AbsolutePath path)
        {
            return path?.ToString();
        }

        #endregion

        #region Equality

        protected bool Equals(AbsolutePath other)
        {
            var stringComparison = HasWinRoot(_path) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return string.Equals(_path, other._path, stringComparison);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(objA: null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((AbsolutePath) obj);
        }

        public override int GetHashCode()
        {
            return _path?.GetHashCode() ?? 0;
        }

        #endregion

        public override string ToString()
        {
            return _path;
        }
    }
}
