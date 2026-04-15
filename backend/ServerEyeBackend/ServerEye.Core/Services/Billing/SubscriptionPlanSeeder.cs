namespace ServerEye.Core.Services.Billing;

using Microsoft.Extensions.Logging;
using ServerEye.Core.Configuration.Plans;
using ServerEye.Core.Entities.Billing;
using ServerEye.Core.Interfaces.Repository.Billing;

/// <summary>
/// Seeds subscription plans from code definitions to database on application startup.
/// Ensures database is in sync with the static plan definitions.
/// </summary>
public class SubscriptionPlanSeeder
{
    private readonly ISubscriptionPlanRepository planRepository;
    private readonly ILogger<SubscriptionPlanSeeder> logger;

    public SubscriptionPlanSeeder(
        ISubscriptionPlanRepository planRepository,
        ILogger<SubscriptionPlanSeeder> logger)
    {
        this.planRepository = planRepository;
        this.logger = logger;
    }

    /// <summary>
    /// Synchronizes plan definitions from code to database.
    /// Creates missing plans and updates existing ones if they differ from code definitions.
    /// </summary>
    public async Task SeedAsync()
    {
        this.logger.LogInformation("Starting subscription plan seeding...");

        var planDefinitions = SubscriptionPlanDefinitions.GetAll();

        foreach (var definition in planDefinitions)
        {
            var existingPlan = await this.planRepository.GetByIdAsync(definition.Id);

            if (existingPlan == null)
            {
                // Create new plan
                await this.planRepository.AddAsync(MapToEntity(definition));
                this.logger.LogInformation(
                    "Created new subscription plan: {PlanName} (ID: {PlanId})",
                    definition.Name,
                    definition.Id);
            }
            else
            {
                // Update existing plan if it differs
                if (HasChanges(existingPlan, definition))
                {
                    var updatedPlan = MapToEntity(definition);
                    updatedPlan.CreatedAt = existingPlan.CreatedAt; // Preserve creation date
                    updatedPlan.UpdatedAt = DateTime.UtcNow;

                    await this.planRepository.UpdateAsync(updatedPlan);
                    this.logger.LogInformation(
                        "Updated subscription plan: {PlanName} (ID: {PlanId})",
                        definition.Name,
                        definition.Id);
                }
                else
                {
                    this.logger.LogDebug(
                        "Subscription plan already up to date: {PlanName} (ID: {PlanId})",
                        definition.Name,
                        definition.Id);
                }
            }
        }

        this.logger.LogInformation("Subscription plan seeding completed successfully.");
    }

    private static SubscriptionPlanEntity MapToEntity(SubscriptionPlanDefinition definition)
    {
        return new SubscriptionPlanEntity
        {
            Id = definition.Id,
            Name = definition.Name,
            Description = definition.Description,
            Price = definition.MonthlyPrice, // Legacy field, mapped to monthly price
            Features = definition.Features.ToArray(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            MonthlyPrice = definition.MonthlyPrice,
            YearlyPrice = definition.YearlyPrice,
            MaxServers = definition.MaxServers,
            MetricsRetentionDays = definition.MetricsRetentionDays,
            HasAlerts = definition.HasAlerts,
            HasApiAccess = definition.HasApiAccess,
            HasPrioritySupport = definition.HasPrioritySupport
        };
    }

    private static bool HasChanges(SubscriptionPlanEntity entity, SubscriptionPlanDefinition definition)
    {
        return entity.Name != definition.Name ||
               entity.Description != definition.Description ||
               entity.Price != definition.MonthlyPrice ||
               !entity.Features.SequenceEqual(definition.Features) ||
               entity.MonthlyPrice != definition.MonthlyPrice ||
               entity.YearlyPrice != definition.YearlyPrice ||
               entity.MaxServers != definition.MaxServers ||
               entity.MetricsRetentionDays != definition.MetricsRetentionDays ||
               entity.HasAlerts != definition.HasAlerts ||
               entity.HasApiAccess != definition.HasApiAccess ||
               entity.HasPrioritySupport != definition.HasPrioritySupport;
    }
}
