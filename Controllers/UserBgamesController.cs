using StardewModdingAPI;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json; // Necesitarás instalar este paquete para trabajar con JSON.
using bGamesPointsMod.Models;
using System.Text;

namespace bGamesPointsMod.Controllers
{
    public class UserBgamesController
    {
        private const string Auth = "http://localhost:8001";
        private const string Core = "http://localhost:8012";

        private readonly IMonitor Monitor;
        private readonly IModHelper Helper;
        private readonly HttpClient httpClient; // Cliente HTTP para hacer las peticiones a la API externa
        private readonly string credentialsFilePath; // Ruta de acceso al archivo de credenciales


        public UserBgamesModel UserBgamesModel { get; private set; }
        public List<PointsBgamesModel> UserPoints { get; private set; } = new();

        public LoginResponse? LoginResponse { get; private set; }
        public TokenModel? TokenModel { get; private set; }
        public SaveSessionModel? SavedSession { get; private set; }

        public UserBgamesController(
            IMonitor monitor,
            IModHelper helper,
            UserBgamesModel userBgamesModel)
        {
            this.Monitor = monitor;
            this.Helper = helper;
            this.UserBgamesModel = userBgamesModel; // Usa la instancia original pasada
            this.httpClient = new HttpClient(); // Inicializa el cliente HTTP
            this.credentialsFilePath = Path.Combine(helper.DirectoryPath, "users.json");
        }

        public async Task<bool> UserCheck(string email, string password)
        {
            const string apiUrl = $"{Auth}/login";
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("username", email),
                    new KeyValuePair<string, string>("password", password)
                });

                using var response = await httpClient.PostAsync(apiUrl, content);
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var userFromApi = JsonConvert.DeserializeObject<LoginResponse>(jsonResponse);

                if (response.IsSuccessStatusCode && userFromApi != null &&
                    !string.IsNullOrEmpty(userFromApi.AccessToken))
                {
                    // Guarda el token de acceso en la propiedad de la clase
                    LoginResponse = userFromApi;

                    Monitor.Log($"[UserBgamesController] AccessToken: {userFromApi?.AccessToken}", LogLevel.Info);
                    return true;
                }

