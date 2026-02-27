using AionPlugin.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;

namespace AionPlugin.Services;

/// <summary>
/// aion2tools.com 에서 캐릭터 아툴 점수를 가져옵니다.
///
/// aion2tools.com 의 공개 API 엔드포인트를 사용합니다.
/// API 응답 구조가 변경되면 파서를 업데이트하세요.
/// </summary>
public class AionToolsService : IDisposable
{
    private const string BaseUrl = "https://aion2tools.com";

    private readonly HttpClient _http;

    // 같은 캐릭터를 반복 요청하지 않도록 캐시 (TTL: 10분)
    private readonly Dictionary<string, (AtuulScore Score, DateTime CachedAt)> _cache = new();
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(10);

    public AionToolsService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("User-Agent",
            "AionPlugin/1.0 (https://github.com/taengkim/aion2plugin)");
        _http.Timeout = TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// 캐릭터명으로 아툴 점수를 가져옵니다.
    /// </summary>
    /// <param name="characterName">캐릭터명</param>
    /// <param name="serverName">서버명 (예: "Israphel")</param>
    public async Task<AtuulScore?> GetScoreAsync(
        string characterName,
        string serverName = "",
        CancellationToken ct = default)
    {
        string cacheKey = $"{serverName}:{characterName}".ToLowerInvariant();

        // 캐시 확인
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            if (DateTime.UtcNow - cached.CachedAt < _cacheTtl)
                return cached.Score;
        }

        try
        {
            var score = await FetchScoreAsync(characterName, serverName, ct);
            if (score is not null)
                _cache[cacheKey] = (score, DateTime.UtcNow);
            return score;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // 네트워크 오류 시 캐시된 값 반환 (만료되었더라도)
            return _cache.TryGetValue(cacheKey, out var old) ? old.Score : null;
        }
    }

    private async Task<AtuulScore?> FetchScoreAsync(
        string characterName,
        string serverName,
        CancellationToken ct)
    {
        // aion2tools.com API 엔드포인트
        // 실제 API 경로는 사이트를 확인하여 업데이트 필요
        string url = string.IsNullOrEmpty(serverName)
            ? $"{BaseUrl}/api/character/{Uri.EscapeDataString(characterName)}"
            : $"{BaseUrl}/api/character/{Uri.EscapeDataString(serverName)}/{Uri.EscapeDataString(characterName)}";

        var response = await _http.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            // 404 = 캐릭터 없음
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
        }

        string json = await response.Content.ReadAsStringAsync(ct);
        return ParseResponse(json, characterName, serverName);
    }

    /// <summary>
    /// API 응답 JSON 파싱
    /// aion2tools.com 의 실제 응답 구조에 맞게 업데이트 필요
    /// </summary>
    private static AtuulScore? ParseResponse(string json, string characterName, string serverName)
    {
        try
        {
            var obj = JObject.Parse(json);

            // aion2tools.com 응답 예시 구조 (실제와 다를 수 있음):
            // {
            //   "name": "...",
            //   "server": "...",
            //   "class": "...",
            //   "level": 60,
            //   "score": 12345,
            //   "grade": "A",
            //   "gear_score": 9876,
            //   "combat_power": 45000,
            //   "updated_at": "2025-01-01T00:00:00Z"
            // }

            double score = obj["score"]?.Value<double>() ?? 0;
            string grade = obj["grade"]?.Value<string>() ?? "";

            if (score <= 0 && string.IsNullOrEmpty(grade))
                return null;

            return new AtuulScore
            {
                CharacterName = obj["name"]?.Value<string>() ?? characterName,
                ServerName    = obj["server"]?.Value<string>() ?? serverName,
                ClassName     = obj["class"]?.Value<string>() ?? "",
                Level         = obj["level"]?.Value<int>() ?? 0,
                Score         = score,
                Grade         = grade,
                GearScore     = obj["gear_score"]?.Value<double>() ?? 0,
                CombatPower   = obj["combat_power"]?.Value<long>() ?? 0,
                LastUpdated   = obj["updated_at"]?.Value<DateTime>() ?? DateTime.UtcNow,
                ProfileUrl    = $"{BaseUrl}/character/{Uri.EscapeDataString(characterName)}"
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 여러 캐릭터의 점수를 동시에 조회 (파티원 전체)
    /// </summary>
    public async Task<Dictionary<string, AtuulScore?>> GetPartyScoresAsync(
        IEnumerable<string> characterNames,
        string serverName = "",
        CancellationToken ct = default)
    {
        var tasks = characterNames.ToDictionary(
            name => name,
            name => GetScoreAsync(name, serverName, ct));

        await Task.WhenAll(tasks.Values);

        return tasks.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Result);
    }

    public void ClearCache() => _cache.Clear();

    public void Dispose() => _http.Dispose();
}
