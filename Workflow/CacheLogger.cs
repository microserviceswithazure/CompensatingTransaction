namespace Workflow
{
    using System.Configuration;

    using Newtonsoft.Json;

    using StackExchange.Redis;

    public class CacheLogger
    {
        private readonly IDatabase cache;

        public CacheLogger()
        {
            var redisConnection = ConnectionMultiplexer.Connect(ConfigurationManager.AppSettings["redisConnection"]);
            this.cache = redisConnection.GetDatabase();
        }

        public void DeleteKey(string key)
        {
            if (this.cache.KeyExists(key))
            {
                this.cache.KeyDelete(key);
            }
        }

        public T ReadLog<T>(string key)
        {
            return JsonConvert.DeserializeObject<T>(this.cache.StringGet(key));
        }

        public void WriteLog<T>(string key, T logObject)
        {
            this.cache.StringSet(key, JsonConvert.SerializeObject(logObject));
        }
    }
}