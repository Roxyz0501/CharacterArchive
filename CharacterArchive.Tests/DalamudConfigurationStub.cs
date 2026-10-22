namespace Dalamud.Configuration;

// Only the marker interface is stubbed. Tests execute the production config, codec and repository.
public interface IPluginConfiguration { int Version { get; set; } }
