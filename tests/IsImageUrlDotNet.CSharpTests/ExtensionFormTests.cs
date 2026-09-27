using System;
using System.Threading.Tasks;
using IsImageUrlDotNet;
using NUnit.Framework;

namespace IsImageUrlDotNet.CSharpTests
{
    public class ExtensionFormTests
    {
        // IsImageUrl is obsolete in 2.x on purpose; these tests prove the 1.0.2 call still compiles and answers.
#pragma warning disable CS0618
        [TestCase("http://fixture.test/a.png", true)]
        [TestCase("IMAGE.PNG", true)]
        [TestCase("x.pdf", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void The_1_0_2_extension_form_compiles_and_answers(string? url, bool expected)
        {
            Assert.That(url!.IsImageUrl(), Is.EqualTo(expected));
            Assert.That(IsImageUrlDotNetLib.IsImageUrl(url), Is.EqualTo(expected));
        }

        [Test]
        public void The_1_0_2_lists_are_FSharp_lists_readable_from_CSharp()
        {
            Assert.That(IsImageUrlDotNetLib.ImageFileExtensions, Has.Exactly(8).Items);
            Assert.That(IsImageUrlDotNetLib.NonImageFileExtensions, Does.Contain("pdf"));
        }
#pragma warning restore CS0618

        [TestCase("http://fixture.test/a.png?size=1", true)]
        [TestCase("photo.WEBP", true)]
        [TestCase("http://fixture.test/a.pdf", false)]
        [TestCase(null, false)]
        public void HasImageExtension_is_an_extension_method(string? url, bool expected)
        {
            Assert.That(url.HasImageExtension(), Is.EqualTo(expected));
        }

        [Test]
        public async Task IsImageUrlAsync_is_an_extension_method()
        {
            Assert.That(await "http://fixture.test/a.png".IsImageUrlAsync(), Is.True);
            Assert.That(await "mailto:x@fixture.test".IsImageUrlAsync(), Is.False);
            Assert.That(ImageUrl.ImageExtensions, Does.Contain("avif"));
        }

        [Test]
        public void FSharp_Core_arrives_through_the_package_dependency_at_its_floor()
        {
            var fsharpCore = IsImageUrlDotNetLib.ImageFileExtensions.GetType().Assembly.GetName();
            Assert.That(fsharpCore.Name, Is.EqualTo("FSharp.Core"));
            Assert.That(fsharpCore.Version, Is.EqualTo(new Version(6, 0, 0, 0)));
        }
    }
}
