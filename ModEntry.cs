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
                settings = Helper.Data.ReadSaveData<ModData>(SettingsKey) ?? settings;
                if (settings.SpecializedSkillType.HasValue)
                {
                    string skillName = Skills[settings.SpecializedSkillType.Value];
                    Game1.addHUDMessage(new HUDMessage($"Currently specialized in {skillName}, experience to all other skills will be removed.", 2));
                }
            };
            
            Helper.ConsoleCommands.Add("list_specialized_skill", "List currently specialized skill.", ShowCurrentSkill);
            Helper.ConsoleCommands.Add("set_specialized_skill", "Set currently specialized skill.", SetCurrentSkill);
        }

        private void ShowCurrentSkill(string command, string[] args)
        {
            if (!settings.SpecializedSkillType.HasValue)
            {
                Game1.addHUDMessage(new HUDMessage("You are not currently specialized in any skill.", 2));
                return;
            }
            string skillName = Skills[settings.SpecializedSkillType.Value];
            Game1.addHUDMessage(new HUDMessage($"Currently specialized in {skillName}, experience to all other skills will be removed.", 2));
        }

        private void SetCurrentSkill(string command, string[] args)
        {
            if (args.Length != 1)
                return;
            string skill = args[0];
            if (!Skills.ContainsValue(skill))
            {
                string options = string.Join(", ", Skills.Values);
                Game1.addHUDMessage(new HUDMessage($"Unknown skill {skill}, available options are {options}.", 2));
                return;
            }
            SkillType skillType = Skills.First(x => x.Value.Equals(skill)).Key;
            settings.SpecializedSkillType = skillType;
            Helper.Data.WriteSaveData(SettingsKey, settings);
            Game1.addHUDMessage(new HUDMessage($"Updated specialized skill to {skill}!", 2));
        }
        
        private void OnUpdateTicked_UpdateExperience(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (!e.IsMultipleOf(15)) // quarter second
                return;

            if (!settings.SpecializedSkillType.HasValue)
                return;

            SkillType[] allowedSkills = { SkillType.Luck, settings.SpecializedSkillType.Value };

            foreach (SkillType skill in Skills.Keys)
            {
                if (allowedSkills.Contains(skill))
                    continue;
                int skillNum = (int)skill;
                if (Game1.player.experiencePoints[skillNum] == 0)
                    continue;
                string skillName = Skills[skill];
                Monitor.Log($"Experience reset for {skillName} as it is not the currently specialized skill", LogLevel.Debug);
                Game1.player.experiencePoints[skillNum] = 0;
            }
        }

        private void OnLevelChanged(object sender, LevelChangedEventArgs e)
        {
            if (!e.IsLocalPlayer || !settings.SpecializedSkillType.HasValue)
                return;

            SkillType[] allowedSkills = { SkillType.Luck, settings.SpecializedSkillType.Value };

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
    }
}