# Shared memory index

- [Atomic commits and full-solution build/test](feedback_build_and_commit.md) — commit each logical change; always run `dotnet build` and `dotnet test` with no params
- [Never accept IConfiguration as a parameter](feedback_iconfiguration_parameter.md) — use `AddOptions<T>().BindConfiguration("Section")`; expose only the section name (defaulted, last)
- ["SDK reference" means FrameworkReference, not the Sdk attribute](feedback_sdk_reference.md) — add `<FrameworkReference Include="..." />`, never swap the project's `Sdk=`
