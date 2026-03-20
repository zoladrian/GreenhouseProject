namespace Greenhouse.Api;

/// <summary>
/// Identyfikator wdrożenia: zmienna <c>GREENHOUSE_DEPLOY_ID</c>, plik <c>deploy-id</c> (obraz Docker) lub <c>development</c>.
/// </summary>
internal static class DeployInfo
{
    private static readonly Lazy<string> LazyId = new(Resolve, LazyThreadSafetyMode.ExecutionAndPublication);

    public static string DeployId => LazyId.Value;

    private static string Resolve()
    {
        var env = Environment.GetEnvironmentVariable("GREENHOUSE_DEPLOY_ID");
        if (!string.IsNullOrWhiteSpace(env))
            return env.Trim();

        var path = Path.Combine(AppContext.BaseDirectory, "deploy-id");
        if (File.Exists(path))
        {
            try
            {
                var text = File.ReadAllText(path).Trim();
                if (text.Length > 0)
                    return text;
            }
            catch
            {
                /* ignore */
            }
        }

        return "development";
    }
}
