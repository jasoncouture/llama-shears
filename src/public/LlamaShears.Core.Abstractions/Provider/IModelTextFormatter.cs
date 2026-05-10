using System.Collections.Immutable;
using System.Text;
using LlamaShears.Core.Abstractions.Content;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Renders a <see cref="ModelTurn"/> into the textual shape a specific model
/// or transport expects (e.g. role-tagged transcript, chat-template form).
/// </summary>
public interface IModelTextFormatter
{
    /// <summary>Formats the supplied turn for transport.</summary>
    /// <param name="turn">Turn to render.</param>
    /// <returns>Provider-specific textual rendering of <paramref name="turn"/>.</returns>
    string Format(ModelTurn turn);
}
