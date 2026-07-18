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
        public LevelUpModel farmingSkill = null!;

        // Controladores
        public BuffController buffController = null!;
        public LevelUpController levelUpController = null!;

        public override void Entry(IModHelper helper)
        {
            // Inicializar modelos de level up
            miningSkill = new LevelUpModel(50, "Mining");
            foraningSkill = new LevelUpModel(50, "Foraging");
            fishingSkill = new LevelUpModel(50, "Fishing");
            combatSkill = new LevelUpModel(50, "Combat");
            farmingSkill = new LevelUpModel(50, "Farming");

            // Crear controladores
            buffController = new BuffController(this.Monitor, this.Helper);
            levelUpController = new LevelUpController(this.Monitor, this.Helper, miningSkill, foraningSkill,
                fishingSkill, combatSkill, farmingSkill);

            // Inicia el servidor HTTP, ApplyReward ejecuta el canje en el hilo principal.
            RewardHttpServer.Start(ApplyReward, message => this.Monitor.Log(message, LogLevel.Info));

            // Suscribir eventos del juego
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            // Actualiza la disponibilidad de recompensas al cargar una partida.
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            // Marca las recompensas como no disponibles al volver al título.
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            // Aplica los canjes pendientes en cada tick desde el hilo principal de Stardew.
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        // Se ejecuta al terminar de cargar una partida.
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            UpdateGameReady();
        }

        // Se ejecuta al volver al título. El listener queda activo para que /health informe la disponibilidad.
        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            RewardHttpServer.SetGameReady(false);
            RewardHttpServer.DrainPendingAndCancel();
        }

        // Transfiere cualquier reward recibido por el HTTP thread al main thread del juego.
        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            UpdateGameReady();
            RewardHttpServer.DrainPending();
        }

        // Actualiza el estado del juego para permitir o no la aplicacion de recompensas.
        private static void UpdateGameReady()
        {
            RewardHttpServer.SetGameReady(Context.IsWorldReady && Game1.player != null);
        }

        // Aplica una recompensa en Stardew y devuelve el resultado al servidor HTTP.
        // El server no conoce gameplay, solo entrega el id (la idea es que sea agnostico razon por la que se usa el mismo codigo en terraria).
        private RewardApplicationResult ApplyReward(int rewardId)
        {
            if (!Context.IsWorldReady || Game1.player == null)
            {
                Monitor.Log($"Reward '{rewardId}' ignored because the world is not ready.", LogLevel.Warn);
                return new RewardApplicationResult(false, "game_not_ready",
                    "No game is ready to apply rewards.");
            }

            switch (rewardId)
            {
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
                    if (!wasReducedEnergyActive)
                    {
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
                    return new RewardApplicationResult(false, "unknown_reward",
                        "Reward is not supported.");
            }

            Monitor.Log($"Reward applied: reward_id={rewardId}.", LogLevel.Info);
            return new RewardApplicationResult(true);
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            // Verifica si el jugador tiene un pico equipado
            if (Game1.player.CurrentTool is Pickaxe pickaxe)
            {
                // Restauramos la velocidad de animación del pico a su valor por defecto (1.0)
                pickaxe.AnimationSpeedModifier = 1.0f;
                this.Monitor.Log("Nuevo día: Se restauró la velocidad de animación del pico a 1.0.", LogLevel.Info);
            }
        }
    }
}