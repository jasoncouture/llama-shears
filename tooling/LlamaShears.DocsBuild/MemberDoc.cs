using System.Xml.Linq;

namespace LlamaShears.DocsBuild;

internal sealed class MemberDoc
{
    public MemberKind Kind { get; private set; }

    public string OwningType { get; private set; } = "";

    public string MemberName { get; private set; } = "";

    public string? ParameterSignature { get; private set; }

    public XElement Element { get; private set; } = new("member");

    public static MemberDoc? Parse(XElement element)
    {
        var raw = (string?)element.Attribute("name");
        if (string.IsNullOrWhiteSpace(raw) || raw!.Length < 3 || raw[1] != ':')
        {
            return null;
        }

        var prefix = raw[0];
        var body = raw.Substring(2);

        var kind = prefix switch
        {
            'T' => MemberKind.Type,
            'M' => MemberKind.Method,
            'P' => MemberKind.Property,
            'F' => MemberKind.Field,
            'E' => MemberKind.Event,
            _ => MemberKind.Unknown,
        };

        if (kind == MemberKind.Unknown)
        {
            return null;
        }

        string owningType;
        string memberName;
        string? parameters = null;

        if (kind == MemberKind.Type)
        {
            owningType = body;
            memberName = body;
        }
        else
        {
            var signatureStart = body.IndexOf('(');
            var withoutSignature = signatureStart >= 0 ? body.Substring(0, signatureStart) : body;
            if (signatureStart >= 0)
            {
                parameters = body.Substring(signatureStart);
            }

            var lastDot = withoutSignature.LastIndexOf('.');
            if (lastDot < 0)
            {
                return null;
            }

            owningType = withoutSignature.Substring(0, lastDot);
            memberName = withoutSignature.Substring(lastDot + 1);
        }

        return new MemberDoc
        {
            Kind = kind,
            OwningType = owningType,
            MemberName = memberName,
            ParameterSignature = parameters,
            Element = element,
        };
    }
}
