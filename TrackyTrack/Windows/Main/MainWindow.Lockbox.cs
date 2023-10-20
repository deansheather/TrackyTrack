﻿using Dalamud.Interface.Utility;
using TrackyTrack.Data;

namespace TrackyTrack.Windows.Main;

public partial class MainWindow
{
    private int SelectedType;

    private void LockboxTab()
    {
        if (ImGui.BeginTabItem("Lockbox"))
        {
            if (ImGui.BeginTabBar("##LockboxTabBar"))
            {
                var characters = Plugin.CharacterStorage.Values;
                if (!characters.Any())
                {
                    Helper.NoEurekaCofferData();

                    ImGui.EndTabBar();
                    ImGui.EndTabItem();
                    return;
                }

                var characterLockboxes = characters.Where(c => c.Lockbox.Opened > 0).ToArray();
                if (!characterLockboxes.Any())
                {
                    Helper.NoEurekaCofferData();

                    ImGui.EndTabBar();
                    ImGui.EndTabItem();
                    return;
                }

                LockboxStats(characterLockboxes);

                foreach (var type in LockboxExtensions.AsArray)
                {
                    if (ImGui.BeginTabItem(type.ToArea()))
                    {
                        LockboxHistory(type, characterLockboxes);
                        ImGui.EndTabItem();
                    }
                }

                ImGui.EndTabBar();
            }
            ImGui.EndTabItem();
        }
    }

    private void LockboxStats(CharacterConfiguration[] characters)
    {
        if (!ImGui.BeginTabItem("Stats"))
            return;

        var totalNumber = 0u;
        var openedTypes = new Dictionary<LockboxTypes, uint>();
        foreach (var (type, dict) in characters.SelectMany(c => c.Lockbox.History))
        {
            foreach (var amount in dict.Values)
            {
                totalNumber += amount;
                if (!openedTypes.TryAdd(type, amount))
                    openedTypes[type] += amount;
            }

        }

        ImGuiHelpers.ScaledDummy(5.0f);
        ImGui.TextColored(ImGuiColors.DalamudViolet, "General:");
        if (ImGui.BeginTable($"##TotalStatsTable", 2, 0, new Vector2(300 * ImGuiHelpers.GlobalScale, 0)))
        {
            ImGui.TableSetupColumn("##stat", 0, 0.6f);
            ImGui.TableSetupColumn("##opened");

            ImGui.TableNextColumn();
            ImGui.Indent(10.0f);
            ImGui.TextColored(ImGuiColors.HealerGreen, "Opened");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{totalNumber:N0} Lockboxe{(totalNumber > 1 ? "s" : "")}");

            // We set zadnor because it is last in list
            var lastType = string.Empty;
            foreach (var (type, amount) in openedTypes)
            {
                var area = type.ToArea();
                if (lastType != area)
                {
                    lastType = area;

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiHelpers.ScaledDummy(5.0f);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiColors.HealerGreen, area);
                }

                ImGui.Indent(10.0f);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextColored(ImGuiColors.HealerGreen, type.ToName());

                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{amount:N0} opened");
                ImGui.Unindent(10.0f);
            }
            ImGui.Unindent(10.0f);
            ImGui.EndTable();
        }
        ImGui.EndTabItem();
    }

    private void LockboxHistory(LockboxTypes types, CharacterConfiguration[] characters)
    {
        var bothTypes = types.ToMultiple();
        if (types.HasMultiple())
        {
            if (SelectedType != (int) bothTypes.Main && SelectedType != (int) bothTypes.Secondary)
                SelectedType = (int) bothTypes.Main;

            ImGui.RadioButton(bothTypes.Main.ToName(), ref SelectedType, (int) bothTypes.Main);
            ImGui.SameLine();
            ImGui.RadioButton(bothTypes.Secondary.ToName(), ref SelectedType, (int) bothTypes.Secondary);
        }
        else
        {
            SelectedType = (int) types;
        }

        // fill dict with real values
        var selectedType = (LockboxTypes) SelectedType;
        var dict = new Dictionary<LockboxTypes, Dictionary<uint, uint>>();
        foreach (var (type, lockboxDict) in characters.SelectMany(c => c.Lockbox.History).Where(pair => pair.Key == selectedType))
        {
            dict[type] = new Dictionary<uint, uint>();
            foreach (var (itemId, amount) in lockboxDict)
            {
                if (!dict[type].TryAdd(itemId, amount))
                    dict[type][itemId] += amount;
            }
        }

        ImGuiHelpers.ScaledDummy(5.0f);
        if (!dict[selectedType].Any())
        {
            ImGui.TextColored(ImGuiColors.ParsedOrange, $"Haven't opened any {selectedType.ToName()} Lockboxes.");
            return;
        }

        var opened = dict[selectedType].Values.Sum(s => s);
        var unsortedList = dict[selectedType].Select(pair =>
        {
            var item = ItemSheet.GetRow(pair.Key)!;
            var count = pair.Value;
            var percentage = (double) pair.Value / opened * 100.0;
            return new Utils.SortedEntry(item.RowId, item.Icon, Utils.ToStr(item.Name), count, percentage);
        });

        ImGui.TextColored(ImGuiColors.ParsedOrange, $"Opened: {opened:N0}");
        if (ImGui.BeginTable($"##HistoryTable", 4, ImGuiTableFlags.Sortable))
        {
            ImGui.TableSetupColumn("##icon", ImGuiTableColumnFlags.NoSort, 0.17f);
            ImGui.TableSetupColumn("Item##item");
            ImGui.TableSetupColumn("Num##amount", 0, 0.2f);
            ImGui.TableSetupColumn("Pct##percentage", ImGuiTableColumnFlags.DefaultSort, 0.25f);

            ImGui.TableHeadersRow();

            ImGui.Indent(10.0f);
            foreach (var sortedEntry in Utils.SortEntries(unsortedList, ImGui.TableGetSortSpecs().Specs))
            {
                ImGui.TableNextColumn();
                DrawIcon(sortedEntry.Icon);
                ImGui.TableNextColumn();

                ImGui.TextUnformatted(sortedEntry.Name);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(sortedEntry.Name);

                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"x{sortedEntry.Count}");

                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{sortedEntry.Percentage:F2}%");
                ImGui.TableNextRow();
            }

            ImGui.Unindent(10.0f);
            ImGui.EndTable();
        }
    }
}