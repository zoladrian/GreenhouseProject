namespace Greenhouse.Infrastructure.Hosting;

/// <summary>
/// W którym hoście rejestrujemy hosted-services. Decyduje, czy wstrzykiwać MQTT ingest
/// w oparciu o flagi <c>EnableInApiHost</c> / <c>EnableInWorkerHost</c>.
/// </summary>
public enum GreenhouseHostMode
{
    Api,
    Worker
}
