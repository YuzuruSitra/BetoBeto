using System.IO;
using BetoBeto.UI;
using NUnit.Framework;
using UnityEngine;

namespace BetoBeto.Tests
{
    public sealed class TitleOpeningTests
    {
        static string ClipPath => Path.Combine(Application.streamingAssetsPath, "Video", TitleOpening.ClipFile);

        [Test]
        public void OpeningMovieShipsInStreamingAssetsWhereEveryPlatformCanReadIt()
        {
            // WebGL cannot play VideoClip assets, so the movie is streamed from this fixed URL instead.
            Assert.That(File.Exists(ClipPath), Is.True, ClipPath);
            Assert.That(new FileInfo(ClipPath).Length, Is.GreaterThan(1 << 20), "映像ファイルが差し替わっていないか。");
            Assert.That(TitleOpening.ClipUrl.Replace('\\', '/'), Is.EqualTo(ClipPath.Replace('\\', '/')));
            Assert.That(TitleOpening.ClipUrl, Does.StartWith(Application.streamingAssetsPath));
        }

        [Test]
        public void OpeningIsOfferedOnlyOncePerLaunch()
        {
            TitleOpening.Enabled = true;
            TitleOpening.Rewind();
            Assert.That(TitleOpening.ShouldPlay, Is.True);
            TitleOpening.Enabled = false;
            Assert.That(TitleOpening.ShouldPlay, Is.False, "テストやツールは映像を止められる。");
            TitleOpening.Enabled = true;
        }
    }
}
