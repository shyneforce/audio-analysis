// <copyright file="AppConfigHelperTests.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace Acoustics.Test.Shared
{
    using System;
    using System.IO;
    using Acoustics.Shared;
    using Acoustics.Test.TestHelpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using static Acoustics.Shared.AppConfigHelper;

    [TestClass]
    public class AppConfigHelperTests
    {
        [TestMethod]
        public void DefaultSampleRate()
        {
            var actual = DefaultTargetSampleRate;

            Assert.AreEqual(22050, actual);
        }

        [TestMethod]
        public void ExecutingAssemblyDirectoryIsSet()
        {
            var actual = ExecutingAssemblyDirectory;

            Assert.That.DirectoryExists(actual);
            Assert.That.FileExists(Path.Join(actual, Meta.Name));
        }

        [TestMethod]
        public void IsMonoShouldAlwaysFail()
        {
            Assert.IsFalse(IsMono);
        }

        [TestMethod]
        public void IsMuslShouldBe()
        {
            bool expected = false;
#if BUILT_AGAINST_MUSL
            expected = true;
#endif
            Assert.AreEqual(expected, WasBuiltAgainstMusl);
        }

        [RuntimeIdentifierSpecificDataTestMethod]
        [DataRow("win-x64", "ffmpeg", "audio-utils/win-x64/ffmpeg/ffmpeg.exe", true)]
        [DataRow("win-x64", "ffprobe", "audio-utils/win-x64/ffmpeg/ffprobe.exe", true)]
        [DataRow("win-x64", "sox", "audio-utils/win-x64/sox/sox.exe", true)]
        [DataRow("win-x64", "wvunpack", "audio-utils/win-x64/wavpack/wvunpack.exe", true)]

        [DataRow("win-arm64", "ffmpeg", "audio-utils/win-arm64/ffmpeg/ffmpeg.exe", true)]
        [DataRow("win-arm64", "ffprobe", "audio-utils/win-arm64/ffmpeg/ffprobe.exe", true)]
        [DataRow("win-arm64", "sox", "sox.exe", true)]
        [DataRow("win-arm64", "wvunpack", null, false)]

        [DataRow("osx-x64", "ffmpeg", "audio-utils/osx-x64/ffmpeg/ffmpeg", true)]
        [DataRow("osx-x64", "ffprobe", "audio-utils/osx-x64/ffmpeg/ffprobe", true)]
        [DataRow("osx-x64", "sox", "audio-utils/osx-x64/sox/sox", true)]
        [DataRow("osx-x64", "wvunpack", "audio-utils/osx-x64/wavpack/wvunpack", true)]

        [DataRow("linux-x64", "ffmpeg", "audio-utils/linux-x64/ffmpeg/ffmpeg", true)]
        [DataRow("linux-x64", "ffprobe", "audio-utils/linux-x64/ffmpeg/ffprobe", true)]
        [DataRow("linux-x64", "sox", "sox", true)]
        [DataRow("linux-x64", "wvunpack", null, false)]

        [DataRow("linux-musl-x64", "ffmpeg", "audio-utils/linux-musl-x64/ffmpeg/ffmpeg", true)]
        [DataRow("linux-musl-x64", "ffprobe", "audio-utils/linux-musl-x64/ffmpeg/ffprobe", true)]
        [DataRow("linux-musl-x64", "sox", "sox", true)]
        [DataRow("linux-musl-x64", "wvunpack", null, false)]

        [DataRow("linux-arm", "ffmpeg", "audio-utils/linux-arm/ffmpeg/ffmpeg", true)]
        [DataRow("linux-arm", "ffprobe", "audio-utils/linux-arm/ffmpeg/ffprobe", true)]
        [DataRow("linux-arm", "sox", "sox", true)]
        [DataRow("linux-arm", "wvunpack", null, false)]

        [DataRow("linux-arm64", "ffmpeg", "audio-utils/linux-arm64/ffmpeg/ffmpeg", true)]
        [DataRow("linux-arm64", "ffprobe", "audio-utils/linux-arm64/ffmpeg/ffprobe", true)]
        [DataRow("linux-arm64", "sox", "sox", true)]
        [DataRow("linux-arm64", "wvunpack", null, false)]
        public void ResolveExecutableMethods(string rid, string toolName, string expected, bool required)
        {
            if (rid != PseudoRuntimeIdentifier)
            {
                Assert.Inconclusive($"Testing for RID {rid} on current build {PseudoRuntimeIdentifier} is not supported");
            }

            Func<string> get = () => GetExeFile(toolName);

            if (expected == null)
            {
                if (required)
                {
                    Assert.ThrowsException<FileNotFoundException>(
                        get,
                        $"Could not find {toolName} in audio-utils or in the system. Please install {toolName}.");
                }
                else
                {
                    Assert.IsNull(get.Invoke());
                }
            }
            else
            {
                var actual = get.Invoke();

                Assert.IsNotNull(actual);
                Assert.That.FileExists(actual);
                StringAssert.EndsWith(actual, expected.NormalizeDirectorySeparators());
            }

        }
    }
}
