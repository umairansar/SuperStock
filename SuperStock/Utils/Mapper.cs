namespace SuperStock.Utils;

public static class Mapper
{
    public static string ToProductId(this string productName)
    {
        return productName switch
        {
            "NVDA" => "dcbc9f373e7c96cae045a589",
            "TSLA" => "dcbc9f373e7c96cae045a590",
            "AMD" => "dcbc9f373e7c96cae045a591",
            _ => "dcbc9f373e7c96caeunknown"
        };
    }
}