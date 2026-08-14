namespace NurSite.Application.Services;

/// <summary>
/// شماره‌های صفحه‌بندی، پنجره‌ای.
///
/// چاپ همه شماره‌ها فقط تا ده‌بیست صفحه جواب می‌دهد؛ با هزار صوت،
/// نوار صفحه‌بندی خودش می‌شود یک دیوار عدد. اینجا فقط صفحه اول و آخر و
/// چند صفحه دور و بر صفحه فعلی برمی‌گردد و بقیه با سه‌نقطه جمع می‌شود.
/// </summary>
public static class Pager
{
    /// <summary>
    /// شماره صفحه‌ها به ترتیب. مقدار null یعنی «چند صفحه اینجا حذف شده»
    /// و باید به شکل سه‌نقطه نمایش داده شود.
    /// </summary>
    /// <param name="current">صفحه فعلی، از یک.</param>
    /// <param name="total">تعداد کل صفحه‌ها.</param>
    /// <param name="around">چند صفحه از هر طرفِ صفحه فعلی دیده شود.</param>
    public static IReadOnlyList<int?> Pages(int current, int total, int around = 2)
    {
        if (total <= 1) return [];

        current = Math.Clamp(current, 1, total);

        // تا وقتی همه شماره‌ها در یک خط جا می‌شوند، سه‌نقطه فقط شلوغی است
        var visible = around * 2 + 5;
        if (total <= visible)
            return Enumerable.Range(1, total).Select(p => (int?)p).ToList();

        var wanted = new SortedSet<int> { 1, total };
        for (var p = current - around; p <= current + around; p++)
            if (p >= 1 && p <= total) wanted.Add(p);

        var result = new List<int?>();
        var previous = 0;

        foreach (var page in wanted)
        {
            // فاصله یک‌تایی ارزش سه‌نقطه ندارد؛ خود شماره کوتاه‌تر است
            if (previous != 0 && page - previous > 1)
            {
                if (page - previous == 2) result.Add(previous + 1);
                else result.Add(null);
            }

            result.Add(page);
            previous = page;
        }

        return result;
    }
}