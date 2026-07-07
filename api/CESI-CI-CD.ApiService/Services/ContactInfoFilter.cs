using System.Text.RegularExpressions;

namespace CESI_CI_CD.ApiService.Services;

public static partial class ContactInfoFilter
{
    public static bool ContainsContactInfo(string text)
    {
        return EmailRegex().IsMatch(text) || PhoneRegex().IsMatch(text);
    }

    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"(\+33|0)[\s.-]?[1-9](?:[\s.-]?\d{2}){4}")]
    private static partial Regex PhoneRegex();
}
