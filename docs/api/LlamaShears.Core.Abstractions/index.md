# LlamaShears.Core.Abstractions

The full contract surface that plugins and third-party consumers compile against. Take this when you want every public abstraction LlamaShears exposes — every interface, DTO, and shared type lives in this single package.

## What this contains

The package is organised into namespaces by concern; each namespace was previously its own `LlamaShears.Core.Abstractions.*` sub-package and has been collapsed into this assembly:

- `LlamaShears.Core.Abstractions.Agent` — agent identity, configuration, lifecycle, persistence, sessions, todos.
- `LlamaShears.Core.Abstractions.Caching` — file-parser cache and shears cache contracts.
- `LlamaShears.Core.Abstractions.Commands` — slash-command registry and contracts.
- `LlamaShears.Core.Abstractions.Common` — data-context primitives shared across the surface.
- `LlamaShears.Core.Abstractions.Content` — attachment + content kinds.
- `LlamaShears.Core.Abstractions.Context` — agent / language-model / system / tool / plugin context plus compaction.
- `LlamaShears.Core.Abstractions.Events` — event bus, envelopes, filters, delivery modes, agent + channel messages.
- `LlamaShears.Core.Abstractions.Memory` — memory store/indexer/searcher contracts and reconciliation types.
- `LlamaShears.Core.Abstractions.Paths` — `IShearsPaths`, file-protection policy, expansion contracts.
- `LlamaShears.Core.Abstractions.PromptContext` — prompt context provider + memory.
- `LlamaShears.Core.Abstractions.Provider` — language-model / embedding provider factories, prompts, turns, tool descriptors, model identity.
- `LlamaShears.Core.Abstractions.SystemPrompt` — system-prompt provider, template renderer, template file locator, workspace files.

## See also

