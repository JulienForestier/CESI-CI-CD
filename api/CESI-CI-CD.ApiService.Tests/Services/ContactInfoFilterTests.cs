using CESI_CI_CD.ApiService.Services;

namespace CESI_CI_CD.ApiService.Tests.Services;

public class ContactInfoFilterTests
{
    [Theory]
    [InlineData("Contactez-moi à jean.dupont@gmail.com")]
    [InlineData("mon mail: test123@collector.shop")]
    [InlineData("Appelez au 06 12 34 56 78")]
    [InlineData("Appelez au 06.12.34.56.78")]
    [InlineData("+33 6 12 34 56 78 c'est mon numéro")]
    [InlineData("0612345678")]
    public void ContainsContactInfo_ReturnsTrue_ForEmailsAndPhoneNumbers(string text)
    {
        Assert.True(ContactInfoFilter.ContainsContactInfo(text));
    }

    [Theory]
    [InlineData("Bonjour, l'objet est en très bon état, prix 45,90 €")]
    [InlineData("Disponible depuis 1995, taille 42")]
    [InlineData("Merci, je vous confirme l'envoi demain")]
    [InlineData("")]
    public void ContainsContactInfo_ReturnsFalse_ForNormalMessages(string text)
    {
        Assert.False(ContactInfoFilter.ContainsContactInfo(text));
    }
}
