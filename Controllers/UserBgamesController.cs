using StardewModdingAPI;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using bGamesPointsMod.Models;

namespace bGamesPointsMod.Controllers
{
    public class UserBgamesController
    {
        private const string LocalApi = "http://localhost:3333";

        private readonly IMonitor Monitor;
        private readonly HttpClient httpClient;


        public UserBgamesModel UserBgamesModel { get; private set; }
        public List<PointsBgamesModel> UserPoints { get; private set; } = new();

        public UserBgamesController(
            IMonitor monitor,
            UserBgamesModel userBgamesModel)
        {
            this.Monitor = monitor;
            this.UserBgamesModel = userBgamesModel;
            this.httpClient = new HttpClient();
        }

        public async Task<bool> TryLoadCurrentSession()
        {
            try
            {
                ClearCurrentUserState();

                string whoAmIUrl = $"{LocalApi}/whoami";
                using var response = await httpClient.GetAsync(whoAmIUrl);
                string jsonResponse = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Monitor.Log("[UserBgamesController] lsg-client esta activo pero no hay sesion autenticada.", LogLevel.Info);
                    return false;
                }

                if (!response.IsSuccessStatusCode)
                {
                    Monitor.Log($"[UserBgamesController] No se pudo cargar whoami. Status: {(int)response.StatusCode}. Body: {jsonResponse}", LogLevel.Warn);
                    return false;
                }

                var currentUser = JsonConvert.DeserializeObject<UserBgamesModel>(jsonResponse);
                if (currentUser == null || currentUser.IdPlayers <= 0)
                {
                    Monitor.Log("[UserBgamesController] whoami respondio sin un player valido.", LogLevel.Warn);
                    return false;
                }

                ApplyCurrentUser(currentUser);
                await SaveUserPoints(currentUser.IdPlayers);

                Monitor.Log($"[UserBgamesController] Sesion cargada desde lsg-client para player_id={currentUser.IdPlayers}.", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                Monitor.Log($"[UserBgamesController] Error al cargar la sesion desde lsg-client: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        public async Task SaveUserPoints(int idPlayer)
        {
            string apiUrl = $"{LocalApi}/players/{idPlayer}/attributes/points";
            try
            {
                Monitor.Log($"[UserBgamesController] Solicitando puntos a {apiUrl} para player_id={idPlayer}.", LogLevel.Debug);
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                string jsonResponse = await response.Content.ReadAsStringAsync();

                Monitor.Log($"[UserBgamesController] Respuesta puntos status={(int)response.StatusCode}. body={jsonResponse}", LogLevel.Debug);

                if (response.IsSuccessStatusCode)
                {
                    List<PointsBgamesModel>? pointsFromApi =
                        JsonConvert.DeserializeObject<List<PointsBgamesModel>>(jsonResponse);
                    UserPoints = pointsFromApi ?? new List<PointsBgamesModel>();
                    Monitor.Log($"[UserBgamesController] Puntos cargados correctamente. total={UserPoints.Count}", LogLevel.Info);

                    foreach (var points in UserPoints)
                    {
                        Monitor.Log($"[UserBgamesController] Punto -> attr_id={points.Id_attributes}, attr={points.AttributeName}, snapshot={points.SnapshotPoints}, ledger={points.BalanceLedger}, diff={points.DiffLedgerMinusSnapshot}", LogLevel.Debug);
                    }
                }
                else
                {
                    UserPoints = new List<PointsBgamesModel>();
                    Monitor.Log("No se pudo cargar el puntaje del usuario porque no hay datos disponibles.",
                        LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error al cargar los puntos: {ex.Message}", LogLevel.Error);
            }
        }

        private void ApplyCurrentUser(UserBgamesModel currentUser)
        {
            UserBgamesModel.IdPlayers = currentUser.IdPlayers;
            UserBgamesModel.Name = currentUser.Name;
            UserBgamesModel.Email = currentUser.Email;
            UserBgamesModel.Age = currentUser.Age;
            UserBgamesModel.Roles = currentUser.Roles ?? new List<string>();
        }

        public void ClearCurrentUserState()
        {
            UserBgamesModel.IdPlayers = 0;
            UserBgamesModel.Name = string.Empty;
            UserBgamesModel.Email = string.Empty;
            UserBgamesModel.Age = 0;
            UserBgamesModel.Roles = new List<string>();
            UserPoints = new List<PointsBgamesModel>();
        }

        public IReadOnlyList<PointsBgamesModel> GetUserPoints()
        {
            return UserPoints;
        }

        // Health check
        public async Task<bool> CheckConnectionLsg()
        {
            try
            {
                var response = await httpClient.GetAsync($"{LocalApi}/health");
                if (!response.IsSuccessStatusCode)
                {
                    Monitor.Log(
                        $"[UserBgamesController] No se pudo establecer la conexion con lsg-client health (StatusCode: {response.StatusCode}).",
                        LogLevel.Warn);
                    return false;
                }

                var json = await response.Content.ReadAsStringAsync();
                var health = JsonConvert.DeserializeObject<HealthResponse>(json);
                if (health?.Status?.Equals("ok", StringComparison.OrdinalIgnoreCase) == true)
                {
                    Monitor.Log("[UserBgamesController] Conexion con lsg-client establecida.", LogLevel.Info);
                    return true;
                }

                Monitor.Log(
                    $"[UserBgamesController] Health de lsg-client respondio sin status ok. Status: {health?.Status}",
                    LogLevel.Warn);
                return false;
            }
            catch (Exception ex)
            {
                Monitor.Log($"[UserBgamesController] Error al intentar conectar con lsg-client health: {ex.Message}",
                    LogLevel.Error);
                return false;
            }
        }
    }
}
