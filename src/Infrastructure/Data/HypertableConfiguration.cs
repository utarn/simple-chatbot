using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatbotApi.Infrastructure.Data;

public class HypertableConfiguration
{
    public Type EntityType { get; set; } = default!;
    public string SchemaName { get; set; } = "public"; // Default schema if not specified
    public string TimeColumnName { get; set; } = default!;
}

public static class EntityTypeBuilderExtensions
{
    public static EntityTypeBuilder<TEntity> IsHypertable<TEntity, TProperty>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, TProperty>> timeColumnExpression,
        string schemaName = "public") where TEntity : class
    {
        if (timeColumnExpression.Body is not MemberExpression memberExpression)
        {
            throw new ArgumentException("Argument must be a member expression.", nameof(timeColumnExpression));
        }

        var propertyName = memberExpression.Member.Name;
        var modelBuilder = entityTypeBuilder.Metadata.Model;
        var hypertableConfigurations = GetOrCreateHypertableConfigurations(modelBuilder);

        hypertableConfigurations.Add(new HypertableConfiguration
        {
            EntityType = typeof(TEntity),
            TimeColumnName = propertyName,
            SchemaName = schemaName
        });

        return entityTypeBuilder;
    }

    private static List<HypertableConfiguration> GetOrCreateHypertableConfigurations(IMutableModel model)
    {
        const string HypertableConfigurationsKey = "HypertableConfigurations";
        if (model.FindAnnotation(HypertableConfigurationsKey) is not List<HypertableConfiguration> configurations)
        {
            configurations = new List<HypertableConfiguration>();
            model.AddAnnotation(HypertableConfigurationsKey, configurations);
        }

        return configurations;
    }
}
