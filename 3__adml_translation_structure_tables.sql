CREATE TABLE Translations (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    StringId TEXT NOT NULL,                     -- "PolicyName", "UID_Policy123_label"
    LangCode TEXT NOT NULL,                     -- "ru-RU", "en-US", "de-DE"
    TextValue TEXT NOT NULL COLLATE NOCASE,     -- перевод текста
    AdmlFilename TEXT,                         -- "windows.admx" (опционально)
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(StringId, LangCode)
);

CREATE INDEX idx_lang_string ON Translations(LangCode, LangCode);
CREATE INDEX idx_filename ON Translations(AdmlFilename);