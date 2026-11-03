// أكمل الدوال الأربع. استبدل كل throw new NotImplementedException() بالكود الصحيح.
public static class PriceCalculator
{
    // سعر الوحدة × الكمية. ارمِ ArgumentOutOfRangeException إذا كان أحدهما سالباً.
    public static decimal Subtotal(decimal unitPrice, int quantity)
    {
        throw new NotImplementedException();
    }

    // خصم بنسبة مئوية (0 إلى 100)، والنتيجة مقرّبة لخانتين عشريتين.
    public static decimal ApplyDiscount(decimal amount, int percent)
    {
        throw new NotImplementedException();
    }

    // الإجمالي = (المجموع بعد الخصم) × (1 + نسبة الضريبة)، مقرّب لخانتين.
    public static decimal Total(decimal unitPrice, int quantity, int discountPercent, decimal taxRate)
    {
        throw new NotImplementedException();
    }

    // 1234.5m → "1,234.50 USD"
    public static string Format(decimal amount)
    {
        throw new NotImplementedException();
    }
}
