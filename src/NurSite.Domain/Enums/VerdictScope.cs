namespace NurSite.Domain.Enums;

/// <summary>دامنه اعتبار یک حکم در نمودار.</summary>
public enum VerdictScope
{
    /// <summary>نظر همه مراجع یکسان است.</summary>
    All = 0,

    /// <summary>فقط مراجع مشخص‌شده این نظر را دارند.</summary>
    SpecificMarjas = 1,

    /// <summary>«دیگر مراجع» — یعنی همه به‌جز آنهایی که در شاخه‌های دیگر نام برده شده‌اند.</summary>
    OtherMarjas = 2
}