using ECommerce.Application;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Unit.Mappings;

[TestClass]
public class AutoMapperConfigurationTests
{
    [TestMethod]
    public void MappingProfile_CanInstantiate()
    {
        var profile = new MappingProfile();
        Assert.IsNotNull(profile);
    }
}
