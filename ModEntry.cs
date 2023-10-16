using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewValley;

namespace StardewFarmersOnly
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        private static readonly string SettingsKey = "FarmersOnlySettings";
        private static readonly Dictionary<SkillType, string> Skills = new()
        {
            { SkillType.Farming, "Farming" },
            { SkillType.Mining, "Mining" },
            { SkillType.Foraging, "Foraging" },
            { SkillType.Fishing, "Fishing" },
            { SkillType.Combat, "Combat" },
        };
        private ModData settings = new ModData();
        
        public override void Entry(IModHelper helper)
        {
            Helper.Events.Player.LevelChanged += OnLevelChanged;
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked_UpdateExperience;
            Helper.Events.GameLoop.SaveLoaded += (sender, args) =>
            {
                Monitor.Log($"Experience points {Game1.player.experiencePoints}");
                Monitor.Log("Reading saved settings...", LogLevel.Debug);
                // TODO: Make this work with multiplayer correctly
                // try
                // {
                //     settings = Helper.Data.ReadSaveData<ModData>(SettingsKey) ?? settings;
                // }
                // catch (Exception e)
                // {
                //     Monitor.Log("Failed to load settings, using default.", LogLevel.Debug);
                // }

                if (settings.SpecializedSkillType.Count > 0)
                {
                    string skillNames = GetSpecializedSkillNames();
                    Game1.addHUDMessage(new HUDMessage($"Currently specialized in {skillNames}, experience to all other skills will be removed.", 2));
                }
                else
                {
                    Game1.addHUDMessage(new HUDMessage("You are not currently specialized in any skills, use the command set_skill to start.", 2));
                }
            };
            
            Helper.ConsoleCommands.Add("list_skill", "List currently specialized skill.", ShowCurrentSkill);
            Helper.ConsoleCommands.Add("set_skill", "Set currently specialized skill.", SetCurrentSkill);
        }

        private void ShowCurrentSkill(string command, string[] args)
        {
            if (settings.SpecializedSkillType.Count == 0)
            {
                string options = string.Join(", ", Skills.Values);
                Game1.addHUDMessage(new HUDMessage($"You are not currently specialized in any skills, available options are {options}.", 2));
                return;
            }
            string skillNames = GetSpecializedSkillNames();
            Game1.addHUDMessage(new HUDMessage($"Currently specialized in {skillNames}, experience to all other skills will be removed.", 2));
        }

        private void SetCurrentSkill(string command, string[] args)
        {
            if (args.Length != 1)
            {
                string options = string.Join(", ", Skills.Values);
                Game1.addHUDMessage(new HUDMessage($"Use /set_skill <{options}> to set current specialized skills.", 2));
                return;
            }
            List<string> skills = args[0].Split(',').ToList().Select(skill => skill.Trim().ToLower()).ToList();
            foreach (var skill in skills)
            {
                if (Skills.Select(x => x.Value.ToLower()).ToList().Contains(skill))
                    continue;
                string options = string.Join(", ", Skills.Values);
                Game1.addHUDMessage(new HUDMessage($"Unknown skill {skill}, available options are {options}.", 2));
                return;
            }
            List<SkillType> skillType = Skills.Where(x => skills.Contains(x.Value.ToLower())).Select(x => x.Key).ToList();
            settings.SpecializedSkillType = skillType;
            // TODO: Make this work with multiplayer correctly
            // Helper.Data.WriteSaveData(SettingsKey, settings);
            string skillNames = GetSpecializedSkillNames();
            Game1.addHUDMessage(new HUDMessage($"Updated specialized skill to {skillNames}!", 2));
        }
        
        private void OnUpdateTicked_UpdateExperience(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (!e.IsMultipleOf(15)) // quarter second
                return;

            if (settings.SpecializedSkillType.Count == 0)
                return;

            List<SkillType> allowedSkills = new List<SkillType> { SkillType.Luck };
            allowedSkills = allowedSkills.Concat(settings.SpecializedSkillType).ToList();

            foreach (SkillType skill in Skills.Keys)
            {
                if (allowedSkills.Contains(skill))
                    continue;
                int skillNum = (int)skill;
                if (Game1.player.experiencePoints[skillNum] == 0)
                    continue;
                int oldExp = Game1.player.experiencePoints[skillNum];
                string skillName = Skills[skill];
                Monitor.Log($"Experience reset for {skillName} (removed {oldExp} XP) as it is not the currently specialized skill", LogLevel.Debug);
                Game1.player.experiencePoints[skillNum] = 0;
            }
        }

        private void OnLevelChanged(object sender, LevelChangedEventArgs e)
        {
            if (!e.IsLocalPlayer || settings.SpecializedSkillType.Count == 0)
                return;

            List<SkillType> allowedSkills = new List<SkillType> { SkillType.Luck };
            allowedSkills = allowedSkills.Concat(settings.SpecializedSkillType).ToList();

            if (allowedSkills.Contains(e.Skill))
            {
                return;
            }

            // We must reset the set skill progress
            switch (e.Skill)
            {
                case SkillType.Combat:
                    e.Player.CombatLevel = 0;
                    break;
                case SkillType.Farming:
                    e.Player.FarmingLevel = 0;
                    break;
                case SkillType.Fishing:
                    e.Player.FishingLevel = 0;
                    break;
                case SkillType.Foraging:
                    e.Player.ForagingLevel = 0;
                    break;
                case SkillType.Mining:
                    e.Player.MiningLevel = 0;
                    break;
            }
            
            string skillName = Skills[e.Skill];
            DisplayResetMessage(skillName);
        }

        private void DisplayResetMessage(string skillName)
        {
            // Display a message informing the player of the skill reset
            Game1.addHUDMessage(new HUDMessage($"Level in {skillName} skill has been reset.", 2));
        }

        private string GetSpecializedSkillNames()
        {
            return string.Join(",", settings.SpecializedSkillType.Select(skill => Skills[skill]));
        }
    }
}