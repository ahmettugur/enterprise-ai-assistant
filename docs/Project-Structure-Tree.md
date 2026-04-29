# 🏗️ AIApplications - Proje Yapısı (Hexagonal Architecture)



### Resources/
Dashboard generation için kullanılan template dosyaları.
DashboardUseCase tarafından hedef klasöre kopyalanır.


```
AIApplications/
│
├── 📁 AI.Api/                                   # Primary Adapters (Presentation Layer)
│   ├── Program.cs
│   ├── Common/
│   │   ├── SignalRHubContextWrapper.cs
│   │   ├── Exceptions/
│   │   │   └── GlobalExceptionHandler.cs
│   │   └── HealthChecks/
│   │       └── SignalRHealthCheck.cs
│   ├── Configuration/
│   │   └── AuthenticationConfiguration.cs
│   ├── Endpoints/
│   │   ├── Auth/
│   │   │   └── AuthEndpoints.cs
│   │   ├── Common/
│   │   │   └── CommonEndpoints.cs
│   │   ├── Dashboard/
│   │   │   └── DashboardEndpoints.cs
│   │   ├── Documents/
│   │   │   ├── DocumentCategoryEndpoints.cs
│   │   │   ├── DocumentDisplayInfoEndpoints.cs
│   │   │   └── DocumentEndpoints.cs
│   │   ├── Feedback/
│   │   │   └── FeedbackEndpoints.cs
│   │   ├── History/
│   │   │   ├── ConversationEndpoints.cs
│   │   │   └── HistoryEndpoints.cs
│   │   ├── Reports/
│   │   │   ├── AdventureWorksReportEndpoints.cs
│   │   │   └── ScheduledReportEndpoints.cs
│   │   └── Search/
│   │       ├── Neo4jEndpoints.cs
│   │       └── SearchEndpoints.cs
│   ├── Extensions/
│   │   ├── ApiVersioningExtensions.cs
│   │   ├── ConversationServiceExtensions.cs
│   │   ├── DependencyInjectionExtensions.cs
│   │   ├── ExceptionHandlingExtensions.cs
│   │   ├── HealthChecksExtensions.cs
│   │   ├── RateLimitingExtensions.cs
│   │   └── StartupExtensions.cs
│   └── Hubs/
│       └── AIHub.cs
│
├── 📁 AI.Application/                            # Application Layer (Use Cases & Ports)
│   ├── Common/
│   │   ├── Constants/                            # Sabitler
│   │   │   ├── CacheKeys.cs
│   │   │   └── QdrantCollections.cs
│   │   ├── Helpers/
│   │   │   ├── TurkishEncodingHelper.cs
│   │   │   └── Helper.cs
│   │   ├── Resources/
│   │   │   ├── Prompts/                          # LLM prompt şablonları
│   │   │   │   ├── SystemPrompt.cs
│   │   │   │   ├── adventurerworks_schema.md
│   │   │   │   ├── adventurerworks_server_assistant_prompt.md
│   │   │   │   ├── adventurerworks_server_assistant_prompt_v2.md
│   │   │   │   ├── adventurerworks_server_assistant_prompt_v2_old.md
│   │   │   │   ├── chunk_analysis_prompt.md
│   │   │   │   ├── conversation-orchestrator.md
│   │   │   │   ├── dashboard_config_generator_prompt.md
│   │   │   │   ├── dashboard_generator_prompt_adventureworks.md
│   │   │   │   ├── dashboard_generator_prompt_socialmedia.md
│   │   │   │   ├── excel_analysis_plan_prompt.md
│   │   │   │   ├── excel_interpret_prompt.md
│   │   │   │   ├── excel_multi_interpret_prompt.md
│   │   │   │   ├── excel_sql_generator_prompt.md
│   │   │   │   ├── file_analysis_prompt.md
│   │   │   │   ├── insight_analysis_prompt.md
│   │   │   │   ├── react-thought.md
│   │   │   │   ├── socialmedia_chunk_analysis_prompt.md
│   │   │   │   ├── socialmedia_dashboard_config_prompt.md
│   │   │   │   ├── socialmedia_insight_analysis_prompt.md
│   │   │   │   ├── sql_optimization_agent_prompt.md
│   │   │   │   └── sql_validation_agent_prompt.md
│   │   │   ├── Templates/                        # HTML template'leri
│   │   │   ├── assets/
│   │   │   ├── css/
│   │   │   ├── js/
│   │   │   └── alias_config_adventureworks.json
│   │   └── Telemetry/
│   │       ├── ActivitySources.cs
│   │       └── BaggageHelper.cs
│   ├── Configuration/                            # Uygulama Ayarları
│   │   ├── ActiveDirectorySettings.cs
│   │   ├── AdvancedRagSettings.cs
│   │   ├── CacheSettings.cs
│   │   ├── ChatHistorySettings.cs
│   │   ├── DashboardSettings.cs
│   │   ├── InsightAnalysisSettings.cs
│   │   ├── JwtSettings.cs
│   │   ├── LLMSettings.cs
│   │   ├── MultiAgentSettings.cs
│   │   ├── Neo4jSettings.cs
│   │   ├── QdrantSettings.cs
│   │   ├── RateLimitSettings.cs
│   │   └── ReActSettings.cs
│   ├── DTOs/
│   │   ├── AdvancedRag/
│   │   │   ├── MetadataFieldInfo.cs
│   │   │   ├── MetadataFieldType.cs
│   │   │   ├── RerankResult.cs
│   │   │   └── SelfQueryResult.cs
│   │   ├── AgentCore/
│   │   │   └── ActionContext.cs
│   │   ├── Auth/
│   │   │   └── AuthDtos.cs
│   │   ├── Chat/
│   │   │   ├── ChatRequest.cs
│   │   │   └── ResponseModels.cs
│   │   ├── ChatMetadata/
│   │   │   ├── DatabaseInfo.cs
│   │   │   ├── DynamicReportCategory.cs
│   │   │   ├── PromptDocumentCategory.cs
│   │   │   ├── PromptDocumentInfo.cs
│   │   │   ├── ReadyReportInfo.cs
│   │   │   └── ReportTypeInfo.cs
│   │   ├── Dashboard/
│   │   │   ├── DashboardConfig.cs
│   │   │   ├── DashboardDtos.cs
│   │   │   ├── DashboardFiles.cs
│   │   │   └── ParseResult.cs
│   │   ├── Database/
│   │   │   └── DbResponseModel.cs
│   │   ├── DatabaseSchema/
│   │   │   ├── AliasConfiguration.cs
│   │   │   ├── ColumnAlias.cs
│   │   │   └── TableAlias.cs
│   │   ├── DocumentProcessing/
│   │   │   ├── DocumentProcessingResult.cs
│   │   │   └── DocumentUploadDtos.cs
│   │   ├── DynamicPrompt/
│   │   │   └── DynamicPromptResult.cs
│   │   ├── ExcelAnalysis/
│   │   │   ├── AnalysisQueryResult.cs
│   │   │   ├── ColumnInfo.cs
│   │   │   ├── ExcelAnalysisPlan.cs
│   │   │   ├── ExcelAnalysisResult.cs
│   │   │   ├── ExcelQueryResult.cs
│   │   │   └── ExcelSchemaResult.cs
│   │   ├── ReAct/
│   │   │   └── ReActStep.cs
│   │   ├── Feedback/
│   │   │   └── FeedbackDtos.cs
│   │   ├── FeedbackAnalysis/
│   │   │   ├── FeedbackAnalysisResponseDtos.cs
│   │   │   ├── FeedbackAnalysisResult.cs
│   │   │   ├── FeedbackCategory.cs
│   │   │   └── ImprovementSuggestion.cs
│   │   ├── History/
│   │   │   ├── ContextSummarizationSettings.cs
│   │   │   ├── MessageType.cs
│   │   │   └── SummaryCacheEntry.cs
│   │   ├── MessageFeedback/
│   │   │   ├── DailyFeedbackStatistics.cs
│   │   │   └── FeedbackStatistics.cs
│   │   ├── Neo4j/
│   │   │   ├── ColumnInfo.cs
│   │   │   ├── JoinPath.cs
│   │   │   ├── SchemaParseResult.cs
│   │   │   ├── TableInfo.cs
│   │   │   └── TableSchema.cs
│   │   ├── PromptImprovement/
│   │   │   └── PromptImprovementStatistics.cs
│   │   ├── Reports/
│   │   │   └── AdventureWorksDtos.cs
│   │   ├── SchemaCatalog/
│   │   │   └── SchemaCatalogStats.cs
│   │   ├── SparseVector/
│   │   │   └── SparseVectorResult.cs
│   │   ├── AddMessageResultDto.cs
│   │   ├── CategoryDto.cs
│   │   ├── ConversationHistoryDtos.cs
│   │   ├── ConversationDto.cs
│   │   ├── ConversationMetadata.cs
│   │   ├── CurrencyDto.cs
│   │   ├── CustomerTypeDto.cs
│   │   ├── DashboardProcessResult.cs
│   │   ├── DepartmentCategoryDto.cs
│   │   ├── DocumentBase64UploadRequest.cs
│   │   ├── DocumentCategoryDto.cs
│   │   ├── DocumentDisplayInfoDto.cs
│   │   ├── DocumentUploadRequest.cs
│   │   ├── DocumentUploadResponseDto.cs
│   │   ├── InsightAnalysisDtos.cs
│   │   ├── OrderStatusDto.cs
│   │   ├── PagedResult.cs
│   │   ├── ProductDto.cs
│   │   ├── PromotionDto.cs
│   │   ├── SalesPersonDto.cs
│   │   ├── SalesReasonDto.cs
│   │   ├── ScheduledReportDtos.cs
│   │   ├── SearchRequestDto.cs
│   │   ├── SearchResult.cs
│   │   ├── SearchResultDto.cs
│   │   ├── ShipMethodDto.cs
│   │   ├── StoreDto.cs
│   │   ├── TerritoryDto.cs
│   │   └── UpdateConversationTitleDto.cs
│   ├── Extensions/
│   │   └── ApplicationExtensions.cs
│   ├── Ports/
│   │   ├── Primary/
│   │   │   └── UseCases/                        # Input Ports (Driving)
│   │   │       ├── IAIChatUseCase.cs
│   │   │       ├── IAuthUseCase.cs
│   │   │       ├── IContextSummarizationUseCase.cs
│   │   │       ├── IDashboardQueryUseCase.cs
│   │   │       ├── IDashboardUseCase.cs
│   │   │       ├── IDocumentCategoryUseCase.cs
│   │   │       ├── IDocumentDisplayInfoUseCase.cs
│   │   │       ├── IDocumentMetadataUseCase.cs
│   │   │       ├── IDocumentProcessingUseCase.cs
│   │   │       ├── IExcelAnalysisUseCase.cs
│   │   │       ├── IFeedbackAnalysisUseCase.cs
│   │   │       ├── IFeedbackUseCase.cs
│   │   │       ├── IConversationUseCase.cs
│   │   │       ├── IRagSearchUseCase.cs
│   │   │       ├── IReportMetadataUseCase.cs
│   │   │       ├── IRouteConversationUseCase.cs
│   │   │       ├── IScheduledReportUseCase.cs
│   │   │       ├── IReActUseCase.cs
│   │   │       └── IUserMemoryUseCase.cs
│   │   └── Secondary/                           # Output Ports (Driven)
│   │       ├── Notifications/
│   │       │   ├── IEmailNotificationService.cs
│   │       │   ├── INotificationService.cs
│   │       │   └── ITeamsNotificationService.cs
│   │       ├── Scheduling/
│   │       │   ├── IJobScheduler.cs
│   │       │   └── ISchedulerDataService.cs
│   │       └── Services/
│   │           ├── AIChat/
│   │           │   ├── IDynamicPromptBuilder.cs
│   │           │   ├── IReranker.cs
│   │           │   └── ISelfQueryExtractor.cs
│   │           ├── Auth/
│   │           │   ├── ICurrentUserService.cs
│   │           │   └── ITokenService.cs
│   │           ├── Cache/
│   │           │   └── IChatCacheService.cs
│   │           ├── Common/
│   │           │   ├── IFileSaver.cs
│   │           │   └── ISignalRHubContext.cs
│   │           ├── Database/
│   │           │   ├── IDatabaseSchemaReader.cs
│   │           │   ├── IDatabaseService.cs
│   │           │   ├── ISchemaGraphService.cs
│   │           │   ├── ISchemaParserService.cs
│   │           │   ├── ISqlAgentPipeline.cs
│   │           │   ├── ISqlOptimizationAgent.cs
│   │           │   ├── ISqlServerConnectionFactory.cs
│   │           │   └── ISqlValidationAgent.cs
│   │           ├── Document/
│   │           │   ├── IDocumentCacheService.cs
│   │           │   ├── IDocumentParser.cs
│   │           │   ├── IDocumentTextExtractor.cs
│   │           │   ├── IJsonQuestionAnswerParser.cs
│   │           │   └── ITextChunker.cs
│   │           ├── Report/
│   │           │   ├── IAdventureWorksReadyReportService.cs
│   │           │   ├── IDashboardParser.cs
│   │           │   ├── IExcelAnalysisService.cs
│   │           │   └── IReportService.cs
│   │           └── Vector/
│   │               ├── IEmbeddingService.cs
│   │               ├── IQdrantService.cs
│   │               └── ISparseVectorService.cs
│   │           ├── AgentCore/                       # Agent Registry Interfaces
│   │           │   ├── IActionAgent.cs
│   │           │   └── IActionAgentRegistry.cs
│   │           └── Query/                           # Application-level Query Services (DTO döndüren sorgular)
│   │               ├── IFeedbackQueryService.cs
│   │               ├── IConversationQueryService.cs
│   │               └── IPromptImprovementQueryService.cs
│   ├── Results/
│   │   ├── IResult.cs
│   │   ├── Result.cs
│   │   └── ResultBase.cs
│   └── UseCases/                                # Use Case Implementations
│       ├── AIChatUseCase.cs
│       ├── AuthUseCase.cs
│       ├── ContextSummarizationUseCase.cs
│       ├── DashboardQueryUseCase.cs
│       ├── DashboardUseCase.cs
│       ├── DocumentCategoryUseCase.cs
│       ├── DocumentDisplayInfoUseCase.cs
│       ├── DocumentMetadataUseCase.cs
│       ├── DocumentProcessingUseCase.cs
│       ├── ExcelAnalysisUseCase.cs
│       ├── FeedbackAnalysisUseCase.cs
│       ├── FeedbackUseCase.cs
│       ├── ConversationUseCase.cs
│       ├── RagSearchUseCase.cs
│       ├── ReportMetadataUseCase.cs
│       ├── RouteConversationUseCase.cs
│       ├── ScheduledReportUseCase.cs
│       ├── ReActUseCase.cs
│       ├── UserMemoryUseCase.cs
│       └── ActionAgents/                            # Agent Implementations
│           ├── ActionAgentRegistry.cs
│           ├── ChatActionAgent.cs
│           ├── DocumentActionAgent.cs
│           ├── ReportActionAgent.cs
│           └── AskActionAgent.cs
│
├── 📁 AI.Domain/                                 # Domain Layer (Modül Bazlı — Aggregate-per-Folder)
│   ├── Common/                                    # DDD Building Blocks
│   │   ├── Entity.cs                              # Entity<TId> base class
│   │   ├── AggregateRoot.cs                       # AggregateRoot<TId> base class (domain events, IHasDomainEvents)
│   │   ├── ValueObject.cs                         # ValueObject base class
│   │   ├── IDomainEvent.cs                        # Domain event marker interface
│   │   ├── IDomainEventDispatcher.cs              # Domain event dispatcher interface
│   │   └── IHasDomainEvents.cs                    # Marker interface for aggregate roots with events
│   ├── Conversations/                             # Aggregate: Conversation
│   │   ├── Conversation.cs                        # AggregateRoot
│   │   ├── Message.cs                             # Entity (child of Conversation)
│   │   └── IConversationRepository.cs              # Repository interface
│   ├── Identity/                                  # Aggregate: User
│   │   ├── User.cs                                # AggregateRoot
│   │   ├── Role.cs                                # AggregateRoot
│   │   ├── RefreshToken.cs                        # Entity (child of User)
│   │   ├── UserRole.cs                            # Entity (child of User)
│   │   ├── IUserRepository.cs                     # Repository interface (User + RefreshToken + Role assignment ops)
│   │   └── IRoleRepository.cs                     # Repository interface
│   ├── Feedback/                                  # Aggregate: MessageFeedback
│   │   ├── MessageFeedback.cs                     # AggregateRoot
│   │   ├── FeedbackAnalysisReport.cs              # AggregateRoot
│   │   ├── PromptImprovement.cs                   # Entity (child of FeedbackAnalysisReport)
│   │   ├── IMessageFeedbackRepository.cs          # Repository interface
│   │   └── IFeedbackAnalysisReportRepository.cs   # Repository interface (Report + PromptImprovement ops)
│   ├── Memory/                                    # Aggregate: UserMemory
│   │   ├── UserMemory.cs                          # AggregateRoot
│   │   └── IUserMemoryRepository.cs               # Repository interface
│   ├── Documents/                                 # Aggregate: DocumentCategory
│   │   ├── DocumentMetadata.cs                    # Entity (in-memory kullanılır, persist edilmez)
│   │   ├── DocumentChunk.cs                       # Entity (child of DocumentMetadata)
│   │   ├── DocumentCategory.cs                    # AggregateRoot (parent of DocumentDisplayInfo)
│   │   ├── DocumentDisplayInfo.cs                 # Entity (child of DocumentCategory)
│   │   └── IDocumentCategoryRepository.cs         # Repository interface (Category + DocumentDisplayInfo ops)
│   ├── Scheduling/                                # Aggregate: ScheduledReport
│   │   ├── ScheduledReport.cs                     # AggregateRoot
│   │   ├── ScheduledReportLog.cs                  # Entity (child of ScheduledReport)
│   │   └── IScheduledReportRepository.cs          # Repository interface
│   ├── Enums/
│   │   ├── AuthenticationSource.cs
│   │   ├── DocumentProcessingStatus.cs
│   │   ├── DocumentType.cs
│   │   ├── FeedbackType.cs
│   │   ├── MemoryCategory.cs
│   │   └── PromptImprovementStatus.cs
│   ├── Events/
│   │   └── DomainEvents.cs                        # 6 domain event (record types)
│   ├── Exceptions/
│   │   ├── DomainException.cs                     # Base exception (Code property)
│   │   ├── ConversationArchivedException.cs
│   │   ├── InvalidEntityStateException.cs
│   │   ├── InvalidPasswordOperationException.cs
│   └── ValueObjects/
│       ├── Confidence.cs                          # 0-1 bounded skor
│       ├── DateRange.cs                           # CreatedAt/UpdatedAt çifti
│       ├── Email.cs                               # Format validasyonu, normalize
│       ├── FileInfo.cs                            # Dosya metadata grubu
│       └── Password.cs                            # Hash+salt kapsülleme
│
├── 📁 AI.Infrastructure/                         # Secondary Adapters (Infrastructure)
│   ├── Adapters/
│   │   ├── AI/                                  # AI Service Adapters
│   │   │   ├── Agents/
│   │   │   │   └── SqlAgents/
│   │   │   │       ├── SqlAgentPipeline.cs
│   │   │   │       ├── SqlOptimizationAgent.cs
│   │   │   │       └── SqlValidationAgent.cs
│   │   │   ├── Common/
│   │   │   │   ├── DashboardFileSaver.cs
│   │   │   │   └── DashboardResponseParser.cs
│   │   │   ├── DocumentServices/
│   │   │   │   ├── JsonQuestionAnswerParser.cs
│   │   │   │   ├── OpenAIEmbeddingService.cs
│   │   │   │   ├── PdfDocumentParser.cs
│   │   │   │   ├── RecursiveCharacterTextSplitter.cs
│   │   │   │   ├── TextChunker.cs
│   │   │   │   ├── TextDocumentParser.cs
│   │   │   │   └── DocumentTextExtractor.cs
│   │   │   ├── ExcelServices/
│   │   │   │   └── DuckDbExcelService.cs
│   │   │   ├── Neo4j/
│   │   │   │   ├── DatabaseSchemaReader.cs
│   │   │   │   ├── DynamicPromptBuilder.cs
│   │   │   │   ├── SchemaGraphService.cs
│   │   │   │   └── SchemaParserService.cs
│   │   │   ├── ReadyReports/
│   │   │   │   └── AdventureWorks/
│   │   │   │       └── AdventureWorksReadyReportService.cs
│   │   │   ├── Reports/
│   │   │   │   └── SqlServer/
│   │   │   │       ├── AdventureWorksReportService.cs
│   │   │   │       ├── SocialMediaReportService.cs
│   │   │   │       └── SqlServerReportServiceBase.cs
│   │   │   ├── Reranking/
│   │   │   │   └── LLMReranker.cs
│   │   │   ├── SelfQuery/
│   │   │   │   └── SelfQueryExtractor.cs
│   │   │   └── VectorServices/
│   │   │       ├── QdrantService.cs
│   │   │       └── SparseVectorService.cs
│   │   ├── External/                            # External Service Adapters
│   │   │   ├── Auth/
│   │   │   │   ├── CurrentUserService.cs
│   │   │   │   └── TokenService.cs
│   │   │   ├── Caching/
│   │   │   │   ├── DocumentCacheService.cs
│   │   │   │   ├── InMemoryCacheService.cs
│   │   │   │   └── RedisCacheService.cs
│   │   │   ├── DatabaseServices/
│   │   │   │   └── SqlServer/
│   │   │   │       ├── SqlServerConnectionFactory.cs
│   │   │   │       └── SqlServerDatabaseService.cs
│   │   │   ├── Notifications/
│   │   │   │   ├── EmailNotificationService.cs
│   │   │   │   ├── NotificationService.cs
│   │   │   │   └── TeamsNotificationService.cs
│   │   │   └── Scheduling/
│   │   │       ├── HangfireJobScheduler.cs
│   │   │       └── SchedulerDataService.cs
│   │   └── Persistence/                         # Database Adapters
│   │       ├── ChatDbContext.cs                   # SaveChangesAsync override — domain event dispatch
│   │       ├── DomainEventDispatcher.cs            # IDomainEventDispatcher impl (DI-based handler resolution)
│   │       ├── Configurations/
│   │       │   ├── ConversationConfiguration.cs
│   │       │   ├── DocumentCategoryConfiguration.cs
│   │       │   ├── DocumentDisplayInfoConfiguration.cs
│   │       │   ├── FeedbackAnalysisReportConfiguration.cs
│   │       │   ├── MessageConfiguration.cs
│   │       │   ├── MessageFeedbackConfiguration.cs
│   │       │   ├── PromptImprovementConfiguration.cs
│   │       │   ├── RefreshTokenConfiguration.cs
│   │       │   ├── RoleConfiguration.cs
│   │       │   ├── ScheduledReportConfiguration.cs
│   │       │   ├── ScheduledReportLogConfiguration.cs
│   │       │   ├── UserConfiguration.cs
│   │       │   ├── UserMemoryConfiguration.cs
│   │       │   └── UserRoleConfiguration.cs
│   │       ├── Migrations/
│   │       │   └── (Migration files...)
│   │       └── Repositories/
│   │           ├── DocumentCategoryRepository.cs
│   │           ├── FeedbackAnalysisReportRepository.cs
│   │           ├── FeedbackQueryService.cs            # CQRS: IFeedbackQueryService impl
│   │           ├── ConversationQueryService.cs        # CQRS: IConversationQueryService impl (PostgreSQL)
│   │           ├── InMemoryConversationQueryService.cs # CQRS: IConversationQueryService impl (InMemory)
│   │           ├── InMemoryConversationRepository.cs
│   │           ├── InMemoryMessageFeedbackRepository.cs
│   │           ├── InMemoryUserRepository.cs
│   │           ├── MessageFeedbackRepository.cs
│   │           ├── PostgreSqlConversationRepository.cs
│   │           ├── PromptImprovementQueryService.cs    # CQRS: IPromptImprovementQueryService impl
│   │           ├── RoleRepository.cs
│   │           ├── ScheduledReportRepository.cs
│   │           ├── UserMemoryRepository.cs
│   │           └── UserRepository.cs
│   ├── Configuration/
│   │   ├── EmailSettings.cs
│   │   └── TeamsSettings.cs
│   ├── Extensions/
│   │   ├── AuthenticationServiceExtensions.cs
│   │   ├── DatabaseSeederExtensions.cs
│   │   ├── InfrastructureExtensions.cs
│   │   ├── Neo4jExtensions.cs
│   │   ├── OpenTelemetryExtensions.cs
│   │   └── SqlAgentExtensions.cs
│   └── Logging/
│       ├── LoggingExtension.cs
│       └── LoggingHelper.cs
│
├── 📁 AI.Scheduler/                              # Background Jobs (Hangfire)
│   ├── Program.cs
│   ├── Configuration/
│   │   ├── HangfireSettings.cs
│   │   └── ScheduledReportSettings.cs
│   ├── Extensions/
│   │   ├── HangfireExtensions.cs
│   │   └── LLMExtensions.cs
│   └── Jobs/
│       ├── FeedbackAnalysisJob.cs
│       ├── ReportSchedulerJob.cs
│       └── ScheduledReportJob.cs
│
└── 📁 frontend/                                  # Angular 21 SPA
    └── src/app/
        ├── core/services/
        ├── pages/
        │   ├── chat/
        │   ├── dashboard/
        │   └── login/
        └── shared/
            ├── components/
            └── sidebar/
```

