using System.Globalization;
using System.Xml.Linq;

namespace DacteNet.Xml;

/// <summary>
/// Namespace-agnostic helpers for walking the CT-e XML tree by local element name. The official CT-e
/// documents always declare the "http://www.portalfiscal.inf.br/cte" namespace, but (mirroring the
/// tolerant, structure-blind behaviour of ACBr's own substring-search reader documented in
/// xml_mapping.md §1.4) we deliberately match by LocalName only, so a missing/extra/prefixed namespace
/// never breaks parsing.
/// </summary>
internal static class XmlHelpers
{
    public static XElement? Child(this XElement? el, string name) =>
        el?.Elements().FirstOrDefault(e => e.Name.LocalName == name);

    public static IEnumerable<XElement> Children(this XElement? el, string name) =>
        el?.Elements().Where(e => e.Name.LocalName == name) ?? Enumerable.Empty<XElement>();

    public static string? Text(this XElement? el)
    {
        if (el is null) return null;
        var s = el.Value?.Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    public static string? ChildText(this XElement? el, string name) => el.Child(name).Text();

    public static string? Attr(this XElement? el, string name) => el?.Attribute(name)?.Value;

    public static decimal? ChildDecimal(this XElement? el, string name)
    {
        var s = el.ChildText(name);
        if (s is null) return null;
        return decimal.TryParse(s, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    public static decimal ChildDecimalOrZero(this XElement? el, string name) => el.ChildDecimal(name) ?? 0m;

    public static double? ChildDouble(this XElement? el, string name)
    {
        var s = el.ChildText(name);
        if (s is null) return null;
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    public static int? ChildInt(this XElement? el, string name)
    {
        var s = el.ChildText(name);
        if (s is null) return null;
        return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    /// <summary>CT-e dhEmi/dhRecbto-style values: "yyyy-MM-ddTHH:mm:sszzz" (with UTC offset) - falls back to a bare date.</summary>
    public static DateTimeOffset? ChildDateTime(this XElement? el, string name)
    {
        var s = el.ChildText(name);
        return ParseDateTime(s);
    }

    public static DateTimeOffset? ParseDateTime(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
            return dto;
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return new DateTimeOffset(dt);
        return null;
    }

    /// <summary>CT-e dEmi/dPrev-style bare-date values: "yyyy-MM-dd".</summary>
    public static DateTimeOffset? ChildDate(this XElement? el, string name) => el.ChildDateTime(name);
}
