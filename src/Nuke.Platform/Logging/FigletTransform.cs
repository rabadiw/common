// Copyright 2018 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Colorful;
using Nuke.Platform.Extensions;
using Nuke.Platform.Utilities;

namespace Nuke.Platform.Logging
{
    [ExcludeFromCodeCoverage]
    public class FigletTransform
    {
        public static string GetText(string text, string integratedFontName = null)
        {
            integratedFontName = integratedFontName ?? "cybermedium";
            var stream = ResourceUtility.GetResource<FigletTransform>($"Fonts.{integratedFontName}.flf");
            return GetText(text, stream);
        }

        public static string GetText(string text, Stream stream)
        {
            var figlet = new Figlet(FigletFont.Load(stream));

            var textWithFont = figlet.ToAscii(text).ToString()
                .Split(new[] { EnvironmentInfo.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x));

            return EnvironmentInfo.NewLine +
                   textWithFont.JoinNewLine() +
                   EnvironmentInfo.NewLine;
        }
    }
}
