using System.Globalization;

namespace Greenhouse.Application.Voice;

internal static class VoiceAssessmentTexts
{
    public static (string Moisture, string Temperature) DailyAverages(
        decimal? avgMoisture,
        decimal? avgTemperature,
        decimal? moistureMin,
        decimal? moistureMax,
        decimal? temperatureMin,
        decimal? temperatureMax,
        int assignedSensorCount,
        int readingCount)
    {
        if (assignedSensorCount == 0)
        {
            return (
                "Wilgotność: brak przypisanych czujników — nie oceniam.",
                "Temperatura: brak przypisanych czujników — nie oceniam.");
        }

        if (readingCount == 0)
        {
            return (
                "Wilgotność: brak zapisanych odczytów od lokalnej północy.",
                "Temperatura: brak zapisanych odczytów od lokalnej północy.");
        }

        var m = DailyMoisture(avgMoisture, moistureMin, moistureMax);
        var t = DailyTemperature(avgTemperature, temperatureMin, temperatureMax);
        return (m, t);
    }

    private static string DailyMoisture(decimal? avg, decimal? minTh, decimal? maxTh)
    {
        if (!minTh.HasValue && !maxTh.HasValue)
            return "Wilgotność: w nawie nie ustawiono progów — średnia z dzisiaj nie jest porównywana z celem.";

        if (avg is null)
            return "Wilgotność: brak danych o wilgotności gleby od północy.";

        var a = avg.Value;
        if (minTh.HasValue && a < minTh.Value)
            return $"Uwaga, anomalia wilgotności. Średnia od północy to {Fmt(a)} procent, czyli poniżej progu podlania, który wynosi {Fmt(minTh.Value)} procent.";

        if (maxTh.HasValue && a > maxTh.Value)
            return $"Uwaga, anomalia wilgotności. Średnia od północy to {Fmt(a)} procent, czyli powyżej progu za mokro, który wynosi {Fmt(maxTh.Value)} procent.";

        return "Wilgotność poprawna.";
    }

    private static string DailyTemperature(decimal? avg, decimal? minTh, decimal? maxTh)
    {
        if (!minTh.HasValue && !maxTh.HasValue)
            return "Temperatura: brak progów alertu — nie oceniam średniej z dzisiaj.";

        if (avg is null)
            return "Temperatura: brak danych o temperaturze od północy.";

        var a = avg.Value;
        if (minTh.HasValue && a < minTh.Value)
            return $"Uwaga, anomalia temperatury. Średnia od północy to {Fmt(a)} stopni Celsjusza, a próg dolny to {Fmt(minTh.Value)} stopni.";

        if (maxTh.HasValue && a > maxTh.Value)
            return $"Uwaga, anomalia temperatury. Średnia od północy to {Fmt(a)} stopni Celsjusza, a próg górny to {Fmt(maxTh.Value)} stopni.";

        return "Temperatura poprawna.";
    }

    private static string Fmt(decimal d) =>
        d.ToString("0.#", CultureInfo.GetCultureInfo("pl-PL"));
}
