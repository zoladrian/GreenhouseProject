import type { VoiceDailyReportDto } from '../api/client';

function fmtNum(n: number): string {
  return n.toLocaleString('pl-PL', { maximumFractionDigits: 1, minimumFractionDigits: 0 });
}

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
        parts.push(`Średnia temperatura z czujników od północy: ${fmtNum(n.avgTemperature)} stopni Celsjusza.`);
      } else {
        parts.push('Brak danych o temperaturze od północy.');
      }

      if (n.avgSoilMoisture != null) {
        parts.push(`Średnia wilgotność gleby od północy: ${fmtNum(n.avgSoilMoisture)} procent.`);
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
