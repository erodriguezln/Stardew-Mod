using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;
using bGamesPointsMod.Models;
using bGamesPointsMod.Controllers;
using StardewValley.Buffs;

namespace bGamesPointsMod
{
    internal sealed class ModEntry : Mod
    {
        // Modelos de Habilidades
        public LevelUpModel miningSkill = null!;
        public LevelUpModel foraningSkill = null!;
        public LevelUpModel fishingSkill = null!;
        public LevelUpModel combatSkill = null!;
        public LevelUpModel luckSkill = null!;

        // Controladores
        public BuffController buffController = null!;
        public LevelUpController levelUpController = null!;

        public override void Entry(IModHelper helper)
        {
            // Inicializar Buffs
            Buff miningBuff = new Buff(id: "Mining speed",
                displayName: "Speed mining buff",
                iconTexture: null,
                iconSheetIndex: 0,
                duration: 10_000, // 10 segundos
                effects: new BuffEffects()
                {MiningLevel = { 10 }});

            Buff foragingBuff = new Buff(
                id: "Foraging speed",
                displayName: "Speed foraging buff",
                iconTexture: null,
                iconSheetIndex: 0,
                duration: 10_000, // 10 segundos
                effects: new BuffEffects()
                {ForagingLevel = { 10 }});

            Buff speedBuff = new Buff(
                id: "walking speed",
                displayName: "Walking speed buff",
                iconTexture: null,
                iconSheetIndex: 0,
                duration: 10_000, // 10 segundos
                effects: new BuffEffects()
                {Speed = { 10 }});

            Buff reducedEnergyBuff = new Buff(
                id: "Reduced Energy",
                displayName: "Speed mining buff",
                iconTexture: null,
                iconSheetIndex: 0,
                duration: 30_000); // 10 segundos

            Buff luckLevelBuff = new Buff(id: "Luck level up",
                displayName: "Luck level up",
                iconTexture: null,
                iconSheetIndex: 0,
                duration: 10_000, // 10 segundos
                effects: new BuffEffects()
                {LuckLevel = { 1 }});

            Buff fishingBuff = new Buff(id: "Fishing speed",
                displayName: "Fishing speed buff",
                iconTexture: null,
                iconSheetIndex: 0,
                duration: 10_000, // 10 segundos
                effects: new BuffEffects()
                {FishingLevel = { 5 }});

            Buff farmingBuff = new Buff(id: "Farming speed",
                displayName: "Speed farming buff",
                iconTexture: null,
                iconSheetIndex: 0,
                duration: 10_000, // 10 segundos
                effects: new BuffEffects()
                {FarmingLevel = { 5 }});

            // Inicializar modelos de level up
            miningSkill = new LevelUpModel(50, "Mining");
            foraningSkill = new LevelUpModel(50, "Foraging");
            fishingSkill = new LevelUpModel(50, "Fishing");
            combatSkill = new LevelUpModel(50, "Combat");
            luckSkill = new LevelUpModel(50, "Luck");

            // Crear controladores
            buffController = new BuffController(
                this.Monitor, 
                this.Helper, 
                miningBuff, 
                foragingBuff, 
                speedBuff, 
                reducedEnergyBuff,
                luckLevelBuff,
                fishingBuff,
                farmingBuff);
            levelUpController = new LevelUpController(this.Monitor, this.Helper, miningSkill, foraningSkill, fishingSkill, combatSkill, luckSkill);

            // Inicia el servidor HTTP de recompensas en segundo plano.
            // El server solo recibe reward_id y deja la ejecucion final para ApplyReward en main thread.
            RewardHttpServer.Start(ApplyReward, message => this.Monitor.Log(message, LogLevel.Info));

            // Suscribir eventos del juego
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            // Reintenta iniciar el servidor cuando se carga una partida, por si fue detenido al volver al titulo.
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            // Detiene el servidor al volver al titulo para limpiar el listener y el estado pendiente.
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            // Drena recompensas pendientes en cada tick para aplicar efectos desde el main thread de Stardew.
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        // Se ejecuta cuando termina de cargar una partida (se carga un save, etc). Mantiene activo el endpoint POST /reward durante gameplay.
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) {
            RewardHttpServer.Start(ApplyReward, message => this.Monitor.Log(message, LogLevel.Info));
        }

        // Se ejecuta al volver al título. Cierra el listener HTTP para evitar estado viejo entre saves.
        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e) {
            RewardHttpServer.Stop();
        }

        // Transfiere cualquier reward recibido por el HTTP thread al main thread del juego.
        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e) {
            RewardHttpServer.DrainPending();
        }

        // Callback registrado en RewardHttpServer.Start(). Materializa el reward_id recibido por HTTP.
        // El server no conoce gameplay, solo entrega el id (la idea es que sea agnostico razon por la que se usa el mismo codigo en terraria).
        private void ApplyReward(int rewardId) {
            if (!Context.IsWorldReady || Game1.player == null) {
                Monitor.Log($"Reward '{rewardId}' ignored because the world is not ready.", LogLevel.Warn);
                return;
            }

            switch (rewardId) {
                // Buffs temporales.
                case 1:
                    buffController.BuffSpeed();
                    break;
                case 2:
                    buffController.BuffMining();
                    break;
                case 3:
                    buffController.BuffForaging();
                    break;
                case 4:
                    buffController.BuffFishing();
                    break;
                case 5:
                    buffController.BuffFarming();
                    break;
                case 6:
                    buffController.BuffLuckLevel();
                    break;
                case 7:
                    bool wasReducedEnergyActive = buffController.IsActiveReducedEnergyBuff;
                    buffController.ReducedEnergyBuff();
                    if (!wasReducedEnergyActive) {
                        this.Helper.Events.GameLoop.UpdateTicked += buffController.OnUpdateTickedReducedEnergyBuff;
                    }
                    break;

                // Recompensas de experiencia.
                case 101:
                    levelUpController.SkillMining();
                    break;
                case 102:
                    levelUpController.SkillForaging();
                    break;
                case 103:
                    levelUpController.SkillFishing();
                    break;
                case 104:
                    levelUpController.SkillCombat();
                    break;
                case 105:
                    levelUpController.SkillFarming();
                    break;
                default:
                    Monitor.Log($"Unknown reward_id={rewardId}. Reward ignored.", LogLevel.Warn);
                    return;
            }

            Monitor.Log($"Reward applied: reward_id={rewardId}.", LogLevel.Info);
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e) {
            // Verifica si el jugador tiene un pico equipado
            if (Game1.player.CurrentTool is Pickaxe pickaxe) {
                // Restauramos la velocidad de animación del pico a su valor por defecto (1.0)
                pickaxe.AnimationSpeedModifier = 1.0f;
                this.Monitor.Log("Nuevo día: Se restauró la velocidad de animación del pico a 1.0.", LogLevel.Info);
            }
        }
    }
}
