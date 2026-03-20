import type { VoiceDailyReportDto } from '../api/client';

function fmtNum(n: number): string {
  return n.toLocaleString('pl-PL', { maximumFractionDigits: 1, minimumFractionDigits: 0 });
}

/** Tekst pod Web Speech API — w pełni offline (dane z API na malince). */
export function buildVoiceDailyReportText(r: VoiceDailyReportDto): string {
  const parts: string[] = [];

  parts.push(`${r.greetingLeadin}, jest godzina ${r.localTime}. Dnia ${r.localDateLong}.`);

  if (!r.nawy.length) {
    parts.push('Nie masz zdefiniowanych aktywnych naw.');
  }

  for (const n of r.nawy) {
    parts.push(`W nawie numer ${n.order}, ${n.nawaName}.`);
    if (n.assignedSensorCount === 0) {
      parts.push('Brak przypisanych czujników do tej nawy.');
    } else if (n.readingCount === 0) {
      parts.push('Od północy brak zapisanych odczytów z czujników.');
    } else {
      if (n.avgTemperature != null) {
        parts.push(`Średnia temperatura z czujników od północy wynosi ${fmtNum(n.avgTemperature)} stopni Celsjusza.`);
      } else {
        parts.push('Brak danych o temperaturze od północy.');
      }

      if (n.avgSoilMoisture != null) {
        parts.push(`Średnia wilgotność gleby od północy wynosi ${fmtNum(n.avgSoilMoisture)} procent.`);
      } else {
        parts.push('Brak danych o wilgotności gleby od północy.');
      }
    }
  }

  parts.push('Miłego dnia.');
  return parts.join(' ');
}