---

## 📊 Özet İstatistikler

| Proje | Klasör Sayısı | Dosya Sayısı | Ana Sorumluluk |
|-------|---------------|--------------|----------------|
| **AI.Api** | 12 | 26 | HTTP Endpoints, SignalR Hub |
| **AI.Application** | 33 | 163+ | Use Cases, Ports, DTOs, Configuration |
| **AI.Domain** | 12 | 51 | Conversations, Identity, Feedback, Memory, Documents, Scheduling + Enums, Events, Exceptions, ValueObjects |
| **AI.Infrastructure** | 22 | 84+ | Adapters (DB, AI, External, Notifications), CQRS Query Services |
| **AI.Scheduler** | 4 | 8 | Background Jobs |

---

## 📋 Detaylı Dosya Açıklamaları

### 🔵 AI.Domain - Domain Katmanı

> **Modül Bazlı Organizasyon (Aggregate-per-Folder)**: Her aggregate root, entity'leri ve repository interface'leri ile birlikte aynı klasörde yaşar.

#### Common (DDD Building Blocks)

| Dosya Adı | Gerekçe | Açıklama |
|-----------|---------|----------|
| `Entity.cs` | Base Class | Tüm entity'ler için generic base class. `Entity<TId>` — kimlik bazlı eşitlik sağlar. |
| `AggregateRoot.cs` | Base Class | Aggregate Root base class. `AggregateRoot<TId>` — domain event koleksiyonu, `IHasDomainEvents` impl. |
| `ValueObject.cs` | Base Class | Value Object base class. Değer bazlı eşitlik ve immutability sağlar. |
| `IDomainEvent.cs` | Interface | Domain event marker interface'i. `OccurredOn` property. |
| `IDomainEventDispatcher.cs` | Interface | Domain event dispatch interface'i. `DispatchEventsAsync()` — Infrastructure'da implement edilir. |
| `IHasDomainEvents.cs` | Interface | Marker interface. Aggregate root'ların event taşıdığını belirtir, `SaveChangesAsync` override tarafından kullanılır. |

