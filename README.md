# IsImageUrlDotNet
String Extension Function and Static method to check if a url is for an image.  

Example: 
```
var googleLogoUrl = "https://www.google.com/images/branding/googlelogo/1x/googlelogo_color_272x92dp.png";

var isAnImage = googleLogoUrl.IsImageUrl();

Assert.IsTrue(isAnImage)
```

