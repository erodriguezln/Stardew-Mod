using bGamesPointsMod.Models;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Tools;
using StardewValley;

namespace bGamesPointsMod.Controllers
{
    public class LevelUpController
    {
        private readonly IMonitor Monitor;
        private readonly IModHelper Helper;
        private LevelUpModel miningSkill;
        private LevelUpModel foraningSkill;
        private LevelUpModel fishingSkill;
        private LevelUpModel combatSkill;
        private LevelUpModel farmingSkill;

        public LevelUpController(
            IMonitor monitor,
            IModHelper helper,
            LevelUpModel miningSkill,
            LevelUpModel foraningSkill,
            LevelUpModel fishingSkill,
            LevelUpModel combatSkill,
            LevelUpModel farmingSkill
        )
        {
            Monitor = monitor;
            Helper = helper;
            this.miningSkill = miningSkill;
            this.foraningSkill = foraningSkill;
            this.fishingSkill = fishingSkill;
            this.combatSkill = combatSkill;
            this.farmingSkill = farmingSkill;
        }

        public void SkillMining()
        {
            if (!Context.IsWorldReady || Game1.player == null)
                return;
            else
            {
                // Incrementa el nivel de minería del jugador en los puntos de experiencia del modelo.
                Game1.player.gainExperience(Farmer.miningSkill, miningSkill.Experience);

                // Muestra un mensaje en la consola del mod y en el chat del juego de forma dinámica.
                Monitor.Log($"¡Has ganado {miningSkill.Experience} puntos de experiencia en minería!", LogLevel.Info);
                Game1.addHUDMessage(new HUDMessage("¡Tu habilidad de Mineria ha incrementado!", 2));
            }
        }

        public void SkillForaging()
        {
            if (!Context.IsWorldReady || Game1.player == null)
                return;
            else
            {
                // Incrementa el nivel de forrajeo del jugador en los puntos de experiencia del modelo.
                Game1.player.gainExperience(Farmer.foragingSkill, foraningSkill.Experience);

                // Muestra un mensaje en la consola del mod y en el chat del juego de forma dinámica.
                Monitor.Log($"¡Has ganado {foraningSkill.Experience} puntos de experiencia en tala!", LogLevel.Info);
                Game1.addHUDMessage(new HUDMessage("¡Tu habilidad de Recoleccion ha incrementado!", 2));
            }
        }

        public void SkillFishing()
        {
            if (!Context.IsWorldReady || Game1.player == null)
                return;
            else
            {
                // Incrementa el nivel de pesca del jugador en los puntos de experiencia del modelo.
                Game1.player.gainExperience(Farmer.fishingSkill, fishingSkill.Experience);

                // Muestra un mensaje en la consola del mod y en el chat del juego de forma dinámica.
                Monitor.Log($"¡Has ganado {fishingSkill.Experience} puntos de experiencia en pesca!", LogLevel.Info);
                Game1.addHUDMessage(new HUDMessage("¡Tu habilidad de Pesca ha incrementado!", 2));
            }
        }

        public void SkillCombat()
        {
            if (!Context.IsWorldReady || Game1.player == null)
                return;
            else
            {
                // Incrementa el nivel de combate del jugador en los puntos de experiencia del modelo.
                Game1.player.gainExperience(Farmer.combatSkill, combatSkill.Experience);

                // Muestra un mensaje en la consola del mod y en el chat del juego de forma dinámica.
                Monitor.Log($"¡Has ganado {combatSkill.Experience} puntos de experiencia en combate!", LogLevel.Info);
                Game1.addHUDMessage(new HUDMessage("¡Tu habilidad de Combate ha incrementado!", 2));
            }
        }

        public void SkillFarming()
        {
            if (!Context.IsWorldReady || Game1.player == null)
                return;
            else
            {
                // FIX: Se aplicó exp a Farmer.farmingSkill, ya que en Stardew Valley base, Luck
                // no es una habilidad con nivel ni experiencia, sino una estadística diaria/temporal.
                Game1.player.gainExperience(Farmer.farmingSkill, farmingSkill.Experience);

                // Muestra un mensaje en la consola del mod y en el chat del juego de forma dinámica.
                Monitor.Log($"¡Has ganado {farmingSkill.Experience} puntos de experiencia en Farmeo!", LogLevel.Info);
                Game1.addHUDMessage(new HUDMessage("¡Tu habilidad de Agricultura ha incrementado!", 2));
            }
        }
    }
}