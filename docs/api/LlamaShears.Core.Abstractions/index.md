# LlamaShears.Core.Abstractions

Public API surface, organized by namespace.

## LlamaShears.Core.Abstractions.Agent

- [AgentConfig](LlamaShears/Core/Abstractions/Agent/AgentConfig.md)
- [AgentConfiguration](LlamaShears/Core/Abstractions/Agent/AgentConfiguration.md)
- [AgentEmbeddingConfig](LlamaShears/Core/Abstractions/Agent/AgentEmbeddingConfig.md)
- [AgentInfo](LlamaShears/Core/Abstractions/Agent/AgentInfo.md)
- [AgentMemoryConfig](LlamaShears/Core/Abstractions/Agent/AgentMemoryConfig.md)
- [AgentModelConfig](LlamaShears/Core/Abstractions/Agent/AgentModelConfig.md)
- [AgentToolConfig](LlamaShears/Core/Abstractions/Agent/AgentToolConfig.md)
- [IAgent](LlamaShears/Core/Abstractions/Agent/IAgent.md)
- [IAgentConfigProvider](LlamaShears/Core/Abstractions/Agent/IAgentConfigProvider.md)
- [IAgentFactory](LlamaShears/Core/Abstractions/Agent/IAgentFactory.md)
- [IAgentManager](LlamaShears/Core/Abstractions/Agent/IAgentManager.md)
- [IAgentTokenStore](LlamaShears/Core/Abstractions/Agent/IAgentTokenStore.md)
- [SystemTick](LlamaShears/Core/Abstractions/Agent/SystemTick.md)

## LlamaShears.Core.Abstractions.Agent.Persistence

- [ArchiveId](LlamaShears/Core/Abstractions/Agent/Persistence/ArchiveId.md)
- [IAgentContext](LlamaShears/Core/Abstractions/Agent/Persistence/IAgentContext.md)
- [IContextStore](LlamaShears/Core/Abstractions/Agent/Persistence/IContextStore.md)

## LlamaShears.Core.Abstractions.Caching