#### Conversations/ (Aggregate: Conversation)

| Dosya Adı | DDD Rolü | Açıklama |
|-----------|----------|----------|
| `Conversation.cs` | **Aggregate Root** | Chat oturumunu temsil eder. Factory method pattern kullanır. |
| `Message.cs` | Entity (child) | Tek bir chat mesajını temsil eder. Soft delete destekler. |
| `IConversationRepository.cs` | Repository Interface | Conversation/Message CRUD ve aggregate root persistence. |

#### Identity/ (Aggregate: User)

| Dosya Adı | DDD Rolü | Açıklama |
|-----------|----------|----------|
| `User.cs` | **Aggregate Root** | Kullanıcı entity'si. AD ve local auth destekler. |
| `Role.cs` | **Aggregate Root** | Kullanıcı rollerini tanımlar (Admin, User, vb.). `AggregateRoot<string>` türetir. |
| `RefreshToken.cs` | Entity (child) | JWT refresh token'larını saklar. `Create()` `internal`. |
| `UserRole.cs` | Entity (child) | User-Role many-to-many bridge tablo. |
| `IUserRepository.cs` | Repository Interface | User CRUD, role assignment, RefreshToken operations. |
| `IRoleRepository.cs` | Repository Interface | Role CRUD. |

#### Feedback/ (Aggregate: MessageFeedback)