                Monitor.Log("[UserBgamesController] Credenciales incorrectas.", LogLevel.Info);
                return false;
            }
            catch (Exception ex)
            {
                Monitor.Log($"[UserBgamesController] Error al intentar hacer login: {ex.Message}", LogLevel.Error);
                return false; // Error durante la llamada a la API
            }
        }

        public async Task<bool> SaveSession()
        {
            const string apiUrl = $"{Auth}/token/remaining";
            try
            {
                if (LoginResponse == null || string.IsNullOrWhiteSpace(LoginResponse.AccessToken))
                {
                    Monitor.Log("[UserBgamesController] No se puede guardar la sesión porque no hay un token de acceso válido.",
                        LogLevel.Warn);
                    return false;
                }

                using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", LoginResponse.AccessToken);

                using var response = await httpClient.SendAsync(request);
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var tokenInfo = JsonConvert.DeserializeObject<TokenModel>(jsonResponse);

                if (response.IsSuccessStatusCode && tokenInfo != null)
                {
                    TokenModel = tokenInfo;
                    SaveCredentials(LoginResponse.AccessToken, TokenModel.ExpiresAt);
                    Monitor.Log("[UserBgamesController] Sesión guardada exitosamente.", LogLevel.Info);
                    return true;
                }

                Monitor.Log($"[UserBgamesController] No se pudo guardar la sesion. Status: {response.StatusCode}. Body: {jsonResponse}",
                    LogLevel.Warn);
                return false;
            }
            catch (Exception ex)
            {
                Monitor.Log($"[UserBgamesController] Error al guardar la sesion: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        public bool CredentialsFileExists()
        {
            return File.Exists(this.credentialsFilePath);
        }

        // Guardar credenciales en un archivo JSON
        private void SaveCredentials(string accessToken, DateTime expiresAt)
        {
            try
            {
                SavedSession = new SaveSessionModel
                {
                    AccessToken = accessToken,
                    ExpiresAt = expiresAt.ToUniversalTime() // Guardar en UTC para evitar problemas de zona horaria
                };


                // Serializar el objeto a JSON
                var jsonContent = JsonConvert.SerializeObject(SavedSession, Formatting.Indented);
                File.WriteAllText(credentialsFilePath, jsonContent);
                Monitor.Log("Credenciales guardadas exitosamente.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error al guardar la credenciales: {ex.Message}", LogLevel.Error);
            }
        }

        // Cargar credenciales desde el archivo
        public async Task<int> LoadCredentials()
        {
            try
            {
                // Verificar si el archivo existe antes de leerlo
                if (!CredentialsFileExists())
                {
                    this.Monitor.Log("No se encontraron credenciales para cargar.", LogLevel.Warn);
                    return 0;
                }

                // Leer y deserializar las credenciales
                string jsonContent = File.ReadAllText(this.credentialsFilePath);
                var credentials = JsonConvert.DeserializeObject<dynamic>(jsonContent);

                string accessToken = credentials.accessToken;

                this.Monitor.Log($"Credenciales cargadas: Usuario: {accessToken}", LogLevel.Info);

                // TODO: Si no existe el archivo forzar login.

                return 0; // Credenciales incorrectas
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Error al cargar las credenciales: {ex.Message}", LogLevel.Error);
                return 0;
            }
        }

        public async void SaveUserPoints(int idPlayer)
        {
            string apiUrl = $"{Core}/player_all_attributes/{idPlayer}";
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                string jsonResponse = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && response != null)
                {
                    List<PointsBgamesModel> pointsFromApi =
                        JsonConvert.DeserializeObject<List<PointsBgamesModel>>(jsonResponse);
                    UserPoints = pointsFromApi ?? new List<PointsBgamesModel>();
                    Monitor.Log("Puntos del usuario guardados correctamente.", LogLevel.Info);
                }
                else
                {
                    Monitor.Log("No se pudo guardar el puntaje del usuario porque no hay datos disponibles.",
                        LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error al guardar los puntos: {ex.Message}", LogLevel.Error);
            }
        }

        public UserBgamesModel GetUser()
        {
            return UserBgamesModel;
        }

        public IReadOnlyList<PointsBgamesModel> GetUserPoints()
        {
            return UserPoints;
        }

        // funcion para gastar puntos en las habilidades
        public int SpendPoints(int spendPoints)
        {
            //Cada habilidad va a costar 5 puntos bGames o mas
            int i = spendPoints;
            if (UserPoints.Count > 0)
            {
                while (i > 0)
                {
                    foreach (var points in UserPoints)
                    {
                        // recorre el puntaje de cada tributo
                        if (points.SnapshotPoints > 0 && spendPoints > 0)
                        {
                            spendPoints--; //le se resta uno a los puntos a gastar
                            points.SnapshotPoints -= 1; // tambien resto uno a los puntos actuales
                        }
                    }

                    if (spendPoints == 0)
                    {
                        return 1;
                    }

                    i--;
                }

                if (spendPoints > 0)
                {
                    return 0; // si no se gasto todo lo que se queria gastar se sale de la funcion
                }
            }

            return 0;
        }

        public async Task SavePointsBgames()
        {
            try
            {
                int userId = UserBgamesModel.IdPlayers;

                foreach (var points in UserPoints)
                {
                    // Crear el cuerpo del JSON
                    var payload = new
                    {
                        id_player = userId,
                        id_attributes = points.Id_attributes,
                        new_data = points.SnapshotPoints
                    };

                    // Serializar el JSON
                    string jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

                    // Configurar la solicitud HTTP
                    using (var content = new StringContent(jsonContent, Encoding.UTF8, "application/json"))
                    {
                        // Realizar la solicitud PUT
                        HttpResponseMessage response =
                            await httpClient.PutAsync($"{Core}/player_attributes_single", content);

                        // Verificar la respuesta
                        if (response.IsSuccessStatusCode)
                        {
                            Monitor.Log($"Puntos actualizados correctamente: {points.AttributeName} ({points.SnapshotPoints})",
                                LogLevel.Info);
                        }
                        else
                        {
                            string error = await response.Content.ReadAsStringAsync();
                            Monitor.Log($"Error al actualizar los puntos para {points.AttributeName}: {error}", LogLevel.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error al guardar los puntos: {ex.Message}", LogLevel.Error);
            }
        }


        // Health check
        public async Task<bool> CheckConnectionLsg()
        {
            try
            {
                var response = await httpClient.GetAsync($"{Core}/health");
                if (!response.IsSuccessStatusCode)
                {
                    Monitor.Log(
                        $"[UserBgamesController] No se pudo establecer la conexión a health (StatusCode: {response.StatusCode}).",
                        LogLevel.Warn);
                    return false;
                }

                var json = await response.Content.ReadAsStringAsync();
                var health = JsonConvert.DeserializeObject<HealthResponse>(json);
                if (health?.Status?.Equals("ok", StringComparison.OrdinalIgnoreCase) == true)
                {
                    Monitor.Log("[UserBgamesController] Conexion a health establecida.", LogLevel.Info);
                    return true;
                }

                Monitor.Log(
                    $"[UserBgamesController] Conexión a health establecida, pero status no fue 'ok'. Status: {health?.Status}",
                    LogLevel.Warn);
                return false;
            }
            catch (Exception ex)
            {
                Monitor.Log($"[UserBgamesController] Error al intentar conectar con health: {ex.Message}",
                    LogLevel.Error);
                return false;
            }
        }
    }
}
