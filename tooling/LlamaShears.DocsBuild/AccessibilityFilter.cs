using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace LlamaShears.DocsBuild;

internal sealed class AccessibilityFilter
{
    private readonly HashSet<string> _exposedTypes = new HashSet<string>(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _exposedMembers =
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
    private readonly Dictionary<string, string[]> _parameterNames =
        new Dictionary<string, string[]>(StringComparer.Ordinal);

    public static AccessibilityFilter Build(string assemblyPath)
    {
        var filter = new AccessibilityFilter();
        using var stream = File.OpenRead(assemblyPath);
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();

        foreach (var typeHandle in reader.TypeDefinitions)
        {
            var typeDef = reader.GetTypeDefinition(typeHandle);
            if (!IsTypeExposed(reader, typeDef))
            {
                continue;
            }
            if (HasGeneratedAttribute(reader, typeDef.GetCustomAttributes()))
            {
                continue;
            }

            var fqn = GetTypeFqn(reader, typeDef);
            if (fqn is null)
            {
                continue;
            }

            filter._exposedTypes.Add(fqn);
            var memberNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var methodHandle in typeDef.GetMethods())
            {
                var method = reader.GetMethodDefinition(methodHandle);
                if (!IsExposed(method.Attributes))
                {
                    continue;
                }
                if (HasGeneratedAttribute(reader, method.GetCustomAttributes()))
                {
                    continue;
                }
                var name = reader.GetString(method.Name);
                if (name == ".ctor")
                {
                    name = "#ctor";
                }
                else if (name == ".cctor")
                {
                    name = "#cctor";
                }
                memberNames.Add(name);

                var parameterNames = method.GetParameters()
                    .Select(reader.GetParameter)
                    .Where(static p => p.SequenceNumber > 0)
                    .OrderBy(static p => p.SequenceNumber)
                    .Select(p => reader.GetString(p.Name))
                    .ToArray();
                if (parameterNames.Length > 0)
                {
                    filter._parameterNames[$"{fqn}|{name}|{parameterNames.Length}"] = parameterNames;
                }
            }

            foreach (var fieldHandle in typeDef.GetFields())
            {
                var field = reader.GetFieldDefinition(fieldHandle);
                if (!IsExposed(field.Attributes))
                {
                    continue;
                }
                if (HasGeneratedAttribute(reader, field.GetCustomAttributes()))
                {
                    continue;
                }
                memberNames.Add(reader.GetString(field.Name));
            }

            foreach (var propertyHandle in typeDef.GetProperties())
            {
                var property = reader.GetPropertyDefinition(propertyHandle);
                if (!IsAnyAccessorExposed(reader, property.GetAccessors()))
                {
                    continue;
                }
                memberNames.Add(reader.GetString(property.Name));
            }

            foreach (var eventHandle in typeDef.GetEvents())
            {
                var evt = reader.GetEventDefinition(eventHandle);
                if (!IsAnyAccessorExposed(reader, evt.GetAccessors()))
                {
                    continue;
                }
                memberNames.Add(reader.GetString(evt.Name));
            }

            filter._exposedMembers[fqn] = memberNames;
        }

        return filter;
    }

    public bool IsAllowed(MemberDoc member)
    {
        if (!_exposedTypes.Contains(member.OwningType))
        {
            return false;
        }
        if (member.Kind == MemberKind.Type)
        {
            return true;
        }
        if (member.MemberName == "#ctor" || member.MemberName == "#cctor")
        {
            return _exposedMembers.TryGetValue(member.OwningType, out var ctorNames)
                && ctorNames.Contains(member.MemberName);
        }
        return _exposedMembers.TryGetValue(member.OwningType, out var names)
            && names.Contains(member.MemberName);
    }

    public string[]? GetParameterNames(string typeFqn, string memberName, int arity)
    {
        return _parameterNames.TryGetValue($"{typeFqn}|{memberName}|{arity}", out var names) ? names : null;
    }

