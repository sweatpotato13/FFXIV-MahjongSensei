using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Mahjong.Plugin.Dalamud.UI;

public sealed class AboutWindow : Window, IDisposable
{
    private const string RepoUrl = "https://github.com/sweatpotato13/FFXIV-MahjongSensei";
    private const string IssuesUrl = "https://github.com/sweatpotato13/FFXIV-MahjongSensei/issues";
    private const string DiscussionsUrl = "https://github.com/sweatpotato13/FFXIV-MahjongSensei/discussions";
    private const string SecurityUrl = "https://github.com/sweatpotato13/FFXIV-MahjongSensei/security/advisories/new";
    private const string Author = "sweatpotato13 (fork of XeldarAlz)";

    private readonly IPluginLog log;
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ITextureProvider textureProvider;
    private readonly Dictionary<string, float> linkHoverPulse = new();

    public AboutWindow(IPluginLog log, IDalamudPluginInterface pluginInterface, ITextureProvider textureProvider)
        : base("Doman Mahjong Sensei: About###domanmahjong-about")
    {
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(pluginInterface);
        ArgumentNullException.ThrowIfNull(textureProvider);
        this.log = log;
        this.pluginInterface = pluginInterface;
        this.textureProvider = textureProvider;

        Flags = ImGuiWindowFlags.NoCollapse;
        Size = new Vector2(560, 300);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(380, 220),
            MaximumSize = new Vector2(900, 2000),
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        using var _s = Theme.PushWindowStyle();

        DrawIcon();
        DrawHeader();
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, 4));
        DrawDetailsTable();
    }

    private void DrawIcon()
    {
        var iconSize = 96f;
        var avail = ImGui.GetContentRegionAvail().X;
        if (avail > iconSize)
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (avail - iconSize) * 0.5f);

        var iconPath = Path.Combine(
            pluginInterface.AssemblyLocation.DirectoryName ?? "",
            "Images", "Icon.png");
        if (!File.Exists(iconPath))
        {
            ImGui.Dummy(new Vector2(iconSize, iconSize));
            return;
        }

        var tex = textureProvider.GetFromFile(iconPath).GetWrapOrDefault();
        if (tex == null)
        {
            ImGui.Dummy(new Vector2(iconSize, iconSize));
            return;
        }

        var alpha = 0.85f + 0.15f * Theme.Pulse(2.0f, 0f, 1f);
        ImGui.Image(tex.Handle, new Vector2(iconSize, iconSize), Vector2.Zero, Vector2.One, new Vector4(1f, 1f, 1f, alpha));
        ImGui.Dummy(new Vector2(0, 4));
    }

    private static void DrawHeader()
    {
        var version = typeof(AboutWindow).Assembly.GetName().Version?.ToString() ?? "?";
        var availWidth = ImGui.GetContentRegionAvail().X;
        var label = $"v {version}";
        var textWidth = ImGui.CalcTextSize(label).X;
        if (availWidth > textWidth)
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (availWidth - textWidth) * 0.5f);

        ImGui.PushStyleColor(ImGuiCol.Text, Theme.Muted);
        ImGui.TextUnformatted(label);
        ImGui.PopStyleColor();
    }

    private void DrawDetailsTable()
    {
        if (!ImGui.BeginTable("##about_table", 2,
                ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.PadOuterX))
            return;

        ImGui.TableSetupColumn("##label", ImGuiTableColumnFlags.WidthFixed, 150f);
        ImGui.TableSetupColumn("##value", ImGuiTableColumnFlags.WidthStretch);

        DrawTextRow("Author", Author);
        DrawLinkRow("GitHub", RepoUrl);
        DrawLinkRow("Report a bug", IssuesUrl);
        DrawLinkRow("Discussions", DiscussionsUrl);
        DrawLinkRow("Security disclosure", SecurityUrl);

        ImGui.EndTable();
    }

    private static void DrawTextRow(string label, string value)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.PushStyleColor(ImGuiCol.Text, Theme.Muted);
        ImGui.TextUnformatted(label);
        ImGui.PopStyleColor();
        ImGui.TableSetColumnIndex(1);
        ImGui.PushStyleColor(ImGuiCol.Text, Theme.Header);
        ImGui.TextUnformatted(value);
        ImGui.PopStyleColor();
    }

    private void DrawLinkRow(string label, string url)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.PushStyleColor(ImGuiCol.Text, Theme.Muted);
        ImGui.TextUnformatted(label);
        ImGui.PopStyleColor();

        ImGui.TableSetColumnIndex(1);

        linkHoverPulse.TryGetValue(url, out var pulse);
        var color = Vector4.Lerp(Theme.Info, Theme.Header, pulse * 0.55f);

        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.PushTextWrapPos(ImGui.GetContentRegionMax().X);
        ImGui.TextUnformatted(url);
        ImGui.PopTextWrapPos();
        ImGui.PopStyleColor();

        var hovered = ImGui.IsItemHovered();
        linkHoverPulse[url] = hovered
            ? MathF.Min(pulse + 0.15f, 1f)
            : MathF.Max(pulse - 0.10f, 0f);

        if (!hovered)
            return;

        ImGui.BeginTooltip();
        ImGui.TextUnformatted("Click to open · right-click to copy");
        ImGui.EndTooltip();

        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            OpenInBrowser(url);
        else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            ImGui.SetClipboardText(url);
    }

    private void OpenInBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            log.Warning(ex, "[Mahjong.Plugin.Dalamud] failed to launch browser for {0}, copied to clipboard instead", url);
            ImGui.SetClipboardText(url);
        }
    }
}
