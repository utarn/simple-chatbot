using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Application.Common.Attributes;
using Microsoft.Extensions.Logging;

namespace ChatbotApi.Application.Common.Services;

public interface IPluginDiscoveryService
{
    IReadOnlyDictionary<string, PluginInfo> GetAvailablePlugins();
    PluginInfo? GetPluginInfo(string pluginName);
}

public class PluginInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public Type ProcessorType { get; set; } = null!;
}

public class PluginDiscoveryService : IPluginDiscoveryService
{
    private readonly ILogger<PluginDiscoveryService> _logger;
    private readonly Lazy<IReadOnlyDictionary<string, PluginInfo>> _plugins;

    public PluginDiscoveryService(ILogger<PluginDiscoveryService> logger)
    {
        _logger = logger;
        _plugins = new Lazy<IReadOnlyDictionary<string, PluginInfo>>(DiscoverPlugins);
    }

    public IReadOnlyDictionary<string, PluginInfo> GetAvailablePlugins()
    {
        return _plugins.Value;
    }

    public PluginInfo? GetPluginInfo(string pluginName)
    {
        return _plugins.Value.TryGetValue(pluginName, out var plugin) ? plugin : null;
    }

    private IReadOnlyDictionary<string, PluginInfo> DiscoverPlugins()
    {
        var plugins = new Dictionary<string, PluginInfo>();

        try
        {
            // Try to ensure the Infrastructure assembly is loaded. Discovery can run early during startup
            // before that assembly gets loaded into the AppDomain.
            const string infraAssemblyName = "ChatbotApi.Infrastructure";
            try
            {
                if (!AppDomain.CurrentDomain.GetAssemblies().Any(a => string.Equals(a.GetName().Name, infraAssemblyName, StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        Assembly.Load(infraAssemblyName);
                    }
                    catch (Exception loadEx)
                    {
                        _logger.LogDebug(loadEx, "Could not explicitly load assembly {AssemblyName}. Proceeding with loaded assemblies.", infraAssemblyName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error when attempting to ensure infrastructure assembly is loaded.");
            }
    
            // Scan any ChatbotApi.* assemblies (infrastructure and other related assemblies).
            var assembliesToScan = AppDomain.CurrentDomain.GetAssemblies()
                .Where(s =>
                {
                    var name = s.GetName().Name ?? string.Empty;
                    return name.StartsWith("ChatbotApi", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
    
            // If nothing matched the ChatbotApi prefix (unlikely), fall back to all loaded assemblies.
            if (!assembliesToScan.Any())
            {
                assembliesToScan = AppDomain.CurrentDomain.GetAssemblies().ToList();
            }
    
            foreach (var assembly in assembliesToScan)
            {
                try
                {
                    // Find all types that look like processors (class name ends with Processor) and
                    // implement any known processor interface. Include ILineEmailProcessor so email processors
                    // defined in Infrastructure are discovered as well.
                    var processorTypes = assembly.GetTypes()
                        .Where(type => type.Name.EndsWith("Processor") &&
                                       !type.IsInterface &&
                                       !type.IsAbstract &&
                                       (typeof(ILineMessageProcessor).IsAssignableFrom(type) ||
                                        typeof(IFacebookMessengerProcessor).IsAssignableFrom(type) ||
                                        typeof(ILineEmailProcessor).IsAssignableFrom(type)))
                        .ToList();
    
                    foreach (var processorType in processorTypes)
                    {
                        try
                        {
                            // Prefer ProcessorAttribute to avoid activating the type.
                            var attr = processorType.GetCustomAttribute<ProcessorAttribute>();
                            string? name = null;
                            string? description = null;
    
                            if (attr != null)
                            {
                                name = attr.Name;
                                description = attr.Description;
                            }
                            else
                            {
                                // Fallback: instantiate only when attribute is not present and try to read properties.
                                try
                                {
                                    var instance = Activator.CreateInstance(processorType);
    
                                    if (instance is ILineMessageProcessor lineProcessor)
                                    {
                                        name = lineProcessor.Name;
                                        description = lineProcessor.Description;
                                    }
                                    else if (instance is IFacebookMessengerProcessor facebookProcessor)
                                    {
                                        name = facebookProcessor.Name;
                                        description = facebookProcessor.Description;
                                    }
                                    // Note: many processors (e.g. ILineEmailProcessor) may rely on the attribute for metadata.
                                }
                                catch (Exception instEx)
                                {
                                    _logger.LogDebug(instEx, "Failed to instantiate {ProcessorType} while discovering plugin metadata", processorType.FullName);
                                }
                            }
    
                            if (!string.IsNullOrEmpty(name))
                            {
                                var pluginInfo = new PluginInfo
                                {
                                    Name = name,
                                    Description = description ?? string.Empty,
                                    IsEnabled = true,
                                    ProcessorType = processorType
                                };
    
                                plugins[name] = pluginInfo;
                                _logger.LogInformation("Discovered plugin: {PluginName} - {Description}",
                                    name, description);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to process type {ProcessorType}", processorType.Name);
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    _logger.LogWarning(ex, "Failed to load types from assembly {AssemblyName}", assembly.FullName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing assembly {AssemblyName}", assembly.FullName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during plugin discovery");
        }

        _logger.LogInformation("Plugin discovery completed. Found {Count} plugins", plugins.Count);
        return plugins;
    }
}