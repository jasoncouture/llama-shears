using System.Collections.Generic;

namespace LlamaShears.DocsBuild;

internal sealed class XmlDocSignatureParser
{
    private readonly string _text;
    private int _position;

    private XmlDocSignatureParser(string text)
    {
        _text = text;
    }

    public static List<TypeRef> ParseParameterList(string parenthesized)
    {
        var inner = parenthesized;
        if (inner.Length >= 2 && inner[0] == '(' && inner[inner.Length - 1] == ')')
        {
            inner = inner.Substring(1, inner.Length - 2);
        }
        if (string.IsNullOrEmpty(inner))
        {
            return [];
        }
        var parser = new XmlDocSignatureParser(inner);
        return parser.ParseTypeList();
    }

    public static TypeRef ParseSingle(string text)
    {
        var parser = new XmlDocSignatureParser(text);
        return parser.ParseType();
    }

    private List<TypeRef> ParseTypeList()
    {
        var list = new List<TypeRef> { ParseType() };
        while (_position < _text.Length && _text[_position] == ',')
        {
            _position++;
            list.Add(ParseType());
        }
        return list;
    }

    private TypeRef ParseType()
    {
        var name = ReadName();
        var typeRef = new TypeRef { FullName = name };

        if (_position < _text.Length && _text[_position] == '{')
        {
            _position++;
            typeRef.GenericArgs.Add(ParseType());
            while (_position < _text.Length && _text[_position] == ',')
            {
                _position++;
                typeRef.GenericArgs.Add(ParseType());
            }
            if (_position < _text.Length && _text[_position] == '}')
            {
                _position++;
            }
        }

        while (_position < _text.Length - 1 && _text[_position] == '[' && _text[_position + 1] == ']')
        {
            typeRef.ArrayRank++;
            _position += 2;
        }

        if (_position < _text.Length && _text[_position] == '@')
        {
            typeRef.IsByRef = true;
            _position++;
        }

        if (_position < _text.Length && _text[_position] == '*')
        {
            typeRef.IsPointer = true;
            _position++;
        }

        return typeRef;
    }

    private string ReadName()
    {
        var start = _position;
        while (_position < _text.Length)
        {
            var ch = _text[_position];
            if (ch == ',' || ch == '{' || ch == '}' || ch == '[' || ch == ']' || ch == '@' || ch == '*')
            {
                break;
            }
            _position++;
        }
        return _text.Substring(start, _position - start);
    }
}
