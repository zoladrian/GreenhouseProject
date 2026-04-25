import type { VoiceDailyReportDto, VoiceWeatherReportDto } from '../api/client';
import { formatNumberPl } from '../utils/formatPl';

/** Tekst pod Web Speech API — w pełni offline (dane z API na malince). Krótkie zdania = lepsze pauzy i odmiana w syntezie. */
export function buildVoiceDailyReportText(r: VoiceDailyReportDto): string {
  const parts: string[] = [];

  parts.push(`${r.greetingLeadin}. Jest godzina ${r.localTime}. ${r.localDateLong}.`);

  if (!r.nawy.length) {
    parts.push('Nie masz zdefiniowanych aktywnych naw.');
  }

  for (const n of r.nawy) {
    parts.push(`Nawa w kolejności ${n.order}: ${n.nawaName}.`);
    if (n.assignedSensorCount === 0) {
      parts.push('Brak przypisanych czujników do tej nawy.');
    } else if (n.readingCount === 0) {
      parts.push('Od północy brak zapisanych odczytów z czujników.');
    } else {
      if (n.avgTemperature != null) {
        parts.push(`Średnia temperatura z czujników od północy: ${formatNumberPl(n.avgTemperature)} stopni Celsjusza.`);
      } else {
        parts.push('Brak danych o temperaturze od północy.');
      }

      if (n.avgSoilMoisture != null) {
        parts.push(`Średnia wilgotność gleby od północy: ${formatNumberPl(n.avgSoilMoisture)} procent.`);
      } else {
        parts.push('Brak danych o wilgotności gleby od północy.');
      }
    }
    parts.push(n.moistureAssessment);
    parts.push(n.temperatureAssessment);
  }

  parts.push('Miłego dnia.');
  return parts.join(' ');
}

export function buildVoiceWeatherReportText(r: VoiceWeatherReportDto): string {
  const parts: string[] = [];
  parts.push(`${r.greetingLeadin}. Jest godzina ${r.localTime}. ${r.localDateLong}.`);
  parts.push(`Raport pogody: status opadu ${rainStatusText(r.rainStatus)}, status nasłonecznienia ${lightStatusText(r.lightStatus)}.`);
  parts.push(r.isNightBySchedule ? 'Według harmonogramu jest noc.' : 'Według harmonogramu jest dzień.');
  if (r.rainIntensityRaw != null) parts.push(`Surowa intensywność opadu: ${formatNumberPl(r.rainIntensityRaw)}.`);
  if (r.illuminanceRaw != null) parts.push(`Surowa jasność: ${formatNumberPl(r.illuminanceRaw)}.`);
  return parts.join(' ');
}

function rainStatusText(v: string): string {
  switch (v) {
    case 'raining':
      return 'aktualnie pada';
    case 'no-rain':
      return 'aktualnie nie pada';
    case 'high-humidity':
      return 'aktualnie duża wilgotność';
    default:
      return 'auto';
  }
}

function lightStatusText(v: string): string {
  switch (v) {
    case 'sunny':
      return 'jest słonecznie';
    case 'cloudy':
      return 'jest zachmurzenie';
    case 'night':
      return 'jest noc';
    default:
      return 'auto';
  }
}
