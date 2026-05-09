using System.Collections.Immutable;
using System.Text;
using LlamaShears.Core.Abstractions.Content;

namespace LlamaShears.Core.Abstractions.Provider;

public interface IModelTextFormatter
{
    string Format(ModelTurn turn);
}
