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
    
            // Scan only a single assembly with fullname starting with ChatbotApi.Infrastructure
            var targetAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(s =>
                {
                    var fullname = s.FullName ?? string.Empty;
                    return fullname.StartsWith("ChatbotApi.Infrastructure", StringComparison.OrdinalIgnoreCase);
                });

            if (targetAssembly != null)
            {
                try
                {
                    // Find all types that look like processors (class name ends with Processor) and
                    // implement any known processor interface. Include ILineEmailProcessor so email processors
                    // defined in Infrastructure are discovered as well.
                    var processorTypes = targetAssembly.GetTypes()
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
                            // Get ProcessorAttribute - processors must have this attribute to be discovered
                            var attr = processorType.GetCustomAttribute<ProcessorAttribute>();
                            
                            if (attr != null && !string.IsNullOrEmpty(attr.Name))
                            {
                                var pluginInfo = new PluginInfo
                                {
                                    Name = attr.Name,
                                    Description = attr.Description ?? string.Empty,
                                    IsEnabled = true,
                                    ProcessorType = processorType
                                };

                                plugins[attr.Name] = pluginInfo;
                                _logger.LogInformation("Discovered plugin: {PluginName} - {Description}",
                                    attr.Name, attr.Description);
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
                    _logger.LogWarning(ex, "Failed to load types from assembly {AssemblyName}", targetAssembly.FullName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing assembly {AssemblyName}", targetAssembly.FullName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during plugin discovery");
        }

        _logger.LogInformation("Plugin discovery completed. Found {Count} plugins", plugins.Count);
        
        // Sort plugins by Name in ascending order
        var sortedPlugins = plugins
            .OrderBy(kvp => kvp.Value.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        
        return sortedPlugins;
    }
}