| Dosya Adı | DDD Rolü | Açıklama |
|-----------|----------|----------|
| `MessageFeedback.cs` | **Aggregate Root** | Kullanıcı geri bildirimi (thumbs up/down). |
| `FeedbackAnalysisReport.cs` | **Aggregate Root** | AI feedback analiz raporları. `AddImprovement()` ile boundary zorlar. |
| `PromptImprovement.cs` | Entity (child) | Prompt iyileştirme önerileri. `Create()` `internal`. |
| `IMessageFeedbackRepository.cs` | Repository Interface | Feedback CRUD (istatistikler `IFeedbackQueryService`'de). |
| `IFeedbackAnalysisReportRepository.cs` | Repository Interface | Feedback analiz raporu + PromptImprovement CRUD (istatistikler `IPromptImprovementQueryService`'de). |

#### Memory/ (Aggregate: UserMemory)

| Dosya Adı | DDD Rolü | Açıklama |
|-----------|----------|----------|
| `UserMemory.cs` | **Aggregate Root** | Kullanıcı long-term memory. Kişiselleştirilmiş yanıtlar. |
| `IUserMemoryRepository.cs` | Repository Interface | Memory CRUD. |

#### Documents/ (Aggregate: DocumentCategory)

| Dosya Adı | DDD Rolü | Açıklama |
|-----------|----------|----------|
| `DocumentMetadata.cs` | **Entity** | Doküman metadata. In-memory kullanılır (Qdrant pipeline). `AddChunk()` ile chunk yönetimi. |
| `DocumentChunk.cs` | Entity (child) | Doküman chunk'ları. `Create()` `internal`. |
| `DocumentCategory.cs` | **Aggregate Root** | Doküman kategorileri. `AddDocument()` ile boundary zorlar. |
| `DocumentDisplayInfo.cs` | Entity (child) | Doküman UI görüntüleme bilgileri. `Create()` `internal`. |
| `IDocumentCategoryRepository.cs` | Repository Interface | Kategori + DocumentDisplayInfo CRUD. |

#### Scheduling/ (Aggregate: ScheduledReport)

| Dosya Adı | DDD Rolü | Açıklama |
|-----------|----------|----------|
| `ScheduledReport.cs` | **Aggregate Root** | Zamanlanmış raporlar. `AddLog()` ile boundary zorlar. |
| `ScheduledReportLog.cs` | Entity (child) | Rapor çalışma logları. `Create()` `internal`. |
| `IScheduledReportRepository.cs` | Repository Interface | Scheduled report CRUD. |

#### Enums (6 dosya)

| Dosya Adı | Açıklama |
|-----------|----------|
| `AuthenticationSource.cs` | Local, ActiveDirectory. |
| `DocumentProcessingStatus.cs` | Pending, Processing, Completed, Failed. |
| `DocumentType.cs` | Document, QuestionAnswer. |
| `FeedbackType.cs` | Positive, Negative. |
| `MemoryCategory.cs` | Preference, Interaction, Feedback, WorkContext. |
| `PromptImprovementStatus.cs` | Pending, UnderReview, Applied, Rejected. |

#### Events (1 dosya)

| Dosya Adı | Açıklama |
|-----------|----------|
| `DomainEvents.cs` | 6 domain event (record type): `ConversationCreatedEvent`, `MessageAddedEvent`, `ConversationArchivedEvent`, `UserCreatedEvent`, `UserLockedOutEvent`, `FeedbackSubmittedEvent` |

#### Exceptions (4 dosya)

| Dosya Adı | Açıklama |
|-----------|----------|
| `DomainException.cs` | Abstract base class. `Code` property. |
| `ConversationArchivedException.cs` | Arşivlenmiş conversation'a mesaj ekleme. |
| `InvalidPasswordOperationException.cs` | AD kullanıcısında şifre işlemi. |
| `InvalidEntityStateException.cs` | Geçersiz entity durumu. |

#### ValueObjects (5 dosya)

| Dosya Adı | Açıklama |
|-----------|----------|
| `Email.cs` | Email format validasyonu ve normalize. |
| `Password.cs` | Hash+salt kapsülleme. `Create()` ve `Verify()`. |
| `DateRange.cs` | CreatedAt/UpdatedAt çifti. |
| `FileInfo.cs` | Dosya metadata grubu. |
| `Confidence.cs` | 0.0–1.0 bounded skor. |

---

### 🟢 AI.Application - Uygulama Katmanı

#### UseCases

| Dosya Adı | Konum | Açıklama |
|-----------|-------|---------|
| `AIChatUseCase.cs` | UseCases/ | AI chat. Streaming yanıt, vector search, dosya işleme. Excel analizi ExcelAnalysisUseCase'e delege edilir. |
| `ExcelAnalysisUseCase.cs` | UseCases/ | Excel/CSV dosya analizi. DuckDB ile SQL üretimi, LLM analiz planı, retry mekanizması, SignalR streaming. |
| `RouteConversationUseCase.cs` | UseCases/ | İstek yönlendirme: Chat/Document/Report/Ask. LLM ile intent classification. |
| `AuthUseCase.cs` | UseCases/ | Login, register, token refresh. AD ve local auth. |
| `ConversationUseCase.cs` | UseCases/ | Conversation CRUD, mesaj listeleme, arşivleme. |
| `DocumentProcessingUseCase.cs` | UseCases/ | Doküman parsing, chunking, embedding üretimi, vector store kayıt. |
| `RagSearchUseCase.cs` | UseCases/ | RAG araması. Hybrid search (dense + sparse). |
| `DashboardUseCase.cs` | UseCases/ | Dashboard oluşturma. AI-generated konfigürasyon. |
| `DashboardQueryUseCase.cs` | UseCases/ | Dashboard sorgulama ve filtreleme. |
| `FeedbackUseCase.cs` | UseCases/ | Kullanıcı geri bildirimi CRUD ve istatistikler. |
| `FeedbackAnalysisUseCase.cs` | UseCases/ | AI ile feedback kategorizasyon ve iyileştirme önerileri. |
| `ScheduledReportUseCase.cs` | UseCases/ | Zamanlanmış rapor CRUD ve aktivasyon. |
| `DocumentCategoryUseCase.cs` | UseCases/ | Doküman kategori CRUD, cache, validasyon. |
| `DocumentDisplayInfoUseCase.cs` | UseCases/ | Doküman görüntüleme bilgisi CRUD. |
| `DocumentMetadataUseCase.cs` | UseCases/ | Doküman metadata yönetimi ve arama. |
| `ReportMetadataUseCase.cs` | UseCases/ | Rapor tipi ve veritabanı listeleme. |
| `ReActUseCase.cs` | UseCases/ | Merkezi ReAct servisi. LLM thought üretimi, SignalR adım gönderimi. |
| `UserMemoryUseCase.cs` | UseCases/ | Kullanıcı long-term memory CRUD. |
| `ContextSummarizationUseCase.cs` | UseCases/ | Uzun chat geçmişi özetleme, context compression. |

#### Configuration (17 dosya)

| Dosya Adı | Açıklama |
|-----------|----------|
| `ActiveDirectorySettings.cs` | AD ayarları: domain, LDAP bağlantısı. |
| `AdvancedRagSettings.cs` | Reranking, self-query, hybrid search parametreleri. |
| `CacheSettings.cs` | Redis connection string, expiration süreleri. |
| `ChatHistorySettings.cs` | Max mesaj sayısı, context pencere boyutu. |
| `DashboardSettings.cs` | Output klasörü, fast dashboard modu, template seçenekleri. |
| `InsightAnalysisSettings.cs` | Chunk boyutu, paralel işleme, analiz derinliği. |
| `JwtSettings.cs` | Secret key, issuer, audience, expiration. |
| `LLMSettings.cs` | Model adı, temperature, max tokens, API endpoint. |
| `MultiAgentSettings.cs` | Agent sayısı, pipeline konfigürasyonu. |
| `Neo4jSettings.cs` | Neo4j URI, credentials. |
| `QdrantSettings.cs` | Host, port, collection parametreleri. |
| `RateLimitSettings.cs` | Pencere boyutu, istek limitleri. |
| `ReActSettings.cs` | Enabled, VerboseLogging, SendStepsToFrontend. |

#### Common (29 dosya)

| Dosya Adı | Konum | Açıklama |
|-----------|-------|---------|
| `CacheKeys.cs` | Common/Constants/ | Cache key sabitleri. |
| `QdrantCollections.cs` | Common/Constants/ | Qdrant collection adı sabitleri. |
| `TurkishEncodingHelper.cs` | Common/Helpers/ | Türkçe karakter encoding, UTF-8 normalizasyonu. |
| `Helper.cs` | Common/Helpers/ | Genel yardımcı metotlar. |
| `ActivitySources.cs` | Common/Telemetry/ | OpenTelemetry activity sources. |
| `BaggageHelper.cs` | Common/Telemetry/ | OpenTelemetry baggage yönetimi. |
| `SystemPrompt.cs` | Common/Resources/Prompts/ | Sistem prompt sabitleri. |
| `excel_analysis_plan_prompt.md` | Common/Resources/Prompts/ | Çoklu SQL analiz planı üretme. |
| `excel_interpret_prompt.md` | Common/Resources/Prompts/ | Tek sonuç yorumlama + grafik üretimi. |
| `excel_multi_interpret_prompt.md` | Common/Resources/Prompts/ | Çoklu sonuç birleştirme ve rapor yorumlama. |
| `excel_sql_generator_prompt.md` | Common/Resources/Prompts/ | LLM'den SQL üretimi, güvenlik kuralları, JSON çıktı. |
| `adventurerworks_schema.md` | Common/Resources/Prompts/ | AdventureWorks veritabanı şema bilgisi. |
| `adventurerworks_server_assistant_prompt.md` | Common/Resources/Prompts/ | SQL Server asistan prompt. |
| `adventurerworks_server_assistant_prompt_v2.md` | Common/Resources/Prompts/ | SQL Server asistan prompt v2. |
| `adventurerworks_server_assistant_prompt_v2_old.md` | Common/Resources/Prompts/ | SQL Server asistan prompt v2 (eski). |
| `chunk_analysis_prompt.md` | Common/Resources/Prompts/ | Doküman chunk analiz prompt. |
| `conversation-orchestrator.md` | Common/Resources/Prompts/ | Conversation orchestrator intent classification. |
| `dashboard_config_generator_prompt.md` | Common/Resources/Prompts/ | Dashboard config üretme prompt. |
| `dashboard_generator_prompt_adventureworks.md` | Common/Resources/Prompts/ | AW dashboard HTML üretme. |
| `dashboard_generator_prompt_socialmedia.md` | Common/Resources/Prompts/ | Sosyal medya dashboard üretme. |
| `file_analysis_prompt.md` | Common/Resources/Prompts/ | Dosya analiz prompt. |
| `insight_analysis_prompt.md` | Common/Resources/Prompts/ | İçgörü analiz prompt. |
| `react-thought.md` | Common/Resources/Prompts/ | ReAct thought üretim prompt. |
| `socialmedia_chunk_analysis_prompt.md` | Common/Resources/Prompts/ | Sosyal medya chunk analiz prompt. |
| `socialmedia_dashboard_config_prompt.md` | Common/Resources/Prompts/ | Sosyal medya dashboard config prompt. |
| `socialmedia_insight_analysis_prompt.md` | Common/Resources/Prompts/ | Sosyal medya içgörü analiz prompt. |
| `sql_optimization_agent_prompt.md` | Common/Resources/Prompts/ | SQL optimizasyon agent prompt. |
| `sql_validation_agent_prompt.md` | Common/Resources/Prompts/ | SQL validasyon agent prompt. |

#### DTOs (80 dosya)

| Dosya Adı | Konum | Açıklama |
|-----------|-------|---------|
| `AuthDtos.cs` | DTOs/Auth/ | Login/register request ve response modelleri. |
| `ActionContext.cs` | DTOs/AgentCore/ | Agent eylem bağlamı: ConversationId, ChatHistory, vb. |
| `ChatRequest.cs` | DTOs/Chat/ | Prompt, conversation ID, dosya, mod seçimi. |
| `ResponseModels.cs` | DTOs/Chat/ | Streaming response, completion modelleri. |
| `AddMessageResultDto.cs` | DTOs/ | Mesaj ekleme sonucu: başarı, message ID. |
| `DatabaseInfo.cs` | DTOs/ChatMetadata/ | Veritabanı connection, schema bilgisi. |
| `DynamicReportCategory.cs` | DTOs/ChatMetadata/ | Dinamik rapor kategorisi. |
| `PromptDocumentCategory.cs` | DTOs/ChatMetadata/ | Prompt doküman kategorisi. |
| `PromptDocumentInfo.cs` | DTOs/ChatMetadata/ | Prompt doküman: id, title, content. |
| `ReadyReportInfo.cs` | DTOs/ChatMetadata/ | Hazır rapor: ad, açıklama, parametreler. |
| `ReportTypeInfo.cs` | DTOs/ChatMetadata/ | Rapor tipi: veritabanı, servis tipi. |
| `DashboardConfig.cs` | DTOs/Dashboard/ | KPI, grafik, tablo, filtre konfigürasyonu. |
| `DashboardDtos.cs` | DTOs/Dashboard/ | Dashboard list, detail, create, update. |
| `DashboardFiles.cs` | DTOs/Dashboard/ | Dashboard dosya path'leri: HTML, CSS, JS. |
| `ParseResult.cs` | DTOs/Dashboard/ | Dashboard parse sonucu. |
| `DashboardProcessResult.cs` | DTOs/ | Dashboard işleme status ve output path. |
| `DbResponseModel.cs` | DTOs/Database/ | SQL sorgu yanıtı: SQL, data, metadata. |
| `AliasConfiguration.cs` | DTOs/DatabaseSchema/ | Tablo/kolon alias konfigürasyonu. |
| `ColumnAlias.cs` | DTOs/DatabaseSchema/ | Kolon alias tanımı. |
| `TableAlias.cs` | DTOs/DatabaseSchema/ | Tablo alias tanımı. |
| `DocumentBase64UploadRequest.cs` | DTOs/ | Base64 doküman yükleme isteği. |
| `DocumentCategoryDto.cs` | DTOs/ | Doküman kategori bilgileri. |
| `DocumentDisplayInfoDto.cs` | DTOs/ | Doküman görüntüleme bilgileri. |
| `DocumentProcessingResult.cs` | DTOs/DocumentProcessing/ | İşleme sonucu: chunk sayısı, embeddings. |
| `DocumentUploadDtos.cs` | DTOs/DocumentProcessing/ | Upload DTO'ları: `DocumentUploadDto`, `DocumentUploadResultDto`. Entity oluşturma UseCase'de yapılır. |
| `DocumentUploadRequest.cs` | DTOs/ | Doküman yükleme: file, category. |
| `DocumentUploadResponseDto.cs` | DTOs/ | Yükleme yanıtı: id, status. |
| `AnalysisQueryResult.cs` | DTOs/ExcelAnalysis/ | Multi-query sonucu: Title, SQL, QueryResult. |
| `ColumnInfo.cs` | DTOs/ExcelAnalysis/ | Excel kolon: name, type. |
| `ExcelAnalysisPlan.cs` | DTOs/ExcelAnalysis/ | LLM analiz planı: AnalysisType, Queries. |
| `ExcelAnalysisResult.cs` | DTOs/ExcelAnalysis/ | Excel analiz: schema, data preview. |
| `ExcelQueryResult.cs` | DTOs/ExcelAnalysis/ | Excel sorgu: rows, columns. |
| `ExcelSchemaResult.cs` | DTOs/ExcelAnalysis/ | Excel schema: sheets, columns. |
| `ReActStep.cs` | DTOs/ReAct/ | ReAct adım: StepNumber, StepType, Content. SignalR ile gönderilir. |
| `InsightAnalysisDtos.cs` | DTOs/ | İçgörü analizi modelleri. |
| `FeedbackDtos.cs` | DTOs/Feedback/ | Feedback create, response modelleri. |
| `FeedbackAnalysisResult.cs` | DTOs/FeedbackAnalysis/ | Feedback analiz sonucu. |
| `FeedbackAnalysisResponseDtos.cs` | DTOs/FeedbackAnalysis/ | API yanıt DTO'ları: `FeedbackAnalysisResponseDto`, `CategoryResponseDto`, `SuggestionResponseDto`, `AddFeedbackRequest`. |
| `FeedbackCategory.cs` | DTOs/FeedbackAnalysis/ | Feedback kategori bilgisi. |
| `ImprovementSuggestion.cs` | DTOs/FeedbackAnalysis/ | İyileştirme önerisi. |
| `DailyFeedbackStatistics.cs` | DTOs/MessageFeedback/ | Günlük feedback istatistikleri. |
| `FeedbackStatistics.cs` | DTOs/MessageFeedback/ | Genel feedback istatistikleri. |
| `ContextSummarizationSettings.cs` | DTOs/History/ | Context özetleme: max tokens. |
| `MessageType.cs` | DTOs/History/ | Mesaj tipi enum. |
| `SummaryCacheEntry.cs` | DTOs/History/ | Özet cache entry. |
| `ConversationDto.cs` | DTOs/ | Conversation: id, title, messages. |
| `ConversationHistoryDtos.cs` | DTOs/ | History query DTO'ları: `ConversationListResultDto`, `ConversationDetailResultDto`, `ConversationStatsDto` vb. |
| `ConversationMetadata.cs` | DTOs/ | Conversation metadata: title, message count, timestamps. |
| `UpdateConversationTitleDto.cs` | DTOs/ | Başlık güncelleme. |
| `ColumnInfo.cs` | DTOs/Neo4j/ | Neo4j kolon bilgisi. |
| `JoinPath.cs` | DTOs/Neo4j/ | Tablo join yolu. |
| `SchemaParseResult.cs` | DTOs/Neo4j/ | Schema parse sonucu. |
| `TableInfo.cs` | DTOs/Neo4j/ | Tablo: name, columns. |
| `TableSchema.cs` | DTOs/Neo4j/ | Tablo schema detayları. |
| `AdventureWorksDtos.cs` | DTOs/Reports/ | AdventureWorks rapor modelleri. |
| `ScheduledReportDtos.cs` | DTOs/ | Zamanlanmış rapor modelleri. |
| `SearchRequestDto.cs` | DTOs/ | Arama: query, filters. |
| `SearchResult.cs` | DTOs/ | Vector search: content, score. |
| `SearchResultDto.cs` | DTOs/ | Arama sonucu. |
| `MetadataFieldInfo.cs` | DTOs/AdvancedRag/ | Metadata alan bilgisi. |
| `MetadataFieldType.cs` | DTOs/AdvancedRag/ | Metadata alan tipi. |
| `RerankResult.cs` | DTOs/AdvancedRag/ | Rerank: score, position. |
| `SelfQueryResult.cs` | DTOs/AdvancedRag/ | Self-query: filter, search params. |
| `DynamicPromptResult.cs` | DTOs/DynamicPrompt/ | Dinamik prompt sonucu. |
| `SparseVectorResult.cs` | DTOs/SparseVector/ | Sparse vector: indices, values. |
| `CategoryDto.cs` | DTOs/ | Genel kategori. |
| `CurrencyDto.cs` | DTOs/ | Para birimi. |
| `CustomerTypeDto.cs` | DTOs/ | Müşteri tipi. |
| `DepartmentCategoryDto.cs` | DTOs/ | Departman kategorisi. |
| `OrderStatusDto.cs` | DTOs/ | Sipariş durumu. |
| `PagedResult.cs` | DTOs/ | Sayfalı sonuç: items, total, page. |
| `ProductDto.cs` | DTOs/ | Ürün. |
| `PromotionDto.cs` | DTOs/ | Promosyon. |
| `PromptImprovementStatistics.cs` | DTOs/PromptImprovement/ | Prompt iyileştirme istatistikleri. |
| `SalesPersonDto.cs` | DTOs/ | Satış personeli. |
| `SalesReasonDto.cs` | DTOs/ | Satış nedeni. |
| `SchemaCatalogStats.cs` | DTOs/SchemaCatalog/ | Schema catalog istatistikleri. |
| `ShipMethodDto.cs` | DTOs/ | Kargo metodu. |
| `StoreDto.cs` | DTOs/ | Mağaza. |
| `TerritoryDto.cs` | DTOs/ | Bölge. |

#### Ports (58 dosya)

**Primary Ports** — `Ports/Primary/UseCases/`

`IAIChatUseCase`, `IAuthUseCase`, `IContextSummarizationUseCase`, `IConversationUseCase`, `IDashboardQueryUseCase`, `IDashboardUseCase`, `IDocumentCategoryUseCase`, `IDocumentDisplayInfoUseCase`, `IDocumentMetadataUseCase`, `IDocumentProcessingUseCase`, `IExcelAnalysisUseCase`, `IFeedbackAnalysisUseCase`, `IFeedbackUseCase`, `IRagSearchUseCase`, `IReportMetadataUseCase`, `IRouteConversationUseCase`, `IReActUseCase`, `IScheduledReportUseCase`, `IUserMemoryUseCase`

**Secondary Ports** — Notifications, Scheduling, Services

| Grup | Dosyalar |
|------|----------|
| Notifications | `IEmailNotificationService`, `INotificationService`, `ITeamsNotificationService` |
| Scheduling | `IJobScheduler`, `ISchedulerDataService` |
| Services/AIChat | `IDynamicPromptBuilder`, `IReranker`, `ISelfQueryExtractor` |
| Services/Auth | `ICurrentUserService`, `ITokenService` |
| Services/Cache | `IChatCacheService` |
| Services/Common | `IFileSaver`, `ISignalRHubContext` |
| Services/Database | `IDatabaseSchemaReader`, `IDatabaseService`, `ISchemaGraphService`, `ISchemaParserService`, `ISqlAgentPipeline`, `ISqlOptimizationAgent`, `ISqlServerConnectionFactory`, `ISqlValidationAgent` |
| Services/Document | `IDocumentCacheService`, `IDocumentParser`, `IDocumentTextExtractor`, `ITextChunker`, `IJsonQuestionAnswerParser` |
| Services/Report | `IAdventureWorksReadyReportService`, `IDashboardParser`, `IExcelAnalysisService`, `IReportService` |
| Services/Vector | `IEmbeddingService`, `IQdrantService`, `ISparseVectorService` |
| Services/Query | `IConversationQueryService`, `IFeedbackQueryService`, `IPromptImprovementQueryService` |

---

### 🟡 AI.Infrastructure - Altyapı Katmanı

#### Adapters/AI (23 dosya)

| Dosya Adı | Konum | Açıklama |
|-----------|-------|---------|
| `QdrantService.cs` | VectorServices/ | Qdrant client. Collection CRUD, upsert, hybrid search. |
| `SparseVectorService.cs` | VectorServices/ | Sparse vector (BM25) üretimi. |
| `OpenAIEmbeddingService.cs` | DocumentServices/ | Text → embedding vektör üretimi. |
| `PdfDocumentParser.cs` | DocumentServices/ | PDF parse ve text çıkarma. |
| `TextDocumentParser.cs` | DocumentServices/ | TXT, MD parse. |
| `TextChunker.cs` | DocumentServices/ | Metin chunk'lama. Recursive splitting. |
| `RecursiveCharacterTextSplitter.cs` | DocumentServices/ | LangChain tarzı recursive splitting. |
| `JsonQuestionAnswerParser.cs` | DocumentServices/ | JSON Q&A parsing. |
| `DocumentTextExtractor.cs` | DocumentServices/ | PDF, Excel, Word, PowerPoint, CSV, TXT metin çıkarma. |
| `SqlAgentPipeline.cs` | Agents/SqlAgents/ | NL → SQL dönüşümü pipeline. |
| `SqlOptimizationAgent.cs` | Agents/SqlAgents/ | SQL query optimizasyonu. |
| `SqlValidationAgent.cs` | Agents/SqlAgents/ | SQL syntax kontrolü. |
| `DashboardFileSaver.cs` | Common/ | Dashboard dosyalarını diske kaydeder. |
| `DashboardResponseParser.cs` | Common/ | LLM dashboard yanıtını parse eder. |
| `DuckDbExcelService.cs` | ExcelServices/ | Excel analizi (DuckDB). |
| `AdventureWorksReadyReportService.cs` | ReadyReports/ | Hazır AdventureWorks raporları. |
| `AdventureWorksReportService.cs` | Reports/SqlServer/ | AdventureWorks rapor servisi. |
| `SocialMediaReportService.cs` | Reports/SqlServer/ | Sosyal medya rapor servisi. |
| `SqlServerReportServiceBase.cs` | Reports/SqlServer/ | SQL Server rapor base class. |
| `DatabaseSchemaReader.cs` | Neo4j/ | Neo4j'den şema okuma. |
| `SchemaGraphService.cs` | Neo4j/ | Graph DB şema yönetimi. |
| `DynamicPromptBuilder.cs` | Neo4j/ | Dinamik SQL prompt oluşturma. |
| `SchemaParserService.cs` | Neo4j/ | Schema parser. |
| `LLMReranker.cs` | Reranking/ | LLM-based search sonuç sıralama. |
| `SelfQueryExtractor.cs` | SelfQuery/ | Sorgudan filter çıkarma. |

#### Adapters/External (12 dosya)

| Dosya Adı | Konum | Açıklama |
|-----------|-------|---------|
| `NotificationService.cs` | Notifications/ | Unified bildirim. Email ve Teams'e delegate. |
| `EmailNotificationService.cs` | Notifications/ | SMTP email gönderimi. |
| `TeamsNotificationService.cs` | Notifications/ | MS Teams webhook. |
| `TokenService.cs` | Auth/ | JWT token üretimi ve validasyonu. |
| `CurrentUserService.cs` | Auth/ | HttpContext'ten user bilgisi. |
| `RedisCacheService.cs` | Caching/ | Redis cache. |
| `InMemoryCacheService.cs` | Caching/ | In-memory cache (development). |
| `DocumentCacheService.cs` | Caching/ | Doküman cache. |
| `SqlServerConnectionFactory.cs` | DatabaseServices/SqlServer/ | SQL Server connection factory. |
| `SqlServerDatabaseService.cs` | DatabaseServices/SqlServer/ | SQL Server sorgu çalıştırma. |
| `HangfireJobScheduler.cs` | Scheduling/ | Hangfire job scheduler. |
| `SchedulerDataService.cs` | Scheduling/ | Scheduler data access. |

#### Adapters/Persistence (36 dosya)

| Dosya Adı | Açıklama |
|-----------|----------|
| `ChatDbContext.cs` | EF Core DbContext. PostgreSQL. `SaveChangesAsync` override ile domain event dispatch. |
| `DomainEventDispatcher.cs` | `IDomainEventDispatcher` implementasyonu. DI container'dan handler'ları resolve eder. |
| **Repositories (Command)** | |
| `PostgreSqlConversationRepository.cs` | Conversation/Message — PostgreSQL (sadece command). |
| `InMemoryConversationRepository.cs` | Conversation/Message — in-memory (sadece command). |
| `UserRepository.cs` | User CRUD (AD + local) + RefreshToken operations. |
| `InMemoryUserRepository.cs` | User — in-memory (test). |
| `RoleRepository.cs` | Rol CRUD. |
| `UserMemoryRepository.cs` | Kullanıcı memory CRUD. |
| `ScheduledReportRepository.cs` | Scheduled report CRUD. |
| `MessageFeedbackRepository.cs` | Feedback CRUD (sadece command). |
| `InMemoryMessageFeedbackRepository.cs` | Feedback — in-memory (test). |
| `FeedbackAnalysisReportRepository.cs` | Feedback analiz raporu + PromptImprovement CRUD. |
| `DocumentCategoryRepository.cs` | Doküman kategori + DocumentDisplayInfo CRUD. |
| **Query Services (CQRS)** | |
| `ConversationQueryService.cs` | `IConversationQueryService` impl — PostgreSQL conversation metadata sorguları. |
| `InMemoryConversationQueryService.cs` | `IConversationQueryService` impl — InMemory mode. |
| `FeedbackQueryService.cs` | `IFeedbackQueryService` impl — feedback istatistik sorguları. |
| `PromptImprovementQueryService.cs` | `IPromptImprovementQueryService` impl — prompt improvement istatistikleri. |
| **EF Configurations** | Tüm entity'ler için Fluent API konfigürasyonları: |
| | `ConversationConfiguration`, `MessageConfiguration`, `UserConfiguration`, `RoleConfiguration`, `UserRoleConfiguration`, `RefreshTokenConfiguration`, `UserMemoryConfiguration`, `ScheduledReportConfiguration`, `ScheduledReportLogConfiguration`, `MessageFeedbackConfiguration`, `FeedbackAnalysisReportConfiguration`, `PromptImprovementConfiguration`, `DocumentCategoryConfiguration`, `DocumentDisplayInfoConfiguration` |

#### Configuration & Extensions

| Dosya Adı | Açıklama |
|-----------|----------|
| `EmailSettings.cs` | SMTP ayarları. |
| `TeamsSettings.cs` | Teams webhook ayarları. |
| `InfrastructureExtensions.cs` | Tüm Infrastructure DI registration. |
| `AuthenticationServiceExtensions.cs` | JWT authentication konfigürasyonu. |
| `Neo4jExtensions.cs` | Neo4j driver ve servis registration. |
| `OpenTelemetryExtensions.cs` | Tracing ve metrics. |
| `SqlAgentExtensions.cs` | SQL Agent pipeline registration. |
| `DatabaseSeederExtensions.cs` | Seed data: default admin user, roller. |
| `LoggingExtension.cs` | Serilog konfigürasyonu. |
| `LoggingHelper.cs` | Structured logging helper. |

---

### 🔴 AI.Api - API Katmanı

| Dosya Adı | Konum | Açıklama |
|-----------|-------|---------|
| `Program.cs` | / | Uygulama başlangıç noktası. DI, middleware, endpoint konfigürasyonu. |
| `AIHub.cs` | Hubs/ | SignalR hub. Real-time streaming mesaj gönderimi. |
| `SignalRHubContextWrapper.cs` | Common/ | ISignalRHubContext implementasyonu. |
| `GlobalExceptionHandler.cs` | Common/Exceptions/ | Global exception handling. |
| `SignalRHealthCheck.cs` | Common/HealthChecks/ | SignalR sağlık kontrolü. |
| `AuthenticationConfiguration.cs` | Configuration/ | JWT token validation parametreleri. |
| `AuthEndpoints.cs` | Endpoints/Auth/ | Login, register, refresh token. |
| `CommonEndpoints.cs` | Endpoints/Common/ | AdventureWorks lookup verileri (bölge, mağaza, ürün vb.). `IReportMetadataUseCase` üzerinden. |
| `DashboardEndpoints.cs` | Endpoints/Dashboard/ | Dashboard CRUD ve query. |
| `DocumentCategoryEndpoints.cs` | Endpoints/Documents/ | Doküman kategori CRUD. |
| `DocumentDisplayInfoEndpoints.cs` | Endpoints/Documents/ | Doküman görüntüleme bilgisi. |
| `DocumentEndpoints.cs` | Endpoints/Documents/ | Doküman yükleme, silme. `ProcessDocumentFromUploadAsync` DTO üzerinden. |
| `FeedbackEndpoints.cs` | Endpoints/Feedback/ | Geri bildirim CRUD + analiz. Domain entity kullanmaz, tamamen DTO tabanlı. |
| `ConversationEndpoints.cs` | Endpoints/History/ | POST /api/v1/chatbot tek giriş noktası. |
| `HistoryEndpoints.cs` | Endpoints/History/ | Conversation listesi, mesajlar. `IConversationUseCase` üzerinden. |
| `AdventureWorksReportEndpoints.cs` | Endpoints/Reports/ | Hazır rapor endpoint'leri. |
| `ScheduledReportEndpoints.cs` | Endpoints/Reports/ | Zamanlanmış rapor yönetimi. |
| `Neo4jEndpoints.cs` | Endpoints/Search/ | Neo4j graph arama. |
| `SearchEndpoints.cs` | Endpoints/Search/ | RAG vector search API. |
| `ApiVersioningExtensions.cs` | Extensions/ | API versiyonlama. |
| `ConversationServiceExtensions.cs` | Extensions/ | Conversation servis registration (InMemory/PostgreSQL). |
| `DependencyInjectionExtensions.cs` | Extensions/ | API layer DI registration. |
| `ExceptionHandlingExtensions.cs` | Extensions/ | Exception handling middleware. |
| `HealthChecksExtensions.cs` | Extensions/ | Health check servisleri. |
| `RateLimitingExtensions.cs` | Extensions/ | Rate limiting middleware. |
| `StartupExtensions.cs` | Extensions/ | CORS, Swagger, pipeline konfigürasyonu. |

---

### 🟣 AI.Scheduler - Scheduler Katmanı

| Dosya Adı | Açıklama |
|-----------|----------|
| `Program.cs` | Hangfire dashboard ve worker başlangıç noktası. |
| `ReportSchedulerJob.cs` | Zamanlanmış raporları cron'a göre tetikler. |
| `ScheduledReportJob.cs` | Tek rapor çalıştırma: SQL execute, bildirim gönderme. |
| `FeedbackAnalysisJob.cs` | Periyodik feedback analizi: AI kategorizasyon ve öneri. |
| `HangfireExtensions.cs` | Hangfire server ve dashboard konfigürasyonu. |
| `LLMExtensions.cs` | LLM servis registration. |
| `HangfireSettings.cs` | Connection string, worker count. |
| `ScheduledReportSettings.cs` | Default cron, timeout değerleri. |

---

## İlgili Dökümanlar

| Döküman | Açıklama |
|---------|----------|
| [System-Overview.md](System-Overview.md) | Genel sistem mimarisi |
| [Hexagonal-Architecture.md](Hexagonal-Architecture.md) | Hexagonal mimari analizi |
| [Application-Layer.md](Application-Layer.md) | UseCase katmanı detayları |
