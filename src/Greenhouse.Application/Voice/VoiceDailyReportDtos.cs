namespace Greenhouse.Application.Voice;

public sealed record VoiceDailyReportDto(
    string GreetingLeadin,
    string LocalTime,
    string LocalDateLong,
    IReadOnlyList<NawaVoiceLineDto> Nawy);

public sealed record NawaVoiceLineDto(
    int Order,
    string NawaName,
    decimal? AvgTemperature,
    decimal? AvgSoilMoisture,
    int ReadingCount,
    int AssignedSensorCount);
