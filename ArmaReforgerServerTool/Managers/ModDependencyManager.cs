/******************************************************************************
 * File Name:    ModDependencyManager.cs
 * Project:      Longbow
 * Description:  Resolves Arma Reforger mod dependency order by scraping the
 *               workshop page for each enabled mod. Performs a topological sort
 *               so that required mods load before the mods that depend on them.
 *               Auto-adds any missing dependency mods to the enabled list.
 *
 * Author:       Longbow contributors
 ******************************************************************************/

using HtmlAgilityPack;
using ReforgerServerApp.Managers;
using Serilog;
using System.Text.RegularExpressions;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace ReforgerServerApp
{
  internal static class ModDependencyManager
  {
    // Matches a workshop URL path ending in exactly one 16-char hex mod ID.
    // e.g. href="/workshop/591AF5BDA9F7CE8B"
    private static readonly Regex s_depHrefPattern =
        new(@"/workshop/([0-9A-Fa-f]{16})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Loads and returns the workshop HTML page for <paramref name="modId"/>.
    /// Throws on network or parse error — caller is responsible for handling.
    /// </summary>
    private static HtmlDocument LoadModPage(string modId)
    {
      string url = $"{ToolPropertiesManager.GetInstance().GetToolProperties().armaWorkshopUrl}/{modId}";
      HtmlWeb web = new();
      return web.Load(url);
    }

    /// <summary>
    /// Extracts the display name from an already-loaded workshop page.
    /// Falls back to the mod ID string if the title can't be parsed.
    /// </summary>
    private static string ExtractModName(HtmlDocument doc, string modId)
    {
      // Try h1 first, then page <title>
      string? raw = doc.DocumentNode.SelectSingleNode("//h1")?.InnerText
                 ?? doc.DocumentNode.SelectSingleNode("//title")?.InnerText;
      if (string.IsNullOrWhiteSpace(raw)) return modId;
      // Strip " | Arma Reforger Workshop" suffix if present
      int pipe = raw.IndexOf('|');
      return (pipe > 0 ? raw[..pipe] : raw).Trim();
    }

    /// <summary>
    /// Returns the mod IDs of all declared dependencies for <paramref name="modId"/>
    /// using an already-loaded <paramref name="doc"/>.
    /// </summary>
    private static List<string> ExtractDependencyIds(HtmlDocument doc, string modId)
    {
      List<string> deps = new();
      HtmlNodeCollection? links = doc.DocumentNode.SelectNodes("//a[@href]");
      if (links == null) return deps;

      foreach (HtmlNode link in links)
      {
        string href = link.GetAttributeValue("href", string.Empty);
        Match m = s_depHrefPattern.Match(href);
        if (!m.Success) continue;
        string depId = m.Groups[1].Value.ToUpperInvariant();
        if (!depId.Equals(modId, StringComparison.OrdinalIgnoreCase) && !deps.Contains(depId))
          deps.Add(depId);
      }
      return deps;
    }

    /// <summary>
    /// Fetches mod name for a dependency that is not yet in the enabled list.
    /// Returns a <see cref="Mod"/> with <c>name = modId</c> on failure.
    /// </summary>
    private static Mod FetchNewMod(string modId)
    {
      try
      {
        HtmlDocument doc = LoadModPage(modId);
        string name = ExtractModName(doc, modId);
        return new Mod(modId, name);
      }
      catch (Exception ex)
      {
        Log.Warning("ModDependencyManager - Could not fetch info for mod {id}: {msg}", modId, ex.Message);
        return new Mod(modId, modId);
      }
    }

    /// <summary>
    /// Resolves mod load order for <paramref name="enabled"/>.
    /// <para>
    /// Phase 1 — BFS: fetches each mod's workshop page to discover direct and transitive
    /// dependencies.  Any dependency not already in the list is added automatically.
    /// </para>
    /// <para>
    /// Phase 2 — Topological sort (DFS): orders the full set so every dependency
    /// appears before the mod that requires it.
    /// </para>
    /// </summary>
    /// <param name="enabled">Current enabled mod list.</param>
    /// <param name="progress">
    /// Optional callback invoked as <c>(completed, total)</c> after each mod is
    /// fetched so the caller can update a progress bar.
    /// </param>
    /// <returns>
    /// A tuple of (sorted list, auto-added mods, warning strings).
    /// On fatal network failure the original order is returned unchanged with a warning.
    /// </returns>
    public static (List<Mod> sorted, List<Mod> added, List<string> warnings) ResolveDependencies(
        IList<Mod> enabled,
        Action<int, int>? progress = null)
    {
      List<Mod> added = new();
      List<string> warnings = new();

      // Working set: modId (upper) → Mod
      Dictionary<string, Mod> workingSet = new(StringComparer.OrdinalIgnoreCase);
      foreach (Mod mod in enabled)
        workingSet[mod.modId.ToUpperInvariant()] = mod;

      // Dependency graph: modId → list of dep modIds
      Dictionary<string, List<string>> graph = new(StringComparer.OrdinalIgnoreCase);

      // BFS queue
      Queue<string> queue = new(workingSet.Keys);
      int processed = 0;

      try
      {
        while (queue.Count > 0)
        {
          string modId = queue.Dequeue();
          if (graph.ContainsKey(modId))
          {
            // Already processed (could be a dup from auto-added deps)
            processed++;
            progress?.Invoke(processed, workingSet.Count);
            continue;
          }

          HtmlDocument? doc = null;
          try
          {
            doc = LoadModPage(modId);
          }
          catch (Exception ex)
          {
            Log.Warning("ModDependencyManager - Could not load page for {id}: {msg}", modId, ex.Message);
            warnings.Add($"Could not fetch workshop page for mod {modId}: {ex.Message}");
            graph[modId] = new List<string>();
            processed++;
            progress?.Invoke(processed, workingSet.Count);
            continue;
          }

          List<string> deps = ExtractDependencyIds(doc, modId);
          graph[modId] = deps;

          foreach (string depId in deps)
          {
            if (!workingSet.ContainsKey(depId))
            {
              Mod newMod = FetchNewMod(depId);
              workingSet[depId] = newMod;
              added.Add(newMod);
              queue.Enqueue(depId);
            }
          }

          processed++;
          progress?.Invoke(processed, workingSet.Count);
        }
      }
      catch (Exception ex)
      {
        Log.Error("ModDependencyManager - Unexpected error during resolution: {msg}", ex.Message);
        warnings.Add($"Dependency resolution failed: {ex.Message}. Original mod order preserved.");
        progress?.Invoke(workingSet.Count, workingSet.Count);
        return (new List<Mod>(enabled), added, warnings);
      }

      // Topological sort (iterative DFS to avoid stack overflow on deep graphs)
      List<Mod> sorted = new();
      HashSet<string> visited = new(StringComparer.OrdinalIgnoreCase);
      HashSet<string> inStack = new(StringComparer.OrdinalIgnoreCase);

      void Visit(string id)
      {
        if (visited.Contains(id)) return;
        if (inStack.Contains(id))
        {
          warnings.Add($"Circular dependency detected involving mod {id}. Load order may be incorrect.");
          return;
        }
        inStack.Add(id);
        if (graph.TryGetValue(id, out List<string>? deps))
        {
          foreach (string dep in deps)
            Visit(dep);
        }
        inStack.Remove(id);
        visited.Add(id);
        if (workingSet.TryGetValue(id, out Mod? mod))
          sorted.Add(mod);
      }

      foreach (string id in workingSet.Keys)
        Visit(id);

      progress?.Invoke(workingSet.Count, workingSet.Count);
      return (sorted, added, warnings);
    }
  }
}
