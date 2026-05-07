using System.Reflection;

namespace StrangeSoft.Plugins.Host;

/// <summary>
/// Notification sink for events that happen inside the plugin loader
/// — host graph walk, plugin loader instantiation, plugin loader
/// invocation. The loader prefers to swallow per-item failures so a
/// single bad assembly or loader doesn't take everything down; this
/// interface gives the host a place to surface those failures (or
/// not) in whatever logging stack it owns.
/// </summary>
public interface IPluginContextLogger
{
    /// <summary>
    /// Reported when <see cref="System.Reflection.Assembly.Load(AssemblyName)"/>
    /// throws during the host's transitive-reference walk. The walk
    /// continues; <paramref name="assemblyName"/> simply won't be in
    /// the host-owned set.
    /// </summary>
    void AssemblyLoadFailed(AssemblyName assemblyName, Exception exception);

    /// <summary>
    /// Reported when an <c>IPluginLoader&lt;T&gt;</c> implementation
    /// can't be constructed — missing parameterless constructor, ctor
    /// throws, etc. The discovery walk skips it and continues with
    /// the next type.
    /// </summary>
    void LoaderInstantiationFailed(Type loaderType, Exception exception);

    /// <summary>
    /// Reported when an <c>IPluginLoader&lt;T&gt;.LoadAsync</c> call
    /// throws. The loader's contribution is treated as empty; other
    /// loaders' results are still streamed out.
    /// </summary>
    void LoaderInvocationFailed(Type loaderType, Exception exception);
}
