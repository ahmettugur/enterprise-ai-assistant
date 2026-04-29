using AI.Domain.Common;
using AI.Domain.Conversations;
using AI.Domain.Feedback;
using AI.Domain.Identity;
using AI.Domain.Memory;
using AI.Domain.Documents;
using AI.Domain.Scheduling;
using AI.Infrastructure.Adapters.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AI.Infrastructure.Adapters.Persistence;

/// <summary>
/// EF Core DbContext for chat history and identity.
/// Domain event dispatch: SaveChangesAsync override ile aggregate root'lardan
/// event'leri toplar ve dispatch eder (post-commit pattern).
/// </summary>
public sealed class ChatDbContext : DbContext
{
    private IServiceProvider? _serviceProvider;

    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Inject IServiceProvider after creation — used for domain event dispatch.
    /// Pooled factory creates instances without DI, so we set it after creation.
    /// </summary>
    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Collect domain events BEFORE save (save might clear tracking)
        var domainEvents = ChangeTracker
            .Entries()
            .Where(e => e.Entity is IHasDomainEvents)
            .Select(e => (IHasDomainEvents)e.Entity)
            .Where(ar => ar.DomainEvents.Any())
            .SelectMany(ar =>
            {
                var events = ar.DomainEvents.ToList();
                ar.ClearDomainEvents();
                return events;
            })
            .ToList();

        // Execute the actual save
        var result = await base.SaveChangesAsync(cancellationToken);

        // Dispatch domain events AFTER successful save (post-commit)
        if (domainEvents.Any() && _serviceProvider is not null)
        {
            var dispatcher = _serviceProvider.GetService<IDomainEventDispatcher>();
            if (dispatcher is not null)
            {
                await dispatcher.DispatchEventsAsync(domainEvents, cancellationToken);
            }
        }

        return result;
    }

    // Chat entities
    public DbSet<Conversation> Conversations { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<MessageFeedback> MessageFeedbacks { get; set; } = null!;

    // Document entities
    public DbSet<DocumentCategory> DocumentCategories { get; set; } = null!;
    public DbSet<DocumentDisplayInfo> DocumentDisplayInfos { get; set; } = null!;

    // Identity entities
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    // Scheduling entities
    public DbSet<ScheduledReport> ScheduledReports { get; set; } = null!;
    public DbSet<ScheduledReportLog> ScheduledReportLogs { get; set; } = null!;

    // Memory entities
    public DbSet<UserMemory> UserMemories { get; set; } = null!;

    // Feedback Analysis entities
    public DbSet<FeedbackAnalysisReport> FeedbackAnalysisReports { get; set; } = null!;
    public DbSet<PromptImprovement> PromptImprovements { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Chat configurations
        modelBuilder.ApplyConfiguration(new ConversationConfiguration());
        modelBuilder.ApplyConfiguration(new MessageConfiguration());
        modelBuilder.ApplyConfiguration(new MessageFeedbackConfiguration());

        // Document configurations
        modelBuilder.ApplyConfiguration(new DocumentCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentDisplayInfoConfiguration());

        // Identity configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());

        // Scheduling configurations
        modelBuilder.ApplyConfiguration(new ScheduledReportConfiguration());
        modelBuilder.ApplyConfiguration(new ScheduledReportLogConfiguration());

        // Memory configurations
        modelBuilder.ApplyConfiguration(new UserMemoryConfiguration());

        // Feedback Analysis configurations
        modelBuilder.ApplyConfiguration(new Configurations.FeedbackAnalysisReportConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.PromptImprovementConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