    private static bool IsTypeExposed(MetadataReader reader, TypeDefinition typeDef)
    {
        var visibility = typeDef.Attributes & TypeAttributes.VisibilityMask;
        if (typeDef.IsNested)
        {
            var nestedExposed = visibility == TypeAttributes.NestedPublic
                || visibility == TypeAttributes.NestedFamily
                || visibility == TypeAttributes.NestedFamORAssem;
            if (!nestedExposed)
            {
                return false;
            }
            var parent = reader.GetTypeDefinition(typeDef.GetDeclaringType());
            return IsTypeExposed(reader, parent);
        }
        return visibility == TypeAttributes.Public;
    }

    private static bool IsExposed(MethodAttributes attrs)
    {
        var access = attrs & MethodAttributes.MemberAccessMask;
        return access == MethodAttributes.Public
            || access == MethodAttributes.Family
            || access == MethodAttributes.FamORAssem;
    }

    private static bool IsExposed(FieldAttributes attrs)
    {
        var access = attrs & FieldAttributes.FieldAccessMask;
        return access == FieldAttributes.Public
            || access == FieldAttributes.Family
            || access == FieldAttributes.FamORAssem;
    }

    private static bool IsAnyAccessorExposed(MetadataReader reader, PropertyAccessors accessors)
    {
        if (!accessors.Getter.IsNil
            && IsExposed(reader.GetMethodDefinition(accessors.Getter).Attributes))
        {
            return true;
        }
        if (!accessors.Setter.IsNil
            && IsExposed(reader.GetMethodDefinition(accessors.Setter).Attributes))
        {
            return true;
        }
        return false;
    }

    private static bool IsAnyAccessorExposed(MetadataReader reader, EventAccessors accessors)
    {
        if (!accessors.Adder.IsNil
            && IsExposed(reader.GetMethodDefinition(accessors.Adder).Attributes))
        {
            return true;
        }
        if (!accessors.Remover.IsNil
            && IsExposed(reader.GetMethodDefinition(accessors.Remover).Attributes))
        {
            return true;
        }
        if (!accessors.Raiser.IsNil
            && IsExposed(reader.GetMethodDefinition(accessors.Raiser).Attributes))
        {
            return true;
        }
        return false;
    }

    private static bool HasGeneratedAttribute(MetadataReader reader, CustomAttributeHandleCollection attrs)
    {
        foreach (var attrHandle in attrs)
        {
            var attr = reader.GetCustomAttribute(attrHandle);
            var typeName = GetAttributeTypeFullName(reader, attr);
            if (typeName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute"
                || typeName == "System.CodeDom.Compiler.GeneratedCodeAttribute")
            {
                return true;
            }
        }
        return false;
    }

    private static string? GetAttributeTypeFullName(MetadataReader reader, CustomAttribute attr)
    {
        var ctor = attr.Constructor;
        switch (ctor.Kind)
        {
            case HandleKind.MemberReference:
                {
                    var memberRef = reader.GetMemberReference((MemberReferenceHandle)ctor);
                    return GetEntityFullName(reader, memberRef.Parent);
                }
            case HandleKind.MethodDefinition:
                {
                    var methodDef = reader.GetMethodDefinition((MethodDefinitionHandle)ctor);
                    var declaring = methodDef.GetDeclaringType();
                    var typeDef = reader.GetTypeDefinition(declaring);
                    return GetTypeFqn(reader, typeDef);
                }
            default:
                return null;
        }
    }

    private static string? GetEntityFullName(MetadataReader reader, EntityHandle handle)
    {
        if (handle.Kind == HandleKind.TypeReference)
        {
            var typeRef = reader.GetTypeReference((TypeReferenceHandle)handle);
            var ns = reader.GetString(typeRef.Namespace);
            var name = reader.GetString(typeRef.Name);
            return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
        }
        if (handle.Kind == HandleKind.TypeDefinition)
        {
            var typeDef = reader.GetTypeDefinition((TypeDefinitionHandle)handle);
            return GetTypeFqn(reader, typeDef);
        }
        return null;
    }

    private static string? GetTypeFqn(MetadataReader reader, TypeDefinition typeDef)
    {
        var name = reader.GetString(typeDef.Name);
        if (typeDef.IsNested)
        {
            var parent = reader.GetTypeDefinition(typeDef.GetDeclaringType());
            var parentFqn = GetTypeFqn(reader, parent);
            return parentFqn is null ? null : $"{parentFqn}.{name}";
        }
        var ns = reader.GetString(typeDef.Namespace);
        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }
}
