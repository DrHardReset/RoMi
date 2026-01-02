namespace RoMi.Tests;

public class AppInfoTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void AppInfoCreation()
    {
        var appInfo = new AppConfig { Environment = "Test" };

        Assert.That(appInfo, Is.Not.Null);
        Assert.That(appInfo.Environment, Is.EqualTo("Test"));
    }
}
