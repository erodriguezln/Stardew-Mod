using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;
using bGamesPointsMod.Models;
using bGamesPointsMod.Controllers;

namespace bGamesPointsMod.Views
{
    public class MenuModView : IClickableMenu
    {
        private readonly IMonitor Monitor;
        private readonly IModHelper Helper;

        // Fondo del men�
        public Texture2D menuBg;
        public Rectangle positionMenuBg;

        // Estado de usuario
        public UserBgamesModel userBgamesModel;
        public UserBgamesController userBgamesController;

        // Control para mostrar/ocultar el men�
        private bool isMenuVisible = false;
        public bool IsMenuVisible => isMenuVisible;

        private ClickableComponent closeButton;

        // Guarda el ancho y alto de la ventana para detectar cambios
        private int lastViewportWidth;
        private int lastViewportHeight;

        public MenuModView(
            IModHelper helper,
            IMonitor monitor,
            UserBgamesModel userBgamesModel,
            UserBgamesController userBgamesController)
        {
            // Inicializaci�n
            this.Helper = helper ?? throw new ArgumentNullException(nameof(helper));
            this.Monitor = monitor;
            this.userBgamesModel = userBgamesModel;
            this.userBgamesController = userBgamesController;

            // Cargar el fondo del men�
            menuBg = helper.ModContent.Load<Texture2D>("assets/menubg.png");

            this.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            UpdateLayout();

        }
        public void UpdateLayout() {
            // Tama�o fijo del men�
            int menuWidth = 600; // Ancho fijo
            int menuHeight = 400; // Alto fijo

            // Posici�n centrada del men�
            positionMenuBg = new Rectangle(
                (Game1.viewport.Width - menuWidth) / 2,
                (Game1.viewport.Height - menuHeight) / 2,
                menuWidth,
                menuHeight
            );

            // Bot�n de cierre fijo en la esquina superior derecha del men�
            closeButton = new ClickableComponent(
                new Rectangle(
                    positionMenuBg.Right - 40, // Ajustado para estar al borde derecho del men�
                    positionMenuBg.Top + 10,  // Espaciado desde la parte superior
                    30,  // Ancho fijo
                    30   // Alto fijo
                ), "X");

            lastViewportWidth = Game1.viewport.Width;
            lastViewportHeight = Game1.viewport.Height;
        }


        public void ToggleMenu() {
            isMenuVisible = !isMenuVisible;
            Game1.activeClickableMenu = isMenuVisible ? this : null;

            if (isMenuVisible)
            {
                var userPoints = userBgamesController.GetUserPoints();
                Monitor.Log($"[MenuModView] Menu abierto. user_points_count={userPoints.Count}, user_name='{userBgamesModel.Name}', user_email='{userBgamesModel.Email}', user_age={userBgamesModel.Age}", LogLevel.Debug);
            }
        }

        public void RenderMenu(SpriteBatch spriteBatch)
        {
            if (isMenuVisible) {
                // Dibujar el fondo del men�
                spriteBatch.Draw(menuBg, positionMenuBg, Color.White);

                DrawCloseButton(spriteBatch);
                drawMouse(spriteBatch);

                // Dibujar informaci�n del usuario
                if (userBgamesModel != null)
                {
                    string userInfo = $"Name: {userBgamesModel.Name}\nEmail: {userBgamesModel.Email}\nAge: {userBgamesModel.Age}";
                    spriteBatch.DrawString(Game1.smallFont, userInfo, new Vector2(positionMenuBg.X + 20, positionMenuBg.Y + 40), Color.Black);
                }
                var userPoints = userBgamesController.GetUserPoints();
                if (userPoints.Count > 0)
                {
                    string userPointsInfo = "";
                    foreach (var points in userPoints)
                    {
                        userPointsInfo += $"{points.AttributeName}: {points.SnapshotPoints}\n";
                    }
                    spriteBatch.DrawString(Game1.smallFont, userPointsInfo, new Vector2(positionMenuBg.X + 400, positionMenuBg.Y + 40), Color.Black);
                }
            }
        }

        public void HandleButtonClick(ButtonPressedEventArgs e)
        {
            if (!isMenuVisible)
            {
                return;
            }

            if (e.Button == SButton.MouseLeft && closeButton.bounds.Contains(Game1.getMouseX(), Game1.getMouseY())) {
                Monitor.Log("Cerrar men�.", LogLevel.Info);
                Game1.activeClickableMenu = null; // Cierra el men�
                ToggleMenu(); // Cierra el menu
            }
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e) {
            // Detectar cambio en el tama�o de la ventana
            if (Game1.viewport.Width != lastViewportWidth || Game1.viewport.Height != lastViewportHeight) {
                UpdateLayout();
            }
        }

        private void DrawCloseButton(SpriteBatch spriteBatch) {
            int borderThickness = 2;

            // Dibujar el fondo del bot�n
            spriteBatch.Draw(Game1.staminaRect, closeButton.bounds, Color.Red);

            // Dibujar bordes del bot�n
            spriteBatch.Draw(Game1.staminaRect, new Rectangle(closeButton.bounds.X, closeButton.bounds.Y, closeButton.bounds.Width, borderThickness), Color.White);
            spriteBatch.Draw(Game1.staminaRect, new Rectangle(closeButton.bounds.X, closeButton.bounds.Y, borderThickness, closeButton.bounds.Height), Color.White);
            spriteBatch.Draw(Game1.staminaRect, new Rectangle(closeButton.bounds.X + closeButton.bounds.Width - borderThickness, closeButton.bounds.Y, borderThickness, closeButton.bounds.Height), Color.White);
            spriteBatch.Draw(Game1.staminaRect, new Rectangle(closeButton.bounds.X, closeButton.bounds.Y + closeButton.bounds.Height - borderThickness, closeButton.bounds.Width, borderThickness), Color.White);

            // Dibujar el texto "X"
            Vector2 textSize = Game1.smallFont.MeasureString(closeButton.name);
            Vector2 textPosition = new Vector2(
                closeButton.bounds.X + (closeButton.bounds.Width - textSize.X) / 2,
                closeButton.bounds.Y + (closeButton.bounds.Height - textSize.Y) / 2
            );
            Utility.drawTextWithShadow(spriteBatch, closeButton.name, Game1.smallFont, textPosition, Color.White);
        }
    }
}
