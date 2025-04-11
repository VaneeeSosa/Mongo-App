using MongoDB.Bson;
using MongoDB.Driver;

namespace WebApplication1.Services
{
    public class SecurityService
    {
        private readonly IMongoClient _client;

        public SecurityService(IMongoClient client)
        {
            _client = client;
        }

        // Crear usuario
        public async Task CreateUserAsync(string database, string user, string password, string role)
        {
            var db = _client.GetDatabase(database);
            var command = new BsonDocument
            {
                ["createUser"] = user,
                ["pwd"] = password,
                ["roles"] = new BsonArray
                {
                    new BsonDocument
                    {
                        ["role"] = role,
                        ["db"] = database
                    }
                }
            };
            await db.RunCommandAsync<BsonDocument>(command);
        }

        // Listar usuarios
        public async Task<List<BsonDocument>> ListUsersAsync(string database)
        {
            var db = _client.GetDatabase(database);
            var users = await db.RunCommandAsync<BsonDocument>(new BsonDocument("usersInfo", 1));
            return users["users"].AsBsonArray.Select(x => x.AsBsonDocument).ToList();
        }

        // Eliminar usuario
        public async Task DeleteUserAsync(string database, string username)
        {
            var db = _client.GetDatabase(database);
            var command = new BsonDocument
    {
        { "dropUser", username }
    };

            try
            {
                await db.RunCommandAsync<BsonDocument>(command);
            }
            catch (Exception ex)
            {
                // Manejo general para cualquier tipo de error
                throw new Exception($"Error al eliminar el usuario {username}: {ex.Message}", ex);
            }
        }

    }
}
