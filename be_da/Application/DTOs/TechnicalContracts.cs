namespace be_da.Application.DTOs;
public sealed record TechnicalSetting(string Key, bool Enabled, long Revision);
public sealed record TechnicalSettingChange(bool Enabled, long Revision, string Reason);
public sealed record TechnicalIncident(long Id, long Revision, string Title, string Status, string Note, DateTimeOffset At);
public sealed record TechnicalIncidentChange(string Title, string Status, string Note, long Revision = 0);
