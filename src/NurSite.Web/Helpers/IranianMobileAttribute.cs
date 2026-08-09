using System.ComponentModel.DataAnnotations;

namespace NurSite.Web.Helpers;

/// <summary>اعتبارسنجی شماره موبایل ایرانی، با پذیرش قالب‌های مختلف ورودی.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class IranianMobileAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is null) return false;
        return MobileNumber.Normalize(value.ToString()) is not null;
    }

    public override string FormatErrorMessage(string name) =>
        ErrorMessage ?? "شماره موبایل معتبر نیست. مثال: ۰۹۱۲۳۴۵۶۷۸۹";
}