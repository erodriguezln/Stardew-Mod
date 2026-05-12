using StardewModdingAPI;
using System.Net.Http;
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
        public PointsBgamesModel PointsBgamesModel { get; private set; }

        public LoginResponse LoginResponse { get; private set; }
        public TokenModel TokenModel { get; private set; }

        public UserBgamesController(
            IMonitor monitor,
            IModHelper helper,
            UserBgamesModel userBgamesModel,
            PointsBgamesModel pointsBgamesModel)
        {
            this.Monitor = monitor;
            this.Helper = helper;
            this.UserBgamesModel = userBgamesModel; // Usa la instancia original pasada
            this.PointsBgamesModel = pointsBgamesModel;
            this.httpClient = new HttpClient(); // Inicializa el cliente HTTP
            this.credentialsFilePath = Path.Combine(helper.DirectoryPath, "users.json");
        }

        public async Task<bool> UserCheck(string email, string password)
        {
            string apiUrl = $"{Auth}/login";
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("username", email),
                    new KeyValuePair<string, string>("password", password)
                });

                HttpResponseMessage response = await httpClient.PostAsync(apiUrl, content);
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var userFromApi = JsonConvert.DeserializeObject<LoginResponse>(jsonResponse);

                if (response.IsSuccessStatusCode)
                {
                    Monitor.Log($"AccessToken: {userFromApi?.AccessToken}", LogLevel.Info);
                    return true; // Usuario encontrado
                }
                else
                {
                    Monitor.Log("Credenciales incorrectas.", LogLevel.Info);
                    return false; // Usuario no encontrado
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error al intentar hacer login: {ex.Message}", LogLevel.Error);
                return false; // Error durante la llamada a la API
            }
        }

        public async void SaveUserBgames(string email)
        {
            string loginUrl = $"{Auth}/login";
            var tokenRemainingUrl = $"{Auth}/token/remaining";
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(loginUrl);
                string jsonResponse = await response.Content.ReadAsStringAsync();
                LoginResponse userFromApi = JsonConvert.DeserializeObject<LoginResponse>(jsonResponse);

                if (response.IsSuccessStatusCode && userFromApi != null)
                {
                    LoginResponse.AccessToken = userFromApi.AccessToken;
                    LoginResponse.TokenType = userFromApi.TokenType;

                    HttpResponseMessage tokenResponse = await httpClient.GetAsync(tokenRemainingUrl);
                    string tokenJsonResponse = await tokenResponse.Content.ReadAsStringAsync();
                    TokenModel tokenInfo = JsonConvert.DeserializeObject<TokenModel>(tokenJsonResponse);

                    TokenModel.ExpiresAt = tokenInfo.ExpiresAt;

                    // Guarda los puntos del jugador en el objeto UserBgamesModel
                    // SaveUserPoints(int.Parse(userFromApi.Id_players));
                    SaveCredentials(LoginResponse.AccessToken, TokenModel.ExpiresAt);
                }
                else
                {
                    Monitor.Log("No se pudo guardar el usuario porque no hay datos disponibles.", LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error al guardar el usuario: {ex.Message}", LogLevel.Error);
            }
        }

        public bool CredentialsFileExists()
        {
            return File.Exists(this.credentialsFilePath);
        }

        // Esta funcion guarda el access_token en un archivo JSON y al moment ode cargar dichos datos ocupa la de usercheck
        public void SaveCredentials(string accessToken, DateTime expiresAt)
        {
            try
            {
                string jsonContent = $"{accessToken}";
                File.WriteAllText(this.credentialsFilePath, jsonContent);
                this.Monitor.Log("Access Token guardado exitosamente.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Error al guardar Access Token: {ex.Message}", LogLevel.Error);
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

                // Usar await para obtener el resultado de UserCheck
                int userCheckResult = await UserCheck(username, password);

                if (userCheckResult)
                {
                    SaveUserBgames(username);
                    return 1;
                }

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
                    UserBgamesModel.Points = pointsFromApi; // Actualiza la lista de puntos en la instancia original
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

        // funcion para gastar puntos en las habilidades
        public int SpendPoints(int spendPoints)
        {
            //Cada habilidad va a costar 5 puntos bGames o mas
            int i = spendPoints;
            if (UserBgamesModel.Points != null)
            {
                while (i > 0)
                {
                    foreach (var points in UserBgamesModel.Points)
                    {
                        // recorre el puntaje de cada tributo
                        if (0 < Int32.Parse(points.Data) && spendPoints > 0)
                        {
                            spendPoints--; //le se resta uno a los puntos a gastar
                            points.Data =
                                (Int32.Parse(points.Data) - 1).ToString(); // tambien resto uno a los puntos ctuales
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
                string userId = UserBgamesModel.Id_players;

                foreach (var points in UserBgamesModel.Points)
                {
                    // Crear el cuerpo del JSON
                    var payload = new
                    {
                        id_player = userId,
                        id_attributes = points.Id_attributes,
                        new_data = points.Data
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
                            Monitor.Log($"Puntos actualizados correctamente: {points.Name} ({points.Data})",
                                LogLevel.Info);
                        }
                        else
                        {
                            string error = await response.Content.ReadAsStringAsync();
                            Monitor.Log($"Error al actualizar los puntos para {points.Name}: {error}", LogLevel.Error);
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