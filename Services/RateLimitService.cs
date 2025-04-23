using Microsoft.Extensions.Caching.Memory;

namespace AstroCloud.Services
{
    public class RateLimitService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<RateLimitService> _logger;

        public RateLimitService(IMemoryCache cache, ILogger<RateLimitService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public bool IsRateLimited(string key, int maxAttempts, TimeSpan interval)
        {
            var cacheKey = $"rate_limit_{key}";

            if (!_cache.TryGetValue<RateLimitRecord>(cacheKey, out var record))
            {
                record = new RateLimitRecord
                {
                    Attempts = 1,
                    FirstAttempt = DateTime.UtcNow
                };
                _cache.Set(cacheKey, record, interval);
                return false;
            }

            record.Attempts++;
            _cache.Set(cacheKey, record, interval);

            if (record.Attempts > maxAttempts)
            {
                _logger.LogWarning($"Rate limit exceeded for {key}. Attempts: {record.Attempts}");
                return true;
            }

            return false;
        }

        private class RateLimitRecord
        {
            public int Attempts { get; set; }
            public DateTime FirstAttempt { get; set; }
        }
    }
}