- [CacheResult](LlamaShears/Core/Abstractions/Caching/CacheResult`1.md)
- [IFileParserCache](LlamaShears/Core/Abstractions/Caching/IFileParserCache`1.md)
- [IShearsCache](LlamaShears/Core/Abstractions/Caching/IShearsCache`1.md)

## LlamaShears.Core.Abstractions.Content

- [Attachment](LlamaShears/Core/Abstractions/Content/Attachment.md)
- [AttachmentKind](LlamaShears/Core/Abstractions/Content/AttachmentKind.md)

## LlamaShears.Core.Abstractions.Context

- [AgentContext](LlamaShears/Core/Abstractions/Context/AgentContext.md)
- [IAgentContextProvider](LlamaShears/Core/Abstractions/Context/IAgentContextProvider.md)
- [LanguageModelContext](LlamaShears/Core/Abstractions/Context/LanguageModelContext.md)
- [PluginContext](LlamaShears/Core/Abstractions/Context/PluginContext.md)
- [SystemContext](LlamaShears/Core/Abstractions/Context/SystemContext.md)
- [ToolContext](LlamaShears/Core/Abstractions/Context/ToolContext.md)
- [ToolDescriptor](LlamaShears/Core/Abstractions/Context/ToolDescriptor.md)
- [ToolGroup](LlamaShears/Core/Abstractions/Context/ToolGroup.md)
- [ToolParameter](LlamaShears/Core/Abstractions/Context/ToolParameter.md)

## LlamaShears.Core.Abstractions.Events

- [Event](LlamaShears/Core/Abstractions/Events/Event.md)
- [EventBusExtensions](LlamaShears/Core/Abstractions/Events/EventBusExtensions.md)
- [EventDeliveryMask](LlamaShears/Core/Abstractions/Events/EventDeliveryMask.md)
- [EventDeliveryMode](LlamaShears/Core/Abstractions/Events/EventDeliveryMode.md)
- [EventPublisherExtensions](LlamaShears/Core/Abstractions/Events/EventPublisherExtensions.md)
- [EventType](LlamaShears/Core/Abstractions/Events/EventType.md)
- [IEventBus](LlamaShears/Core/Abstractions/Events/IEventBus.md)
- [IEventEnvelope](LlamaShears/Core/Abstractions/Events/IEventEnvelope`1.md)
- [IEventFilter](LlamaShears/Core/Abstractions/Events/IEventFilter.md)
- [IEventHandler](LlamaShears/Core/Abstractions/Events/IEventHandler`1.md)
- [IEventPublisher](LlamaShears/Core/Abstractions/Events/IEventPublisher.md)

## LlamaShears.Core.Abstractions.Events.Agent

- [AgentCompactionMarker](LlamaShears/Core/Abstractions/Events/Agent/AgentCompactionMarker.md)
- [AgentMessageBase](LlamaShears/Core/Abstractions/Events/Agent/AgentMessageBase.md)
- [AgentMessageFragment](LlamaShears/Core/Abstractions/Events/Agent/AgentMessageFragment.md)
- [AgentThoughtFragment](LlamaShears/Core/Abstractions/Events/Agent/AgentThoughtFragment.md)
- [AgentToolCallFragment](LlamaShears/Core/Abstractions/Events/Agent/AgentToolCallFragment.md)
- [AgentToolResultFragment](LlamaShears/Core/Abstractions/Events/Agent/AgentToolResultFragment.md)

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

- [IShearsPaths](LlamaShears/Core/Abstractions/Paths/IShearsPaths.md)
- [PathKind](LlamaShears/Core/Abstractions/Paths/PathKind.md)

## LlamaShears.Core.Abstractions.PromptContext

- [IPromptContextProvider](LlamaShears/Core/Abstractions/PromptContext/IPromptContextProvider.md)
- [PromptContextMemory](LlamaShears/Core/Abstractions/PromptContext/PromptContextMemory.md)
- [PromptContextParameters](LlamaShears/Core/Abstractions/PromptContext/PromptContextParameters.md)

## LlamaShears.Core.Abstractions.Provider

- [AgentProviderOptions](LlamaShears/Core/Abstractions/Provider/AgentProviderOptions.md)
- [IContextCompactor](LlamaShears/Core/Abstractions/Provider/IContextCompactor.md)
- [IContextEntry](LlamaShears/Core/Abstractions/Provider/IContextEntry.md)
- [IEmbeddingModel](LlamaShears/Core/Abstractions/Provider/IEmbeddingModel.md)
- [IEmbeddingProviderFactory](LlamaShears/Core/Abstractions/Provider/IEmbeddingProviderFactory.md)
- [IInferenceRunner](LlamaShears/Core/Abstractions/Provider/IInferenceRunner.md)
- [ILanguageModel](LlamaShears/Core/Abstractions/Provider/ILanguageModel.md)
- [IModelCompletionResponse](LlamaShears/Core/Abstractions/Provider/IModelCompletionResponse.md)
- [IModelResponseFragment](LlamaShears/Core/Abstractions/Provider/IModelResponseFragment.md)
- [IModelTextResponse](LlamaShears/Core/Abstractions/Provider/IModelTextResponse.md)
- [IModelThoughtResponse](LlamaShears/Core/Abstractions/Provider/IModelThoughtResponse.md)
- [IModelToolCallFragment](LlamaShears/Core/Abstractions/Provider/IModelToolCallFragment.md)
- [IProviderFactory](LlamaShears/Core/Abstractions/Provider/IProviderFactory.md)
- [InferenceOutcome](LlamaShears/Core/Abstractions/Provider/InferenceOutcome.md)
- [LanguageModelExtensions](LlamaShears/Core/Abstractions/Provider/LanguageModelExtensions.md)
- [ModelConfiguration](LlamaShears/Core/Abstractions/Provider/ModelConfiguration.md)
- [ModelIdentity](LlamaShears/Core/Abstractions/Provider/ModelIdentity.md)
- [ModelIdentityJsonConverter](LlamaShears/Core/Abstractions/Provider/ModelIdentityJsonConverter.md)
- [ModelIdentityTypeConverter](LlamaShears/Core/Abstractions/Provider/ModelIdentityTypeConverter.md)
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

## LlamaShears.Core.Abstractions.Seeding

- [IDirectorySeeder](LlamaShears/Core/Abstractions/Seeding/IDirectorySeeder.md)

## LlamaShears.Core.Abstractions.SystemPrompt

- [ISystemPromptProvider](LlamaShears/Core/Abstractions/SystemPrompt/ISystemPromptProvider.md)
- [SystemPromptTemplateParameters](LlamaShears/Core/Abstractions/SystemPrompt/SystemPromptTemplateParameters.md)
- [WorkspaceFile](LlamaShears/Core/Abstractions/SystemPrompt/WorkspaceFile.md)

## LlamaShears.Core.Abstractions.Templating

- [ITemplateRenderer](LlamaShears/Core/Abstractions/Templating/ITemplateRenderer.md)

