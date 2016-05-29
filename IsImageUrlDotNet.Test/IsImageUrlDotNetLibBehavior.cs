using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IsImageUrlDotNet.Test
{
    [TestClass]
    public class IsImageUrlDotNetLibBehavior
    {
        private const string CommonImageUrl = "https://www.google.com/images/branding/googlelogo/1x/googlelogo_color_272x92dp.png";
        private const string NotAnImageUrl = "https://www.google.com";
        private const string ImageUrlExmapleWithoutExtension = "http://placehold.it/350x150";
        private const string PdfUrlExample = "http://www.sanface.com/pdf/test.pdf";

        [TestMethod]
        public void ShouldBeAbleToIdentifyACommonImageUrl()
        {
            var isImage = CommonImageUrl.IsImageUrl();

            Assert.IsTrue(isImage);
        }

        [TestMethod]
        public void ShouldBeAbleToIdentifyAnExtensionlessImageUrl()
        {
            var isImage = ImageUrlExmapleWithoutExtension.IsImageUrl();

            Assert.IsTrue(isImage);
        }

        [TestMethod]
        public void ShouldBeAbleToTellIfACommonUrlIsNotForAnImageUrl()
        {
            var isImage = NotAnImageUrl.IsImageUrl();

            Assert.IsFalse(isImage);
        }
        [TestMethod]
        public void ShouldBeAbleToTellThatACommonPdfUrlIsNotAnImageUrl()
        {
            var isImage = PdfUrlExample.IsImageUrl();

            Assert.IsFalse(isImage);
        }

        [TestMethod]
        public void ShouldBeAbleToAccessAsAStaticMethod()
        {
            // ReSharper disable once InvokeAsExtensionMethod
            var isImage = IsImageUrlDotNetLib.IsImageUrl(CommonImageUrl);

            Assert.IsTrue(isImage);
        }
    }
}