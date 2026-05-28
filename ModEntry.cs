using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;
using bGamesPointsMod.Models;
using bGamesPointsMod.Controllers;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Buffs;
using bGamesPointsMod.Views;

namespace bGamesPointsMod
{
    internal sealed class ModEntry : Mod
    {
        // Modelos de Buffs
        public UserBgamesModel userBgamesModel;
        public LevelUpModel miningSkill;
        public LevelUpModel foraningSkill;
        public LevelUpModel fishingSkill;
        public LevelUpModel combatSkill;
        public LevelUpModel luckSkill;
        bool CheckConnection;

        // Controlador de Buffs
        public BuffController buffController;
        public UserBgamesController userController;
        public LevelUpController levelUpController;

        // Boton de menu de mod
        private Texture2D bTMenuMod;
        private Rectangle bBMenuMod;
        private MenuModView menu;// Instancia de la clase Menu

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


            // Mostrar boton en pantalla del menu
            bTMenuMod = helper.ModContent.Load<Texture2D>("assets/logo.png");
            bBMenuMod = new Rectangle(10, 10, 40, 40);

            // Crear instancia de BuffController
            userBgamesModel = new UserBgamesModel();
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
            userController = new UserBgamesController(this.Monitor, userBgamesModel);

            // Crear instancia de LevelUpController
            levelUpController = new LevelUpController(this.Monitor, this.Helper, miningSkill, foraningSkill, fishingSkill, combatSkill, luckSkill);

            // Crear instancia de Menu
            menu = new MenuModView(helper, Monitor, userBgamesModel, userController);

            

            // Mostrar boton en pantalla
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Display.RenderedHud += OnRenderedHud;
            //helper.Events.Input.ButtonPressed += OnButtonPressedLogin;
        }
        private async void OnDayStarted(object sender, DayStartedEventArgs e) {
            CheckConnection = await userController.CheckConnectionLsg();

            // Verifica si el jugador tiene un pico equipado
            if (Game1.player.CurrentTool is Pickaxe pickaxe) {
                // Restauramos la velocidad de animación del pico a su valor por defecto (1.0)
                pickaxe.AnimationSpeedModifier = 1.0f;
                this.Monitor.Log("Nuevo día: Se restauró la velocidad de animación del pico a 1.0.", LogLevel.Info);
            }
            if (CheckConnection) {
                this.Monitor.Log("Conexion con lsg-client establecida!", LogLevel.Info);

                bool hasSession = await userController.TryLoadCurrentSession();
                if (hasSession) {
                    this.Monitor.Log($"Sesion detectada en lsg-client para player_id={userBgamesModel.IdPlayers}.", LogLevel.Info);
                }
                else {
                    this.Monitor.Log("lsg-client esta activo pero no hay sesion iniciada.", LogLevel.Info);
                }
            }
            else {
                userController.ClearCurrentUserState();
                this.Monitor.Log("Sin conexion con lsg-client!", LogLevel.Info);
            }
        }
        private async void OnButtonPressed(object? sender, ButtonPressedEventArgs e) {
            CheckConnection = await userController.CheckConnectionLsg();

            if (!Context.IsWorldReady) return;

            if (e.Button == SButton.MouseLeft && bBMenuMod.Contains(Game1.getMouseX(), Game1.getMouseY())) {
                if (CheckConnection) {
                    this.Monitor.Log("Conexion con lsg-client establecida!", LogLevel.Info);

                    bool hasSession = await userController.TryLoadCurrentSession();
                    if (hasSession) {
                        Game1.addHUDMessage(new HUDMessage("LSG Client session loaded!", 2));
                        Monitor.Log("Boton de menu clickeado.", LogLevel.Info);
                        menu.ToggleMenu();
                    }
                    else {
                        Game1.addHUDMessage(new HUDMessage("Sign in from LSG Client first!", 2));
                        this.Monitor.Log("No hay sesion activa en lsg-client.", LogLevel.Info);
                    }
                }
                else {
                    userController.ClearCurrentUserState();
                    this.Monitor.Log("Sin conexion con lsg-client!", LogLevel.Info);
                    Game1.addHUDMessage(new HUDMessage("No connection with LSG Client!", 2));
                    Game1.addHUDMessage(new HUDMessage("Start lsg-client to continue!", 2));

                }
            }
            else {
                menu.HandleButtonClick(e);
            }
        }
        private void OnRenderedHud(object sender, RenderedHudEventArgs e) {
            var spriteBatch = e.SpriteBatch;

            spriteBatch.Draw(bTMenuMod, bBMenuMod, Color.White);
            menu.RenderMenu(spriteBatch);
        }
    }
}