- [Architecture overview](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/architecture.md)
- [LlamaShears on GitHub](https://github.com/jasoncouture/llama-shears)

## Licensing

[AGPL-3.0-or-later](https://github.com/jasoncouture/llama-shears/blob/main/LICENSE.md). [Commercial licensing](https://github.com/jasoncouture/llama-shears/blob/main/COMMERCIAL.md) is available.

---

## LlamaShears.Core.Abstractions.Agent

- [AgentConfig](LlamaShears/Core/Abstractions/Agent/AgentConfig.md)
- [AgentConfigExtensions](LlamaShears/Core/Abstractions/Agent/AgentConfigExtensions.md)
- [AgentConfigFile](LlamaShears/Core/Abstractions/Agent/AgentConfigFile.md)
- [AgentInfo](LlamaShears/Core/Abstractions/Agent/AgentInfo.md)
- [AgentMemoryConfig](LlamaShears/Core/Abstractions/Agent/AgentMemoryConfig.md)
- [AgentState](LlamaShears/Core/Abstractions/Agent/AgentState.md)
- [AgentStateExtensions](LlamaShears/Core/Abstractions/Agent/AgentStateExtensions.md)
- [AgentToolConfig](LlamaShears/Core/Abstractions/Agent/AgentToolConfig.md)
- [IAgent](LlamaShears/Core/Abstractions/Agent/IAgent.md)
- [IAgentConfigProvider](LlamaShears/Core/Abstractions/Agent/IAgentConfigProvider.md)
- [IAgentManager](LlamaShears/Core/Abstractions/Agent/IAgentManager.md)
- [IAgentStateTracker](LlamaShears/Core/Abstractions/Agent/IAgentStateTracker.md)
- [IAgentTokenStore](LlamaShears/Core/Abstractions/Agent/IAgentTokenStore.md)
- [SaveAgentConfigResult](LlamaShears/Core/Abstractions/Agent/SaveAgentConfigResult.md)
- [SystemTick](LlamaShears/Core/Abstractions/Agent/SystemTick.md)

## LlamaShears.Core.Abstractions.Agent.Persistence

- [ArchiveId](LlamaShears/Core/Abstractions/Agent/Persistence/ArchiveId.md)
- [IAgentContext](LlamaShears/Core/Abstractions/Agent/Persistence/IAgentContext.md)
- [IContextStore](LlamaShears/Core/Abstractions/Agent/Persistence/IContextStore.md)

## LlamaShears.Core.Abstractions.Agent.SaveAgentConfigResult

- [Conflict](LlamaShears/Core/Abstractions/Agent/SaveAgentConfigResult/Conflict.md)
- [InvalidJson](LlamaShears/Core/Abstractions/Agent/SaveAgentConfigResult/InvalidJson.md)
- [Ok](LlamaShears/Core/Abstractions/Agent/SaveAgentConfigResult/Ok.md)

## LlamaShears.Core.Abstractions.Agent.Sessions

- [ISessionFactory](LlamaShears/Core/Abstractions/Agent/Sessions/ISessionFactory.md)
- [ISessionQueue](LlamaShears/Core/Abstractions/Agent/Sessions/ISessionQueue.md)
- [SessionId](LlamaShears/Core/Abstractions/Agent/Sessions/SessionId.md)

## LlamaShears.Core.Abstractions.Agent.Todo

- [ITodoStorage](LlamaShears/Core/Abstractions/Agent/Todo/ITodoStorage.md)
- [TodoCommandResult](LlamaShears/Core/Abstractions/Agent/Todo/TodoCommandResult.md)
- [TodoItem](LlamaShears/Core/Abstractions/Agent/Todo/TodoItem.md)
- [TodoItemUpdate](LlamaShears/Core/Abstractions/Agent/Todo/TodoItemUpdate.md)
- [TodoResultState](LlamaShears/Core/Abstractions/Agent/Todo/TodoResultState.md)
- [TodoStorageConstants](LlamaShears/Core/Abstractions/Agent/Todo/TodoStorageConstants.md)

## LlamaShears.Core.Abstractions.Caching

- [CacheResult<T>](LlamaShears/Core/Abstractions/Caching/CacheResult-1.md)
- [IFileParserCache<T>](LlamaShears/Core/Abstractions/Caching/IFileParserCache-1.md)
- [IShearsCache<T>](LlamaShears/Core/Abstractions/Caching/IShearsCache-1.md)

## LlamaShears.Core.Abstractions.Commands

- [ISlashCommand](LlamaShears/Core/Abstractions/Commands/ISlashCommand.md)
- [ISlashCommandRegistry](LlamaShears/Core/Abstractions/Commands/ISlashCommandRegistry.md)
- [SlashCommandContext](LlamaShears/Core/Abstractions/Commands/SlashCommandContext.md)
- [SlashCommandParameter](LlamaShears/Core/Abstractions/Commands/SlashCommandParameter.md)
- [SlashCommandResult](LlamaShears/Core/Abstractions/Commands/SlashCommandResult.md)

## LlamaShears.Core.Abstractions.Common

- [AsyncDataContextServiceScope](LlamaShears/Core/Abstractions/Common/AsyncDataContextServiceScope.md)
- [CompositeIdentity](LlamaShears/Core/Abstractions/Common/CompositeIdentity.md)
- [CompositeIdentityJsonConverter](LlamaShears/Core/Abstractions/Common/CompositeIdentityJsonConverter.md)
- [CompositeIdentityTypeConverter](LlamaShears/Core/Abstractions/Common/CompositeIdentityTypeConverter.md)
- [DataContextConstants](LlamaShears/Core/Abstractions/Common/DataContextConstants.md)
- [DataContextScopeExtensions](LlamaShears/Core/Abstractions/Common/DataContextScopeExtensions.md)
- [DataContextScopeFactoryExtensions](LlamaShears/Core/Abstractions/Common/DataContextScopeFactoryExtensions.md)
- [DataContextServiceCollectionExtensions](LlamaShears/Core/Abstractions/Common/DataContextServiceCollectionExtensions.md)
- [IDataContextFactory](LlamaShears/Core/Abstractions/Common/IDataContextFactory.md)
- [IDataContextItemProvider](LlamaShears/Core/Abstractions/Common/IDataContextItemProvider.md)
- [IDataContextScope](LlamaShears/Core/Abstractions/Common/IDataContextScope.md)
- [IPersistentDataContextItem](LlamaShears/Core/Abstractions/Common/IPersistentDataContextItem.md)

## LlamaShears.Core.Abstractions.Content

- [Attachment](LlamaShears/Core/Abstractions/Content/Attachment.md)
- [AttachmentKind](LlamaShears/Core/Abstractions/Content/AttachmentKind.md)

## LlamaShears.Core.Abstractions.Context

- [AgentContext](LlamaShears/Core/Abstractions/Context/AgentContext.md)
- [IAgentContextProvider](LlamaShears/Core/Abstractions/Context/IAgentContextProvider.md)
- [IContextCompactor](LlamaShears/Core/Abstractions/Context/IContextCompactor.md)
- [LanguageModelContext](LlamaShears/Core/Abstractions/Context/LanguageModelContext.md)
- [PluginContext](LlamaShears/Core/Abstractions/Context/PluginContext.md)
- [SystemContext](LlamaShears/Core/Abstractions/Context/SystemContext.md)
- [ToolContext](LlamaShears/Core/Abstractions/Context/ToolContext.md)

## LlamaShears.Core.Abstractions.Events

- [Event](LlamaShears/Core/Abstractions/Events/Event.md)
- [EventBusExtensions](LlamaShears/Core/Abstractions/Events/EventBusExtensions.md)
- [EventDeliveryMask](LlamaShears/Core/Abstractions/Events/EventDeliveryMask.md)
- [EventDeliveryMode](LlamaShears/Core/Abstractions/Events/EventDeliveryMode.md)
- [EventPublisherExtensions](LlamaShears/Core/Abstractions/Events/EventPublisherExtensions.md)
- [EventType](LlamaShears/Core/Abstractions/Events/EventType.md)
- [IEventBus](LlamaShears/Core/Abstractions/Events/IEventBus.md)
- [IEventEnvelope<T>](LlamaShears/Core/Abstractions/Events/IEventEnvelope-1.md)
- [IEventFilter](LlamaShears/Core/Abstractions/Events/IEventFilter.md)
- [IEventHandler<T>](LlamaShears/Core/Abstractions/Events/IEventHandler-1.md)
- [IEventPublisher](LlamaShears/Core/Abstractions/Events/IEventPublisher.md)

## LlamaShears.Core.Abstractions.Events.Agent

- [AgentCompactionMarker](LlamaShears/Core/Abstractions/Events/Agent/AgentCompactionMarker.md)
- [AgentLifecycleMarker](LlamaShears/Core/Abstractions/Events/Agent/AgentLifecycleMarker.md)
- [AgentMessageBase](LlamaShears/Core/Abstractions/Events/Agent/AgentMessageBase.md)
- [AgentMessageFragment](LlamaShears/Core/Abstractions/Events/Agent/AgentMessageFragment.md)
- [AgentThoughtFragment](LlamaShears/Core/Abstractions/Events/Agent/AgentThoughtFragment.md)
- [AgentToolCallFragment](LlamaShears/Core/Abstractions/Events/Agent/AgentToolCallFragment.md)
- [AgentToolResultFragment](LlamaShears/Core/Abstractions/Events/Agent/AgentToolResultFragment.md)
- [IAgentMessage](LlamaShears/Core/Abstractions/Events/Agent/IAgentMessage.md)

## LlamaShears.Core.Abstractions.Events.Channel

- [ChannelMessage](LlamaShears/Core/Abstractions/Events/Channel/ChannelMessage.md)

## LlamaShears.Core.Abstractions.Events.Event

- [Sources](LlamaShears/Core/Abstractions/Events/Event/Sources.md)
- [WellKnown](LlamaShears/Core/Abstractions/Events/Event/WellKnown.md)

## LlamaShears.Core.Abstractions.Events.Event.WellKnown

- [Agent](LlamaShears/Core/Abstractions/Events/Event/WellKnown/Agent.md)
- [Channel](LlamaShears/Core/Abstractions/Events/Event/WellKnown/Channel.md)
- [Host](LlamaShears/Core/Abstractions/Events/Event/WellKnown/Host.md)

## LlamaShears.Core.Abstractions.Memory

- [IMemoryIndexer](LlamaShears/Core/Abstractions/Memory/IMemoryIndexer.md)
- [IMemorySearcher](LlamaShears/Core/Abstractions/Memory/IMemorySearcher.md)
- [IMemoryStore](LlamaShears/Core/Abstractions/Memory/IMemoryStore.md)
- [MemoryReconciliation](LlamaShears/Core/Abstractions/Memory/MemoryReconciliation.md)
- [MemoryRef](LlamaShears/Core/Abstractions/Memory/MemoryRef.md)
- [MemorySearchResult](LlamaShears/Core/Abstractions/Memory/MemorySearchResult.md)

## LlamaShears.Core.Abstractions.Paths

- [FileType](LlamaShears/Core/Abstractions/Paths/FileType.md)
- [IFileProtectionPolicy](LlamaShears/Core/Abstractions/Paths/IFileProtectionPolicy.md)
- [IPathExpander](LlamaShears/Core/Abstractions/Paths/IPathExpander.md)
- [IShearsPaths](LlamaShears/Core/Abstractions/Paths/IShearsPaths.md)
- [PathKind](LlamaShears/Core/Abstractions/Paths/PathKind.md)
- [ProtectedFile](LlamaShears/Core/Abstractions/Paths/ProtectedFile.md)
- [ProtectionMode](LlamaShears/Core/Abstractions/Paths/ProtectionMode.md)

## LlamaShears.Core.Abstractions.PromptContext

- [IPromptContextProvider](LlamaShears/Core/Abstractions/PromptContext/IPromptContextProvider.md)
- [PromptContextMemory](LlamaShears/Core/Abstractions/PromptContext/PromptContextMemory.md)

## LlamaShears.Core.Abstractions.Provider

- [AgentProviderOptions](LlamaShears/Core/Abstractions/Provider/AgentProviderOptions.md)
- [EmbeddingModelConfigurationExtensions](LlamaShears/Core/Abstractions/Provider/EmbeddingModelConfigurationExtensions.md)
- [IContextEntry](LlamaShears/Core/Abstractions/Provider/IContextEntry.md)
- [IEmbeddingModel](LlamaShears/Core/Abstractions/Provider/IEmbeddingModel.md)
- [IEmbeddingProviderFactory](LlamaShears/Core/Abstractions/Provider/IEmbeddingProviderFactory.md)
- [IInferenceRunner](LlamaShears/Core/Abstractions/Provider/IInferenceRunner.md)
- [ILanguageModel](LlamaShears/Core/Abstractions/Provider/ILanguageModel.md)
- [IModelCompletionResponse](LlamaShears/Core/Abstractions/Provider/IModelCompletionResponse.md)
- [IModelResponseFragment](LlamaShears/Core/Abstractions/Provider/IModelResponseFragment.md)
- [IModelTextFormatter](LlamaShears/Core/Abstractions/Provider/IModelTextFormatter.md)
- [IModelTextResponse](LlamaShears/Core/Abstractions/Provider/IModelTextResponse.md)
- [IModelThoughtResponse](LlamaShears/Core/Abstractions/Provider/IModelThoughtResponse.md)
- [IModelToolCallFragment](LlamaShears/Core/Abstractions/Provider/IModelToolCallFragment.md)
- [IProviderFactory](LlamaShears/Core/Abstractions/Provider/IProviderFactory.md)
- [InferenceOutcome](LlamaShears/Core/Abstractions/Provider/InferenceOutcome.md)
- [LanguageModelExtensions](LlamaShears/Core/Abstractions/Provider/LanguageModelExtensions.md)
- [ModelConfiguration](LlamaShears/Core/Abstractions/Provider/ModelConfiguration.md)
- [ModelConfigurationExtensions](LlamaShears/Core/Abstractions/Provider/ModelConfigurationExtensions.md)
- [ModelConfigurationJsonConverter](LlamaShears/Core/Abstractions/Provider/ModelConfigurationJsonConverter.md)
- [ModelInfo](LlamaShears/Core/Abstractions/Provider/ModelInfo.md)
- [ModelPrompt](LlamaShears/Core/Abstractions/Provider/ModelPrompt.md)
- [ModelRole](LlamaShears/Core/Abstractions/Provider/ModelRole.md)
- [ModelTokenInformationContextEntry](LlamaShears/Core/Abstractions/Provider/ModelTokenInformationContextEntry.md)
- [ModelTurn](LlamaShears/Core/Abstractions/Provider/ModelTurn.md)
- [PromptOptions](LlamaShears/Core/Abstractions/Provider/PromptOptions.md)
- [SupportedInputType](LlamaShears/Core/Abstractions/Provider/SupportedInputType.md)
- [ThinkLevel](LlamaShears/Core/Abstractions/Provider/ThinkLevel.md)
- [ToolCall](LlamaShears/Core/Abstractions/Provider/ToolCall.md)
- [ToolCallResult](LlamaShears/Core/Abstractions/Provider/ToolCallResult.md)
- [ToolDescriptor](LlamaShears/Core/Abstractions/Provider/ToolDescriptor.md)
- [ToolGroup](LlamaShears/Core/Abstractions/Provider/ToolGroup.md)
- [ToolParameter](LlamaShears/Core/Abstractions/Provider/ToolParameter.md)

## LlamaShears.Core.Abstractions.SystemPrompt

- [ISystemPromptProvider](LlamaShears/Core/Abstractions/SystemPrompt/ISystemPromptProvider.md)
- [ITemplateFileLocator](LlamaShears/Core/Abstractions/SystemPrompt/ITemplateFileLocator.md)
- [ITemplateRenderer](LlamaShears/Core/Abstractions/SystemPrompt/ITemplateRenderer.md)
- [WorkspaceContext](LlamaShears/Core/Abstractions/SystemPrompt/WorkspaceContext.md)
- [WorkspaceFile](LlamaShears/Core/Abstractions/SystemPrompt/WorkspaceFile.md)

